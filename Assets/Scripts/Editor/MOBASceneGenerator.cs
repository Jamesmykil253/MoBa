using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using TMPro;
using UnityEngine.InputSystem;
using MOBA.Networking;
using MOBA;

namespace MOBA.Editor
{
    /// <summary>
    /// Comprehensive scene setup generator for MOBA project
    /// Creates complete scene hierarchy with all necessary components and systems
    /// </summary>
    public class MOBASceneGenerator : EditorWindow
    {
        private bool createNetworkSystems = true;
        private bool createGameplaySystems = true;
        private bool createUISystem = true;
        private bool createEnvironment = true;
        private bool createTestPlayer = true;
        private bool autoConfigureReferences = true;

        private string sceneSetupName = "MOBA_SceneSetup";

        [MenuItem("MOBA/Generate Complete Scene")]
        public static void ShowWindow()
        {
            GetWindow<MOBASceneGenerator>("MOBA Scene Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("MOBA Complete Scene Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Generate Systems:", EditorStyles.boldLabel);
            createNetworkSystems = EditorGUILayout.Toggle("Network Systems", createNetworkSystems);
            createGameplaySystems = EditorGUILayout.Toggle("Gameplay Systems", createGameplaySystems);
            createUISystem = EditorGUILayout.Toggle("UI System", createUISystem);
            createEnvironment = EditorGUILayout.Toggle("Environment", createEnvironment);
            createTestPlayer = EditorGUILayout.Toggle("Test Player", createTestPlayer);
            autoConfigureReferences = EditorGUILayout.Toggle("Auto-Configure References", autoConfigureReferences);

            GUILayout.Space(10);
            sceneSetupName = EditorGUILayout.TextField("Scene Setup Name:", sceneSetupName);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate Complete Scene", GUILayout.Height(40)))
            {
                GenerateCompleteScene();
            }

            GUILayout.Space(10);

            // Individual generation buttons
            GUILayout.Label("Individual Systems:", EditorStyles.boldLabel);

            if (GUILayout.Button("Network Systems Only"))
                CreateNetworkSystems();

            if (GUILayout.Button("Gameplay Systems Only"))
                CreateGameplaySystems();

            if (GUILayout.Button("Environment Only"))
                CreateEnvironment();

            if (GUILayout.Button("Complete Test Setup"))
                CreateCompleteTestSetup();
        }

        private void GenerateCompleteScene()
        {
            Debug.Log("[MOBASceneGenerator] Generating complete MOBA scene...");

            try
            {
                // Create root scene setup object
                GameObject sceneRoot = CreateSceneRoot();

                if (createNetworkSystems)
                    CreateNetworkSystems();

                if (createGameplaySystems)
                    CreateGameplaySystems();

                if (createUISystem)
                    CreateUISystem();

                if (createEnvironment)
                    CreateEnvironment();

                if (createTestPlayer)
                    CreateTestPlayer();

                if (autoConfigureReferences)
                    AutoConfigureReferences();

                Debug.Log("[MOBASceneGenerator] Complete scene generated successfully!");
                EditorUtility.DisplayDialog("Success", "Complete MOBA scene generated successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBASceneGenerator] Error generating scene: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate scene: {e.Message}", "OK");
            }
        }

        #region Scene Root

        private GameObject CreateSceneRoot()
        {
            GameObject sceneRoot = new GameObject(sceneSetupName);
            // Use modern MOBASceneInstantiator instead of old setup classes
            // sceneRoot.AddComponent<MOBASceneInstantiator>(); // Removed - use MOBASceneInstantiator
            // sceneRoot.AddComponent<// MOBATestScene (removed)>(); // Removed - use MOBASceneInstantiator

            Debug.Log("[MOBASceneGenerator] Created scene root with setup components");
            return sceneRoot;
        }

        #endregion

        #region Network Systems

        private void CreateNetworkSystems()
        {
            Debug.Log("[MOBASceneGenerator] Creating network systems...");

            // Create NetworkManager
            CreateNetworkManager();

            // Create NetworkSystemIntegration
            CreateNetworkSystemIntegration();

            // REMOVED - // NetworkTestSetup (integrated into MOBASceneInstantiator) functionality integrated into MOBASceneInstantiator
            // Create// NetworkTestSetup (integrated into MOBASceneInstantiator)();

            Debug.Log("[MOBASceneGenerator] Network systems created");
        }

        private GameObject CreateNetworkManager()
        {
            GameObject networkManager = new GameObject("NetworkManager");

            // Add NetworkManager component
            var netManager = networkManager.AddComponent<NetworkManager>();

            // Add Unity Transport
            var transport = networkManager.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            netManager.NetworkConfig.NetworkTransport = transport;

            // Configure NetworkManager
            netManager.NetworkConfig.TickRate = 60;
            netManager.NetworkConfig.EnableSceneManagement = false;

            // Add NetworkGameManager
            networkManager.AddComponent<NetworkGameManager>();

            Debug.Log("[MOBASceneGenerator] NetworkManager created and configured");
            return networkManager;
        }

        private GameObject CreateNetworkSystemIntegration()
        {
            GameObject integration = new GameObject("NetworkSystemIntegration");
            var systemIntegration = integration.AddComponent<NetworkSystemIntegration>();

            Debug.Log("[MOBASceneGenerator] NetworkSystemIntegration created");
            return integration;
        }

        // REMOVED - // NetworkTestSetup (integrated into MOBASceneInstantiator) functionality integrated into MOBASceneInstantiator
        /*
        private GameObject Create// NetworkTestSetup (integrated into MOBASceneInstantiator)()
        {
            GameObject testSetup = new GameObject("// NetworkTestSetup (integrated into MOBASceneInstantiator)");
            testSetup.AddComponent<// NetworkTestSetup (integrated into MOBASceneInstantiator)>();

            Debug.Log("[MOBASceneGenerator] // NetworkTestSetup (integrated into MOBASceneInstantiator) created");
            return testSetup;
        }
        */

        #endregion

        #region Gameplay Systems

        private void CreateGameplaySystems()
        {
            Debug.Log("[MOBASceneGenerator] Creating gameplay systems...");

            // Core systems
            CreateCommandManager();
            CreateAbilitySystem();
            CreateFlyweightFactory();
            CreateProjectilePool();

            // Additional systems
            CreateEventBus();
            CreateInputSystem();

            Debug.Log("[MOBASceneGenerator] Gameplay systems created");
        }

        private GameObject CreateCommandManager()
        {
            GameObject cmdManager = new GameObject("CommandManager");
            cmdManager.AddComponent<CommandManager>();

            Debug.Log("[MOBASceneGenerator] CommandManager created");
            return cmdManager;
        }

        private GameObject CreateAbilitySystem()
        {
            GameObject abilitySystem = new GameObject("AbilitySystem");
            abilitySystem.AddComponent<AbilitySystem>();

            Debug.Log("[MOBASceneGenerator] AbilitySystem created");
            return abilitySystem;
        }

        private GameObject CreateFlyweightFactory()
        {
            GameObject factory = new GameObject("FlyweightFactory");
            factory.AddComponent<FlyweightFactory>();

            Debug.Log("[MOBASceneGenerator] FlyweightFactory created");
            return factory;
        }

        private GameObject CreateProjectilePool()
        {
            GameObject poolObj = new GameObject("ProjectilePool");
            var projectilePool = poolObj.AddComponent<ProjectilePool>();

            // Create a simple projectile prefab if none exists
            CreateDefaultProjectilePrefab(projectilePool);

            Debug.Log("[MOBASceneGenerator] ProjectilePool created");
            return poolObj;
        }

        private void CreateDefaultProjectilePrefab(ProjectilePool pool)
        {
            // Check if projectile prefab already exists
            string prefabPath = "Assets/Prefabs/Gameplay/Projectile.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                pool.projectilePrefab = existingPrefab;
                Debug.Log("[MOBASceneGenerator] Using existing Projectile prefab");
                return;
            }

            // Create default projectile prefab
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "DefaultProjectile";
            projectile.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Configure collider as trigger
            var collider = projectile.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            // Add Rigidbody
            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;

            // Add Projectile component
            projectile.AddComponent<Projectile>();

            // Set material
            var renderer = projectile.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;

            // Assign to pool
            pool.projectilePrefab = projectile;

            Debug.Log("[MOBASceneGenerator] Created default projectile prefab");
        }

