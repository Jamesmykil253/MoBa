using UnityEngine;
using Unity.Netcode;

namespace MOBA.Networking
{
    /// <summary>
    /// Automatically sets up lobby system components in the scene
    /// Ensures proper lobby configuration for development workflow
    /// </summary>
    [System.Serializable]
    public class LobbySceneSetup : MonoBehaviour
    {
        [Header("Auto Setup Configuration")]
        [SerializeField] private bool autoSetupOnAwake = false; // Changed to manual setup only
        [SerializeField] private bool createLobbySystemIfMissing = true;
        // REMOVED: UI system simplified
        [SerializeField] private bool createLobbyIntegrationIfMissing = true;
        
        [Header("Lobby System Prefabs (Optional)")]
        [SerializeField] private GameObject lobbySystemPrefab;
        [SerializeField] private GameObject lobbyUIPrefab;
        
        [Header("Default Settings")]
        [SerializeField] private int defaultMaxPlayers = 8;
        [SerializeField] private string defaultLobbyName = "MOBA Development Lobby";
        [SerializeField] private bool defaultAutoCreate = true;
        
        private void Awake()
        {
            if (autoSetupOnAwake)
            {
                SetupLobbyScene();
            }
        }
        
        [ContextMenu("Setup Lobby Scene")]
        public void SetupLobbyScene()
        {
            Debug.Log("[LobbySceneSetup] 🔧 Setting up lobby scene...");
            
            // Ensure NetworkManager exists
            EnsureNetworkManager();
            
            // Setup lobby systems
            SetupLobbySystem();
            // SetupLobbyUI(); // REMOVED: LobbyUI was removed during cleanup
            SetupLobbyIntegration();
            
            Debug.Log("[LobbySceneSetup] ✅ Lobby scene setup complete!");
        }
        
        private void EnsureNetworkManager()
        {
            var networkManager = FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.Log("[LobbySceneSetup] Creating NetworkManager...");
                var networkManagerGO = new GameObject("NetworkManager");
                networkManager = networkManagerGO.AddComponent<NetworkManager>();
                
                // Basic configuration
                var config = new Unity.Netcode.NetworkConfig();
                config.TickRate = 60;
                config.EnableSceneManagement = false;
                networkManager.NetworkConfig = config;
            }
        }
        
        private void SetupLobbySystem()
        {
            var lobbySystem = FindFirstObjectByType<LobbySystem>();
            if (lobbySystem == null && createLobbySystemIfMissing)
            {
                Debug.Log("[LobbySceneSetup] Creating LobbySystem...");
                
                GameObject lobbySystemGO;
                if (lobbySystemPrefab != null)
                {
                    lobbySystemGO = Instantiate(lobbySystemPrefab);
                    lobbySystemGO.name = "LobbySystem";
                }
                else
                {
                    lobbySystemGO = new GameObject("LobbySystem");
                    lobbySystem = lobbySystemGO.AddComponent<LobbySystem>();
                    
                    // Add NetworkObject component if not present
                    if (lobbySystemGO.GetComponent<NetworkObject>() == null)
                    {
                        lobbySystemGO.AddComponent<NetworkObject>();
                    }
                }
                
                // Configure lobby system
                ConfigureLobbySystem(lobbySystem ?? lobbySystemGO.GetComponent<LobbySystem>());
            }
        }
        
