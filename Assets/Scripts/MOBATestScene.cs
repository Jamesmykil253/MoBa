using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using MOBA.Networking;
#if UNITY_ADAPTIVE_PERFORMANCE
using UnityEngine.AdaptivePerformance;
#endif

namespace MOBA
{
    /// <summary>
    /// Comprehensive test scene for the MOBA game system
    /// Demonstrates all functionality with on-screen controls
    /// </summary>
    public class MOBATestScene : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MOBACharacterController characterController;
        [SerializeField] private InputRelay inputRelay;
        [SerializeField] private MOBACameraController cameraController;
        [SerializeField] private CommandManager commandManager;
        [SerializeField] private AbilitySystem abilitySystem;
        [SerializeField] private FlyweightFactory flyweightFactory;
        [SerializeField] private ProjectilePool projectilePool;

        [Header("Test Objects")]
        [SerializeField] private Transform testTarget;
        [SerializeField] private Transform scoringZone;
        [SerializeField] private GameObject enemyPrefab;

        [Header("UI")]
        [SerializeField] public TMP_Text statusText;
        [SerializeField] public TMP_Text controlsText;
        [SerializeField] public TMP_Text debugText;

        private PlayerInput playerInput;
        private InputActionAsset inputActions;
        private GameObject testEnemy;

        // Removed automatic Awake() and Start() methods to prevent automatic loading
        // Use InitializeTestScene() and SetupTestEnvironment() methods manually instead

        /// <summary>
        /// Suppress Adaptive Performance warnings if the system is not properly configured
        /// </summary>
        private void SuppressAdaptivePerformanceWarnings()
        {
#if UNITY_ADAPTIVE_PERFORMANCE
            // Disable Adaptive Performance for desktop development to avoid warnings
            try
            {
                var settings = AdaptivePerformanceGeneralSettings.Instance;
                if (settings != null)
                {
                    // Disable automatic initialization to prevent warnings
                    settings.m_AdaptivePerformanceManager = null;
                    Debug.Log("Adaptive Performance disabled for desktop development");
                }
            }
            catch (System.Exception e)
            {
                // Silently handle any Adaptive Performance errors
                Debug.Log($"Adaptive Performance initialization error suppressed: {e.Message}");
            }
#endif
        }

        private void InitializeTestScene()
        {
            // Create Input Actions from the auto-generated class
            var inputSystemActions = new InputSystem_Actions();
            inputActions = inputSystemActions.asset;
            if (inputActions == null)
            {
                Debug.LogError("InputSystem_Actions asset not found!");
                return;
            }

            // Get or create PlayerInput
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = gameObject.AddComponent<PlayerInput>();
            }
            playerInput.actions = inputActions;

            // Ensure player has required components
            EnsurePlayerComponents();

            // Find or create systems
            FindOrCreateSystems();

            // Subscribe to input actions
            SubscribeToInputs();

