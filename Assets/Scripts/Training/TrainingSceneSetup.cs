using UnityEngine;
using MOBA.Training;
using MOBA.UI;

namespace MOBA.Training
{
    /// <summary>
    /// Auto-setup script for training scenes
    /// Automatically configures the scene for local training when added
    /// </summary>
    public class TrainingSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup Configuration")]
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private bool createUI = true;
        [SerializeField] private bool createSpawnPoints = true;
        [SerializeField] private bool autoStartTraining = false; // Changed to manual start only
        
        [Header("Scene Configuration")]
        [SerializeField] private string sceneName = "TrainingScene";
        [SerializeField] private int numberOfSpawnPoints = 4;
        [SerializeField] private float spawnPointSpacing = 5f;
        [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
        
        [Header("Prefab References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject lobbyUIPrefab;
        
        // Created objects
        private LocalTrainingLobby trainingLobby;
        private TrainingLobbyUI trainingUI;
        private Transform[] spawnPoints;
        private Camera mainCamera;
        
        private void Awake()
        {
            if (setupOnAwake)
            {
                SetupTrainingScene();
            }
        }
        
        /// <summary>
        /// Setup the complete training scene
        /// </summary>
        [ContextMenu("Setup Training Scene")]
        public void SetupTrainingScene()
        {
            Debug.Log($"[TrainingSceneSetup] 🎯 Setting up training scene: {sceneName}");
            
            // Step 1: Setup camera
            SetupCamera();
            
            // Step 2: Create spawn points
            if (createSpawnPoints)
            {
                CreateSpawnPoints();
            }
            
            // Step 3: Create training lobby system
            CreateTrainingLobby();
            
            // Step 4: Create UI if needed
            if (createUI)
            {
                CreateTrainingUI();
            }
            
            // Step 5: Final configuration
            FinalizeSetup();
            
            Debug.Log("[TrainingSceneSetup] ✅ Training scene setup complete!");
        }
        
        private void SetupCamera()
        {
            // Find or create main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                cameraObj.tag = "MainCamera";
                
                // Position camera for good overview
                mainCamera.transform.position = new Vector3(0, 10, -10);
                mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
                
                Debug.Log("[TrainingSceneSetup] Created main camera");
            }
            
            // Ensure camera has appropriate settings for training
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.backgroundColor = Color.black;
        }
        
        private void CreateSpawnPoints()
        {
            Debug.Log("[TrainingSceneSetup] Creating spawn points...");
            
            GameObject spawnParent = new GameObject("TrainingSpawnPoints");
            spawnPoints = new Transform[numberOfSpawnPoints];
            
            for (int i = 0; i < numberOfSpawnPoints; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i + 1}");
                spawnPoint.transform.SetParent(spawnParent.transform);
                
                // Arrange spawn points in a circle
                float angle = (360f / numberOfSpawnPoints) * i;
                float radians = angle * Mathf.Deg2Rad;
                
                Vector3 position = spawnAreaCenter + new Vector3(
                    Mathf.Cos(radians) * spawnPointSpacing,
                    0,
                    Mathf.Sin(radians) * spawnPointSpacing
                );
                
                spawnPoint.transform.position = position;
                spawnPoints[i] = spawnPoint.transform;
                
                // Add visual indicator
                CreateSpawnPointVisual(spawnPoint);
            }
            
            Debug.Log($"[TrainingSceneSetup] Created {numberOfSpawnPoints} spawn points");
        }
        
        private void CreateSpawnPointVisual(GameObject spawnPoint)
        {
            // Create a simple visual indicator for spawn points
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "SpawnIndicator";
            visual.transform.SetParent(spawnPoint.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            
            // Make it semi-transparent green
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0, 1, 0, 0.5f);
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                renderer.material = mat;
            }
            
            // Remove collider
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
        }
        
