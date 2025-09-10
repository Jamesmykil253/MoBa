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
            // Force creation of NetworkObjectPoolManager singleton
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

            // Force creation of other network singletons if they exist
            try
            {
                var eventBus = NetworkEventBus.Instance;
                if (enableDebugLogging && eventBus != null)
                    Debug.Log("[NetworkInitializer] ✅ NetworkEventBus singleton ready");
            }
            catch (System.Exception e)
            {
                if (enableDebugLogging)
                    Debug.Log($"[NetworkInitializer] NetworkEventBus not available: {e.Message}");
            }

            try
            {
                var lagManager = LagCompensationManager.Instance;
                if (enableDebugLogging && lagManager != null)
                    Debug.Log("[NetworkInitializer] ✅ LagCompensationManager singleton ready");
            }
            catch (System.Exception e)
            {
                if (enableDebugLogging)
                    Debug.Log($"[NetworkInitializer] LagCompensationManager not available: {e.Message}");
            }

            try
            {
                var antiCheat = AntiCheatSystem.Instance;
                if (enableDebugLogging && antiCheat != null)
                    Debug.Log("[NetworkInitializer] ✅ AntiCheatSystem singleton ready");
            }
            catch (System.Exception e)
            {
                if (enableDebugLogging)
                    Debug.Log($"[NetworkInitializer] AntiCheatSystem not available: {e.Message}");
            }

            if (enableDebugLogging)
                Debug.Log("[NetworkInitializer] ✅ Network singleton initialization complete");
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
