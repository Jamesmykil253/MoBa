using UnityEngine;

namespace MOBA.Testing
{
    /// <summary>
    /// Basic MOBA system validation tester
    /// Tests scene setup and component configuration
    /// </summary>
    public class MOBASystemTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enableDetailedLogging = true;
        
        // Test results tracking
        private int testsRun = 0;
        private int testsPassed = 0;
        private int testsFailed = 0;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunBasicValidation();
            }
            else
            {
                Log("MOBASystemTester ready - call RunBasicValidation() manually");
            }
        }
        
        /// <summary>
        /// Run basic MOBA system validation
        /// </summary>
        [ContextMenu("Run Basic Validation")]
        public void RunBasicValidation()
        {
            Log("=== MOBA System Validation Started ===");
            testsRun = 0;
            testsPassed = 0;
            testsFailed = 0;
            
            TestGameObjectSetup();
            TestSceneConfiguration();
            TestBasicComponents();
            
            LogResults();
        }
        
        private void TestGameObjectSetup()
        {
            testsRun++;
            Log("Testing GameObject setup...");
            
            if (gameObject != null && gameObject.activeInHierarchy)
            {
                testsPassed++;
                Log("✅ GameObject setup - PASSED");
            }
            else
            {
                testsFailed++;
                Log("❌ GameObject setup - FAILED");
            }
        }
        
        private void TestSceneConfiguration()
        {
            testsRun++;
            Log("Testing scene configuration...");
            
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                testsPassed++;
                Log($"✅ Scene has {cameras.Length} camera(s) - PASSED");
            }
            else
            {
                testsFailed++;
                Log("❌ No cameras found in scene - FAILED");
            }
        }
        
        private void TestBasicComponents()
        {
            testsRun++;
            Log("Testing basic components...");
            
            // Test for common Unity components
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            
            if (renderers.Length > 0 || colliders.Length > 0)
            {
                testsPassed++;
                Log($"✅ Scene has {renderers.Length} renderer(s) and {colliders.Length} collider(s) - PASSED");
            }
            else
            {
                testsFailed++;
                Log("❌ No basic components found - FAILED");
            }
        }
        
        private void LogResults()
        {
            Log("=== Test Results Summary ===");
            Log($"Tests Run: {testsRun}");
            Log($"Tests Passed: {testsPassed}");
            Log($"Tests Failed: {testsFailed}");
            Log($"Success Rate: {(testsPassed * 100f / testsRun):F1}%");
            
            if (testsFailed == 0)
            {
                Log("🎉 All tests PASSED - MOBA systems validation successful!");
            }
            else
            {
                Log($"⚠️ {testsFailed} test(s) FAILED - Review configuration");
            }
        }
        
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[MOBASystemTester] {message}");
            }
        }
    }
}