        private void CreateTrainingLobby()
        {
            Debug.Log("[TrainingSceneSetup] Creating training lobby system...");
            
            // Create LocalTrainingLobby
            GameObject lobbyObj = new GameObject("LocalTrainingLobby");
            trainingLobby = lobbyObj.AddComponent<LocalTrainingLobby>();
            
            // Configure training lobby
            if (playerPrefab != null)
            {
                // Set player prefab via reflection since it might be private
                var prefabField = typeof(LocalTrainingLobby).GetField("trainingPlayerPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prefabField != null)
                {
                    prefabField.SetValue(trainingLobby, playerPrefab);
                }
            }
            
            // Set spawn points
            if (spawnPoints != null)
            {
                var spawnField = typeof(LocalTrainingLobby).GetField("trainingSpawnPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnField != null)
                {
                    spawnField.SetValue(trainingLobby, spawnPoints);
                }
            }
            
            // Configure auto-start
            var autoStartField = typeof(LocalTrainingLobby).GetField("autoStartOnAwake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (autoStartField != null)
            {
                autoStartField.SetValue(trainingLobby, autoStartTraining);
            }
            
            Debug.Log("[TrainingSceneSetup] Training lobby configured");
        }
        
        private void CreateTrainingUI()
        {
            Debug.Log("[TrainingSceneSetup] Creating training UI...");
            
            // Create canvas if it doesn't exist
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TrainingCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                Debug.Log("[TrainingSceneSetup] Created training canvas");
            }
            
            // Create TrainingLobbyUI
            GameObject uiObj = new GameObject("TrainingLobbyUI");
            uiObj.transform.SetParent(canvas.transform, false);
            trainingUI = uiObj.AddComponent<TrainingLobbyUI>();
            
            // Create EventSystem if it doesn't exist
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                
                Debug.Log("[TrainingSceneSetup] Created EventSystem");
            }
            
            Debug.Log("[TrainingSceneSetup] Training UI created");
        }
        
        private void FinalizeSetup()
        {
            // Create a simple ground plane for reference
            CreateGroundPlane();
            
            // Add some basic lighting
            SetupLighting();
            
            // Configure scene settings
            ConfigureSceneSettings();
            
            Debug.Log("[TrainingSceneSetup] Scene finalization complete");
        }
        
        private void CreateGroundPlane()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TrainingGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 10; // 100x100 unit plane
            
            // Give it a simple material
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray
                renderer.material = mat;
            }
            
            Debug.Log("[TrainingSceneSetup] Created ground plane");
        }
        
        private void SetupLighting()
        {
            // Ensure there's a directional light
            Light mainLight = FindFirstObjectByType<Light>();
            if (mainLight == null || mainLight.type != LightType.Directional)
            {
                GameObject lightObj = new GameObject("Directional Light");
                mainLight = lightObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
                mainLight.transform.rotation = Quaternion.Euler(50, -30, 0);
                mainLight.intensity = 1f;
                mainLight.color = Color.white;
                
                Debug.Log("[TrainingSceneSetup] Created directional light");
            }
        }
        
        private void ConfigureSceneSettings()
        {
            // Set reasonable frame rate for training
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            
            // Configure physics for responsive training
            Time.fixedDeltaTime = 1f / 60f; // 60Hz physics
            
            Debug.Log("[TrainingSceneSetup] Scene settings configured");
        }
        
        /// <summary>
        /// Get the created training lobby component
        /// </summary>
        public LocalTrainingLobby GetTrainingLobby()
        {
            return trainingLobby;
        }
        
        /// <summary>
        /// Get the created training UI component
        /// </summary>
        public TrainingLobbyUI GetTrainingUI()
        {
            return trainingUI;
        }
        
        /// <summary>
        /// Get the created spawn points
        /// </summary>
        public Transform[] GetSpawnPoints()
        {
            return spawnPoints;
        }
        
        // Context menu helpers
        [ContextMenu("Create Player Prefab Reference")]
        public void FindPlayerPrefab()
        {
            // Try to find a player prefab in the project
            var prefabs = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var prefab in prefabs)
            {
                if (prefab.name.ToLower().Contains("player"))
                {
                    playerPrefab = prefab;
                    Debug.Log($"[TrainingSceneSetup] Found player prefab: {prefab.name}");
                    break;
                }
            }
            
            if (playerPrefab == null)
            {
                Debug.LogWarning("[TrainingSceneSetup] No player prefab found. Please assign manually.");
            }
        }
        
        [ContextMenu("Clear Training Setup")]
        public void ClearTrainingSetup()
        {
            // Clean up created objects
            var trainingObjs = FindObjectsByType<LocalTrainingLobby>(FindObjectsSortMode.None);
            foreach (var obj in trainingObjs)
            {
                if (obj != null) DestroyImmediate(obj.gameObject);
            }
            
            var spawnParent = GameObject.Find("TrainingSpawnPoints");
            if (spawnParent != null) DestroyImmediate(spawnParent);
            
            var ground = GameObject.Find("TrainingGround");
            if (ground != null) DestroyImmediate(ground);
            
            Debug.Log("[TrainingSceneSetup] Training setup cleared");
        }
    }
}
