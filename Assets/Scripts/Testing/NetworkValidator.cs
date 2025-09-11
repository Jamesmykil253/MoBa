using UnityEngine;
using System.Linq;

namespace MOBA.Testing
{
    /// <summary>
    /// Network configuration validator
    /// Checks for common network setup issues and provides solutions
    /// </summary>
    public class NetworkValidator : MonoBehaviour
    {
        [Header("Network Validation")]
        [SerializeField] private bool validateOnStart = false;
        [SerializeField] private bool enableDetailedLogging = true;
        
        private void Start()
        {
            if (validateOnStart)
            {
                ValidateNetworkSetup();
            }
            else
            {
                Log("NetworkValidator ready - call ValidateNetworkSetup() manually");
            }
        }
        
        [ContextMenu("Validate Network Setup")]
        public void ValidateNetworkSetup()
        {
            Log("🔍 === Network Setup Validation Started ===");
            
            CheckNetworkManager();
            CheckNetworkPrefabs();
            CheckAdaptivePerformance();
            CheckCameraSystem();
            
            Log("✅ === Network Validation Complete ===");
        }
        
        private void CheckNetworkManager()
        {
            Log("Checking NetworkManager...");
            
            var networkManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.Contains("NetworkManager"))
                .ToArray();
                
            if (networkManagers.Length > 0)
            {
                Log($"✅ Found {networkManagers.Length} NetworkManager(s)");
            }
            else
            {
                Log("❌ No NetworkManager found - add NetworkManager to scene");
            }
            
            if (networkManagers.Length > 1)
            {
                Log("⚠️ Multiple NetworkManagers detected - should only have one");
            }
        }
        
        private void CheckNetworkPrefabs()
        {
            Log("Checking Network Prefabs...");
            
            // Check for NetworkObject components in scene
            var networkObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.Contains("NetworkObject"))
                .ToArray();
                
            Log($"📦 Found {networkObjects.Length} NetworkObject(s) in scene");
            
            // Look for potential duplicate issues
            var prefabPaths = new System.Collections.Generic.List<string>();
            // This would need to be expanded with actual prefab checking logic
            
            Log("✅ Network prefabs check complete");
        }
        
        private void CheckAdaptivePerformance()
        {
            Log("Checking Adaptive Performance...");
            
            // Check if Adaptive Performance is causing warnings
            var adaptivePerformanceObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.Contains("AdaptivePerformance"))
                .ToArray();
                
            if (adaptivePerformanceObjects.Length == 0)
            {
                Log("💡 Adaptive Performance not configured (this is normal for MOBA games)");
                Log("💡 To remove warnings: Project Settings → XR → Adaptive Performance → Uncheck 'Initialize on Startup'");
            }
            else
            {
                Log($"✅ Adaptive Performance configured with {adaptivePerformanceObjects.Length} component(s)");
            }
        }
        
        private void CheckCameraSystem()
        {
            Log("Checking Camera System...");
            
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                Log($"✅ Found {cameras.Length} camera(s)");
                
                // Check for MOBA camera controller
                var mobaCameras = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Where(mb => mb.GetType().Name.Contains("MOBACameraController"))
                    .ToArray();
                    
                if (mobaCameras.Length > 0)
                {
                    Log("✅ MOBA Camera Controller found");
                }
                else
                {
                    Log("💡 Consider adding MOBACameraController for MOBA-specific camera behavior");
                }
            }
            else
            {
                Log("❌ No cameras found in scene");
            }
        }
        
        [ContextMenu("Generate Network Setup Report")]
        public void GenerateNetworkReport()
        {
            Log("📊 === Generating Network Setup Report ===");
            
            var report = new System.Text.StringBuilder();
            report.AppendLine("MOBA Network Configuration Report");
            report.AppendLine("Generated: " + System.DateTime.Now);
            report.AppendLine();
            
            // Scene objects count
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            report.AppendLine($"Total GameObjects: {allObjects.Length}");
            
            // Network components
            var networkComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.ToLower().Contains("network"))
                .ToArray();
            report.AppendLine($"Network Components: {networkComponents.Length}");
            
            // Cameras
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            report.AppendLine($"Cameras: {cameras.Length}");
            
            report.AppendLine();
            report.AppendLine("Recommendations:");
            report.AppendLine("- Ensure only one NetworkManager exists");
            report.AppendLine("- Check DefaultNetworkPrefabs.asset for duplicates");
            report.AppendLine("- Consider disabling Adaptive Performance if not needed");
            report.AppendLine("- Verify all network prefabs have NetworkObject components");
            
            Log(report.ToString());
        }
        
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[NetworkValidator] {message}");
            }
        }
    }
}
