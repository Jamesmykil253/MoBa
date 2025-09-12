using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Ensures critical singleton components are initialized early
    /// Place this on a GameObject with a high execution order
    /// </summary>
    public class NetworkInitializer : MonoBehaviour
    {
        [Header("Initialization")]
        [SerializeField] private bool enableDebugLogging = true;
        
        private void Awake()
        {
            if (enableDebugLogging)
                Debug.Log("[NetworkInitializer] Starting early network component initialization...");
            
            InitializeNetworkSingletons();
        }

        private void InitializeNetworkSingletons()
        {
            // Based on Clean Code principles - proper error handling and defensive programming
            try
            {
                // Force creation of NetworkObjectPoolManager singleton with proper error handling
                var poolManager = NetworkObjectPoolManager.Instance;
                if (poolManager != null)
                {
                    if (enableDebugLogging)
                        Debug.Log($"[NetworkInitializer] ✅ NetworkObjectPoolManager singleton created: {poolManager.gameObject.name}");
                }
                else
                {
                    Debug.LogError("[NetworkInitializer] ❌ Failed to create NetworkObjectPoolManager singleton!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkInitializer] ❌ NetworkObjectPoolManager initialization failed: {e.Message}");
            }

            // Initialize other network singletons with proper error handling
            InitializeSingletonSafely<NetworkEventBus>("NetworkEventBus");
            // REMOVED: LagCompensationManager was removed during cleanup
            InitializeSingletonSafely<AntiCheatSystem>("AntiCheatSystem");

            if (enableDebugLogging)
                Debug.Log("[NetworkInitializer] ✅ Network singleton initialization complete");
        }

        /// <summary>
        /// Safe singleton initialization with proper exception handling
        /// Implements Clean Code defensive programming principles
        /// </summary>
        private void InitializeSingletonSafely<T>(string singletonName) where T : MonoBehaviour
        {
            try
            {
                // Use reflection to access the Instance property
                var instanceProperty = typeof(T).GetProperty("Instance", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null) as T;
                    if (enableDebugLogging && instance != null)
                        Debug.Log($"[NetworkInitializer] ✅ {singletonName} singleton ready");
                }
                else
                {
                    Debug.LogWarning($"[NetworkInitializer] ⚠️ {singletonName} does not have Instance property");
                }
            }
            catch (System.Exception e)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[NetworkInitializer] ⚠️ {singletonName} not available: {e.Message}");
            }
        }

        /// <summary>
        /// Manually initialize network singletons (can be called from other scripts)
        /// </summary>
        public static void ForceInitializeNetworkSingletons()
        {
            Debug.Log("[NetworkInitializer] Force initializing network singletons...");
            
            // This will create the singleton if it doesn't exist
            var poolManager = NetworkObjectPoolManager.Instance;
            Debug.Log($"[NetworkInitializer] NetworkObjectPoolManager: {(poolManager != null ? "READY" : "FAILED")}");
        }
    }
}
