using UnityEngine;
using Unity.Netcode;

public class PlayerTeamManager : NetworkBehaviour
{
    [Header("Team Configuration")]
    // 0 = Team A, 1 = Team B
    public NetworkVariable<int> teamIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Animator Settings")]
    [Tooltip("The Animator component to modify.")]
    [SerializeField] private Animator playerAnimator;

    [Tooltip("Controller for Team A (Default).")]
    [SerializeField] private RuntimeAnimatorController teamAController;

    [Tooltip("Controller for Team B.")]
    [SerializeField] private RuntimeAnimatorController teamBController;

    [Tooltip("Controller for Team C.")]
    [SerializeField] private RuntimeAnimatorController teamCController;

    public override void OnNetworkSpawn()
    {
        // 0. Auto-reference if null
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();

        // 1. Assign Team (Only Server)
        if (IsServer)
        {
            AssignTeam();
        }

        // 2. Apply initial Animator based on current data
        UpdateAnimatorController(teamIndex.Value);

        // 3. Subscribe to changes (Sync for late joiners or runtime switches)
        teamIndex.OnValueChanged += OnTeamChanged;
    }

    public override void OnNetworkDespawn()
    {
        teamIndex.OnValueChanged -= OnTeamChanged;
    }

    private void AssignTeam()
    {
        // Simple logic: Even ID = Team A, Odd ID = Team B
        int assignedTeam = (int)OwnerClientId % 3;
        teamIndex.Value = assignedTeam;
    }

    private void OnTeamChanged(int oldTeam, int newTeam)
    {
        UpdateAnimatorController(newTeam);
    }

    private void UpdateAnimatorController(int teamId)
    {
        if (playerAnimator == null) return;

        // Swap the asset used by the Animator
        if (teamId == 0)
        {
            if (teamAController != null)
                playerAnimator.runtimeAnimatorController = teamAController;
        }
        else if (teamId == 1)
        {
            if (teamBController != null)
                playerAnimator.runtimeAnimatorController = teamBController;
        }
        else if (teamId == 2)
        {
            if (teamCController != null)
                playerAnimator.runtimeAnimatorController = teamCController;
        }

        // Optional: Trigger a small rebind to ensure parameters reset correctly
        // playerAnimator.Rebind(); 
        // Note: Rebind might reset the animation to start. Use carefully.

        Debug.Log($"[TeamManager] Player {OwnerClientId} switched to Team {teamId} Animator.");
    }

    // --- TELEPORT LOGIC (Kept from previous request) ---

    [ClientRpc]
    public void TeleportToPositionClientRpc(Vector3 newPosition)
    {
        if (IsOwner)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;

            transform.position = newPosition;
            // Physics sync forces update
            Physics2D.SyncTransforms();
        }
    }
}