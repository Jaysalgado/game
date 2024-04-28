using System;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour {
    [SerializeField] private Transform[] spawnPoints;
    private NetworkList<bool> spawnPointsTaken;

    // Singleton instance for easy access
    public static SpawnManager Instance { get; private set; }

    public enum PlayerRole { Normal, Enemy }

    // Network synchronized IsReady property
    private NetworkVariable<bool> isReady = new NetworkVariable<bool>();

    // Event to notify when roles are assigned
    public static event Action<PlayerRole, ulong> OnRoleAssigned;

     public bool IsReady {
        get { return isReady.Value; } // Public getter to access the private NetworkVariable
    }


    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
            spawnPointsTaken = new NetworkList<bool>();
        }
    }

    public override void OnNetworkSpawn() {
        Debug.Log("SpawnManager OnNetworkSpawn called on: " + (IsServer ? "Server" : "Client"));
        if (IsServer) {
            InitializeSpawnPoints();
        }
    }

    private void InitializeSpawnPoints() {
        for (int i = 0; i < spawnPoints.Length; i++) {
            spawnPointsTaken.Add(false);
        }
        isReady.Value = true;
        Debug.Log("SpawnManager is now ready.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPointServerRpc(ServerRpcParams rpcParams = default) {
        if (!isReady.Value) {
            Debug.LogError("SpawnManager is not ready but RPC was called.");
            return;
        }

        ulong clientId = rpcParams.Receive.SenderClientId;
        AssignSpawnPoint(clientId);
    }

    private void AssignSpawnPoint(ulong clientId) {
        for (int i = 0; i < spawnPoints.Length; i++) {
            if (!spawnPointsTaken[i]) {
                spawnPointsTaken[i] = true;
                PlayerRole role = (i == 0) ? PlayerRole.Enemy : PlayerRole.Normal;
                Vector3 spawnPos = spawnPoints[i].position;
                UpdateClientWithSpawnPointClientRpc(spawnPos, role, clientId);
                return;
            }
        }

        Debug.LogError("No available spawn points!");
    }

[ClientRpc]
private void UpdateClientWithSpawnPointClientRpc(Vector3 spawnPosition, SpawnManager.PlayerRole role, ulong clientId, ClientRpcParams rpcParams = default) {
    // This check ensures the RPC only affects the intended client
    //   Debug.Log($"client {NetworkManager.Singleton.LocalClientId}");
    if (NetworkManager.Singleton.LocalClientId == clientId) {
        // on the correct client and their player GameObject.
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null) {
            playerMovement.SetInitialPositionAndRole(spawnPosition, role);
            OnRoleAssigned?.Invoke(role, clientId);

        } else {
            Debug.LogError("PlayerMovement component not found on any GameObject. Ensure this RPC is being called on the player's GameObject.");
        }
    }
}




    public void ReleaseSpawnPoint(Vector3 position) {
        int index = System.Array.IndexOf(spawnPoints, position);
        if (index != -1) {
            spawnPointsTaken[index] = false;
        }
    }
}
