using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : NetworkBehaviour {
    public static GameManager Instance { get; private set; }

    public NetworkVariable<bool> gameStarted = new();
    

[SerializeField] private TextMeshProUGUI statusText;

  [SerializeField] private Button startButton;

   
   

 private void Awake() {
     Instance = this;
     SpawnManager.OnRoleAssigned += HandleRoleAssignment;
     startButton.gameObject.SetActive(false);
 }

public override void OnDestroy() {
     // Unsubscribe from events to avoid memory leaks
    SpawnManager.OnRoleAssigned -= HandleRoleAssignment;

    // Call the base class OnDestroy to ensure any base functionality is also executed
     base.OnDestroy();
}

public override void OnNetworkSpawn() {
    if (IsServer) {
            gameStarted.Value = false;  // Default game state is not started
    }

    gameStarted.OnValueChanged += HandleGameStartedChanged;
    HandleGameStartedChanged(gameStarted.Value, gameStarted.Value);
}

[ServerRpc(RequireOwnership = false)]
public void StartGameServerRpc(ServerRpcParams rpcParams = default) {
    ulong clientId = rpcParams.Receive.SenderClientId; // Use the SenderClientId from rpcParams
    // Retrieve the player and check if they have the 'Enemy' role
    NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
    PlayerMovement playerMovement = playerNetworkObject.GetComponent<PlayerMovement>();
    Debug.Log($"Server: StartGameServerRpc called by client: {clientId} with role: {playerMovement.playerRole.Value}");

    if (playerMovement != null && playerMovement.playerRole.Value == SpawnManager.PlayerRole.Enemy) {
        Debug.Log("Server: Game has started by Enemy!");
        gameStarted.Value = true;
    } else {
        Debug.Log("Non-Enemy player attempted to start the game");
    }
}
private void HandleGameStartedChanged(bool oldValue, bool newValue) {
    if (newValue) {
        statusText.text = "Game has started!";
        Invoke(nameof(HideStatusText), 1f);
    }
}

private void HandleRoleAssignment(SpawnManager.PlayerRole role, ulong clientId) {
        // Check if the assigned role is for the local client
        if (NetworkManager.Singleton.LocalClientId == clientId) {
            Debug.Log($"Role assigned: {role} to client: {clientId}");
            if (role == SpawnManager.PlayerRole.Enemy) {
                Debug.Log("Local player is Enemy, can start game.");
                startButton.gameObject.SetActive(true);
                statusText.text = "Press start to begin the game";
            }else {
                statusText.text = "Waiting for the Player to start the game...";
        }
        }
    }
public void OnStartGameButtonClicked() {
    StartGameServerRpc(); // Call the ServerRpc without parameters
}
    public bool IsGameStarted() {
        return gameStarted.Value;
    }

    public void HideStatusText() {
    if (statusText != null) {
        statusText.gameObject.SetActive(false);  // Hides the entire GameObject
    }
}

}
