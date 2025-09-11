using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode;
using MOBA.Networking;

namespace MOBA.Testing
{
    /// <summary>
    /// Core MOBA Test Framework - Provides base testing infrastructure
    /// Supports unit tests, integration tests, and network tests
    /// Based on Clean Code testing principles and Unity Test Framework
    /// </summary>
    public class MOBATestFramework
    {
        protected GameObject testGameObject;
        protected Transform testTransform;
        protected Camera testCamera;
        
        // Network testing support
        protected NetworkManager networkManager;
        protected NetworkGameManager gameManager;
        
        // Component testing support
        protected PlayerController testPlayer;
        protected AbilitySystem testAbilitySystem;
        protected RSBCombatSystem testCombatSystem;
        protected CommandManager testCommandManager;
        
        [SetUp]
        public virtual void SetUp()
        {
            // Create test GameObject hierarchy
            CreateTestEnvironment();
            
            // Initialize core systems
            InitializeCoreComponents();
            
            // Log test start
            Debug.Log($"[MOBATestFramework] Test setup complete: {TestContext.CurrentContext.Test.Name}");
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            // Clean up network connections
            CleanupNetworkComponents();
            
            // Destroy test objects
            CleanupTestEnvironment();
            
            // Force garbage collection
            GC.Collect();
            
            Debug.Log($"[MOBATestFramework] Test cleanup complete: {TestContext.CurrentContext.Test.Name}");
        }
        
        protected virtual void CreateTestEnvironment()
        {
            // Create main test object
            testGameObject = new GameObject("TestEnvironment");
            testTransform = testGameObject.transform;
            
            // Create test camera
            var cameraObj = new GameObject("TestCamera");
            testCamera = cameraObj.AddComponent<Camera>();
            testCamera.transform.position = new Vector3(0, 10, -10);
            testCamera.transform.LookAt(Vector3.zero);
            
            // Create ground for physics tests
            CreateTestGround();
        }
        
        protected virtual void CreateTestGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TestGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.layer = LayerMask.NameToLayer("Ground");
        }
        
        protected virtual void InitializeCoreComponents()
        {
            // Add essential MOBA components
            testCommandManager = testGameObject.AddComponent<CommandManager>();
            testAbilitySystem = testGameObject.AddComponent<AbilitySystem>();
            testCombatSystem = testGameObject.AddComponent<RSBCombatSystem>();
            
            // Components will initialize automatically via Start()
        }
        
        protected virtual void CleanupNetworkComponents()
        {
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
            
            if (gameManager != null)
            {
                UnityEngine.Object.DestroyImmediate(gameManager.gameObject);
            }
        }
        
        protected virtual void CleanupTestEnvironment()
        {
            // Destroy all test objects
            if (testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testGameObject);
            }
            
            if (testCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(testCamera.gameObject);
            }
            
            // Clean up any remaining test objects
            CleanupTestObjects();
        }
        
        protected virtual void CleanupTestObjects()
        {
            var testObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in testObjects)
            {
                if (obj.name.StartsWith("Test") || obj.name.Contains("MOBA_Test"))
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }
        
        // Test helper methods
        protected PlayerController CreateTestPlayer(Vector3 position = default)
        {
            var playerObj = new GameObject("TestPlayer");
            playerObj.transform.position = position;
            
            // Add essential components
            var rigidbody = playerObj.AddComponent<Rigidbody>();
            var collider = playerObj.AddComponent<CapsuleCollider>();
            var player = playerObj.AddComponent<PlayerController>();
            
            // Configure for testing
            rigidbody.useGravity = false; // Prevent physics interference
            collider.height = 2f;
            collider.radius = 0.5f;
            
            return player;
        }
        
        protected IEnumerator WaitForCondition(Func<bool> condition, float timeout = 5f)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (elapsed >= timeout)
            {
                Assert.Fail($"Condition not met within {timeout} seconds");
            }
        }
        
        protected void AssertVector3Equal(Vector3 expected, Vector3 actual, float tolerance = 0.1f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance, $"X component mismatch. Expected: {expected.x}, Actual: {actual.x}");
            Assert.AreEqual(expected.y, actual.y, tolerance, $"Y component mismatch. Expected: {expected.y}, Actual: {actual.y}");
            Assert.AreEqual(expected.z, actual.z, tolerance, $"Z component mismatch. Expected: {expected.z}, Actual: {actual.z}");
        }
        
        protected void AssertHealthInRange(float health, float min, float max)
        {
            Assert.GreaterOrEqual(health, min, $"Health {health} is below minimum {min}");
            Assert.LessOrEqual(health, max, $"Health {health} is above maximum {max}");
        }
        
        // Network testing support
        protected IEnumerator SetupNetworkTest(bool asHost = true)
        {
            // Create NetworkManager
            var networkObj = new GameObject("TestNetworkManager");
            networkManager = networkObj.AddComponent<NetworkManager>();
            
            // Configure for testing
            networkManager.NetworkConfig.EnableSceneManagement = false;
            
            if (asHost)
            {
                networkManager.StartHost();
            }
            else
            {
                networkManager.StartClient();
            }
            
            // Wait for network to initialize
            yield return new WaitUntil(() => networkManager.IsListening);
            
            Debug.Log($"[MOBATestFramework] Network test setup complete. IsHost: {asHost}");
        }
        
        // Performance testing support
        protected float MeasureExecutionTime(System.Action action)
        {
            var startTime = Time.realtimeSinceStartup;
            action.Invoke();
            return Time.realtimeSinceStartup - startTime;
        }
        
        protected void AssertPerformance(System.Action action, float maxExecutionTime, string operationName)
        {
            float executionTime = MeasureExecutionTime(action);
            Assert.LessOrEqual(executionTime, maxExecutionTime, 
                $"{operationName} took {executionTime:F4}s, expected <= {maxExecutionTime:F4}s");
        }
    }
    
    /// <summary>
    /// Specialized test base for network integration tests
    /// </summary>
    public class MOBANetworkTestBase : MOBATestFramework
    {
        public override void SetUp()
        {
            base.SetUp();
            // Network-specific setup will be handled by individual tests
            // using SetupNetworkTest() coroutine
        }
        
        public override void TearDown()
        {
            // Ensure network cleanup happens first
            CleanupNetworkComponents();
            base.TearDown();
        }
    }
    
    /// <summary>
    /// Specialized test base for performance integration tests
    /// </summary>
    public class MOBAPerformanceTestBase : MOBATestFramework
    {
        protected Dictionary<string, float> performanceBaselines;
        
        public override void SetUp()
        {
            base.SetUp();
            InitializePerformanceBaselines();
        }
        
        protected virtual void InitializePerformanceBaselines()
        {
            performanceBaselines = new Dictionary<string, float>
            {
                ["PlayerSpawn"] = 0.01f,         // 10ms max
                ["AbilityCast"] = 0.005f,        // 5ms max  
                ["CombatCalculation"] = 0.002f,  // 2ms max
                ["StateTransition"] = 0.001f,   // 1ms max
                ["NetworkUpdate"] = 0.016f       // 16ms max (60fps)
            };
        }
        
        protected void AssertPerformanceBaseline(string operation, System.Action action)
        {
            if (performanceBaselines.TryGetValue(operation, out float baseline))
            {
                AssertPerformance(action, baseline, operation);
            }
            else
            {
                Debug.LogWarning($"[MOBAPerformanceTestBase] No baseline defined for operation: {operation}");
                MeasureExecutionTime(action); // Still measure for logging
            }
        }
    }
}