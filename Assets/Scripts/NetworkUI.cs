using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    // Track the current network state
    private bool isHosting = false;
    private bool isClient = false;

    private void Awake()
    {
        // Add listener for host button
        hostButton.onClick.AddListener(ToggleHost);

        // Add listener for client button
        clientButton.onClick.AddListener(ToggleClient);
    }

    private void ToggleHost()
    {
        if (!isHosting)
        {
            NetworkManager.Singleton.StartHost();
            isHosting = true;
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            isHosting = false;
        }
    }

    private void ToggleClient()
    {
        if (!isClient)
        {
            NetworkManager.Singleton.StartClient();
            isClient = true;
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            isClient = false;
        }
    }

    private void OnDestroy()
    {
        // Ensure to remove listeners when the UI is destroyed to avoid memory leaks
        hostButton.onClick.RemoveListener(ToggleHost);
        clientButton.onClick.RemoveListener(ToggleClient);
    }
}
