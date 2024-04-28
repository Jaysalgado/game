using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
     public static PlayerMovement LocalPlayerInstance { get; private set; }
    [SerializeField] private float speed = 25f;
    private Rigidbody rb;
    private MeshRenderer meshRenderer; // Reference to the Mesh Renderer 
    [SerializeField] private List<Color> colors = new();
    [SerializeField] public NetworkVariable<SpawnManager.PlayerRole> playerRole = new NetworkVariable<SpawnManager.PlayerRole>();

    public SpawnManager.PlayerRole PlayerRole
    {
        get { return playerRole.Value; }
        private set { playerRole.Value = value; }
    }

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
            LocalPlayerInstance = this;
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
        if (!GameManager.Instance.gameStarted.Value) return;
        
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 movement = new(-vertical, 0, horizontal);
            rb.AddForce(movement * speed);
        
    }

    // Called when the spawn position and role are determined
    public void SetInitialPositionAndRole(Vector3 position, SpawnManager.PlayerRole assignedRole)
    {
        playerRole.Value = assignedRole; // Set the player role
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
}