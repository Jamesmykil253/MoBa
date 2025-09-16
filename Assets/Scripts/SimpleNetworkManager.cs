using UnityEngine;
using Unity.Netcode;
using MOBA.Debugging;

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

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                mechanic,
                subsystem: nameof(SimpleNetworkManager));
        }

        void Start()
        {
            if (!autoStart)
            {
                return;
            }

            if (!TryGetNetworkManager(out var manager))
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Unable to auto-start; NetworkManager.Singleton is null.");
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
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Cannot start host; NetworkManager.Singleton is null.");
                return;
            }

            StartHost(manager);
        }

        private void StartHost(NetworkManager manager)
        {
            manager.StartHost();
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking),
                "NetworkManager started as host.");
        }

        public void StartClient()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Cannot start client; NetworkManager.Singleton is null.");
                return;
            }

            StartClient(manager);
        }

        private void StartClient(NetworkManager manager)
        {
            manager.StartClient();
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking),
                "NetworkManager started as client.");
        }

        public void StartServer()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Cannot start server; NetworkManager.Singleton is null.");
                return;
            }

            manager.StartServer();
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking),
                "NetworkManager started as server.");
        }

        public void Disconnect()
        {
            if (!TryGetNetworkManager(out var manager))
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Networking),
                    "Disconnect requested but NetworkManager.Singleton is null.");
                return;
            }

            manager.Shutdown();
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking),
                "NetworkManager shutdown invoked.");
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
