using UnityEngine;
using Unity.Netcode;
using MOBA.Abilities;

namespace MOBA.Networking
{
    /// <summary>
    /// Helper component that automatically integrates lag compensation with existing systems
    /// Should be attached to a NetworkManager or scene object on server startup
    /// </summary>
    public class LagCompensationIntegration : NetworkBehaviour
    {
        #region Configuration
        
        [Header("Integration Settings")]
        [SerializeField, Tooltip("Automatically find and integrate with LagCompensationManager")]
        private bool autoIntegrate = true;
        
        [SerializeField, Tooltip("Create LagCompensationManager if not found")]
        private bool createIfMissing = true;
        
        [SerializeField, Tooltip("Log integration status for debugging")]
        private bool logIntegrationStatus = true;
        
        #endregion
        
        #region Private Fields
        
        private LagCompensationManager lagCompensationManager;
        private bool isIntegrated = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            
            if (autoIntegrate)
            {
                IntegrateWithSystems();
            }
        }
        
        #endregion
        
        #region Integration Methods
        
        /// <summary>
        /// Integrate lag compensation with existing MOBA systems
        /// </summary>
        public void IntegrateWithSystems()
        {
            if (isIntegrated)
            {
                if (logIntegrationStatus)
                {
                    Debug.Log("[LagCompensationIntegration] Already integrated");
                }
                return;
            }
            
            // Find or create lag compensation manager
            SetupLagCompensationManager();
            
            if (lagCompensationManager != null)
            {
                // Integration is primarily done through references in combat systems
                // The LagCompensationManager automatically integrates with IDamageable components
                isIntegrated = true;
                
                if (logIntegrationStatus)
                {
                    Debug.Log("[LagCompensationIntegration] Successfully integrated lag compensation with MOBA systems");
                }
            }
            else
            {
                Debug.LogError("[LagCompensationIntegration] Failed to setup LagCompensationManager");
            }
        }
        
        /// <summary>
        /// Setup lag compensation manager
        /// </summary>
        private void SetupLagCompensationManager()
        {
            // First try to find existing manager
            lagCompensationManager = FindFirstObjectByType<LagCompensationManager>();
            
            if (lagCompensationManager == null && createIfMissing)
            {
                // Create new lag compensation manager
                var managerObject = new GameObject("LagCompensationManager");
                lagCompensationManager = managerObject.AddComponent<LagCompensationManager>();
                
                // Make it a network object if needed
                var networkObject = managerObject.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    networkObject = managerObject.AddComponent<NetworkObject>();
                }
                
                // Spawn the network object
                if (IsServer && !networkObject.IsSpawned)
                {
                    networkObject.Spawn();
                }
                
                if (logIntegrationStatus)
                {
                    Debug.Log("[LagCompensationIntegration] Created new LagCompensationManager");
                }
            }
            else if (lagCompensationManager != null && logIntegrationStatus)
            {
                Debug.Log("[LagCompensationIntegration] Found existing LagCompensationManager");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get the integrated lag compensation manager
        /// </summary>
        /// <returns>LagCompensationManager instance or null</returns>
        public LagCompensationManager GetLagCompensationManager()
        {
            return lagCompensationManager;
        }
        
        /// <summary>
        /// Check if lag compensation is integrated and active
        /// </summary>
        /// <returns>True if integrated and active</returns>
        public bool IsLagCompensationActive()
        {
            return isIntegrated && lagCompensationManager != null && lagCompensationManager.enabled;
        }
        
        /// <summary>
        /// Manually trigger integration (useful for testing)
        /// </summary>
        public void ForceIntegration()
        {
            isIntegrated = false;
            IntegrateWithSystems();
        }
        
        #endregion
        
        #region Network Events
        
        public override void OnNetworkSpawn()
        {
            if (IsServer && autoIntegrate && !isIntegrated)
            {
                IntegrateWithSystems();
            }
        }
        
        #endregion
    }
}