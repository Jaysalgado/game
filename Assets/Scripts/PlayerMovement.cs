using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    private Rigidbody rb;
    private MeshRenderer meshRenderer; // Reference to the Mesh Renderer

    // Network variables for color synchronization
    private NetworkVariable<float> colorR = new NetworkVariable<float>();
    private NetworkVariable<float> colorG = new NetworkVariable<float>();
    private NetworkVariable<float> colorB = new NetworkVariable<float>();

    void Start()
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

        // Subscribe to color changes
        colorR.OnValueChanged += OnColorChanged;
        colorG.OnValueChanged += OnColorChanged;
        colorB.OnValueChanged += OnColorChanged;

        // Initial application of color
        ApplyColor(new Color(colorR.Value, colorG.Value, colorB.Value));
    }

    new void OnDestroy()
    {
        colorR.OnValueChanged -= OnColorChanged;
        colorG.OnValueChanged -= OnColorChanged;
        colorB.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(float oldVal, float newVal)
    {
        ApplyColor(new Color(colorR.Value, colorG.Value, colorB.Value));
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
            Color newColor;
            if (assignedRole == SpawnManager.PlayerRole.Enemy)
            {
                 Debug.Log("Spawned as the Enemy");
                newColor = Color.red; // Enemy color
            }
            else
            {
                Debug.Log("Spawned as a Normal Player");
                newColor = Random.ColorHSV(); // Assign a random color
            }
            // Request server to set and synchronize color
            RequestColorChangeServerRpc(newColor.r, newColor.g, newColor.b);
        }
    }

    [ServerRpc]
    private void RequestColorChangeServerRpc(float r, float g, float b)
    {
        // Set color on server and propagate to all clients
        SetColor(new Color(r, g, b));
    }

    private void SetColor(Color color)
    {
        // Server updates the network variables which trigger color changes on all clients
        colorR.Value = color.r;
        colorG.Value = color.g;
        colorB.Value = color.b;
    }

    private void ApplyColor(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;  // Apply the color locally
        }
    }
}
