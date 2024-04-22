using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    private Rigidbody rb;
    private MeshRenderer meshRenderer; // Reference to the Mesh Renderer 
    [SerializeField] List<Color> colors = new List<Color>();
     public SpawnManager.PlayerRole PlayerRole { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>(); 

        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer component not found on this GameObject.");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Local player spawned");
            StartCoroutine(WaitForSpawnManager());
        }
         meshRenderer.material.color = colors[(int)OwnerClientId];
    }

    private IEnumerator WaitForSpawnManager()
    {
        while (SpawnManager.Instance == null || !SpawnManager.Instance.IsReady)
        {
            Debug.Log("Waiting for spawn manager...");
            yield return null; // Wait for the next frame
        }

        Debug.Log("Requesting Spawn Point from Server RPC");
        SpawnManager.Instance.RequestSpawnPointServerRpc();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return; // Only process input for the owner of this player object

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(-vertical, 0, horizontal);
        rb.AddForce(movement * speed);
    }

    // Called when the spawn position and role are determined
    public void SetInitialPositionAndRole(Vector3 position, SpawnManager.PlayerRole assignedRole)
    {
        transform.position = position;  // Set the spawn position
        if (meshRenderer != null)
        {
            if (assignedRole == SpawnManager.PlayerRole.Enemy)
            {
                Debug.Log("Spawned as the Enemy");
            }
            else
            {
                Debug.Log("Spawned as a Normal Player");
            }
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (IsClient && IsOwner)
        {
            // Correctly accessing the PlayerMovement component from the collision object
            PlayerMovement otherPlayer = col.GetComponent<Collider>().GetComponent<PlayerMovement>();
            if (otherPlayer != null && this.PlayerRole == SpawnManager.PlayerRole.Enemy && otherPlayer.PlayerRole == SpawnManager.PlayerRole.Normal)
            {
                Debug.Log($"Player {OwnerClientId} with role {PlayerRole} collided with player {otherPlayer.OwnerClientId} with role {otherPlayer.PlayerRole}");
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