        private GameObject CreateEventBus()
        {
            // EventBus is a static class, so we create a placeholder GameObject for organization
            GameObject eventBus = new GameObject("EventBus (Static)");

            Debug.Log("[MOBASceneGenerator] EventBus placeholder created (EventBus is static)");
            return eventBus;
        }

        private GameObject CreateInputSystem()
        {
            GameObject inputSystem = new GameObject("InputSystem");
            
            // Add PlayerInput component
            var playerInput = inputSystem.AddComponent<PlayerInput>();
            
            // Try to assign InputSystem_Actions asset
            var inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                Debug.Log("[MOBASceneGenerator] InputSystem configured with InputSystem_Actions");
            }
            else
            {
                Debug.LogWarning("[MOBASceneGenerator] InputSystem_Actions asset not found");
            }

            return inputSystem;
        }

        #endregion

        #region UI System

        private void CreateUISystem()
        {
            Debug.Log("[MOBASceneGenerator] Creating UI system...");

            // Create EventSystem for UI
            CreateEventSystem();

            // Create main UI Canvas
            CreateMainUICanvas();

            // Create Network Test UI
            CreateNetworkTestUI();

            Debug.Log("[MOBASceneGenerator] UI system created");
        }

        private GameObject CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("[MOBASceneGenerator] EventSystem created");
            return eventSystem;
        }

        private GameObject CreateMainUICanvas()
        {
            GameObject canvas = new GameObject("MainUICanvas");

            // Add Canvas component
            var canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 1;

            // Add CanvasScaler
            var scaler = canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create HUD elements
            CreateGameHUD(canvas);

            Debug.Log("[MOBASceneGenerator] Main UI Canvas created");
            return canvas;
        }

        private void CreateGameHUD(GameObject canvas)
        {
            // Status display
            CreateStatusDisplay(canvas);

            // Controls display
            CreateControlsDisplay(canvas);

            // Debug display
            CreateDebugDisplay(canvas);
        }

        private void CreateStatusDisplay(GameObject parent)
        {
            GameObject statusDisplay = new GameObject("StatusDisplay");
            statusDisplay.transform.SetParent(parent.transform);

            var rectTransform = statusDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.02f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.3f, 0.98f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = statusDisplay.AddComponent<TextMeshProUGUI>();
            text.text = "MOBA Test Scene\nHealth: 100\nState: Idle\nCoins: 0";
            text.color = Color.white;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.TopLeft;

            Debug.Log("[MOBASceneGenerator] Status display created");
        }

        private void CreateControlsDisplay(GameObject parent)
        {
            GameObject controlsDisplay = new GameObject("ControlsDisplay");
            controlsDisplay.transform.SetParent(parent.transform);

            var rectTransform = controlsDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.02f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.3f, 0.78f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = controlsDisplay.AddComponent<TextMeshProUGUI>();
            text.text = "CONTROLS:\nWASD: Move\nSpace: Jump\nQ/E/R: Abilities\nF1: Test All\nF2: Projectile\nF3: Damage";
            text.color = Color.yellow;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.TopLeft;

            Debug.Log("[MOBASceneGenerator] Controls display created");
        }

        private void CreateDebugDisplay(GameObject parent)
        {
            GameObject debugDisplay = new GameObject("DebugDisplay");
            debugDisplay.transform.SetParent(parent.transform);

            var rectTransform = debugDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
            rectTransform.anchorMax = new Vector2(0.3f, 0.48f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = debugDisplay.AddComponent<TextMeshProUGUI>();
            text.text = "DEBUG:\nPosition: (0,0,0)\nVelocity: 0\nCommands: 0\nPools: 0";
            text.color = Color.cyan;
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.TopLeft;

            Debug.Log("[MOBASceneGenerator] Debug display created");
        }

        private GameObject CreateNetworkTestUI()
        {
            GameObject networkUI = new GameObject("NetworkTestUI");

            var rectTransform = networkUI.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // REMOVED - // NetworkTestSetup (integrated into MOBASceneInstantiator) functionality integrated into MOBASceneInstantiator
            // networkUI.AddComponent<// NetworkTestSetup (integrated into MOBASceneInstantiator)>();

            // Create network control buttons
            CreateNetworkButtons(networkUI);

            Debug.Log("[MOBASceneGenerator] Network Test UI created");
            return networkUI;
        }

        private void CreateNetworkButtons(GameObject parent)
        {
            // Host button
            CreateUIButton(parent, "HostButton", new Vector2(0.7f, 0.85f), new Vector2(0.85f, 0.9f), "Host");

            // Client button
            CreateUIButton(parent, "ClientButton", new Vector2(0.7f, 0.8f), new Vector2(0.85f, 0.85f), "Client");

            // Server button
            CreateUIButton(parent, "ServerButton", new Vector2(0.7f, 0.75f), new Vector2(0.85f, 0.8f), "Server");

            // Disconnect button
            CreateUIButton(parent, "DisconnectButton", new Vector2(0.7f, 0.7f), new Vector2(0.85f, 0.75f), "Disconnect");

            // Network status text
            GameObject statusText = new GameObject("NetworkStatusText");
            statusText.transform.SetParent(parent.transform);

            var statusRect = statusText.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.7f, 0.65f);
            statusRect.anchorMax = new Vector2(0.98f, 0.7f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            var statusTMP = statusText.AddComponent<TextMeshProUGUI>();
            statusTMP.text = "Status: Disconnected";
            statusTMP.color = Color.white;
            statusTMP.fontSize = 14;
            statusTMP.alignment = TextAlignmentOptions.Center;

            Debug.Log("[MOBASceneGenerator] Network buttons created");
        }

        private void CreateUIButton(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, string text)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent.transform);

            var rectTransform = button.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var buttonComponent = button.AddComponent<UnityEngine.UI.Button>();
            var buttonImage = button.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = Color.white;

            // Button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(button.transform);

            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textTMP = buttonText.AddComponent<TextMeshProUGUI>();
            textTMP.text = text;
            textTMP.color = Color.black;
            textTMP.fontSize = 12;
            textTMP.alignment = TextAlignmentOptions.Center;
        }

        #endregion

        #region Environment

        private void CreateEnvironment()
        {
            Debug.Log("[MOBASceneGenerator] Creating environment...");

            // Create ground
            CreateGround();

            // Create test targets
            CreateTestTarget();

            // Create scoring zone
            CreateScoringZone();

            // Create spawn points
            CreateSpawnPoints();

            Debug.Log("[MOBASceneGenerator] Environment created");
        }

        private GameObject CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -1f, 0f);
            ground.transform.localScale = new Vector3(50f, 2f, 50f);

            // Configure collider
            var collider = ground.GetComponent<BoxCollider>();
            collider.isTrigger = false;

            // Add kinematic Rigidbody
            var rigidbody = ground.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            // Set material
            var renderer = ground.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0.2f, 0.6f, 0.2f);

            Debug.Log("[MOBASceneGenerator] Ground created");
            return ground;
        }

        private GameObject CreateTestTarget()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "TestTarget";
            target.transform.position = new Vector3(5f, 1f, 0f);
            target.transform.localScale = new Vector3(1f, 2f, 1f);

            // Add health component - use the HealthComponent class from MOBA namespace
            var healthComponent = target.AddComponent<HealthComponent>();

            // Set material
            var renderer = target.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.red;

            Debug.Log("[MOBASceneGenerator] Test target created");
            return target;
        }

        private GameObject CreateScoringZone()
        {
            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zone.name = "ScoringZone";
            zone.transform.position = new Vector3(-5f, 0f, 0f);
            zone.transform.localScale = new Vector3(3f, 1f, 3f);

            // Configure as trigger
            var collider = zone.GetComponent<CapsuleCollider>();
            collider.isTrigger = true;

            // Set material
            var renderer = zone.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.green;

            Debug.Log("[MOBASceneGenerator] Scoring zone created");
            return zone;
        }

        private GameObject CreateSpawnPoints()
        {
            GameObject spawnPointsParent = new GameObject("SpawnPoints");

            // Create multiple spawn points in a circle
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 8f, 0f, Mathf.Sin(angle) * 8f);

                GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                spawnPoint.transform.SetParent(spawnPointsParent.transform);
                spawnPoint.transform.position = position;

                // Visual indicator
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                indicator.name = "Indicator";
                indicator.transform.SetParent(spawnPoint.transform);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

                var renderer = indicator.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.blue;

                // Remove collider from indicator
                DestroyImmediate(indicator.GetComponent<CapsuleCollider>());
            }

            Debug.Log("[MOBASceneGenerator] Spawn points created");
            return spawnPointsParent;
        }

        #endregion

        #region Test Player

        private void CreateTestPlayer()
        {
            Debug.Log("[MOBASceneGenerator] Creating test player...");

            GameObject player = new GameObject("TestPlayer");
            player.transform.position = new Vector3(0f, 1f, 0f);

            // Core components
            var capsuleCollider = player.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;
            capsuleCollider.center = Vector3.up;

            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0.05f;
            rigidbody.useGravity = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // MOBA gameplay components
            player.AddComponent<PlayerController>();
            player.AddComponent<MOBACharacterController>();
            player.AddComponent<InputRelay>();
            player.AddComponent<StateMachineIntegration>();
            // REMOVED - // MOBATestScene (removed) functionality integrated into MOBASceneInstantiator
            // player.AddComponent<// MOBATestScene (removed)>();

            // Visual representation
            CreatePlayerVisual(player);

            // Create camera
            CreatePlayerCamera(player);

            Debug.Log("[MOBASceneGenerator] Test player created");
        }

        private void CreatePlayerVisual(GameObject player)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = Vector3.up;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Remove collider from visual (parent has the functional collider)
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // Set material
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.blue;

            Debug.Log("[MOBASceneGenerator] Player visual created");
        }

        private void CreatePlayerCamera(GameObject player)
        {
            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = new Vector3(0f, 8f, -8f);
            cameraObj.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Add Camera component
            var camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            // Add AudioListener
            cameraObj.AddComponent<AudioListener>();

            // Add MOBA camera controller
            var cameraController = cameraObj.AddComponent<MOBACameraController>();

            Debug.Log("[MOBASceneGenerator] Player camera created");
        }

        #endregion

        #region Complete Test Setup

        private void CreateCompleteTestSetup()
        {
            Debug.Log("[MOBASceneGenerator] Creating complete test setup...");

            // Clear existing objects (optional)
            if (EditorUtility.DisplayDialog("Clear Scene", "Clear existing objects before creating test setup?", "Yes", "No"))
            {
                ClearScene();
            }

            // Generate everything
            createNetworkSystems = true;
            createGameplaySystems = true;
            createUISystem = true;
            createEnvironment = true;
            createTestPlayer = true;
            autoConfigureReferences = true;

            GenerateCompleteScene();

            Debug.Log("[MOBASceneGenerator] Complete test setup created");
        }

        private void ClearScene()
        {
            // Find all root objects and destroy them (except essentials)
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                if (obj.name != "Main Camera" && obj.name != "Directional Light")
                {
                    DestroyImmediate(obj);
                }
            }

            Debug.Log("[MOBASceneGenerator] Scene cleared");
        }

        #endregion

        #region Auto Configuration

        private void AutoConfigureReferences()
        {
            Debug.Log("[MOBASceneGenerator] Auto-configuring component references...");

            // Configure ProjectilePool references
            var projectilePool = FindFirstObjectByType<ProjectilePool>();
            var flyweightFactory = FindFirstObjectByType<FlyweightFactory>();
            if (projectilePool != null && flyweightFactory != null)
            {
                projectilePool.flyweightFactory = flyweightFactory;
                Debug.Log("[MOBASceneGenerator] ProjectilePool -> FlyweightFactory reference configured");
            }

            // REMOVED - // NetworkTestSetup (integrated into MOBASceneInstantiator) and // MOBATestScene (removed) functionality integrated into MOBASceneInstantiator
            /*
            // Configure // NetworkTestSetup (integrated into MOBASceneInstantiator) UI references
            var networkTestSetup = FindFirstObjectByType<// NetworkTestSetup (integrated into MOBASceneInstantiator)>();
            if (networkTestSetup != null)
            {
                Configure// NetworkTestSetup (integrated into MOBASceneInstantiator)References(networkTestSetup);
            }

            // Configure // MOBATestScene (removed) UI references
            var testScene = FindFirstObjectByType<// MOBATestScene (removed)>();
            if (testScene != null)
            {
                Configure// MOBATestScene (removed)References(testScene);
            }
            */

            // Configure Camera references
            var cameraController = FindFirstObjectByType<MOBACameraController>();
            var player = GameObject.FindWithTag("Player");
            if (cameraController != null && player == null)
            {
                // Find player by component instead
                var playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    cameraController.SetTarget(playerController.transform);
                    Debug.Log("[MOBASceneGenerator] Camera target configured");
                }
            }

            Debug.Log("[MOBASceneGenerator] Auto-configuration completed");
        }

        // REMOVED - // NetworkTestSetup (integrated into MOBASceneInstantiator) class deleted, functionality integrated into MOBASceneInstantiator
        /*
        private void Configure// NetworkTestSetup (integrated into MOBASceneInstantiator)References(// NetworkTestSetup (integrated into MOBASceneInstantiator) networkTestSetup)
        {
            // Find UI elements by name and assign them
            var hostButton = GameObject.Find("HostButton")?.GetComponent<UnityEngine.UI.Button>();
            var clientButton = GameObject.Find("ClientButton")?.GetComponent<UnityEngine.UI.Button>();
            var serverButton = GameObject.Find("ServerButton")?.GetComponent<UnityEngine.UI.Button>();
            var disconnectButton = GameObject.Find("DisconnectButton")?.GetComponent<UnityEngine.UI.Button>();
            var statusText = GameObject.Find("NetworkStatusText")?.GetComponent<TextMeshProUGUI>();

            // Use reflection to set private fields (for demo purposes)
            var networkTestSetupType = typeof(// NetworkTestSetup (integrated into MOBASceneInstantiator));
            
            if (hostButton != null)
            {
                var hostButtonField = networkTestSetupType.GetField("hostButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                hostButtonField?.SetValue(networkTestSetup, hostButton);
            }

            if (clientButton != null)
            {
                var clientButtonField = networkTestSetupType.GetField("clientButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                clientButtonField?.SetValue(networkTestSetup, clientButton);
            }

            if (serverButton != null)
            {
                var serverButtonField = networkTestSetupType.GetField("serverButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                serverButtonField?.SetValue(networkTestSetup, serverButton);
            }

            if (disconnectButton != null)
            {
                var disconnectButtonField = networkTestSetupType.GetField("disconnectButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                disconnectButtonField?.SetValue(networkTestSetup, disconnectButton);
            }

            if (statusText != null)
            {
                var statusTextField = networkTestSetupType.GetField("statusText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                statusTextField?.SetValue(networkTestSetup, statusText);
            }

            Debug.Log("[MOBASceneGenerator] // NetworkTestSetup (integrated into MOBASceneInstantiator) UI references configured");
        }
        */

        // REMOVED - // MOBATestScene (removed) class deleted, functionality integrated into MOBASceneInstantiator
        /*
        private void Configure// MOBATestScene (removed)References(// MOBATestScene (removed) testScene)
        {
            // Find UI elements and assign them to // MOBATestScene (removed)
            var statusText = GameObject.Find("StatusDisplay")?.GetComponent<TextMeshProUGUI>();
            var controlsText = GameObject.Find("ControlsDisplay")?.GetComponent<TextMeshProUGUI>();
            var debugText = GameObject.Find("DebugDisplay")?.GetComponent<TextMeshProUGUI>();

            if (statusText != null) testScene.statusText = statusText;
            if (controlsText != null) testScene.controlsText = controlsText;
            if (debugText != null) testScene.debugText = debugText;

            Debug.Log("[MOBASceneGenerator] // MOBATestScene (removed) UI references configured");
        }
        */

        #endregion
    }
}
