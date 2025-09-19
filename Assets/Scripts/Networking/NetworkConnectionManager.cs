using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using MOBA.Debugging;

namespace MOBA.Networking
{
    /// <summary>
    /// Manages network connections, disconnections, and reconnection logic
    /// Handles host, server, and client connection modes with robust error handling
    /// </summary>
    public class NetworkConnectionManager : NetworkManagerComponent
    {
        [Header("Network Configuration")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        
        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        
        [Header("Reconnection")]
        [SerializeField] private bool enableReconnection = true;
        [SerializeField] private float reconnectionInterval = 5f;
        [SerializeField] private int maxReconnectionAttempts = 3;
        [SerializeField] private float reconnectCooldown = 2f;
        
        private NetworkConnectionState connectionState = NetworkConnectionState.Disconnected;
        private float lastDisconnectTime = -10f;
        private int reconnectionAttempts = 0;
        private Coroutine reconnectionCoroutine;
        private NetworkErrorCode lastErrorCode = NetworkErrorCode.None;
        private string lastErrorMessage = string.Empty;

        // Events
        public System.Action<NetworkConnectionState> OnConnectionStateChanged;
        public System.Action<string> OnNetworkError;
        public System.Action<string> OnNetworkWarning;

        public NetworkConnectionState ConnectionState => connectionState;
        public NetworkErrorCode LastErrorCode => lastErrorCode;
        public string LastErrorMessage => lastErrorMessage;
        public bool IsConnected => connectionState == NetworkConnectionState.Connected;
        public bool IsConnecting => connectionState == NetworkConnectionState.Connecting;

        protected override void OnInitialized()
        {
            if (netcode != null)
            {
                netcode.OnClientConnectedCallback += OnClientConnected;
                netcode.OnClientDisconnectCallback += OnClientDisconnected;
                netcode.OnServerStarted += OnServerStarted;
                netcode.OnClientStarted += OnClientStarted;
            }

            if (autoStartAsHost)
            {
                StartHost();
            }
        }

        protected override void OnShutdown()
        {
            if (netcode != null)
            {
                netcode.OnClientConnectedCallback -= OnClientConnected;
                netcode.OnClientDisconnectCallback -= OnClientDisconnected;
                netcode.OnServerStarted -= OnServerStarted;
                netcode.OnClientStarted -= OnClientStarted;
            }

            if (reconnectionCoroutine != null)
            {
                StopCoroutine(reconnectionCoroutine);
                reconnectionCoroutine = null;
            }
        }

        /// <summary>
        /// Starts the network session as host (server + local client).
        /// Host mode is ideal for peer-to-peer games and development/testing.
        /// </summary>
        public void StartHost()
        {
            if (netcode == null) return;

            SetConnectionState(NetworkConnectionState.Connecting);

            bool success = netcode.StartHost();
            if (!success)
            {
                HandleConnectionError("Failed to start as host");
            }
            else
            {
                GameDebug.Log(BuildContext(), "Starting network session as host.");
            }
        }

        /// <summary>
        /// Starts the network session as dedicated server (no local player).
        /// Server-only mode for production deployments and headless server hosting.
        /// </summary>
        public void StartServer()
        {
            if (netcode == null) return;

            SetConnectionState(NetworkConnectionState.Connecting);

            bool success = netcode.StartServer();
            if (!success)
            {
                HandleConnectionError("Failed to start as server");
            }
            else
            {
                GameDebug.Log(BuildContext(), "Starting network session as server.");
            }
        }

        /// <summary>
        /// Connects to a remote server as a client player.
        /// Includes reconnection logic and rapid-connect protection.
        /// </summary>
        public void StartClient()
        {
            if (netcode == null) return;

            // Prevent rapid reconnects
            if (Time.time - lastDisconnectTime < reconnectCooldown)
            {
                float remaining = Mathf.Max(0f, reconnectCooldown - (Time.time - lastDisconnectTime));
                string warning = $"Reconnect attempted too quickly. Please wait {remaining:F1}s.";
                HandleConnectionWarning(warning);
                lastErrorCode = NetworkErrorCode.RapidReconnect;
                lastErrorMessage = warning;
                SetConnectionState(NetworkConnectionState.Error);
                OnNetworkError?.Invoke($"[{NetworkErrorCode.RapidReconnect}] {warning}");
                return;
            }

            SetConnectionState(NetworkConnectionState.Connecting);

            bool success = netcode.StartClient();
            if (!success)
            {
                HandleConnectionError("Failed to start as client");
                if (enableReconnection)
                {
                    ScheduleReconnection();
                }
            }
            else
            {
                GameDebug.Log(BuildContext(), "Attempting to connect to server as client.");
            }
        }

        /// <summary>
        /// Disconnects from the current network session.
        /// </summary>
        public void Disconnect()
        {
            if (netcode == null) return;

            lastDisconnectTime = Time.time;
            
            if (reconnectionCoroutine != null)
            {
                StopCoroutine(reconnectionCoroutine);
                reconnectionCoroutine = null;
            }

            netcode.Shutdown();
            SetConnectionState(NetworkConnectionState.Disconnected);
            GameDebug.Log(BuildContext(), "Disconnected from network session.");
        }

        /// <summary>
        /// Sets the server address and port for client connections.
        /// </summary>
        public void SetServerAddress(string ip, ushort port)
        {
            serverIP = ip;
            serverPort = port;

            if (netcode != null)
            {
                var transport = netcode.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(ip, port);
                }
            }

            GameDebug.Log(BuildContext(), "Server address updated", ("IP", ip), ("Port", port));
        }

