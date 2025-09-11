using UnityEngine;

namespace MOBA.Testing
{
    /// <summary>
    /// Quick verification that all testing components are available
    /// Use this to check if testing framework is properly set up
    /// </summary>
    public class TestingFrameworkVerifier : MonoBehaviour
    {
        [ContextMenu("Verify Testing Components")]
        public void VerifyTestingComponents()
        {
            Debug.Log("🔍 [TestingFrameworkVerifier] Checking available testing components...");
            
            // Check if MOBASystemTester exists
            var systemTester = FindFirstObjectByType<MOBASystemTester>();
            if (systemTester != null)
            {
                Debug.Log("✅ MOBASystemTester found and available");
            }
            else
            {
                Debug.LogWarning("❌ MOBASystemTester not found - add to MOBA_Testing GameObject");
            }
            
            // Check if Priority1FixesTester exists
            var priority1Tester = FindFirstObjectByType<Priority1FixesTester>();
            if (priority1Tester != null)
            {
                Debug.Log("✅ Priority1FixesTester found and available");
            }
            else
            {
                Debug.LogWarning("❌ Priority1FixesTester not found - add to MOBA_Testing GameObject");
            }
            
            // Check if QuickMOBASetup exists
            var quickSetup = FindFirstObjectByType<QuickMOBASetup>();
            if (quickSetup != null)
            {
                Debug.Log("✅ QuickMOBASetup found and available");
            }
            else
            {
                Debug.LogWarning("❌ QuickMOBASetup not found - add to empty GameObject for scene setup");
            }
            
            Debug.Log("🎯 [TestingFrameworkVerifier] Verification complete!");
        }
        
        [ContextMenu("Run All Available Tests")]
        public void RunAllAvailableTests()
        {
            Debug.Log("🚀 [TestingFrameworkVerifier] Running all available tests...");
            
            var systemTester = FindFirstObjectByType<MOBASystemTester>();
            if (systemTester != null)
            {
                systemTester.RunBasicValidation();
            }
            
            var priority1Tester = FindFirstObjectByType<Priority1FixesTester>();
            if (priority1Tester != null)
            {
                priority1Tester.RunPriority1Tests();
            }
            
            Debug.Log("✅ [TestingFrameworkVerifier] All available tests executed!");
        }
    }
}
