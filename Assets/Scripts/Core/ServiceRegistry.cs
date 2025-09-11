using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Service Registration Manager - Initialize all services at startup
    /// Replaces manual dependency management throughout the codebase
    /// </summary>
    public class ServiceRegistry : MonoBehaviour
    {
        [Header("Core Services")]
        [SerializeField] private CommandManager commandManager;
        [SerializeField] private AbilitySystem abilitySystem;
        // [SerializeField] private ProjectilePool projectilePool; // DISABLED: Projectile system removed
        [SerializeField] private FlyweightFactory flyweightFactory;
        [SerializeField] private MemoryManager memoryManager;
        [SerializeField] private PerformanceProfiler performanceProfiler;

        [Header("Network Services")]
        [SerializeField] private Networking.NetworkGameManager networkGameManager;
        [SerializeField] private Networking.NetworkSystemIntegration networkIntegration;

        [Header("Auto-Discovery")]
        [SerializeField] private bool autoDiscoverServices = true;
        [SerializeField] private bool logRegistrations = true;

        private void Awake()
        {
            // Ensure this runs before other Awake methods
            RegisterAllServices();
        }

        private void RegisterAllServices()
        {
            if (logRegistrations)
            {
                Debug.Log("[ServiceRegistry] Starting service registration...");
            }

            // Register manually assigned services
            RegisterService(commandManager);
            RegisterService(abilitySystem);
            // RegisterService(projectilePool); // DISABLED: Projectile system removed
            RegisterService(flyweightFactory);
            RegisterService(memoryManager);
            RegisterService(performanceProfiler);
            RegisterService(networkGameManager);
            RegisterService(networkIntegration);

            // Auto-discover services if enabled
            if (autoDiscoverServices)
            {
                AutoDiscoverServices();
            }

            if (logRegistrations)
            {
                Debug.Log("[ServiceRegistry] Service registration complete.");
                LogRegisteredServices();
            }
        }

        private void RegisterService<T>(T service) where T : MonoBehaviour
        {
            if (service != null)
            {
                ServiceLocator.Register<T>(service);
                if (logRegistrations)
                {
                    Debug.Log($"[ServiceRegistry] Registered: {typeof(T).Name}");
                }
            }
        }

        private void AutoDiscoverServices()
        {
            // Auto-discover CommandManager
            if (commandManager == null)
            {
                commandManager = FindAnyObjectByType<CommandManager>();
                RegisterService(commandManager);
            }

            // Auto-discover AbilitySystem
            if (abilitySystem == null)
            {
                abilitySystem = FindAnyObjectByType<AbilitySystem>();
                RegisterService(abilitySystem);
            }

            // DISABLED: ProjectilePool auto-discovery commented out - projectile system removed
            // Auto-discover ProjectilePool
            // if (projectilePool == null)
            // {
            //     projectilePool = FindAnyObjectByType<ProjectilePool>();
            //     RegisterService(projectilePool);
            // }

            // Auto-discover FlyweightFactory
            if (flyweightFactory == null)
            {
                flyweightFactory = FindAnyObjectByType<FlyweightFactory>();
                RegisterService(flyweightFactory);
            }

            // Auto-discover MemoryManager
            if (memoryManager == null)
            {
                memoryManager = FindAnyObjectByType<MemoryManager>();
                RegisterService(memoryManager);
            }

            // Auto-discover PerformanceProfiler
            if (performanceProfiler == null)
            {
                performanceProfiler = FindAnyObjectByType<PerformanceProfiler>();
                RegisterService(performanceProfiler);
            }

            // Auto-discover NetworkGameManager
            if (networkGameManager == null)
            {
                networkGameManager = FindAnyObjectByType<Networking.NetworkGameManager>();
                RegisterService(networkGameManager);
            }

            // Auto-discover NetworkSystemIntegration
            if (networkIntegration == null)
            {
                networkIntegration = FindAnyObjectByType<Networking.NetworkSystemIntegration>();
                RegisterService(networkIntegration);
            }
        }

        private void LogRegisteredServices()
        {
            Debug.Log("[ServiceRegistry] === Registered Services ===");
            foreach (var serviceType in ServiceLocator.GetRegisteredTypes())
            {
                Debug.Log($"[ServiceRegistry] ✓ {serviceType.Name}");
            }
            Debug.Log("[ServiceRegistry] ===========================");
        }

        private void OnDestroy()
        {
            // Clear services when this object is destroyed
            ServiceLocator.Clear();
        }

        /// <summary>
        /// Manual service registration method for runtime additions
        /// </summary>
        public void RegisterRuntimeService<T>(T service) where T : MonoBehaviour
        {
            if (service != null)
            {
                ServiceLocator.Register<T>(service);
                if (logRegistrations)
                {
                    Debug.Log($"[ServiceRegistry] Runtime registered: {typeof(T).Name}");
                }
            }
        }

        /// <summary>
        /// Validate all critical services are registered
        /// </summary>
        public bool ValidateServices()
        {
            bool allValid = true;

            allValid &= ValidateService<CommandManager>("CommandManager");
            allValid &= ValidateService<AbilitySystem>("AbilitySystem");
            // allValid &= ValidateService<ProjectilePool>("ProjectilePool"); // DISABLED: Projectile system removed

            return allValid;
        }

        private bool ValidateService<T>(string serviceName) where T : class
        {
            if (!ServiceLocator.IsRegistered<T>())
            {
                Debug.LogError($"[ServiceRegistry] Critical service missing: {serviceName}");
                return false;
            }
            return true;
        }
    }
}
