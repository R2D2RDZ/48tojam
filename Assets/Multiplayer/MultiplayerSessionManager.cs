using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
//using Unity.Services.Multiplayer; // REMOVED: Too abstract for manual WSS
using Unity.Services.Lobbies;       // ADDED: Direct control
using Unity.Services.Lobbies.Models; // ADDED: Fixes Visibility errors
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class MultiplayerSessionManager : MonoBehaviour
{
    [Header("Session Configuration")]
    [Tooltip("Maximum number of players allowed in the lobby.")]
    [SerializeField] private int maxPlayers = 50;

    [Tooltip("Name of the lobby.")]
    [SerializeField] private string lobbyName = "PlatformerMegaLobby";

    private const string ConnectionType = "wss"; // WebGL requires wss (Secure WebSockets)
    private const string JoinCodeKey = "RelayJoinCode"; // Key for Lobby Data

    // We store the current Lobby instead of ISession
    private Lobby currentLobby;

    private async void Start()
    {
        // Essential for WebGL to keep connection alive in background
        Application.runInBackground = true;
        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Auth] Signed in as {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Auth] Initialization Error: {e.Message}");
        }
    }

    // --- HOST LOGIC ---

    public async void CreateGameSession()
    {
        try
        {
            Debug.Log("[Host] Starting Secure WSS Configuration...");

            // 1. Create Relay Allocation
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            // 2. Get Join Code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 3. Find WSS Endpoint
            RelayServerEndpoint wssEndpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == ConnectionType);

            if (wssEndpoint == null)
            {
                Debug.LogError($"[Host] Critical Error: No '{ConnectionType}' endpoint found in Relay allocation.");
                return;
            }

            // 4. Configure Transport Manually
            ConfigureTransport(allocation, wssEndpoint, true);

            // 5. Create Lobby (Replacing SessionOptions logic)
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        JoinCodeKey,
                        new DataObject(DataObject.VisibilityOptions.Public, joinCode)
                    }
                }
            };

            string finalLobbyName = $"{lobbyName}_{Random.Range(100, 999)}";
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(finalLobbyName, maxPlayers, options);

            // Heartbeat coroutine should be started here to keep lobby alive (not included for brevity)
            StartCoroutine(HeartbeatLobbyCoroutine(currentLobby.Id, 15f));

            // 6. Start Host
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[Host] Started! Lobby Code: {currentLobby.LobbyCode} | Relay Code: {joinCode} | Protocol: {ConnectionType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Host] Failed: {e.Message}");
        }
    }

    // --- CLIENT LOGIC ---

    public async void JoinGameSession(string lobbyId)
    {
        try
        {
            Debug.Log($"[Client] Attempting to join Lobby ID: {lobbyId}...");

            // 1. Join the Lobby to get Data
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            // 2. Extract the Relay Join Code from Lobby Data
            if (currentLobby.Data == null || !currentLobby.Data.ContainsKey(JoinCodeKey))
            {
                Debug.LogError("[Client] Failed: Lobby does not contain a Relay Join Code.");
                return;
            }

            string joinCode = currentLobby.Data[JoinCodeKey].Value;
            Debug.Log($"[Client] Retrieved Relay Code: {joinCode}. Connecting to Relay...");

            // 3. Join the Relay Allocation manually
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 4. Find the Secure Endpoint (WSS) on the Client side
            RelayServerEndpoint wssEndpoint = joinAllocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == ConnectionType);

            if (wssEndpoint == null)
            {
                Debug.LogError($"[Client] Critical Error: No '{ConnectionType}' endpoint found in Relay Join allocation.");
                return;
            }

            Debug.Log($"[Client] Found Secure Endpoint: {wssEndpoint.Host}:{wssEndpoint.Port}");

            // 5. Configure Transport Manually (Force WSS)
            ConfigureTransport(joinAllocation, wssEndpoint, false);

            // 6. Start Client
            NetworkManager.Singleton.StartClient();
            Debug.Log("[Client] Network Client Started via WSS!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Client] Join Failed: {e.Message}");
        }
    }

    public async void QuickJoin()
    {
        try
        {
            Debug.Log("[Client] Searching for lobbies...");
            var options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            var results = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (results.Results.Count > 0)
            {
                var target = results.Results[0];
                Debug.Log($"[Client] Found Lobby: {target.Id}. Initiating join sequence...");
                JoinGameSession(target.Id);
            }
            else
            {
                Debug.LogWarning("[Client] No active lobbies found.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Client] QuickJoin Error: {e.Message}");
        }
    }

    // --- SHARED HELPER ---

    private void ConfigureTransport(object allocationObj, RelayServerEndpoint endpoint, bool isHost)
    {
        byte[] allocationIdBytes, connectionData, hostConnectionData, key;

        if (isHost && allocationObj is Allocation hostAlloc)
        {
            allocationIdBytes = hostAlloc.AllocationIdBytes;
            connectionData = hostAlloc.ConnectionData;
            hostConnectionData = hostAlloc.ConnectionData;
            key = hostAlloc.Key;
        }
        else if (!isHost && allocationObj is JoinAllocation clientAlloc)
        {
            allocationIdBytes = clientAlloc.AllocationIdBytes;
            connectionData = clientAlloc.ConnectionData;
            hostConnectionData = clientAlloc.HostConnectionData;
            key = clientAlloc.Key;
        }
        else
        {
            Debug.LogError("[Transport] Invalid allocation type.");
            return;
        }

        var relayServerData = new RelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocationIdBytes,
            connectionData,
            hostConnectionData,
            key,
            isSecure: true,    // FORCE HTTPS/WSS
            isWebSocket: true  // FORCE WEBSOCKET
        );

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
    }

    // Lobbies die after 30s of inactivity, keep it alive
    private System.Collections.IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (currentLobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
}