            // Force initial state check after setup
            StartCoroutine(DelayedStateCheck());
        }

        private void EnsurePlayerComponents()
        {
            // Check if components already exist (created by MOBASceneSetup)
            var existingCollider = GetComponent<CapsuleCollider>();
            var existingRigidbody = GetComponent<Rigidbody>();

            // Only add collider if missing
            if (existingCollider == null)
            {
                var collider = gameObject.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.5f;
                collider.center = Vector3.up;
                Debug.Log("Added CapsuleCollider to player");
            }
            else
            {
                Debug.Log("Player already has CapsuleCollider");
            }

            // Only add rigidbody if missing
            if (existingRigidbody == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Only freeze rotation, allow position changes
                rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
                Debug.Log("Added Rigidbody to player with gravity enabled");
            }
            else
            {
                // Ensure existing Rigidbody has correct settings
                var rb = existingRigidbody;
                rb.useGravity = true;
                rb.constraints &= ~RigidbodyConstraints.FreezePositionY; // Make sure Y position is not frozen
                rb.mass = 1f;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                Debug.Log("Updated existing Rigidbody with correct gravity settings");
            }

            // Get or add core components
            playerController = GetComponent<PlayerController>() ?? gameObject.AddComponent<PlayerController>();
            characterController = GetComponent<MOBACharacterController>() ?? gameObject.AddComponent<MOBACharacterController>();
            inputRelay = GetComponent<InputRelay>() ?? gameObject.AddComponent<InputRelay>();

            Debug.Log($"Player components configured: Controller={playerController != null}, Character={characterController != null}, InputRelay={inputRelay != null}");
        }

        private void FindOrCreateSystems()
        {
            // Find existing systems
            commandManager = Object.FindAnyObjectByType<CommandManager>();
            abilitySystem = Object.FindAnyObjectByType<AbilitySystem>();
            flyweightFactory = Object.FindAnyObjectByType<FlyweightFactory>();
            projectilePool = Object.FindAnyObjectByType<ProjectilePool>();
            cameraController = Object.FindAnyObjectByType<MOBACameraController>();

            // Create missing systems
            if (commandManager == null)
            {
                var cmdObj = new GameObject("CommandManager");
                commandManager = cmdObj.AddComponent<CommandManager>();
            }

            if (abilitySystem == null)
            {
                var abilityObj = new GameObject("AbilitySystem");
                abilitySystem = abilityObj.AddComponent<AbilitySystem>();
            }

            if (flyweightFactory == null)
            {
                var factoryObj = new GameObject("FlyweightFactory");
                flyweightFactory = factoryObj.AddComponent<FlyweightFactory>();
            }

            if (projectilePool == null)
            {
                var poolObj = new GameObject("ProjectilePool");
                projectilePool = poolObj.AddComponent<ProjectilePool>();
                projectilePool.flyweightFactory = flyweightFactory;
            }

            if (cameraController == null)
            {
                var cameraObj = new GameObject("CameraController");
                cameraObj.AddComponent<Camera>();
                cameraController = cameraObj.AddComponent<MOBACameraController>();
                // Don't set target here - let camera find it automatically
                Debug.Log("[MOBATestScene] CameraController created, will find target automatically");
            }
        }

        private void SetupTestEnvironment()
        {
            // Create ground
            CreateGround();

            // Create test target
            if (testTarget == null)
            {
                var targetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                targetObj.name = "TestTarget";
                targetObj.transform.position = new Vector3(5, 1, 0);
                targetObj.transform.localScale = new Vector3(1, 2, 1);
                testTarget = targetObj.transform;

                // Add damageable component
                var damageable = targetObj.AddComponent<TestDamageable>();
                damageable.maxHealth = 1000f;
                damageable.currentHealth = 1000f;
            }

            // Create scoring zone
            if (scoringZone == null)
            {
                var zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                zoneObj.name = "ScoringZone";
                zoneObj.transform.position = new Vector3(-5, 0, 0);
                zoneObj.transform.localScale = new Vector3(3, 1, 3);
                scoringZone = zoneObj.transform;

                var renderer = zoneObj.GetComponent<Renderer>();
                renderer.material.color = Color.green;
            }

            // Create test enemy
            if (enemyPrefab != null)
            {
                testEnemy = Instantiate(enemyPrefab, new Vector3(0, 1, 5), Quaternion.identity);
                testEnemy.name = "TestEnemy";
            }
        }

        private void CreateGround()
        {
            var groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundObj.name = "Ground";
            groundObj.transform.position = new Vector3(0, -1, 0);
            groundObj.transform.localScale = new Vector3(50, 2, 50);

            var renderer = groundObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.2f, 0.2f);
            }

            var collider = groundObj.GetComponent<BoxCollider>();
            if (collider != null)
            {
                // Safely set layer - use default layer if "Ground" layer doesn't exist
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer == -1)
                {
                    groundLayer = 0; // Default layer
                    Debug.LogWarning("Ground layer not found, using Default layer. Consider creating a 'Ground' layer in Unity for better physics detection.");
                }
                groundObj.layer = groundLayer;

                // Ensure collider is not trigger
                collider.isTrigger = false;
            }

            // Add a kinematic Rigidbody to the ground for better physics interaction
            var groundRb = groundObj.AddComponent<Rigidbody>();
            groundRb.isKinematic = true;
            groundRb.useGravity = false;

            Debug.Log($"Ground created at {groundObj.transform.position} with layer {groundObj.layer}");
        }

        private void SubscribeToInputs()
        {
            if (playerInput == null) return;

            Debug.Log("[MOBATestScene] Subscribing to input actions");

            // Test controls - using available actions from InputSystem_Actions
            try
            {
                // Use existing actions for testing
                playerInput.actions["Player/Attack"].performed += OnTestDamage;
                playerInput.actions["Player/Jump"].performed += OnTestHeal;
                playerInput.actions["Player/Ability1"].performed += OnTestProjectile;
                playerInput.actions["Player/Ability2"].performed += OnTestState;
                playerInput.actions["Player/Ability1Select"].performed += OnTestFlyweight;

                Debug.Log("[MOBATestScene] Input subscriptions successful");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBATestScene] Failed to subscribe to inputs: {e.Message}");
            }
        }

        private void Update()
        {
            UpdateUI();
            HandleTestInputs();
        }

        private void HandleTestInputs()
        {
            if (Keyboard.current == null) return;

            // Additional test controls
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                TestAllSystems();
            }

            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                SpawnTestProjectile();
            }

            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                TestDamageSystem();
            }
        }

        private void UpdateUI()
        {
            if (statusText != null)
            {
                string status = $"MOBA Test Scene - Health: {playerController?.Health ?? 0}\n";
                status += $"State: {GetCurrentStateName()}\n";
                status += $"Coins: {playerController?.CryptoCoins ?? 0}\n";
                status += $"Flyweights: {flyweightFactory?.GetAvailableFlyweightNames().Count ?? 0}";
                statusText.text = status;
            }

            if (controlsText != null)
            {
                string controls = "CONTROLS:\n";
                controls += "WASD: Move | Space: Jump\n";
                controls += "Q/E/R: Abilities | Left Alt: Interact\n";
                controls += "Mouse: Camera Pan | C: Coins | D: Damage\n";
                controls += "F1: Test All | F2: Projectile | F3: Damage\n";
                controls += "H: Help";
                controlsText.text = controls;
            }

            if (debugText != null)
            {
                string debug = $"Position: {transform.position:F1}\n";
                debug += $"Velocity: {GetComponent<Rigidbody>()?.linearVelocity.magnitude ?? 0:F1}\n";
                debug += $"Command Queue: {commandManager?.UndoCount ?? 0}\n";
                debug += $"Projectile Pool: {projectilePool?.GetAvailableFlyweightNames().Count ?? 0} types";
                debugText.text = debug;
            }
        }

        private string GetCurrentStateName()
        {
            if (characterController == null) return "No Controller";

            var stateMachine = characterController.GetComponent<StateMachineIntegration>();
            if (stateMachine != null)
            {
                var sm = stateMachine.GetStateMachine();
                if (sm != null && sm.CurrentState != null)
                {
                    return sm.CurrentState.GetStateName();
                }
            }

            return "No State Machine";
        }

        // Test Methods
        private void TestAllSystems()
        {
            Debug.Log("=== TESTING ALL SYSTEMS ===");

            // Test movement
            characterController?.SetMovementInput(Vector3.forward);

            // Test damage
            playerController?.TakeDamage(50);

            // Test projectile
            SpawnTestProjectile();

            // Test state transition
            var stateMachine = characterController?.GetComponent<StateMachineIntegration>();
            stateMachine?.ForceStateChange<AttackingState>();

            Debug.Log("All systems test completed!");
        }

        private void SpawnTestProjectile()
        {
            if (projectilePool == null || testTarget == null) return;

            Vector3 direction = (testTarget.position - transform.position).normalized;
            projectilePool.SpawnProjectileWithFlyweight(transform.position, direction, "BasicProjectile");
            Debug.Log("Test projectile spawned!");
        }

        private void TestDamageSystem()
        {
            if (testTarget == null) return;

            var damageable = testTarget.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(100);
                Debug.Log("Test damage applied!");
            }
        }

        // Input Action Handlers
        private void OnTestDamage(InputAction.CallbackContext context)
        {
            playerController?.TakeDamage(25);
        }

        private void OnTestHeal(InputAction.CallbackContext context)
        {
            // Implement healing test
            Debug.Log("Heal test triggered");
        }

        private void OnTestProjectile(InputAction.CallbackContext context)
        {
            SpawnTestProjectile();
        }

        private void OnTestState(InputAction.CallbackContext context)
        {
            var stateMachine = characterController?.GetComponent<StateMachineIntegration>();
            stateMachine?.ForceStateChange<JumpingState>();
        }

        private void OnTestFlyweight(InputAction.CallbackContext context)
        {
            if (flyweightFactory != null)
            {
                var flyweight = flyweightFactory.CreateModifiedFlyweight("BasicProjectile",
                    f => f.damage *= 2f);
                Debug.Log("Modified flyweight created!");
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 400, 20), "MOBA Test Scene - Press F1 for Full Test");
            GUI.Label(new Rect(10, 30, 300, 20), $"Health: {playerController?.Health ?? 0}");
            GUI.Label(new Rect(10, 50, 300, 20), $"State: {GetCurrentStateName()}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Coins: {playerController?.CryptoCoins ?? 0}");

            GUI.Label(new Rect(10, 100, 400, 20), "Test Controls:");
            GUI.Label(new Rect(10, 120, 300, 20), "F1: Test All Systems");
            GUI.Label(new Rect(10, 140, 300, 20), "F2: Spawn Projectile");
            GUI.Label(new Rect(10, 160, 300, 20), "F3: Test Damage");
        }

        /// <summary>
        /// Delayed state check to ensure proper initial state after setup
        /// </summary>
        private System.Collections.IEnumerator DelayedStateCheck()
        {
            // Wait for physics to settle
            yield return new WaitForSeconds(0.1f);

            // Force state machine to check current physics state
            var stateMachine = characterController?.GetComponent<StateMachineIntegration>();
            if (stateMachine != null)
            {
                var sm = stateMachine.GetStateMachine();
                if (sm != null)
                {
                    // Trigger physics update to check if player should be falling
                    sm.Update();
                    Debug.Log("[MOBATestScene] Performed delayed state check");
                }
            }
        }
    }

    /// <summary>
    /// Simple damageable test object
    /// </summary>
    public class TestDamageable : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        public float currentHealth = 100f;

        new private Renderer renderer;

        private void Awake()
        {
            renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
                var filter = gameObject.AddComponent<MeshFilter>();
                filter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            }
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

            // Visual feedback
            if (renderer != null)
            {
                renderer.material.color = Color.red;
                Invoke(nameof(ResetColor), 0.2f);
            }

            if (currentHealth <= 0)
            {
                Debug.Log($"{gameObject.name} destroyed!");
                Destroy(gameObject);
            }
        }

        private void ResetColor()
        {
            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }
        }
    }
}