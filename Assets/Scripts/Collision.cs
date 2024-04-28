using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Collision : NetworkBehaviour
{
    
    void OnCollisionEnter(UnityEngine.Collision collision)
    {
        if (IsClient && IsOwner && GameManager.Instance.gameStarted.Value)
        {
            // Correctly accessing the PlayerMovement component from the collision object
            PlayerMovement otherPlayer = collision.collider.GetComponent<PlayerMovement>();
            if (otherPlayer != null && this.GetComponent<PlayerMovement>().playerRole.Value == SpawnManager.PlayerRole.Enemy && otherPlayer.GetComponent<PlayerMovement>().playerRole.Value == SpawnManager.PlayerRole.Normal)
            {
                Debug.Log($"Player {OwnerClientId} with role {this.GetComponent<PlayerMovement>().playerRole.Value} collided with player {otherPlayer.OwnerClientId} with role {otherPlayer.GetComponent<PlayerMovement>().playerRole.Value}");
                RequestVerifyCollisionServerRpc(otherPlayer.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }


    [ServerRpc]
    void RequestVerifyCollisionServerRpc(ulong otherPlayerId, ServerRpcParams rpcParams = default)
    {
        // This function will actually run on the server
        HandleCollision(otherPlayerId);
    }

    private void HandleCollision(ulong otherPlayerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(otherPlayerId, out NetworkObject otherPlayerNetworkObject))
        {
            PlayerMovement otherPlayer = otherPlayerNetworkObject.GetComponent<PlayerMovement>();
            if (otherPlayer != null)
            {
                // Further validation could be done here to make sure the collision is valid
                DestroyPlayerNetworked(otherPlayer.gameObject);
            }
        }
        else
        {
            Debug.LogError("Failed to find the network object for player ID: " + otherPlayerId);
        }
    }

    private void DestroyPlayerNetworked(GameObject player)
    {
        // Assuming proper authority checks are in place
        player.GetComponent<NetworkObject>().Despawn();
        Destroy(player);
    }

}
