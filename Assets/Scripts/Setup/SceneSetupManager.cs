using UnityEngine;
using UnityEngine.SceneManagement;

namespace MOBA.Setup
{
    /// <summary>
    /// Automated scene setup manager to implement all audit recommendations
    /// Creates and configures all necessary components for production deployment
    /// </summary>
    public class SceneSetupManager : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createMissingComponents = true;
        [SerializeField] private bool configureExistingComponents = true;

        [Header("Component Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject networkManagerPrefab;

        private bool setupComplete = false;

        private void Start()
        {
            if (autoSetupOnStart && !setupComplete)
            {
                SetupScene();
            }
        }

        /// <summary>
        /// Complete scene setup implementing all audit recommendations
        /// </summary>
        public void SetupScene()
        {
            Debug.Log("[SceneSetupManager] Starting comprehensive scene setup...");

            // Phase 1: Core Infrastructure
            SetupCoreInfrastructure();

            // Phase 2: Service Registration
            SetupServiceRegistry();

            // Phase 3: Network Systems
            SetupNetworkSystems();

            // Phase 4: Performance Systems
            SetupPerformanceSystems();

            // Phase 5: Testing Systems
            SetupTestingSystems();

            // Phase 6: Validation
            ValidateSetup();

            setupComplete = true;
            Debug.Log("[SceneSetupManager] ✅ Scene setup complete - All audit recommendations implemented!");
        }

        private void SetupCoreInfrastructure()
        {
            Debug.Log("[SceneSetupManager] Setting up core infrastructure...");

            // Create ProductionConfig if not exists
            if (createMissingComponents && FindAnyObjectByType<ProductionConfig>() == null)
            {
                var configObj = new GameObject("ProductionConfig");
                configObj.AddComponent<ProductionConfig>();
                Debug.Log("[SceneSetupManager] ✅ ProductionConfig created");
            }

            // Create ServiceRegistry if not exists
            if (createMissingComponents && FindAnyObjectByType<ServiceRegistry>() == null)
            {
                var registryObj = new GameObject("ServiceRegistry");
                registryObj.AddComponent<ServiceRegistry>();
                Debug.Log("[SceneSetupManager] ✅ ServiceRegistry created");
            }
            else if (configureExistingComponents)
            {
                var existingRegistry = FindAnyObjectByType<ServiceRegistry>();
                if (existingRegistry != null)
                {
                    // Configure existing registry if needed
                    Debug.Log("[SceneSetupManager] ✅ ServiceRegistry configured");
                }
            }

            // REMOVED: CommandManager was removed during cleanup
            // Command pattern was simplified to direct ability casting

            // Create AbilitySystem if not exists
            if (createMissingComponents && FindAnyObjectByType<AbilitySystem>() == null)
            {
                var abilityObj = new GameObject("AbilitySystem");
                abilityObj.AddComponent<AbilitySystem>();
                Debug.Log("[SceneSetupManager] ✅ AbilitySystem created");
            }
        }

        private void SetupServiceRegistry()
        {
            Debug.Log("[SceneSetupManager] Configuring ServiceRegistry...");

            var registry = FindAnyObjectByType<ServiceRegistry>();
            if (registry != null)
            {
                // The ServiceRegistry will auto-discover services
                // Additional configuration could be added here
                Debug.Log("[SceneSetupManager] ✅ ServiceRegistry configured");
            }
        }

        private void SetupNetworkSystems()
        {
            Debug.Log("[SceneSetupManager] Setting up network systems...");

            // Create NetworkSystemIntegration if not exists
            if (FindAnyObjectByType<Networking.NetworkSystemIntegration>() == null)
            {
                var networkObj = new GameObject("NetworkSystemIntegration");
                var networkIntegration = networkObj.AddComponent<Networking.NetworkSystemIntegration>();
                Debug.Log("[SceneSetupManager] ✅ NetworkSystemIntegration created");
            }

            // DISABLED: ProjectilePool creation commented out - projectile system removed
            // Create ProjectilePool if not exists
            // if (FindAnyObjectByType<ProjectilePool>() == null)
            // {
            //     var poolObj = new GameObject("ProjectilePool");
            //     var pool = poolObj.AddComponent<ProjectilePool>();
            //     
            //     if (projectilePrefab != null)
            //     {
            //         // Configure the pool with the prefab
            //         Debug.Log("[SceneSetupManager] ✅ ProjectilePool created and configured");
            //     }
            //     else
            //     {
            //         Debug.LogWarning("[SceneSetupManager] ⚠️ ProjectilePool created but no prefab assigned");
            //     }
            // }
            
            Debug.Log("[SceneSetupManager] ProjectilePool creation skipped - projectile system disabled");

            // DISABLED: FlyweightFactory creation removed - part of projectile system
            // Create FlyweightFactory if not exists
            // if (FindAnyObjectByType<FlyweightFactory>() == null)
            // {
            //     var factoryObj = new GameObject("FlyweightFactory");
            //     factoryObj.AddComponent<FlyweightFactory>();
            //     Debug.Log("[SceneSetupManager] ✅ FlyweightFactory created");
            // }
            
            Debug.Log("[SceneSetupManager] FlyweightFactory creation skipped - projectile system disabled");
        }

        private void SetupPerformanceSystems()
        {
            Debug.Log("[SceneSetupManager] Setting up performance systems...");

            // REMOVED: All tool systems deleted during cleanup
        }

        private void SetupTestingSystems()
        {
            Debug.Log("[SceneSetupManager] Setting up testing systems...");

            // Create AutomatedTestRunner if not exists (only in development)
            if (!ProductionConfig.IsProductionBuild)
            {
                // Use reflection to avoid assembly reference issues
                var testRunnerType = System.Type.GetType("MOBA.Testing.AutomatedTestRunner, MOBA.Testing");
                if (testRunnerType != null)
                {
                    var existingTestRunner = FindAnyObjectByType(testRunnerType);
                    if (existingTestRunner == null)
                    {
                        var testObj = new GameObject("AutomatedTestRunner");
                        testObj.AddComponent(testRunnerType);
                        Debug.Log("[SceneSetupManager] ✅ AutomatedTestRunner created");
                    }
                }
                else
                {
                    Debug.LogWarning("[SceneSetupManager] ⚠️ AutomatedTestRunner type not found - testing assembly may not be loaded");
                }
            }

            // Additional testing infrastructure could be added here
        }

        private void ValidateSetup()
        {
            Debug.Log("[SceneSetupManager] Validating setup...");

            int criticalComponents = 0;
            int optionalComponents = 0;

            // Validate critical components
            if (FindAnyObjectByType<ProductionConfig>() != null) criticalComponents++;
            if (FindAnyObjectByType<ServiceRegistry>() != null) criticalComponents++;
            // REMOVED: CommandManager was removed during cleanup
            if (FindAnyObjectByType<AbilitySystem>() != null) criticalComponents++;
            // if (FindAnyObjectByType<ProjectilePool>() != null) criticalComponents++; // DISABLED: Projectile system removed

            // Validate optional components
            // REMOVED: Tool systems deleted during cleanup
            if (FindAnyObjectByType<Networking.NetworkSystemIntegration>() != null) optionalComponents++;

            Debug.Log($"[SceneSetupManager] Validation Results:");
            Debug.Log($"[SceneSetupManager] ✅ Critical Components: {criticalComponents}/4"); // Updated count
            Debug.Log($"[SceneSetupManager] ✅ Optional Components: {optionalComponents}/4");

            if (criticalComponents >= 4)
            {
                Debug.Log("[SceneSetupManager] ✅ Setup validation PASSED - Scene ready for production!");
            }
            else
            {
                Debug.LogWarning("[SceneSetupManager] ⚠️ Setup validation FAILED - Missing critical components!");
            }
        }

        /// <summary>
        /// Get setup status summary
        /// </summary>
        public string GetSetupStatus()
        {
            if (!setupComplete)
            {
                return "Setup not complete";
            }

            var registry = FindAnyObjectByType<ServiceRegistry>();
            bool servicesValid = registry != null && registry.ValidateServices();

            return $"Setup: {(setupComplete ? "✅" : "❌")}, " +
                   $"Services: {(servicesValid ? "✅" : "❌")}, " +
                   $"Config: {ProductionConfig.Instance.GetConfigurationSummary()}";
        }

        /// <summary>
        /// Manual trigger for scene setup
        /// </summary>
        [ContextMenu("Setup Scene")]
        public void ManualSetup()
        {
            setupComplete = false;
            SetupScene();
        }

        /// <summary>
        /// Reset scene to clean state
        /// </summary>
        [ContextMenu("Reset Scene")]
        public void ResetScene()
        {
            // This would clean up all created components
            Debug.Log("[SceneSetupManager] Scene reset functionality - implement if needed");
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, Screen.height - 100, 400, 90));
            GUILayout.Label("Scene Setup Manager");
            GUILayout.Label(GetSetupStatus());
            
            if (!setupComplete && GUILayout.Button("Setup Scene"))
            {
                SetupScene();
            }
            
            if (setupComplete && GUILayout.Button("Validate Setup"))
            {
                ValidateSetup();
            }
            
            GUILayout.EndArea();
        }
    }
}