        private void SetConnectionState(NetworkConnectionState newState)
        {
            if (connectionState != newState)
            {
                connectionState = newState;
                OnConnectionStateChanged?.Invoke(connectionState);
                GameDebug.Log(BuildContext(), "Connection state changed", ("State", newState.ToString()));
            }
        }

        private void HandleConnectionError(string message)
        {
            lastErrorCode = NetworkErrorCode.Unknown;
            lastErrorMessage = message;
            SetConnectionState(NetworkConnectionState.Error);
            OnNetworkError?.Invoke($"[{NetworkErrorCode.Unknown}] {message}");
            GameDebug.LogError(BuildContext(), message);
        }

        private void HandleConnectionWarning(string message)
        {
            OnNetworkWarning?.Invoke(message);
            GameDebug.LogWarning(BuildContext(), message);
        }

        private void ScheduleReconnection()
        {
            if (reconnectionCoroutine != null)
            {
                StopCoroutine(reconnectionCoroutine);
            }
            reconnectionCoroutine = StartCoroutine(ReconnectionRoutine());
        }

        private IEnumerator ReconnectionRoutine()
        {
            while (reconnectionAttempts < maxReconnectionAttempts && connectionState != NetworkConnectionState.Connected)
            {
                reconnectionAttempts++;
                GameDebug.Log(BuildContext(), "Attempting reconnection", ("Attempt", reconnectionAttempts), ("MaxAttempts", maxReconnectionAttempts));
                
                yield return new WaitForSeconds(reconnectionInterval);
                
                if (connectionState != NetworkConnectionState.Connected)
                {
                    StartClient();
                    yield return new WaitForSeconds(5f); // Wait for connection attempt
                }
            }

            if (connectionState != NetworkConnectionState.Connected)
            {
                string error = $"Max reconnection attempts ({maxReconnectionAttempts}) exceeded";
                lastErrorCode = NetworkErrorCode.MaxReconnectionAttempts;
                lastErrorMessage = error;
                SetConnectionState(NetworkConnectionState.Error);
                OnNetworkError?.Invoke($"[{NetworkErrorCode.MaxReconnectionAttempts}] {error}");
            }

            reconnectionCoroutine = null;
        }

        // Unity Netcode callbacks
        private void OnClientConnected(ulong clientId)
        {
            SetConnectionState(NetworkConnectionState.Connected);
            reconnectionAttempts = 0;
            GameDebug.Log(BuildContext(), "Client connected", ("ClientId", clientId));
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (netcode.IsHost && clientId == netcode.LocalClientId)
            {
                SetConnectionState(NetworkConnectionState.Disconnected);
            }
            else if (!netcode.IsHost && clientId == netcode.LocalClientId)
            {
                SetConnectionState(NetworkConnectionState.Disconnected);
                if (enableReconnection)
                {
                    ScheduleReconnection();
                }
            }
            GameDebug.Log(BuildContext(), "Client disconnected", ("ClientId", clientId));
        }

        private void OnServerStarted()
        {
            SetConnectionState(NetworkConnectionState.Connected);
            GameDebug.Log(BuildContext(), "Server started successfully");
        }

        private void OnClientStarted()
        {
            GameDebug.Log(BuildContext(), "Client started successfully");
        }

        private GameDebugContext BuildContext()
        {
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                GameDebugMechanicTag.Networking,
                subsystem: nameof(NetworkConnectionManager),
                actor: gameObject?.name);
        }
    }
}