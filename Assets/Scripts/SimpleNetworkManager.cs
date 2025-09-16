using UnityEngine;
using Unity.Netcode;

namespace MOBA.Networking
{
    /// <summary>
    /// Simple network manager for basic multiplayer
    /// </summary>
    public class SimpleNetworkManager : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private bool isHost = false;
        [SerializeField] private bool autoStart = true;

        void Start()
        {
            if (!autoStart)
            {
                return;
            }

            if (!TryGetNetworkManager(out var manager))
            {
                Debug.LogError("[SimpleNetworkManager] Unable to auto-start: NetworkManager.Singleton is null. Add a NetworkManager to the scene or disable autoStart.");
                return;
            }

            if (isHost)
            {
                StartHost(manager);
            }
            else
            {
                StartClient(manager);
            }
        }

        public void StartHost()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                Debug.LogError("[SimpleNetworkManager] Cannot start host: NetworkManager.Singleton is null.");
                return;
            }

            StartHost(manager);
        }

        private void StartHost(NetworkManager manager)
        {
            manager.StartHost();
            Debug.Log("Started as Host");
        }

        public void StartClient()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                Debug.LogError("[SimpleNetworkManager] Cannot start client: NetworkManager.Singleton is null.");
                return;
            }

            StartClient(manager);
        }

        private void StartClient(NetworkManager manager)
        {
            manager.StartClient();
            Debug.Log("Started as Client");
        }

        public void StartServer()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                Debug.LogError("[SimpleNetworkManager] Cannot start server: NetworkManager.Singleton is null.");
                return;
            }

            manager.StartServer();
            Debug.Log("Started as Server");
        }

        public void Disconnect()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                Debug.LogWarning("[SimpleNetworkManager] Disconnect requested but NetworkManager.Singleton is null.");
                return;
            }

            manager.Shutdown();
            Debug.Log("Disconnected");
        }

        private string GetNetworkMode()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                return "Offline";
            }

            if (manager.IsHost) return "Host";
            if (manager.IsServer) return "Server";
            return "Client";
        }

        private bool TryGetNetworkManager(out NetworkManager manager)
        {
            manager = NetworkManager.Singleton;
            return manager != null;
        }

        // UI integration should be wired through Unity UI/UIToolkit; legacy OnGUI debug controls removed.
    }
}
