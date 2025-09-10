using UnityEngine;
using UnityEditor;
using MOBA.Networking;

namespace MOBA.Editor
{
    /// <summary>
    /// Custom editor for NetworkPoolObjectManager with enhanced inspector and setup tools
    /// </summary>
    [CustomEditor(typeof(NetworkPoolObjectManager))]
    public class NetworkPoolObjectManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty enableLogging;
        private SerializedProperty initializeOnAwake;
        private SerializedProperty poolParent;
        private SerializedProperty poolConfigurations;
        private SerializedProperty objectsPerFrameLimit;
        private SerializedProperty backgroundInitializationDelay;
        
        private bool showPoolStats = false;
        private bool showAdvancedSettings = false;
        
        private void OnEnable()
        {
            enableLogging = serializedObject.FindProperty("enableLogging");
            initializeOnAwake = serializedObject.FindProperty("initializeOnAwake");
            poolParent = serializedObject.FindProperty("poolParent");
            poolConfigurations = serializedObject.FindProperty("poolConfigurations");
            objectsPerFrameLimit = serializedObject.FindProperty("objectsPerFrameLimit");
            backgroundInitializationDelay = serializedObject.FindProperty("backgroundInitializationDelay");
        }
        
        public override void OnInspectorGUI()
        {
            NetworkPoolObjectManager manager = (NetworkPoolObjectManager)target;
            serializedObject.Update();
            
            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Network Pool Object Manager", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Component-based object pool management for network objects", EditorStyles.helpBox);
            EditorGUILayout.Space();
            
            // Status Information
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Initialized: {manager.IsInitialized}");
                EditorGUILayout.LabelField($"Active Pools: {manager.ActivePoolCount}");
                
                if (GUILayout.Button("Refresh Pool Stats"))
                {
                    showPoolStats = !showPoolStats;
                }
                
