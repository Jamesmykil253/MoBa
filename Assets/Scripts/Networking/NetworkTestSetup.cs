using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace MOBA.Networking
{
    /// <summary>
    /// Network testing setup for development and validation
    /// </summary>
    public class NetworkTestSetup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button serverButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button disconnectButton;

        [Header("Network Settings")]
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        [Header("Test Settings")]
        [SerializeField] private bool autoConnectAsClient = false;
        [SerializeField] private float autoConnectDelay = 2f;

        private NetworkManager networkManager;
        private NetworkGameManager gameManager;

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
            gameManager = FindFirstObjectByType<NetworkGameManager>();

            // Set up UI event listeners
            if (hostButton != null)
                hostButton.onClick.AddListener(StartHost);
            if (clientButton != null)
                clientButton.onClick.AddListener(StartClient);
            if (serverButton != null)
                serverButton.onClick.AddListener(StartServer);
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(Disconnect);

            // Subscribe to network events
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnServerStopped += OnServerStopped;
        }

        private void Start()
        {
            UpdateUI();

            // Auto-connect for testing
            if (autoConnectAsClient)
            {
                StartCoroutine(AutoConnectAsClient());
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from network events
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnServerStopped -= OnServerStopped;
            }
        }

        private void Update()
        {
            UpdateUI();
        }

        private void StartHost()
        {
            if (networkManager.StartHost())
            {
                UpdateStatus("Started as Host");
                UnityEngine.Debug.Log("[NetworkTestSetup] Started as Host");
            }
            else
            {
                UpdateStatus("Failed to start Host");
                UnityEngine.Debug.LogError("[NetworkTestSetup] Failed to start Host");
            }
        }

        private void StartClient()
        {
            // Note: Transport configuration removed due to compatibility issues
            // Transport should be configured in NetworkManager component in Inspector
            // Using configured ipAddress: {ipAddress} and port: {port} for reference
            UnityEngine.Debug.Log($"[NetworkTestSetup] Attempting to connect using configured address {ipAddress}:{port}");

            if (networkManager.StartClient())
            {
                UpdateStatus("Started as Client");
                UnityEngine.Debug.Log("[NetworkTestSetup] Started as Client");
            }
            else
            {
                UpdateStatus("Failed to start Client");
                UnityEngine.Debug.LogError("[NetworkTestSetup] Failed to start Client");
            }
        }

        private void StartServer()
        {
            if (networkManager.StartServer())
            {
                UpdateStatus("Started as Server");
                UnityEngine.Debug.Log("[NetworkTestSetup] Started as Server");
            }
            else
            {
                UpdateStatus("Failed to start Server");
                UnityEngine.Debug.LogError("[NetworkTestSetup] Failed to start Server");
            }
        }

        private void Disconnect()
        {
            networkManager.Shutdown();
            UpdateStatus("Disconnected");
            UnityEngine.Debug.Log("[NetworkTestSetup] Disconnected");
        }

        private void OnClientConnected(ulong clientId)
        {
            UpdateStatus($"Client {clientId} connected");
            UnityEngine.Debug.Log($"[NetworkTestSetup] Client {clientId} connected");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            UpdateStatus($"Client {clientId} disconnected");
            UnityEngine.Debug.Log($"[NetworkTestSetup] Client {clientId} disconnected");
        }

        private void OnServerStarted()
        {
            UpdateStatus("Server started");
            UnityEngine.Debug.Log("[NetworkTestSetup] Server started");
        }

        private void OnServerStopped(bool intentional)
        {
            UpdateStatus($"Server stopped ({(intentional ? "intentional" : "error")})");
            UnityEngine.Debug.Log($"[NetworkTestSetup] Server stopped (intentional: {intentional})");
        }

        private void UpdateUI()
        {
            if (statusText != null)
            {
                string status = "Disconnected";
                if (networkManager.IsHost)
                    status = "Host";
                else if (networkManager.IsServer)
                    status = "Server";
                else if (networkManager.IsClient)
                    status = "Client";

                statusText.text = $"Status: {status}";
            }

            if (playerCountText != null && gameManager != null)
            {
                playerCountText.text = $"Players: {gameManager.ConnectedPlayers}/{gameManager.MaxPlayers}";
            }

            // Enable/disable buttons based on state
            if (hostButton != null)
                hostButton.interactable = !networkManager.IsListening;
            if (clientButton != null)
                clientButton.interactable = !networkManager.IsListening;
            if (serverButton != null)
                serverButton.interactable = !networkManager.IsListening;
            if (disconnectButton != null)
                disconnectButton.interactable = networkManager.IsListening;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = $"Status: {message}";
            }
        }

        private IEnumerator AutoConnectAsClient()
        {
            yield return new WaitForSeconds(autoConnectDelay);
            StartClient();
        }

        // Test methods for validation
        [ContextMenu("Test Movement Sync")]
        private void TestMovementSync()
        {
            if (!networkManager.IsClient) return;

            // Find local player
            var localPlayer = FindFirstObjectByType<NetworkPlayerController>();
            if (localPlayer != null && localPlayer.IsOwner)
            {
                // Test movement input
                localPlayer.SetMovementInput(new Vector3(1f, 0f, 0f));
                UnityEngine.Debug.Log("[NetworkTestSetup] Testing movement sync");
            }
        }

        [ContextMenu("Test Ability Cast")]
        private void TestAbilityCast()
        {
            if (!networkManager.IsClient) return;

            // Find ability system
            var abilitySystem = FindFirstObjectByType<NetworkAbilitySystem>();
            if (abilitySystem != null)
            {
                // Test ability cast
                Vector3 targetPosition = transform.position + transform.forward * 5f;
                abilitySystem.CastAbilityServerRpc(AbilityType.Ability1, targetPosition);
                UnityEngine.Debug.Log("[NetworkTestSetup] Testing ability cast");
            }
        }

        [ContextMenu("Test Damage System")]
        private void TestDamageSystem()
        {
            if (!networkManager.IsServer) return;

            // Find all players and damage them
            var players = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                player.TakeDamage(50f);
                UnityEngine.Debug.Log($"[NetworkTestSetup] Damaged player for 50 HP");
            }
        }

        [ContextMenu("Spawn Test Projectile")]
        private void SpawnTestProjectile()
        {
            if (!networkManager.IsServer) return;

            Vector3 spawnPosition = transform.position + transform.forward * 2f;
            Vector3 direction = transform.forward;

            NetworkProjectile.SpawnProjectile(spawnPosition, direction, 100, 5f, networkManager.LocalClientId);
            UnityEngine.Debug.Log("[NetworkTestSetup] Spawned test projectile");
        }

        // Debug GUI for testing
        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));

            GUILayout.Label("Network Test Controls", GUILayout.Width(280));

            if (GUILayout.Button("Start Host", GUILayout.Width(280)))
                StartHost();
            if (GUILayout.Button("Start Client", GUILayout.Width(280)))
                StartClient();
            if (GUILayout.Button("Start Server", GUILayout.Width(280)))
                StartServer();
            if (GUILayout.Button("Disconnect", GUILayout.Width(280)))
                Disconnect();

            GUILayout.Space(10);

            if (GUILayout.Button("Test Movement", GUILayout.Width(280)))
                TestMovementSync();
            if (GUILayout.Button("Test Ability", GUILayout.Width(280)))
                TestAbilityCast();
            if (GUILayout.Button("Test Damage", GUILayout.Width(280)))
                TestDamageSystem();
            if (GUILayout.Button("Spawn Projectile", GUILayout.Width(280)))
                SpawnTestProjectile();

            GUILayout.Space(10);

            if (gameManager != null)
            {
                GUILayout.Label($"Connected: {gameManager.ConnectedPlayers}/{gameManager.MaxPlayers}");
                GUILayout.Label($"Game Started: {gameManager.IsGameStarted}");
            }

            if (networkManager != null)
            {
                GUILayout.Label($"Is Host: {networkManager.IsHost}");
                GUILayout.Label($"Is Server: {networkManager.IsServer}");
                GUILayout.Label($"Is Client: {networkManager.IsClient}");
                GUILayout.Label($"Local Client ID: {networkManager.LocalClientId}");
            }

            GUILayout.EndArea();
        }
    }
}