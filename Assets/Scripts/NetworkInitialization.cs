using Unity.Networking.Transport;
using UnityEngine;

public class NetworkInitialization : MonoBehaviour
{
    NetworkDriver m_Driver;
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("WebGL platform detected. Using WebSocket transport.");
            m_Driver = NetworkDriver.Create(new WebSocketNetworkInterface());
#else
        Debug.Log("Non-WebGL platform detected. Using UdpCv transport.");
        m_Driver = NetworkDriver.Create(new UDPNetworkInterface());
#endif

    }

    // Update is called once per frame
    void OnDestroy()
    {
        if (m_Driver.IsCreated)
            m_Driver.Dispose();
    }
}