                if (showPoolStats && manager.IsInitialized)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Pool Statistics:", EditorStyles.boldLabel);
                    var stats = manager.GetPoolStats();
                    foreach (var kvp in stats)
                    {
                        EditorGUILayout.LabelField($"{kvp.Key}: Active={kvp.Value.active}, Available={kvp.Value.available}, Total={kvp.Value.total}");
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            // Basic Configuration
            EditorGUILayout.LabelField("Basic Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableLogging, new GUIContent("Enable Logging", "Enable debug logging for pool operations"));
            EditorGUILayout.PropertyField(initializeOnAwake, new GUIContent("Initialize On Awake", "Automatically initialize pools when the component awakens"));
            EditorGUILayout.PropertyField(poolParent, new GUIContent("Pool Parent", "Parent transform for all pool GameObjects (optional)"));
            
            EditorGUILayout.Space();
            
            // Pool Configurations
            EditorGUILayout.LabelField("Pool Configurations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(poolConfigurations, new GUIContent("Pool Configurations", "Configure all object pools"));
            
            EditorGUILayout.Space();
            
            // Advanced Settings
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
            if (showAdvancedSettings)
            {
                EditorGUILayout.PropertyField(objectsPerFrameLimit, new GUIContent("Objects Per Frame Limit", "Maximum objects to process per frame during initialization"));
                EditorGUILayout.PropertyField(backgroundInitializationDelay, new GUIContent("Background Init Delay", "Delay between pool initializations in seconds"));
            }
            
            EditorGUILayout.Space();
            
            // Runtime Controls
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Initialize All Pools"))
                {
                    manager.InitializeAllPools();
                }
                if (GUILayout.Button("Clear All Pools"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Pools", "Are you sure you want to clear all object pools? This cannot be undone.", "Yes", "Cancel"))
                    {
                        manager.ClearAllPools();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Log Pool Information"))
                {
                    Debug.Log(manager.GetPoolInfo());
                }
            }
            
            // Setup Tools
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Validate Configurations"))
            {
                manager.ValidateConfigurations();
            }
            
            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Auto-Setup Standard MOBA Pools"))
                {
                    SetupStandardMOBAPools();
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void SetupStandardMOBAPools()
        {
            // Find common MOBA prefabs in the project
            string[] playerPrefabPaths = AssetDatabase.FindAssets("Player t:Prefab");
            string[] projectilePrefabPaths = AssetDatabase.FindAssets("Projectile t:Prefab");
            string[] effectPrefabPaths = AssetDatabase.FindAssets("Effect t:Prefab");
            
            var configs = new System.Collections.Generic.List<PoolConfiguration>();
            
            // Add Player pool if Player prefab found
            if (playerPrefabPaths.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(playerPrefabPaths[0]);
                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (playerPrefab != null)
                {
                    configs.Add(new PoolConfiguration
                    {
                        poolName = "Players",
                        prefab = playerPrefab,
                        initialSize = 10,
                        maxSize = 50,
                        preloadOnStart = true,
                        allowExpansion = true
                    });
                }
            }
            
            // Add Projectile pool if Projectile prefab found
            if (projectilePrefabPaths.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(projectilePrefabPaths[0]);
                GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (projectilePrefab != null)
                {
                    configs.Add(new PoolConfiguration
                    {
                        poolName = "Projectiles",
                        prefab = projectilePrefab,
                        initialSize = 20,
                        maxSize = 100,
                        preloadOnStart = true,
                        allowExpansion = true
                    });
                }
            }
            
            // Add Effect pool if Effect prefab found
            if (effectPrefabPaths.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(effectPrefabPaths[0]);
                GameObject effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (effectPrefab != null)
                {
                    configs.Add(new PoolConfiguration
                    {
                        poolName = "Effects",
                        prefab = effectPrefab,
                        initialSize = 15,
                        maxSize = 75,
                        preloadOnStart = true,
                        allowExpansion = true
                    });
                }
            }
            
            if (configs.Count > 0)
            {
                SerializedProperty poolConfigsProp = serializedObject.FindProperty("poolConfigurations");
                poolConfigsProp.arraySize = configs.Count;
                
                for (int i = 0; i < configs.Count; i++)
                {
                    SerializedProperty configProp = poolConfigsProp.GetArrayElementAtIndex(i);
                    configProp.FindPropertyRelative("poolName").stringValue = configs[i].poolName;
                    configProp.FindPropertyRelative("prefab").objectReferenceValue = configs[i].prefab;
                    configProp.FindPropertyRelative("initialSize").intValue = configs[i].initialSize;
                    configProp.FindPropertyRelative("maxSize").intValue = configs[i].maxSize;
                    configProp.FindPropertyRelative("preloadOnStart").boolValue = configs[i].preloadOnStart;
                    configProp.FindPropertyRelative("allowExpansion").boolValue = configs[i].allowExpansion;
                }
                
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"[NetworkPoolObjectManagerEditor] Auto-setup complete! Configured {configs.Count} standard MOBA pools.");
            }
            else
            {
                EditorUtility.DisplayDialog("Auto-Setup", "No standard MOBA prefabs found. Please manually configure the pools or ensure you have Player, Projectile, or Effect prefabs in your project.", "OK");
            }
        }
    }
    
    /// <summary>
    /// Menu items for creating NetworkPoolObjectManager
    /// </summary>
    public static class NetworkPoolObjectManagerMenu
    {
        [MenuItem("MOBA/Network/Create Network Pool Object Manager")]
        public static void CreateNetworkPoolObjectManager()
        {
            // Check if one already exists
            NetworkPoolObjectManager existing = Object.FindAnyObjectByType<NetworkPoolObjectManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log($"[NetworkPoolObjectManager] Found existing manager on '{existing.gameObject.name}'. Selected it for you.");
                return;
            }
            
            // Create new GameObject with the component
            GameObject managerGO = new GameObject("NetworkPoolObjectManager");
            NetworkPoolObjectManager manager = managerGO.AddComponent<NetworkPoolObjectManager>();
            
            // Position it appropriately
            if (Selection.activeTransform != null)
            {
                managerGO.transform.SetParent(Selection.activeTransform);
            }
            
            // Select the new object
            Selection.activeGameObject = managerGO;
            
            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            
            Debug.Log("[NetworkPoolObjectManager] Created new NetworkPoolObjectManager. Configure the pool settings in the inspector.");
        }
        
        [MenuItem("MOBA/Network/Find Network Pool Object Manager")]
        public static void FindNetworkPoolObjectManager()
        {
            NetworkPoolObjectManager manager = Object.FindAnyObjectByType<NetworkPoolObjectManager>();
            if (manager != null)
            {
                Selection.activeGameObject = manager.gameObject;
                EditorGUIUtility.PingObject(manager.gameObject);
                Debug.Log($"[NetworkPoolObjectManager] Found manager on '{manager.gameObject.name}'");
            }
            else
            {
                Debug.LogWarning("[NetworkPoolObjectManager] No NetworkPoolObjectManager found in the current scene.");
            }
        }
    }
}
