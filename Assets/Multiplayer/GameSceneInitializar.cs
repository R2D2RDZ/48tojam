using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameSceneInitializer : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Solo el Host organiza las posiciones
        if (!IsServer) return;

        // Esperamos un frame para asegurar que todos los objetos de red estén listos
        StartCoroutine(PositionPlayersRoutine());
    }

    private IEnumerator PositionPlayersRoutine()
    {
        yield return new WaitForSeconds(0.5f); // Pequeña espera de seguridad

        Debug.Log("[GameScene] Positioning players by team...");

        // 1. Encontrar todos los puntos de spawn en la escena
        TeamSpawnPoint[] spawnPoints = FindObjectsByType<TeamSpawnPoint>(FindObjectsSortMode.None);

        // 2. Encontrar todos los jugadores conectados
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject != null)
            {
                var teamManager = playerObject.GetComponent<PlayerTeamManager>();

                if (teamManager != null)
                {
                    int playerTeam = teamManager.teamIndex.Value;

                    // 3. Buscar un punto de spawn que coincida con el equipo
                    Transform targetSpawn = GetSpawnPointForTeam(spawnPoints, playerTeam);

                    if (targetSpawn != null)
                    {
                        // 4. Mover al jugador usando el ClientRpc (porque él es el dueño de su posición)
                        teamManager.TeleportToPositionClientRpc(targetSpawn.position);
                    }
                }
            }
        }
    }

    private Transform GetSpawnPointForTeam(TeamSpawnPoint[] points, int teamId)
    {
        List<Transform> validPoints = new List<Transform>();

        foreach (var point in points)
        {
            if (point.teamID == teamId)
            {
                validPoints.Add(point.transform);
            }
        }

        if (validPoints.Count > 0)
        {
            // Elegir uno al azar si hay varios para el mismo equipo
            return validPoints[Random.Range(0, validPoints.Count)];
        }

        Debug.LogWarning($"No spawn point found for Team {teamId}");
        return null;
    }
}