        private void ConfigureLobbySystem(LobbySystem lobbySystem)
        {
            if (lobbySystem == null) return;
            
            // Use reflection to set private fields if needed
            var fields = typeof(LobbySystem).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.Name.Contains("maxLobbyPlayers"))
                {
                    field.SetValue(lobbySystem, defaultMaxPlayers);
                }
                else if (field.Name.Contains("lobbyName"))
                {
                    field.SetValue(lobbySystem, defaultLobbyName);
                }
                else if (field.Name.Contains("autoCreateLobby"))
                {
                    field.SetValue(lobbySystem, defaultAutoCreate);
                }
            }
            
            Debug.Log("[LobbySceneSetup] ✅ LobbySystem configured");
        }
        
        private void SetupLobbyUI()
        {
            // REMOVED: LobbyUI was removed during cleanup
            // UI is now handled by simpler systems
            Debug.Log("[LobbySceneSetup] LobbyUI setup skipped - UI system simplified");
            return;
            
            /* DISABLED - LobbyUI system removed
            var lobbyUI = FindFirstObjectByType<MOBA.UI.LobbyUI>();
            if (lobbyUI == null && createLobbyUIIfMissing)
            {
                Debug.Log("[LobbySceneSetup] Creating LobbyUI...");
                
                GameObject lobbyUIGO;
                if (lobbyUIPrefab != null)
                {
                    lobbyUIGO = Instantiate(lobbyUIPrefab);
                    lobbyUIGO.name = "LobbyUI";
                }
                else
                {
                    // Create basic UI structure
                    lobbyUIGO = CreateBasicLobbyUI();
                }
            }
        }
        
        private GameObject CreateBasicLobbyUI()
        {
            // Create Canvas if not exists
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Create LobbyUI GameObject
            var lobbyUIGO = new GameObject("LobbyUI");
            lobbyUIGO.transform.SetParent(canvas.transform, false);
            
            var lobbyUI = lobbyUIGO.AddComponent<MOBA.UI.LobbyUI>();
            
            Debug.Log("[LobbySceneSetup] ✅ Basic LobbyUI created");
            return lobbyUIGO;
            */ // End of disabled LobbyUI code
        }
        
        private void SetupLobbyIntegration()
        {
            var lobbyIntegration = FindFirstObjectByType<LobbyIntegration>();
            if (lobbyIntegration == null && createLobbyIntegrationIfMissing)
            {
                Debug.Log("[LobbySceneSetup] Creating LobbyIntegration...");
                
                var lobbyIntegrationGO = new GameObject("LobbyIntegration");
                lobbyIntegration = lobbyIntegrationGO.AddComponent<LobbyIntegration>();
                
                Debug.Log("[LobbySceneSetup] ✅ LobbyIntegration created");
            }
        }
        
        [ContextMenu("Validate Lobby Setup")]
        public void ValidateLobbySetup()
        {
            Debug.Log("[LobbySceneSetup] 🔍 Validating lobby setup...");
            
            var validation = new System.Text.StringBuilder();
            validation.AppendLine("Lobby Scene Validation Report:");
            
            // Check NetworkManager
            var networkManager = FindFirstObjectByType<NetworkManager>();
            validation.AppendLine($"NetworkManager: {(networkManager != null ? "✅ Present" : "❌ Missing")}");
            
            // Check LobbySystem
            var lobbySystem = FindFirstObjectByType<LobbySystem>();
            validation.AppendLine($"LobbySystem: {(lobbySystem != null ? "✅ Present" : "❌ Missing")}");
            
            // Check LobbyUI - REMOVED: UI system simplified
            validation.AppendLine($"LobbyUI: ❌ Removed (UI system simplified)");
            
            // Check LobbyIntegration
            var lobbyIntegration = FindFirstObjectByType<LobbyIntegration>();
            validation.AppendLine($"LobbyIntegration: {(lobbyIntegration != null ? "✅ Present" : "❌ Missing")}");
            
            // Check NetworkSystemIntegration
            var networkIntegration = FindFirstObjectByType<NetworkSystemIntegration>();
            validation.AppendLine($"NetworkSystemIntegration: {(networkIntegration != null ? "✅ Present" : "❌ Missing")}");
            
            Debug.Log(validation.ToString());
        }
        
        [ContextMenu("Create Lobby Prefabs")]
        public void CreateLobbyPrefabs()
        {
            Debug.Log("[LobbySceneSetup] 📦 Creating lobby prefabs for reuse...");
            
            // This would create prefabs in the Prefabs folder
            // Implementation would depend on your project structure
            Debug.Log("[LobbySceneSetup] Note: Prefab creation requires editor scripting");
        }
        
        // Public getters for validation
        public bool IsLobbySystemPresent => FindFirstObjectByType<LobbySystem>() != null;
        public bool IsLobbyUIPresent => false; // REMOVED: UI system simplified
        public bool IsLobbyIntegrationPresent => FindFirstObjectByType<LobbyIntegration>() != null;
        public bool IsNetworkManagerPresent => FindFirstObjectByType<NetworkManager>() != null;
        public bool IsFullyConfigured => IsLobbySystemPresent && IsLobbyIntegrationPresent && IsNetworkManagerPresent;
        
        // Runtime configuration methods
        public void EnableAutoLobbyCreation(bool enable)
        {
            var lobbySystem = FindFirstObjectByType<LobbySystem>();
            if (lobbySystem != null)
            {
                // Set auto-create via reflection or public method
                Debug.Log($"[LobbySceneSetup] Auto lobby creation: {enable}");
            }
        }
        
        public void SetMaxPlayers(int maxPlayers)
        {
            defaultMaxPlayers = maxPlayers;
            var lobbySystem = FindFirstObjectByType<LobbySystem>();
            if (lobbySystem != null)
            {
                ConfigureLobbySystem(lobbySystem);
            }
        }
        
        public void SetLobbyName(string lobbyName)
        {
            defaultLobbyName = lobbyName;
            var lobbySystem = FindFirstObjectByType<LobbySystem>();
            if (lobbySystem != null)
            {
                ConfigureLobbySystem(lobbySystem);
            }
        }
    }
}
