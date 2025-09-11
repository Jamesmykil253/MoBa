using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

namespace MOBA.Editor
{
    /// <summary>
    /// Comprehensive Network Prefab Creator - Complete audit-driven networking prefab generation
    /// Creates all networking prefabs and components for production-ready MOBA architecture
    /// </summary>
    public class ComprehensiveNetworkPrefabCreator : EditorWindow
    {
        // Core Settings
        private bool overwriteExisting = true;
        private string saveLocation = "Assets/Prefabs/Network/";
        
        // Audit Results
        private Dictionary<string, bool> auditResults = new Dictionary<string, bool>();
        private bool auditCompleted = false;
        
        // Network Architecture Selection
        private NetworkArchitecture selectedArchitecture = NetworkArchitecture.DedicatedServer;
        private NetworkTopology selectedTopology = NetworkTopology.ClientServer;
        
        // Prefab Creation Options
        private bool createNetworkManager = true;
        private bool createPlayerPrefab = true;
        private bool createProjectilePrefab = true;
        private bool createGameManager = true;
        private bool createEventBus = true;
        private bool createObjectPools = true;
        private bool createAntiCheat = true;
        private bool createLagCompensation = true;
        private bool createProfiler = true;
        private bool createDedicatedServerConfig = true;
        
        // Network Configuration
        private int maxPlayers = 8;
        private uint tickRate = 60;
        private bool enableSceneManagement = false;
        private bool enableClientPrediction = true;
        private bool enableServerReconciliation = true;
        private bool enableLagCompensation = true;
        
        // Transport Settings
        private TransportType transportType = TransportType.UnityTransport;
        private string serverIP = "127.0.0.1";
        private ushort serverPort = 7777;
        private int connectionTimeoutMS = 10000;
        private int maxConnectionAttempts = 5;
        
        // Anti-Cheat Configuration
        private bool enablePositionValidation = true;
        private bool enableSpeedValidation = true;
        private bool enableAbilityValidation = true;
        private bool enableRateLimit = true;
        private float maxPlayerSpeed = 10f;
        private float positionTolerance = 1f;
        private int maxAbilitiesPerSecond = 5;
        
        // Object Pool Configuration (used in NetworkObjectPool setup)
        [SerializeField] private int defaultPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private bool enablePoolStats = true;
        [SerializeField] private bool enableBackgroundInitialization = true;
        
        // Lag Compensation Settings (used in LagCompensationManager setup)
        [SerializeField] private float historyLength = 1f;
        [SerializeField] private int maxHistoryEntries = 60;
        [SerializeField] private bool enableHitboxRewinding = true;
        [SerializeField] private bool enableMovementPrediction = true;
        
        // Debug Configuration
        private bool enableNetworkDebugging = true;
        private bool enablePerformanceProfiling = true;
        private bool enableNetworkLogging = true;
        private bool createTestComponents = false;
        
        // Visual Configuration
        private bool addNetworkVisualization = true;
        private bool addLatencyDisplay = true;
        private bool addPacketLossDisplay = true;
        
        // Scroll position for UI
        private Vector2 scrollPosition;
        
        public enum NetworkArchitecture
        {
            DedicatedServer,
            ListenServer,
            PeerToPeer,
            CloudServer,
            Hybrid
        }
        
        public enum NetworkTopology
        {
            ClientServer,
            PeerToPeer,
            MeshNetwork,
            StarTopology
        }
        
        public enum TransportType
        {
            UnityTransport,
            UnetTransport,
            SteamNetworking,
            CustomTCP,
            CustomUDP
        }
        
