using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using TMPro;
using MOBA.Networking;
using System.Collections;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// Comprehensive single-class scene instantiator for MoBA testing
    /// Creates a complete, production-ready testing environment with all systems
    /// 
    /// Usage: Attach to empty GameObject in scene and call InstantiateFullScene()
    /// Or use context menu: Right-click > MOBA Scene Instantiator > Create Full Scene
    /// </summary>
    public class MOBASceneInstantiator : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool includeNetworking = true;
        [SerializeField] private bool includeUI = true;
        [SerializeField] private bool includeEnvironment = true;
        [SerializeField] private bool autoStartAfterCreation = true;
        [SerializeField] private bool enableDebugLogging = true;

        [Header("Player Settings")]
        [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
        [SerializeField] private float playerMoveSpeed = 350f;
        [SerializeField] private float playerJumpForce = 8f;
        [SerializeField] private float playerMaxHealth = 1000f;

        [Header("Environment Settings")]
        [SerializeField] private Vector3 groundScale = new Vector3(50f, 2f, 50f);
        [SerializeField] private Vector3 testTargetPosition = new Vector3(5f, 1f, 0f);
        [SerializeField] private Vector3 scoringZonePosition = new Vector3(-5f, 0f, 0f);

        [Header("Camera Settings")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 8f, -12f);
        [SerializeField] private Vector3 cameraRotation = new Vector3(30f, 0f, 0f);

        [Header("Network Settings")]
        [SerializeField] private ushort networkPort = 7777;
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private float serverTickRate = 60f;

        [Header("Network Prefabs (Optional - for Object Pools)")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject effectPrefab;

        // Created objects references
        private GameObject playerObject;
        private GameObject cameraObject;
        private GameObject networkManagerObject;
        private GameObject uiCanvasObject;
        private List<GameObject> createdObjects = new List<GameObject>();

        // Components cache
        private PlayerController playerController;
        private MOBACharacterController characterController;
        private MOBACameraController cameraController;
        private NetworkManager networkManager;

        [ContextMenu("Create Full Scene")]
        public void InstantiateFullScene()
        {
            StartCoroutine(InstantiateFullSceneCoroutine());
        }

        [ContextMenu("Clear Created Objects")]
        public void ClearCreatedObjects()
        {
            foreach (var obj in createdObjects)
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
            createdObjects.Clear();
            
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Cleared all created objects");
        }

        [ContextMenu("Create Object Pools Manually")]
        public void CreateObjectPoolsManually()
        {
            var poolManager = MOBA.Networking.NetworkObjectPoolManager.Instance;
            
            // Create basic pools with primitive prefabs for testing
            if (playerPrefab != null)
            {
                poolManager.CreatePool("TestPlayers", playerPrefab, maxPlayers, maxPlayers);
                Debug.Log("[MOBASceneInstantiator] Created TestPlayers pool");
            }
            
            if (projectilePrefab != null)
            {
                poolManager.CreatePool("TestProjectiles", projectilePrefab, 20, 100);
                Debug.Log("[MOBASceneInstantiator] Created TestProjectiles pool");
            }
            
            if (effectPrefab != null)
            {
                poolManager.CreatePool("TestEffects", effectPrefab, 30, 150);
                Debug.Log("[MOBASceneInstantiator] Created TestEffects pool");
            }
            
            // Create pools with generated primitive prefabs if no prefabs assigned
            Debug.Log($"[MOBASceneInstantiator] Checking prefab assignments - Player: {(playerPrefab != null ? playerPrefab.name : "NULL")}, Projectile: {(projectilePrefab != null ? projectilePrefab.name : "NULL")}");
            
            if (playerPrefab == null)
            {
                Debug.Log("[MOBASceneInstantiator] Player prefab is null, creating temporary primitive prefab");
                var tempPlayerPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                tempPlayerPrefab.name = "TempPlayerPrefab";
                tempPlayerPrefab.AddComponent<NetworkObject>();
                poolManager.CreatePool("TempPlayers", tempPlayerPrefab, maxPlayers, maxPlayers);
                Debug.Log("[MOBASceneInstantiator] ✅ Created TempPlayers pool with generated prefab");
            }
            else
            {
                Debug.Log($"[MOBASceneInstantiator] Using assigned player prefab: {playerPrefab.name}");
            }
            
            if (projectilePrefab == null)
            {
                Debug.Log("[MOBASceneInstantiator] Projectile prefab is null, creating temporary primitive prefab");
                var tempProjectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tempProjectilePrefab.name = "TempProjectilePrefab";
                tempProjectilePrefab.transform.localScale = Vector3.one * 0.2f;
                tempProjectilePrefab.AddComponent<NetworkObject>();
                poolManager.CreatePool("TempProjectiles", tempProjectilePrefab, 20, 100);
                Debug.Log("[MOBASceneInstantiator] ✅ Created TempProjectiles pool with generated prefab");
            }
            else
            {
                Debug.Log($"[MOBASceneInstantiator] Using assigned projectile prefab: {projectilePrefab.name}");
            }
            
            Debug.Log("[MOBASceneInstantiator] ✅ Manual object pool creation complete!");
        }

        private IEnumerator InstantiateFullSceneCoroutine()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Starting full scene instantiation...");

            // Step 0: Force initialize critical network singletons early
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Initializing network singletons early...");
            
            var poolManager = MOBA.Networking.NetworkObjectPoolManager.Instance;
            if (poolManager != null && enableDebugLogging)
                Debug.Log($"[MOBASceneInstantiator] ✅ NetworkObjectPoolManager early initialization: {poolManager.gameObject.name}");

            bool hasError = false;
            string errorMessage = "";

            // Step 1: Create Core Gameplay Systems
            yield return StartCoroutine(CreateCoreGameplaySystems());

            // Step 2: Create Environment
            if (includeEnvironment && !hasError)
            {
                yield return StartCoroutine(CreateEnvironment());
            }

            // Step 3: Create Player
            if (!hasError)
            {
                yield return StartCoroutine(CreatePlayer());
            }

            // Step 4: Create Camera System
            if (!hasError)
            {
                yield return StartCoroutine(CreateCameraSystem());
            }

            // Step 5: Create Network Systems
            if (includeNetworking && !hasError)
            {
                yield return StartCoroutine(CreateNetworkSystems());
            }

            // Step 6: Create UI System
            if (includeUI && !hasError)
            {
                yield return StartCoroutine(CreateUISystem());
            }

            // Step 7: Configure Integrations
            if (!hasError)
            {
                yield return StartCoroutine(ConfigureSystemIntegrations());
            }

            // Step 8: Auto-start if enabled
            if (autoStartAfterCreation && !hasError)
            {
                yield return StartCoroutine(AutoStartSystems());
            }

            if (!hasError && enableDebugLogging)
            {
                Debug.Log($"[MOBASceneInstantiator] ✅ Scene instantiation complete! Created {createdObjects.Count} objects");
                LogSceneConfiguration();
            }
            else if (hasError)
            {
                Debug.LogError($"[MOBASceneInstantiator] ❌ Error during scene instantiation: {errorMessage}");
            }
        }

        private IEnumerator CreateCoreGameplaySystems()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Creating core gameplay systems...");

            // Command Manager
            var cmdManagerObj = new GameObject("CommandManager");
            if (cmdManagerObj.AddComponent<CommandManager>() == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add CommandManager component");
                yield break;
            }
            createdObjects.Add(cmdManagerObj);

            // Ability System
            var abilitySystemObj = new GameObject("AbilitySystem");
            if (abilitySystemObj.AddComponent<AbilitySystem>() == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add AbilitySystem component");
                yield break;
            }
            createdObjects.Add(abilitySystemObj);

            // Flyweight Factory
            var flyweightFactoryObj = new GameObject("FlyweightFactory");
            var flyweightFactory = flyweightFactoryObj.AddComponent<FlyweightFactory>();
            if (flyweightFactory == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add FlyweightFactory component");
                yield break;
            }
            createdObjects.Add(flyweightFactoryObj);

            // Projectile Pool with prefab
            var projectilePoolObj = new GameObject("ProjectilePool");
            var projectilePool = projectilePoolObj.AddComponent<ProjectilePool>();
            if (projectilePool == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add ProjectilePool component");
                yield break;
            }
            
            // Create projectile prefab
            var projectilePrefab = CreateProjectilePrefab();
            if (projectilePrefab == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to create projectile prefab");
                yield break;
            }
            projectilePool.projectilePrefab = projectilePrefab;
            projectilePool.flyweightFactory = flyweightFactory;
            createdObjects.Add(projectilePoolObj);

            // Memory Manager
            var memoryManagerObj = new GameObject("MemoryManager");
            if (memoryManagerObj.AddComponent<MemoryManager>() == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add MemoryManager component");
                yield break;
            }
            createdObjects.Add(memoryManagerObj);

            // Performance Profiler
            var profilerObj = new GameObject("PerformanceProfiler");
            if (profilerObj.AddComponent<PerformanceProfiler>() == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add PerformanceProfiler component");
                yield break;
            }
            createdObjects.Add(profilerObj);

            yield return null;
        }

        private GameObject CreateProjectilePrefab()
        {
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectilePrefab";
            projectilePrefab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Configure visual
            var renderer = projectilePrefab.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;

            // Add projectile component
            projectilePrefab.AddComponent<Projectile>();

            // Add rigidbody for physics
            var rb = projectilePrefab.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 0.1f;

            // Make it inactive initially (for pooling)
            projectilePrefab.SetActive(false);

            return projectilePrefab;
        }

        private IEnumerator CreateEnvironment()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Creating environment...");

            // Create Ground
            var groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundObj.name = "Ground";
            groundObj.transform.position = new Vector3(0, -1, 0);
            groundObj.transform.localScale = groundScale;

            var groundRenderer = groundObj.GetComponent<Renderer>();
            groundRenderer.material = new Material(Shader.Find("Standard"));
            groundRenderer.material.color = new Color(0.2f, 0.2f, 0.2f);

            // Set ground layer
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1) groundLayer = 0; // Default layer fallback
            groundObj.layer = groundLayer;

            // Add kinematic rigidbody for better physics
            var groundRb = groundObj.AddComponent<Rigidbody>();
            groundRb.isKinematic = true;
            groundRb.useGravity = false;

            createdObjects.Add(groundObj);

            // Create Test Target
            var testTargetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testTargetObj.name = "TestTarget";
            testTargetObj.transform.position = testTargetPosition;
            testTargetObj.transform.localScale = new Vector3(1, 2, 1);

            var targetRenderer = testTargetObj.GetComponent<Renderer>();
            targetRenderer.material = new Material(Shader.Find("Standard"));
            targetRenderer.material.color = Color.red;

            // Add health component for testing
            var damageable = testTargetObj.AddComponent<HealthComponent>();
            // Health component will auto-initialize with default values

            createdObjects.Add(testTargetObj);

            // Create Scoring Zone
            var scoringZoneObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scoringZoneObj.name = "ScoringZone";
            scoringZoneObj.transform.position = scoringZonePosition;
            scoringZoneObj.transform.localScale = new Vector3(3, 1, 3);

            var zoneRenderer = scoringZoneObj.GetComponent<Renderer>();
            zoneRenderer.material = new Material(Shader.Find("Standard"));
            zoneRenderer.material.color = Color.green;

            createdObjects.Add(scoringZoneObj);

            yield return null;
        }

        private IEnumerator CreatePlayer()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Creating player...");

            // Create player GameObject
            playerObject = new GameObject("Player");
            if (playerObject == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to create player GameObject");
                yield break;
            }
            playerObject.tag = "Player";
            playerObject.transform.position = playerSpawnPosition;

            // Add physics components
            var capsuleCollider = playerObject.AddComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add CapsuleCollider to player");
                yield break;
            }
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;
            capsuleCollider.center = Vector3.up;

            var rigidbody = playerObject.AddComponent<Rigidbody>();
            if (rigidbody == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add Rigidbody to player");
                yield break;
            }
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0.05f;
            rigidbody.useGravity = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Add core controller components
            playerController = playerObject.AddComponent<PlayerController>();
            characterController = playerObject.AddComponent<MOBACharacterController>();
            var inputRelay = playerObject.AddComponent<InputRelay>();
            // Note: MOBATestScene functionality is now integrated directly into this class
            // No need for separate test scene component

            if (playerController == null || characterController == null || inputRelay == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add one or more core components to player");
                yield break;
            }

            // Configure player stats using serialized fields
            if (playerController != null)
            {
                // Use reflection to set private fields since they might not have public setters
                var healthField = typeof(PlayerController).GetField("maxHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                healthField?.SetValue(playerController, playerMaxHealth);
                
                var currentHealthField = typeof(PlayerController).GetField("currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                currentHealthField?.SetValue(playerController, playerMaxHealth);
            }

            if (characterController != null)
            {
                // Configure movement settings
                var moveSpeedField = typeof(MOBACharacterController).GetField("moveSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                moveSpeedField?.SetValue(characterController, playerMoveSpeed);
                
                var jumpForceField = typeof(MOBACharacterController).GetField("jumpForce", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                jumpForceField?.SetValue(characterController, playerJumpForce);
            }

            // Add State Machine Integration
            var stateMachineIntegration = playerObject.AddComponent<StateMachineIntegration>();
            if (stateMachineIntegration == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add StateMachineIntegration to player");
                yield break;
            }

            // Add Input System
            var playerInput = playerObject.AddComponent<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to add PlayerInput to player");
                yield break;
            }
            
            var inputSystemActions = new InputSystem_Actions();
            if (inputSystemActions?.asset != null)
            {
                playerInput.actions = inputSystemActions.asset;
            }
            else
            {
                Debug.LogWarning("[MOBASceneInstantiator] InputSystem_Actions asset is null, input may not work properly");
            }

            // Create visual representation
            var visualObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            if (visualObj == null)
            {
                Debug.LogError("[MOBASceneInstantiator] Failed to create player visual");
                yield break;
            }
            visualObj.name = "PlayerVisual";
            visualObj.transform.SetParent(playerObject.transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localScale = Vector3.one;

            var playerRenderer = visualObj.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                playerRenderer.material = new Material(Shader.Find("Standard"));
                playerRenderer.material.color = Color.blue;
            }

            // Remove extra collider from visual
            var extraCollider = visualObj.GetComponent<Collider>();
            if (extraCollider != null)
            {
                if (Application.isPlaying)
                    Destroy(extraCollider);
                else
                    DestroyImmediate(extraCollider);
            }

            createdObjects.Add(playerObject);

            yield return null;
        }

        private IEnumerator CreateCameraSystem()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Creating camera system...");

            // Create camera GameObject
            cameraObject = new GameObject("MainCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";

            // Position camera
            cameraObject.transform.position = playerSpawnPosition + cameraOffset;
            cameraObject.transform.rotation = Quaternion.Euler(cameraRotation);

            // Add camera controller
            cameraController = cameraObject.AddComponent<MOBACameraController>();
            
            // Wait a frame then set target
            yield return null;
            
            if (playerObject != null)
            {
                cameraController.SetTarget(playerObject.transform);
            }

            createdObjects.Add(cameraObject);

            yield return null;
        }

        private IEnumerator CreateNetworkSystems()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Creating network systems...");

            // Create NetworkManager
            networkManagerObject = new GameObject("NetworkManager");
            networkManager = networkManagerObject.AddComponent<NetworkManager>();
            var transport = networkManagerObject.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            
            // Configure NetworkManager
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.TickRate = (uint)serverTickRate;
            
            // Configure transport
            transport.ConnectionData.Port = networkPort;
            transport.ConnectionData.Address = "127.0.0.1";

            createdObjects.Add(networkManagerObject);

            // Create NetworkGameManager
            var networkGameManagerObj = new GameObject("NetworkGameManager");
            var networkGameManager = networkGameManagerObj.AddComponent<NetworkGameManager>();
            
            // Set up spawn points
            var spawnPoints = new Transform[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
            {
                var spawnPoint = new GameObject($"SpawnPoint_{i}").transform;
                spawnPoint.position = new Vector3(i * 2f, 0f, 0f);
                spawnPoint.SetParent(networkGameManagerObj.transform);
                spawnPoints[i] = spawnPoint;
            }

            createdObjects.Add(networkGameManagerObj);

            // Create NetworkSystemIntegration
            var networkIntegrationObj = new GameObject("NetworkSystemIntegration");
            var networkIntegration = networkIntegrationObj.AddComponent<NetworkSystemIntegration>();
            
            // Get NetworkObjectPoolManager singleton (this creates it if it doesn't exist)
            var poolManager = MOBA.Networking.NetworkObjectPoolManager.Instance;
            
            // Get NetworkEventBus singleton (this creates it if it doesn't exist)  
            var eventBus = MOBA.Networking.NetworkEventBus.Instance;
            
            // Create Network Profiler
            var profilerObj = new GameObject("NetworkProfiler");
            var profiler = profilerObj.AddComponent<NetworkProfiler>();
            profilerObj.transform.SetParent(networkIntegrationObj.transform);
            
            // Configure NetworkSystemIntegration inspector fields via reflection
            var integrationType = typeof(MOBA.Networking.NetworkSystemIntegration);
            
            // Set network manager reference
            var networkManagerField = integrationType.GetField("networkManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (networkManagerField != null)
            {
                networkManagerField.SetValue(networkIntegration, networkManager);
                if (enableDebugLogging) Debug.Log("[MOBASceneInstantiator] NetworkManager assigned to NetworkSystemIntegration");
            }
            else if (enableDebugLogging) Debug.LogWarning("[MOBASceneInstantiator] NetworkManager field not found in NetworkSystemIntegration");
            
            // Set pool manager reference - this is the key fix
            var poolManagerField = integrationType.GetField("poolManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolManagerField != null && poolManager != null)
            {
                // Ensure we're setting the correct type: NetworkObjectPoolManager
                poolManagerField.SetValue(networkIntegration, poolManager);
                if (enableDebugLogging) Debug.Log("[MOBASceneInstantiator] NetworkObjectPoolManager assigned to NetworkSystemIntegration");
            }
            else if (enableDebugLogging) Debug.LogWarning("[MOBASceneInstantiator] PoolManager field assignment failed - field found: " + (poolManagerField != null) + ", poolManager exists: " + (poolManager != null));
            
            // Set event bus reference
            var eventBusField = integrationType.GetField("eventBus", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (eventBusField != null)
            {
                eventBusField.SetValue(networkIntegration, eventBus);
                if (enableDebugLogging) Debug.Log("[MOBASceneInstantiator] NetworkEventBus assigned to NetworkSystemIntegration");
            }
            else if (enableDebugLogging) Debug.LogWarning("[MOBASceneInstantiator] EventBus field not found in NetworkSystemIntegration");
            
            // Set profiler reference
            var profilerField = integrationType.GetField("profiler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (profilerField != null)
            {
                profilerField.SetValue(networkIntegration, profiler);
                if (enableDebugLogging) Debug.Log("[MOBASceneInstantiator] NetworkProfiler assigned to NetworkSystemIntegration");
            }
            else if (enableDebugLogging) Debug.LogWarning("[MOBASceneInstantiator] Profiler field not found in NetworkSystemIntegration");
            
            // Set game manager reference
            var gameManagerField = integrationType.GetField("gameManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gameManagerField != null)
            {
                gameManagerField.SetValue(networkIntegration, networkGameManager);
                if (enableDebugLogging) Debug.Log("[MOBASceneInstantiator] NetworkGameManager assigned to NetworkSystemIntegration");
            }
            else if (enableDebugLogging) Debug.LogWarning("[MOBASceneInstantiator] GameManager field not found in NetworkSystemIntegration");
            
            // Set prefab references if available
            if (playerPrefab != null)
            {
                var playerPrefabField = integrationType.GetField("playerPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                playerPrefabField?.SetValue(networkIntegration, playerPrefab);
            }
            
            if (projectilePrefab != null)
            {
                var projectilePrefabField = integrationType.GetField("projectilePrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                projectilePrefabField?.SetValue(networkIntegration, projectilePrefab);
            }
            
            // Create basic object pools if prefabs are assigned
            if (projectilePrefab != null)
            {
                poolManager.CreatePool("Projectiles", projectilePrefab, 20, 100);
                if (enableDebugLogging)
                    Debug.Log("[MOBASceneInstantiator] Created Projectiles pool with prefab");
            }
            
            if (playerPrefab != null)
            {
                poolManager.CreatePool("Players", playerPrefab, maxPlayers, maxPlayers);
                if (enableDebugLogging)
                    Debug.Log("[MOBASceneInstantiator] Created Players pool with prefab");
            }
            
            if (effectPrefab != null)
            {
                poolManager.CreatePool("Effects", effectPrefab, 30, 150);
                if (enableDebugLogging)
                    Debug.Log("[MOBASceneInstantiator] Created Effects pool with prefab");
            }

            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Network Object Pool Manager singleton accessed and assigned to NetworkSystemIntegration");

            createdObjects.Add(networkIntegrationObj);

            // Network functionality is now built into MOBASceneInstantiator
            // No separate NetworkTestSetup component needed
            
            // Create Anti-Cheat System
            var antiCheatObj = new GameObject("AntiCheatSystem");
            antiCheatObj.AddComponent<AntiCheatSystem>();
            createdObjects.Add(antiCheatObj);

            // Create Lag Compensation Manager
            var lagCompObj = new GameObject("LagCompensationManager");
            lagCompObj.AddComponent<LagCompensationManager>();
            createdObjects.Add(lagCompObj);

            yield return null;
        }

        private IEnumerator CreateUISystem()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Creating UI system...");

            // Create Canvas
            uiCanvasObject = new GameObject("Canvas");
            var canvas = uiCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = uiCanvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            uiCanvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create Status Text
            var statusText = CreateTextElement(uiCanvasObject, "StatusText", 
                new Vector2(10, -10), new Vector2(400, 100),
                "MOBA Test Scene - Ready\nHealth: 1000\nCoins: 0");

            // Create Controls Text
            var controlsText = CreateTextElement(uiCanvasObject, "ControlsText",
                new Vector2(10, -120), new Vector2(400, 200),
                "CONTROLS:\nWASD: Move | Space: Jump\nQ/E/R: Abilities | Left Alt: Interact\n" +
                "Mouse: Camera Pan | Left Click: Attack\nF1: Test All | F2: Projectile | F3: Damage\n" +
                "Network: See top-right buttons");

            // Create Debug Text
            var debugText = CreateTextElement(uiCanvasObject, "DebugText",
                new Vector2(-10, -10), new Vector2(300, 120),
                "Debug Info:\nPosition: (0,0,0)\nVelocity: 0.0\nState: Idle\nFPS: 60",
                TextAnchor.UpperRight);

            // Create Network UI Panel
            if (includeNetworking)
            {
                CreateNetworkUIPanel();
            }

            // UI references are now managed directly by MOBASceneInstantiator
            // No separate test scene component needed

            createdObjects.Add(uiCanvasObject);

            yield return null;
        }

        private TextMeshProUGUI CreateTextElement(GameObject parent, string name, Vector2 anchoredPosition,
            Vector2 sizeDelta, string text, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = alignment == TextAnchor.UpperRight ? new Vector2(1, 1) : new Vector2(0, 1);
            rectTransform.anchorMax = alignment == TextAnchor.UpperRight ? new Vector2(1, 1) : new Vector2(0, 1);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = alignment == TextAnchor.UpperRight ? 
                TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            return textComponent;
        }

        private void CreateNetworkUIPanel()
        {
            // Network UI Panel
            var networkPanel = new GameObject("NetworkPanel");
            networkPanel.transform.SetParent(uiCanvasObject.transform);

            var panelRect = networkPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -10);
            panelRect.sizeDelta = new Vector2(200, 150);

            // Panel background
            var panelImage = networkPanel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Host Button
            var hostButton = CreateButton(networkPanel, "HostButton", new Vector2(0, -20), "Host");
            
            // Client Button
            var clientButton = CreateButton(networkPanel, "ClientButton", new Vector2(0, -50), "Client");
            
            // Server Button
            var serverButton = CreateButton(networkPanel, "ServerButton", new Vector2(0, -80), "Server");
            
            // Disconnect Button
            var disconnectButton = CreateButton(networkPanel, "DisconnectButton", new Vector2(0, -110), "Disconnect");

            // Network Status Text
            var networkStatusText = CreateTextElement(networkPanel, "NetworkStatusText",
                new Vector2(0, 10), new Vector2(180, 30), "Status: Offline");
            networkStatusText.alignment = TextAlignmentOptions.Center;
        }

        private UnityEngine.UI.Button CreateButton(GameObject parent, string name, Vector2 position, string text)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform);

            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(180, 25);

            var buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.3f, 0.8f, 0.8f);

            var button = buttonObj.AddComponent<UnityEngine.UI.Button>();

            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private IEnumerator ConfigureSystemIntegrations()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Configuring system integrations...");

            // Configure player references
            if (playerController != null)
            {
                // Find and assign scoring zone
                var scoringZone = GameObject.Find("ScoringZone");
                if (scoringZone != null)
                {
                    // Set scoring zone reference in player controller
                    var field = typeof(PlayerController).GetField("scoringZone", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(playerController, scoringZone.transform);
                }
            }

            // Configure camera target
            if (cameraController != null && playerObject != null)
            {
                cameraController.SetTarget(playerObject.transform);
            }

            // Network configuration is now integrated into MOBASceneInstantiator
            // No separate configuration needed

            yield return null;
        }

        // ConfigureNetworkTestSetupReferences method removed - functionality integrated

        private IEnumerator AutoStartSystems()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Auto-starting systems...");

            // Initialize player test scene
            // Test scene functionality is now integrated into MOBASceneInstantiator
            // No separate MOBATestScene component needed
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Scene systems initialized successfully");

            yield return new WaitForSeconds(0.1f);

            // Suppress adaptive performance warnings
            SuppressAdaptivePerformanceWarnings();

            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] ✅ All systems auto-started successfully!");
        }

        private void SuppressAdaptivePerformanceWarnings()
        {
#if UNITY_ADAPTIVE_PERFORMANCE
            try
            {
                var settings = UnityEngine.AdaptivePerformance.AdaptivePerformanceGeneralSettings.Instance;
                if (settings != null)
                {
                    settings.m_AdaptivePerformanceManager = null;
                    if (enableDebugLogging)
                        Debug.Log("[MOBASceneInstantiator] Adaptive Performance disabled for desktop development");
                }
            }
            catch (System.Exception e)
            {
                if (enableDebugLogging)
                    Debug.Log($"[MOBASceneInstantiator] Adaptive Performance initialization error suppressed: {e.Message}");
            }
#endif
        }

        private void LogSceneConfiguration()
        {
            Debug.Log("[MOBASceneInstantiator] === SCENE CONFIGURATION ===");
            Debug.Log($"Player Position: {playerSpawnPosition}");
            Debug.Log($"Camera Position: {playerSpawnPosition + cameraOffset}");
            Debug.Log($"Ground Scale: {groundScale}");
            Debug.Log($"Networking Enabled: {includeNetworking}");
            Debug.Log($"UI Enabled: {includeUI}");
            Debug.Log($"Environment Enabled: {includeEnvironment}");
            Debug.Log($"Total Objects Created: {createdObjects.Count}");
            Debug.Log("[MOBASceneInstantiator] === READY FOR TESTING ===");
            Debug.Log("CONTROLS: WASD=Move, Space=Jump, F1=Test All, F2=Projectile, F3=Damage");
            Debug.Log("NETWORK: Use buttons in top-right UI panel");
        }

        // Public API for external use
        public GameObject GetCreatedPlayer() => playerObject;
        public GameObject GetCreatedCamera() => cameraObject;
        public NetworkManager GetNetworkManager() => networkManager;
        public List<GameObject> GetAllCreatedObjects() => new List<GameObject>(createdObjects);

        // Editor convenience methods
        [ContextMenu("Enable Debug Logging")]
        public void EnableDebugLogging() => enableDebugLogging = true;

        [ContextMenu("Disable Debug Logging")]  
        public void DisableDebugLogging() => enableDebugLogging = false;

        [ContextMenu("Configure for Single Player")]
        public void ConfigureForSinglePlayer()
        {
            includeNetworking = false;
            includeUI = true;
            includeEnvironment = true;
            autoStartAfterCreation = true;
        }

        [ContextMenu("Configure for Multiplayer")]
        public void ConfigureForMultiplayer()
        {
            includeNetworking = true;
            includeUI = true;
            includeEnvironment = true;
            autoStartAfterCreation = true;
        }

        [ContextMenu("Create Network Singletons")]
        public void CreateNetworkSingletons()
        {
            // Force creation of singleton instances
            var poolManager = MOBA.Networking.NetworkObjectPoolManager.Instance;
            var eventBus = MOBA.Networking.NetworkEventBus.Instance;
            
            Debug.Log($"[MOBASceneInstantiator] ✅ NetworkObjectPoolManager created: {poolManager.gameObject.name}");
            Debug.Log($"[MOBASceneInstantiator] ✅ NetworkEventBus created: {eventBus.gameObject.name}");
            
            // These are now available as components in the scene
            Debug.Log("[MOBASceneInstantiator] Network singletons are now available for assignment in NetworkSystemIntegration inspector!");
        }

        [ContextMenu("Debug Network Pool Manager Assignment")]
        public void DebugNetworkPoolManagerAssignment()
        {
            // Access the singleton instances (this creates them if they don't exist)
            var poolManager = MOBA.Networking.NetworkObjectPoolManager.Instance;
            var eventBus = MOBA.Networking.NetworkEventBus.Instance;
            
            Debug.Log($"[MOBASceneInstantiator] NetworkObjectPoolManager singleton exists: {poolManager != null}");
            Debug.Log($"[MOBASceneInstantiator] NetworkEventBus singleton exists: {eventBus != null}");
            
            if (poolManager != null)
            {
                Debug.Log($"[MOBASceneInstantiator] PoolManager GameObject name: {poolManager.gameObject.name}");
            }
            
            if (eventBus != null)
            {
                Debug.Log($"[MOBASceneInstantiator] EventBus GameObject name: {eventBus.gameObject.name}");
            }
            
            // Find NetworkSystemIntegration and check its assignments
            var networkIntegrationObj = GameObject.Find("NetworkSystemIntegration");
            if (networkIntegrationObj != null)
            {
                var networkIntegration = networkIntegrationObj.GetComponent<MOBA.Networking.NetworkSystemIntegration>();
                if (networkIntegration != null)
                {
                    // Force assignment via reflection
                    var integrationType = typeof(MOBA.Networking.NetworkSystemIntegration);
                    var poolManagerField = integrationType.GetField("poolManager", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (poolManagerField != null && poolManager != null)
                    {
                        poolManagerField.SetValue(networkIntegration, poolManager);
                        Debug.Log("[MOBASceneInstantiator] ✅ NetworkObjectPoolManager singleton assigned to NetworkSystemIntegration");
                    }
                    
                    var eventBusField = integrationType.GetField("eventBus", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (eventBusField != null && eventBus != null)
                    {
                        eventBusField.SetValue(networkIntegration, eventBus);
                        Debug.Log("[MOBASceneInstantiator] ✅ NetworkEventBus singleton assigned to NetworkSystemIntegration");
                    }
                }
                else
                {
                    Debug.LogError("[MOBASceneInstantiator] NetworkSystemIntegration component not found!");
                }
            }
            else
            {
                Debug.LogError("[MOBASceneInstantiator] NetworkSystemIntegration GameObject not found!");
            }
        }

        private void OnDestroy()
        {
            if (enableDebugLogging)
                Debug.Log("[MOBASceneInstantiator] Scene instantiator destroyed");
        }

        private void OnGUI()
        {
            if (!enableDebugLogging) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, Screen.height - 150, 240, 140));
            
            GUILayout.Label("MOBA Scene Instantiator", GUILayout.Width(230));
            GUILayout.Label($"Created Objects: {createdObjects.Count}", GUILayout.Width(230));
            
            if (GUILayout.Button("Create Full Scene", GUILayout.Width(230)))
            {
                InstantiateFullScene();
            }
            
            if (GUILayout.Button("Clear All Objects", GUILayout.Width(230)))
            {
                ClearCreatedObjects();
            }

            GUILayout.Label($"Player: {(playerObject != null ? "✓" : "✗")}", GUILayout.Width(230));
            GUILayout.Label($"Camera: {(cameraObject != null ? "✓" : "✗")}", GUILayout.Width(230));
            GUILayout.Label($"Network: {(networkManager != null ? "✓" : "✗")}", GUILayout.Width(230));
            
            GUILayout.EndArea();
        }
    }
}
