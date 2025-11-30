using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using UnityEngine.SceneManagement; // Necesario para LoadSceneMode

public class MultiplayerSessionManager : MonoBehaviour
{
    [Header("Session Configuration")]
    [Tooltip("Maximum number of players allowed in the lobby.")]
    [SerializeField] private int maxPlayers = 50;

    [Tooltip("Name of the lobby.")]
    [SerializeField] private string lobbyName = "PlatformerMegaLobby";

    [Header("Game Settings")]
    [Tooltip("Name of the gameplay scene to load. Must be in Build Settings.")]
    [SerializeField] private string gameSceneName = "Bolita";

    private const string ConnectionType = "wss";
    private const string JoinCodeKey = "RelayJoinCode";

    private Lobby currentLobby;

    private async void Start()
    {
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
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            RelayServerEndpoint wssEndpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == ConnectionType);

            if (wssEndpoint == null)
            {
                Debug.LogError($"[Host] Critical Error: No '{ConnectionType}' endpoint found.");
                return;
            }

            ConfigureTransport(allocation, wssEndpoint, true);

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            string finalLobbyName = $"{lobbyName}_{Random.Range(100, 999)}";
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(finalLobbyName, maxPlayers, options);

            StartCoroutine(HeartbeatLobbyCoroutine(currentLobby.Id, 15f));

            NetworkManager.Singleton.StartHost();
            Debug.Log($"[Host] Started! Lobby Code: {currentLobby.LobbyCode}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Host] Failed: {e.Message}");
        }
    }

    // --- START GAME LOGIC (NEW) ---

    /// <summary>
    /// Loads the game scene for ALL connected players.
    /// Only the Host can call this.
    /// </summary>
    public void StartGame()
    {
        // 1. Validation: Only Host can control the scene
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("[Client] Only the Host can start the game.");
            return;
        }

        // 2. Validate Scene Name
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[Host] Game Scene Name is empty in the Inspector.");
            return;
        }

        Debug.Log($"[Host] Loading Scene: {gameSceneName} for all clients...");

        // 3. Network Scene Loading
        // NetworkManager handles the transition for all connected clients automatically.
        // Ensure 'Enable Scene Management' is checked in your NetworkManager component.
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

        // Optional: Update Lobby data to show game has started (prevent new joins?)
        // UpdateLobbyGameStarted(); 
    }

    // --- CLIENT LOGIC ---

    public async void JoinGameSession(string lobbyId)
    {
        try
        {
            Debug.Log($"[Client] Joining Lobby ID: {lobbyId}...");
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            if (currentLobby.Data == null || !currentLobby.Data.ContainsKey(JoinCodeKey))
            {
                Debug.LogError("[Client] Failed: No Join Code in Lobby.");
                return;
            }

            string joinCode = currentLobby.Data[JoinCodeKey].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerEndpoint wssEndpoint = joinAllocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == ConnectionType);

            if (wssEndpoint == null)
            {
                Debug.LogError($"[Client] Critical Error: No '{ConnectionType}' endpoint found.");
                return;
            }

            ConfigureTransport(joinAllocation, wssEndpoint, false);
            NetworkManager.Singleton.StartClient();
            Debug.Log("[Client] Client Started via WSS!");
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
                JoinGameSession(results.Results[0].Id);
            }
            else
            {
                Debug.LogWarning("[Client] No active lobbies found.");
            }
        }
        catch (System.Exception e) { Debug.LogError(e.Message); }
    }

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
        else return;

        var relayServerData = new RelayServerData(
            endpoint.Host, (ushort)endpoint.Port, allocationIdBytes, connectionData, hostConnectionData, key,
            isSecure: true, isWebSocket: true
        );

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
    }

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