        [MenuItem("MOBA Tools/Network Systems/Comprehensive Network Prefab Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ComprehensiveNetworkPrefabCreator>("Network Creator");
            window.minSize = new Vector2(500, 800);
            window.Show();
        }
        
        private void OnEnable()
        {
            PerformNetworkAudit();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Comprehensive Network Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Show audit results
            DrawAuditResults();
            EditorGUILayout.Space();
            
            // Core Settings
            DrawCoreSettings();
            EditorGUILayout.Space();
            
            // Network Architecture
            DrawNetworkArchitecture();
            EditorGUILayout.Space();
            
            // Prefab Creation Options
            DrawPrefabCreationOptions();
            EditorGUILayout.Space();
            
            // Network Configuration
            DrawNetworkConfiguration();
            EditorGUILayout.Space();
            
            // Transport Settings
            DrawTransportSettings();
            EditorGUILayout.Space();
            
            // Anti-Cheat Configuration
            DrawAntiCheatConfiguration();
            EditorGUILayout.Space();
            
            // Performance & Debugging
            DrawPerformanceAndDebugging();
            EditorGUILayout.Space();
            
            // Create Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Network Prefabs & Systems", GUILayout.Height(40)))
            {
                CreateNetworkSystems();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndScrollView();
        }
        
        private void PerformNetworkAudit()
        {
            auditResults.Clear();
            
            // Audit core networking systems
            auditResults["Unity Netcode"] = HasUnityNetcode();
            auditResults["NetworkManager"] = AssetDatabase.FindAssets("t:MonoScript NetworkManager").Length > 0;
            auditResults["NetworkSystemIntegration"] = AssetDatabase.FindAssets("t:MonoScript NetworkSystemIntegration").Length > 0;
            auditResults["NetworkGameManager"] = AssetDatabase.FindAssets("t:MonoScript NetworkGameManager").Length > 0;
            auditResults["NetworkPlayerController"] = AssetDatabase.FindAssets("t:MonoScript NetworkPlayerController").Length > 0;
            
            // Audit network components
            auditResults["NetworkEventBus"] = AssetDatabase.FindAssets("t:MonoScript NetworkEventBus").Length > 0;
            auditResults["NetworkObjectPool"] = AssetDatabase.FindAssets("t:MonoScript NetworkObjectPool").Length > 0;
            auditResults["NetworkPoolObjectManager"] = AssetDatabase.FindAssets("t:MonoScript NetworkPoolObjectManager").Length > 0;
            auditResults["NetworkAbilitySystem"] = AssetDatabase.FindAssets("t:MonoScript NetworkAbilitySystem").Length > 0;
            auditResults["NetworkProjectile"] = AssetDatabase.FindAssets("t:MonoScript NetworkProjectile").Length > 0;
            
            // Audit advanced systems
            auditResults["LagCompensationManager"] = AssetDatabase.FindAssets("t:MonoScript LagCompensationManager").Length > 0;
            auditResults["AntiCheatSystem"] = AssetDatabase.FindAssets("t:MonoScript AntiCheatSystem").Length > 0;
            auditResults["NetworkProfiler"] = AssetDatabase.FindAssets("t:MonoScript NetworkProfiler").Length > 0;
            auditResults["DedicatedServerConfig"] = AssetDatabase.FindAssets("t:MonoScript DedicatedServerConfig").Length > 0;
            
            // Audit directories
            auditResults["Network Prefab Directory"] = AssetDatabase.IsValidFolder("Assets/Prefabs/Network");
            auditResults["Network Scene Directory"] = AssetDatabase.IsValidFolder("Assets/Scenes/Network");
            
            auditCompleted = true;
        }
        
        private bool HasUnityNetcode()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Any(a => a.GetName().Name.Contains("Unity.Netcode"));
        }
        
        private void DrawAuditResults()
        {
            EditorGUILayout.LabelField("Network System Audit", EditorStyles.boldLabel);
            
            if (!auditCompleted)
            {
                EditorGUILayout.HelpBox("Performing network audit...", MessageType.Info);
                return;
            }
            
            EditorGUI.indentLevel++;
            foreach (var result in auditResults)
            {
                EditorGUILayout.BeginHorizontal();
                var icon = result.Value ? "✓" : "✗";
                var color = result.Value ? Color.green : Color.red;
                
                var oldColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField(icon, GUILayout.Width(20));
                GUI.color = oldColor;
                
                EditorGUILayout.LabelField(result.Key);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
        
        private void DrawCoreSettings()
        {
            EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
            
            saveLocation = EditorGUILayout.TextField("Save Location", saveLocation);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        }
        
        private void DrawNetworkArchitecture()
        {
            EditorGUILayout.LabelField("Network Architecture", EditorStyles.boldLabel);
            
            selectedArchitecture = (NetworkArchitecture)EditorGUILayout.EnumPopup("Architecture", selectedArchitecture);
            selectedTopology = (NetworkTopology)EditorGUILayout.EnumPopup("Topology", selectedTopology);
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(GetArchitectureDescription(), MessageType.Info);
        }
        
        private void DrawPrefabCreationOptions()
        {
            EditorGUILayout.LabelField("Prefab Creation Options", EditorStyles.boldLabel);
            
            createNetworkManager = EditorGUILayout.Toggle("Network Manager", createNetworkManager);
            createPlayerPrefab = EditorGUILayout.Toggle("Network Player Prefab", createPlayerPrefab);
            createProjectilePrefab = EditorGUILayout.Toggle("Network Projectile Prefab", createProjectilePrefab);
            createGameManager = EditorGUILayout.Toggle("Game Manager", createGameManager);
            createEventBus = EditorGUILayout.Toggle("Event Bus", createEventBus);
            createObjectPools = EditorGUILayout.Toggle("Object Pools", createObjectPools);
            createAntiCheat = EditorGUILayout.Toggle("Anti-Cheat System", createAntiCheat);
            createLagCompensation = EditorGUILayout.Toggle("Lag Compensation", createLagCompensation);
            createProfiler = EditorGUILayout.Toggle("Network Profiler", createProfiler);
            createDedicatedServerConfig = EditorGUILayout.Toggle("Dedicated Server Config", createDedicatedServerConfig);
        }
        
        private void DrawNetworkConfiguration()
        {
            EditorGUILayout.LabelField("Network Configuration", EditorStyles.boldLabel);
            
            maxPlayers = EditorGUILayout.IntSlider("Max Players", maxPlayers, 2, 64);
            tickRate = (uint)EditorGUILayout.IntSlider("Tick Rate", (int)tickRate, 20, 128);
            enableSceneManagement = EditorGUILayout.Toggle("Scene Management", enableSceneManagement);
            enableClientPrediction = EditorGUILayout.Toggle("Client Prediction", enableClientPrediction);
            enableServerReconciliation = EditorGUILayout.Toggle("Server Reconciliation", enableServerReconciliation);
            enableLagCompensation = EditorGUILayout.Toggle("Lag Compensation", enableLagCompensation);
        }
        
        private void DrawTransportSettings()
        {
            EditorGUILayout.LabelField("Transport Settings", EditorStyles.boldLabel);
            
            transportType = (TransportType)EditorGUILayout.EnumPopup("Transport Type", transportType);
            serverIP = EditorGUILayout.TextField("Server IP", serverIP);
            serverPort = (ushort)EditorGUILayout.IntField("Server Port", serverPort);
            connectionTimeoutMS = EditorGUILayout.IntField("Connection Timeout (ms)", connectionTimeoutMS);
            maxConnectionAttempts = EditorGUILayout.IntField("Max Connection Attempts", maxConnectionAttempts);
        }
        
        private void DrawAntiCheatConfiguration()
        {
            EditorGUILayout.LabelField("Anti-Cheat Configuration", EditorStyles.boldLabel);
            
            enablePositionValidation = EditorGUILayout.Toggle("Position Validation", enablePositionValidation);
            enableSpeedValidation = EditorGUILayout.Toggle("Speed Validation", enableSpeedValidation);
            enableAbilityValidation = EditorGUILayout.Toggle("Ability Validation", enableAbilityValidation);
            enableRateLimit = EditorGUILayout.Toggle("Rate Limiting", enableRateLimit);
            
            if (enableSpeedValidation)
            {
                EditorGUI.indentLevel++;
                maxPlayerSpeed = EditorGUILayout.FloatField("Max Player Speed", maxPlayerSpeed);
                positionTolerance = EditorGUILayout.FloatField("Position Tolerance", positionTolerance);
                EditorGUI.indentLevel--;
            }
            
            if (enableRateLimit)
            {
                EditorGUI.indentLevel++;
                maxAbilitiesPerSecond = EditorGUILayout.IntField("Max Abilities/Second", maxAbilitiesPerSecond);
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPerformanceAndDebugging()
        {
            EditorGUILayout.LabelField("Performance & Debugging", EditorStyles.boldLabel);
            
            enableNetworkDebugging = EditorGUILayout.Toggle("Network Debugging", enableNetworkDebugging);
            enablePerformanceProfiling = EditorGUILayout.Toggle("Performance Profiling", enablePerformanceProfiling);
            enableNetworkLogging = EditorGUILayout.Toggle("Network Logging", enableNetworkLogging);
            createTestComponents = EditorGUILayout.Toggle("Test Components", createTestComponents);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual Debugging", EditorStyles.miniBoldLabel);
            addNetworkVisualization = EditorGUILayout.Toggle("Network Visualization", addNetworkVisualization);
            addLatencyDisplay = EditorGUILayout.Toggle("Latency Display", addLatencyDisplay);
            addPacketLossDisplay = EditorGUILayout.Toggle("Packet Loss Display", addPacketLossDisplay);
        }
        
        private string GetArchitectureDescription()
        {
            switch (selectedArchitecture)
            {
                case NetworkArchitecture.DedicatedServer:
                    return "Dedicated server architecture with authoritative server and client connections. Best for production MOBA games.";
                case NetworkArchitecture.ListenServer:
                    return "Listen server where one client acts as both player and server. Good for smaller matches.";
                case NetworkArchitecture.PeerToPeer:
                    return "Peer-to-peer networking without central server. Suitable for cooperative gameplay.";
                case NetworkArchitecture.CloudServer:
                    return "Cloud-based server architecture with auto-scaling and global distribution.";
                case NetworkArchitecture.Hybrid:
                    return "Hybrid architecture combining multiple approaches for optimal performance.";
                default:
                    return "Select an architecture to see description.";
            }
        }
        
        private void CreateNetworkSystems()
        {
            try
            {
                // Ensure directories exist
                EnsureDirectoriesExist();
                
                var createdObjects = new List<GameObject>();
                
                // Create Network Manager
                if (createNetworkManager)
                {
                    var networkManager = CreateNetworkManager();
                    if (networkManager != null) createdObjects.Add(networkManager);
                }
                
                // Create Network Player Prefab
                if (createPlayerPrefab)
                {
                    var playerPrefab = CreateNetworkPlayerPrefab();
                    if (playerPrefab != null) createdObjects.Add(playerPrefab);
                }
                
                // Create Network Projectile Prefab
                if (createProjectilePrefab)
                {
                    var projectilePrefab = CreateNetworkProjectilePrefab();
                    if (projectilePrefab != null) createdObjects.Add(projectilePrefab);
                }
                
                // Create Game Manager
                if (createGameManager)
                {
                    var gameManager = CreateNetworkGameManager();
                    if (gameManager != null) createdObjects.Add(gameManager);
                }
                
                // Create Event Bus
                if (createEventBus)
                {
                    var eventBus = CreateNetworkEventBus();
                    if (eventBus != null) createdObjects.Add(eventBus);
                }
                
                // Create Object Pools
                if (createObjectPools)
                {
                    var poolManager = CreateNetworkObjectPoolManager();
                    if (poolManager != null) createdObjects.Add(poolManager);
                }
                
                // Create System Integration
                var systemIntegration = CreateNetworkSystemIntegration();
                if (systemIntegration != null) createdObjects.Add(systemIntegration);
                
                // Save all as prefabs
                SavePrefabs(createdObjects);
                
                Debug.Log($"Successfully created {createdObjects.Count} network prefabs and systems!");
                
                // Show success dialog
                EditorUtility.DisplayDialog("Success", 
                    $"Network systems created successfully!\n\nCreated {createdObjects.Count} prefabs in {saveLocation}", 
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create network systems: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create network systems:\n{e.Message}", "OK");
            }
        }
        
        private void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder(saveLocation.TrimEnd('/')))
            {
                Directory.CreateDirectory(saveLocation.TrimEnd('/'));
                AssetDatabase.Refresh();
            }
            
            // Create subdirectories
            var subdirs = new[] { "Managers", "Players", "Projectiles", "Systems" };
            foreach (var subdir in subdirs)
            {
                var path = Path.Combine(saveLocation.TrimEnd('/'), subdir);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            AssetDatabase.Refresh();
        }
        
        private GameObject CreateNetworkManager()
        {
            var networkManager = new GameObject("NetworkManager");
            
            // Add NetworkManager component
            var netMgr = networkManager.AddComponent<NetworkManager>();
            
            // Add Unity Transport
            var transport = networkManager.AddComponent<UnityTransport>();
            transport.SetConnectionData(NetworkEndpoint.Parse(serverIP, serverPort));
            // Note: SetDebugSimulatorParameters is obsolete. Use Network Simulator from Multiplayer Tools package instead.
            
            // Configure NetworkManager
            netMgr.NetworkConfig.TickRate = tickRate;
            netMgr.NetworkConfig.EnableSceneManagement = enableSceneManagement;
            
            // Add custom components based on audit
            if (auditResults.ContainsKey("NetworkSystemIntegration") && auditResults["NetworkSystemIntegration"])
            {
                networkManager.AddComponent<MOBA.Networking.NetworkSystemIntegration>();
            }
            
            return networkManager;
        }
        
        private GameObject CreateNetworkPlayerPrefab()
        {
            var player = new GameObject("NetworkPlayer");
            
            // Core components
            player.AddComponent<NetworkObject>();
            player.AddComponent<CapsuleCollider>();
            player.AddComponent<Rigidbody>();
            
            // Add NetworkTransform for position synchronization
            player.AddComponent<NetworkTransform>();
            
            // Add custom player controller if available
            if (auditResults.ContainsKey("NetworkPlayerController") && auditResults["NetworkPlayerController"])
            {
                player.AddComponent<MOBA.Networking.NetworkPlayerController>();
            }
            
            // Add ability system if available
            if (auditResults.ContainsKey("NetworkAbilitySystem") && auditResults["NetworkAbilitySystem"])
            {
                player.AddComponent<MOBA.Networking.NetworkAbilitySystem>();
            }
            
            // Visual representation
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = Vector3.up;
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.blue;
            
            return player;
        }
        
        private GameObject CreateNetworkProjectilePrefab()
        {
            var projectile = new GameObject("NetworkProjectile");
            
            // Core components
            projectile.AddComponent<NetworkObject>();
            var collider = projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.1f;
            
            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            
            // Add NetworkTransform
            projectile.AddComponent<NetworkTransform>();
            
            // Add custom projectile component if available
            if (auditResults.ContainsKey("NetworkProjectile") && auditResults["NetworkProjectile"])
            {
                projectile.AddComponent<MOBA.Networking.NetworkProjectile>();
            }
            
            // Visual representation
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(projectile.transform);
            visual.transform.localScale = Vector3.one * 0.2f;
            DestroyImmediate(visual.GetComponent<SphereCollider>());
            
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;
            
            return projectile;
        }
        
        private GameObject CreateNetworkGameManager()
        {
            var gameManager = new GameObject("NetworkGameManager");
            
            // Add NetworkObject
            gameManager.AddComponent<NetworkObject>();
            
            // Add custom game manager if available
            if (auditResults.ContainsKey("NetworkGameManager") && auditResults["NetworkGameManager"])
            {
                gameManager.AddComponent<MOBA.Networking.NetworkGameManager>();
            }
            
            return gameManager;
        }
        
        private GameObject CreateNetworkEventBus()
        {
            var eventBus = new GameObject("NetworkEventBus");
            
            // Add custom event bus if available
            if (auditResults.ContainsKey("NetworkEventBus") && auditResults["NetworkEventBus"])
            {
                eventBus.AddComponent<MOBA.Networking.NetworkEventBus>();
            }
            
            return eventBus;
        }
        
        private GameObject CreateNetworkObjectPoolManager()
        {
            var poolManager = new GameObject("NetworkObjectPoolManager");
            
            // Add custom pool manager if available
            if (auditResults.ContainsKey("NetworkPoolObjectManager") && auditResults["NetworkPoolObjectManager"])
            {
                var poolComponent = poolManager.AddComponent<MOBA.Networking.NetworkPoolObjectManager>();
                
                // Configure pool settings using reflection to avoid compilation issues
                var poolType = poolComponent.GetType();
                
                // Set default pool size if field exists
                var defaultSizeField = poolType.GetField("defaultPoolSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (defaultSizeField != null)
                {
                    defaultSizeField.SetValue(poolComponent, defaultPoolSize);
                }
                
                // Set max pool size if field exists
                var maxSizeField = poolType.GetField("maxPoolSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (maxSizeField != null)
                {
                    maxSizeField.SetValue(poolComponent, maxPoolSize);
                }
                
                // Set enable stats if field exists
                var statsField = poolType.GetField("enableStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (statsField != null)
                {
                    statsField.SetValue(poolComponent, enablePoolStats);
                }
                
                // Set background initialization if field exists
                var backgroundInitField = poolType.GetField("enableBackgroundInitialization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (backgroundInitField != null)
                {
                    backgroundInitField.SetValue(poolComponent, enableBackgroundInitialization);
                }
                
                Debug.Log($"[NetworkPrefabCreator] Configured NetworkPoolObjectManager with DefaultSize: {defaultPoolSize}, MaxSize: {maxPoolSize}, Stats: {enablePoolStats}, BackgroundInit: {enableBackgroundInitialization}");
            }
            
            return poolManager;
        }
        
        private GameObject CreateNetworkSystemIntegration()
        {
            var systemIntegration = new GameObject("NetworkSystemIntegration");
            
            // Add system integration component
            if (auditResults.ContainsKey("NetworkSystemIntegration") && auditResults["NetworkSystemIntegration"])
            {
                systemIntegration.AddComponent<MOBA.Networking.NetworkSystemIntegration>();
            }
            
            // Add anti-cheat system if enabled and available
            if (createAntiCheat && auditResults.ContainsKey("AntiCheatSystem") && auditResults["AntiCheatSystem"])
            {
                systemIntegration.AddComponent<MOBA.Networking.AntiCheatSystem>();
            }
            
            // Add lag compensation if enabled and available
            if (createLagCompensation && auditResults.ContainsKey("LagCompensationManager") && auditResults["LagCompensationManager"])
            {
                var lagCompComponent = systemIntegration.AddComponent<MOBA.Networking.LagCompensationManager>();
                
                // Configure lag compensation settings using reflection
                var lagCompType = lagCompComponent.GetType();
                
                // Set history length if field exists
                var historyField = lagCompType.GetField("historyLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (historyField != null)
                {
                    historyField.SetValue(lagCompComponent, historyLength);
                }
                
                // Set max history entries if field exists
                var maxHistoryField = lagCompType.GetField("maxHistoryEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (maxHistoryField != null)
                {
                    maxHistoryField.SetValue(lagCompComponent, maxHistoryEntries);
                }
                
                // Set hitbox rewinding if field exists
                var rewindingField = lagCompType.GetField("enableHitboxRewinding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (rewindingField != null)
                {
                    rewindingField.SetValue(lagCompComponent, enableHitboxRewinding);
                }
                
                // Set movement prediction if field exists
                var predictionField = lagCompType.GetField("enableMovementPrediction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (predictionField != null)
                {
                    predictionField.SetValue(lagCompComponent, enableMovementPrediction);
                }
                
                Debug.Log($"[NetworkPrefabCreator] Configured LagCompensationManager with HistoryLength: {historyLength}s, MaxEntries: {maxHistoryEntries}, Rewinding: {enableHitboxRewinding}, Prediction: {enableMovementPrediction}");
            }
            
            // Add profiler if enabled and available
            if (createProfiler && auditResults.ContainsKey("NetworkProfiler") && auditResults["NetworkProfiler"])
            {
                systemIntegration.AddComponent<MOBA.Networking.NetworkProfiler>();
            }
            
            return systemIntegration;
        }
        
        private void SavePrefabs(List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                var subfolder = GetSubfolderForObject(obj);
                var prefabPath = Path.Combine(saveLocation.TrimEnd('/'), subfolder, obj.name + ".prefab");
                
                if (File.Exists(prefabPath) && !overwriteExisting)
                {
                    prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                }
                
                var prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
                
                // Clean up scene object
                DestroyImmediate(obj);
                
                Debug.Log($"Created network prefab: {prefabPath}");
            }
            
            AssetDatabase.Refresh();
        }
        
        private string GetSubfolderForObject(GameObject obj)
        {
            if (obj.name.Contains("Player")) return "Players";
            if (obj.name.Contains("Projectile")) return "Projectiles";
            if (obj.name.Contains("Manager") || obj.name.Contains("Integration")) return "Managers";
            return "Systems";
        }
    }
}
