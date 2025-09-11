using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MOBA.Integration
{
    /// <summary>
    /// Advanced Systems Integration Manager
    /// Coordinates all advanced systems and ensures proper initialization order
    /// Based on Clean Code principles and dependency injection patterns
    /// </summary>
    public class AdvancedSystemsManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private UI.AdvancedUISystem uiSystem;
        [SerializeField] private Performance.PerformanceOptimizationSystem performanceSystem;
        [SerializeField] private Analytics.AnalyticsSystem analyticsSystem;
        [SerializeField] private HoldToAimSystem holdToAimSystem;
        [SerializeField] private CryptoCoinSystem cryptoCoinSystem;
        [SerializeField] private RSBCombatSystem rsbCombatSystem;
        
        [Header("Integration Settings")]
        [SerializeField] private bool enableSystemIntegration = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableAdvancedUI = true;
        [SerializeField] private float initializationTimeout = 30f;
        
        [Header("System Status")]
        [SerializeField] private bool allSystemsInitialized = false;
        [SerializeField] private List<string> initializedSystems = new List<string>();
        [SerializeField] private List<string> failedSystems = new List<string>();
        
        // Initialization tracking
        private Dictionary<string, bool> systemInitializationStatus = new Dictionary<string, bool>();
        private float initializationStartTime;
        private bool initializationComplete = false;
        
        // Integration state
        private bool systemsConnected = false;
        private Coroutine initializationCoroutine;
        
        // Events
        public static event System.Action OnAllSystemsInitialized;
        public static event System.Action<string> OnSystemInitialized;
        public static event System.Action<string> OnSystemFailed;

        private void Awake()
        {
            InitializeSystemManager();
        }

        private void Start()
        {
            if (enableSystemIntegration)
            {
                initializationCoroutine = StartCoroutine(InitializeAllSystemsCoroutine());
            }
        }

        private void Update()
        {
            // Monitor system health if initialization is complete
            if (initializationComplete && enablePerformanceMonitoring)
            {
                MonitorSystemHealth();
            }
        }

        private void OnDestroy()
        {
            if (initializationCoroutine != null)
            {
                StopCoroutine(initializationCoroutine);
            }
        }

        /// <summary>
        /// Initialize the system manager
        /// </summary>
        private void InitializeSystemManager()
        {
            initializationStartTime = Time.time;
            
            // Initialize tracking dictionaries
            systemInitializationStatus["UISystem"] = false;
            systemInitializationStatus["PerformanceSystem"] = false;
            systemInitializationStatus["AnalyticsSystem"] = false;
            systemInitializationStatus["HoldToAimSystem"] = false;
            systemInitializationStatus["CryptoCoinSystem"] = false;
            systemInitializationStatus["RSBCombatSystem"] = false;
            
            Debug.Log("[AdvancedSystemsManager] System manager initialized");
        }

        /// <summary>
        /// Initialize all systems in proper order
        /// </summary>
        private IEnumerator InitializeAllSystemsCoroutine()
        {
            Debug.Log("[AdvancedSystemsManager] Starting system initialization sequence...");
            float startTime = Time.time;
            
            // Phase 1: Core Systems (Analytics first for tracking)
            yield return StartCoroutine(InitializeAnalyticsSystem());
            yield return StartCoroutine(InitializePerformanceSystem());
            
            // Check timeout after core systems
            if (Time.time - startTime > initializationTimeout / 2f)
            {
                Debug.LogWarning($"[AdvancedSystemsManager] Core system initialization taking longer than expected");
            }
            
            // Phase 2: Gameplay Systems
            yield return StartCoroutine(InitializeRSBCombatSystem());
            yield return StartCoroutine(InitializeCryptoCoinSystem());
            yield return StartCoroutine(InitializeHoldToAimSystem());
            
            // Phase 3: UI System (last, depends on all others)
            yield return StartCoroutine(InitializeUISystem());
            
            // Check final timeout
            float totalTime = Time.time - startTime;
            if (totalTime > initializationTimeout)
            {
                Debug.LogError($"[AdvancedSystemsManager] System initialization exceeded timeout ({totalTime:F1}s > {initializationTimeout}s)");
                allSystemsInitialized = false;
                yield break;
            }
            
            // Phase 4: Connect Systems
            yield return StartCoroutine(ConnectAllSystems());
            
            // Phase 5: Final Validation
            ValidateAllSystems();
            
            Debug.Log("[AdvancedSystemsManager] System initialization sequence complete");
        }

        /// <summary>
        /// Initialize Analytics System
        /// </summary>
        private IEnumerator InitializeAnalyticsSystem()
        {
            if (!enableAnalytics)
            {
                MarkSystemInitialized("AnalyticsSystem");
                yield break;
            }
            
            Debug.Log("[AdvancedSystemsManager] Initializing Analytics System...");
            
            if (analyticsSystem == null)
            {
                analyticsSystem = FindAnyObjectByType<Analytics.AnalyticsSystem>();
                if (analyticsSystem == null)
                {
                    // Create analytics system if not found
                    GameObject analyticsGO = new GameObject("AnalyticsSystem");
                    analyticsSystem = analyticsGO.AddComponent<Analytics.AnalyticsSystem>();
                }
            }
            
            // Wait for analytics system to be ready
            float timeout = Time.time + 5f;
            while (analyticsSystem == null && Time.time < timeout)
            {
                yield return null;
            }
            
            if (analyticsSystem != null)
            {
                MarkSystemInitialized("AnalyticsSystem");
                analyticsSystem.TrackGameplayEvent("system_initialization", new Dictionary<string, object>
                {
                    ["system"] = "AnalyticsSystem",
                    ["timestamp"] = Time.time
                });
            }
            else
            {
                MarkSystemFailed("AnalyticsSystem");
            }
        }

        /// <summary>
        /// Initialize Performance Optimization System
        /// </summary>
        private IEnumerator InitializePerformanceSystem()
        {
            if (!enablePerformanceMonitoring)
            {
                MarkSystemInitialized("PerformanceSystem");
                yield break;
            }
            
            Debug.Log("[AdvancedSystemsManager] Initializing Performance System...");
            
            if (performanceSystem == null)
            {
                performanceSystem = FindAnyObjectByType<Performance.PerformanceOptimizationSystem>();
                if (performanceSystem == null)
                {
                    GameObject performanceGO = new GameObject("PerformanceOptimizationSystem");
                    performanceSystem = performanceGO.AddComponent<Performance.PerformanceOptimizationSystem>();
                }
            }
            
            // Wait for performance system to be ready
            yield return new WaitForSeconds(1f);
            
            if (performanceSystem != null)
            {
                MarkSystemInitialized("PerformanceSystem");
                TrackSystemEvent("PerformanceSystem", "initialized");
            }
            else
            {
                MarkSystemFailed("PerformanceSystem");
            }
        }

        /// <summary>
        /// Initialize RSB Combat System
        /// </summary>
        private IEnumerator InitializeRSBCombatSystem()
        {
            Debug.Log("[AdvancedSystemsManager] Initializing RSB Combat System...");
            
            if (rsbCombatSystem == null)
            {
                rsbCombatSystem = FindAnyObjectByType<RSBCombatSystem>();
                if (rsbCombatSystem == null)
                {
                    GameObject rsbGO = new GameObject("RSBCombatSystem");
                    rsbCombatSystem = rsbGO.AddComponent<RSBCombatSystem>();
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            if (rsbCombatSystem != null)
            {
                MarkSystemInitialized("RSBCombatSystem");
                TrackSystemEvent("RSBCombatSystem", "initialized");
            }
            else
            {
                MarkSystemFailed("RSBCombatSystem");
            }
        }

        /// <summary>
        /// Initialize Crypto Coin System
        /// </summary>
        private IEnumerator InitializeCryptoCoinSystem()
        {
            Debug.Log("[AdvancedSystemsManager] Initializing Crypto Coin System...");
            
            if (cryptoCoinSystem == null)
            {
                cryptoCoinSystem = FindAnyObjectByType<CryptoCoinSystem>();
                if (cryptoCoinSystem == null)
                {
                    GameObject coinGO = new GameObject("CryptoCoinSystem");
                    cryptoCoinSystem = coinGO.AddComponent<CryptoCoinSystem>();
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            if (cryptoCoinSystem != null)
            {
                MarkSystemInitialized("CryptoCoinSystem");
                TrackSystemEvent("CryptoCoinSystem", "initialized");
            }
            else
            {
                MarkSystemFailed("CryptoCoinSystem");
            }
        }

        /// <summary>
        /// Initialize Hold-to-Aim System
        /// </summary>
        private IEnumerator InitializeHoldToAimSystem()
        {
            Debug.Log("[AdvancedSystemsManager] Initializing Hold-to-Aim System...");
            
            if (holdToAimSystem == null)
            {
                holdToAimSystem = FindAnyObjectByType<HoldToAimSystem>();
                if (holdToAimSystem == null)
                {
                    GameObject aimGO = new GameObject("HoldToAimSystem");
                    holdToAimSystem = aimGO.AddComponent<HoldToAimSystem>();
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            if (holdToAimSystem != null)
            {
                MarkSystemInitialized("HoldToAimSystem");
                TrackSystemEvent("HoldToAimSystem", "initialized");
            }
            else
            {
                MarkSystemFailed("HoldToAimSystem");
            }
        }

        /// <summary>
        /// Initialize UI System
        /// </summary>
        private IEnumerator InitializeUISystem()
        {
            if (!enableAdvancedUI)
            {
                MarkSystemInitialized("UISystem");
                yield break;
            }
            
            Debug.Log("[AdvancedSystemsManager] Initializing UI System...");
            
            if (uiSystem == null)
            {
                uiSystem = FindAnyObjectByType<UI.AdvancedUISystem>();
                if (uiSystem == null)
                {
                    // UI System needs to be in the scene manually or created with proper Canvas setup
                    Debug.LogWarning("[AdvancedSystemsManager] UI System not found - manual setup required");
                }
            }
            
            yield return new WaitForSeconds(1f);
            
            if (uiSystem != null)
            {
                MarkSystemInitialized("UISystem");
                TrackSystemEvent("UISystem", "initialized");
            }
            else
            {
                // UI System is optional, don't mark as failed
                MarkSystemInitialized("UISystem");
                Debug.LogWarning("[AdvancedSystemsManager] UI System not available - continuing without advanced UI");
            }
        }

        /// <summary>
        /// Connect all systems together
        /// </summary>
        private IEnumerator ConnectAllSystems()
        {
            Debug.Log("[AdvancedSystemsManager] Connecting systems...");
            
            // Connect Performance System to Analytics
            if (performanceSystem != null && analyticsSystem != null)
            {
                Performance.PerformanceOptimizationSystem.OnPerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
                Performance.PerformanceOptimizationSystem.OnQualityLevelChanged += OnQualityLevelChanged;
            }
            
            // Connect Crypto Coin System to Analytics
            if (cryptoCoinSystem != null && analyticsSystem != null)
            {
                CryptoCoinSystem.OnCoinPickup += OnCoinPickup;
                CryptoCoinSystem.OnCoinsScored += OnCoinsScored;
                CryptoCoinSystem.OnPlayerKill += OnPlayerKill;
            }
            
            // Connect UI System to Performance System
            if (uiSystem != null && performanceSystem != null)
            {
                Performance.PerformanceOptimizationSystem.OnPerformanceMetricsUpdated += (metrics) =>
                {
                    if (metrics.frameRate < 30f)
                    {
                        uiSystem.ShowPerformanceWarning("Low frame rate detected");
                    }
                    else if (metrics.memoryUsage > 400f)
                    {
                        uiSystem.ShowPerformanceWarning("High memory usage detected");
                    }
                    else
                    {
                        uiSystem.HidePerformanceWarning();
                    }
                };
            }
            
            yield return new WaitForSeconds(0.5f);
            
            systemsConnected = true;
            Debug.Log("[AdvancedSystemsManager] Systems connected successfully");
        }

        /// <summary>
        /// Validate all systems are working properly
        /// </summary>
        private void ValidateAllSystems()
        {
            bool allValid = true;
            
            foreach (var kvp in systemInitializationStatus)
            {
                if (!kvp.Value)
                {
                    allValid = false;
                    Debug.LogError($"[AdvancedSystemsManager] System validation failed: {kvp.Key}");
                }
            }
            
            if (allValid)
            {
                allSystemsInitialized = true;
                initializationComplete = true;
                
                float initTime = Time.time - initializationStartTime;
                Debug.Log($"[AdvancedSystemsManager] ✅ All systems initialized successfully in {initTime:F2} seconds");
                
                TrackSystemEvent("AdvancedSystemsManager", "all_systems_initialized", new Dictionary<string, object>
                {
                    ["initialization_time"] = initTime,
                    ["systems_count"] = systemInitializationStatus.Count
                });
                
                OnAllSystemsInitialized?.Invoke();
            }
            else
            {
                Debug.LogError("[AdvancedSystemsManager] ❌ System validation failed - some systems not initialized");
            }
        }

        /// <summary>
        /// Monitor system health during runtime
        /// </summary>
        private void MonitorSystemHealth()
        {
            // Check for system failures or performance issues
            if (performanceSystem != null)
            {
                var metrics = performanceSystem.GetCurrentMetrics();
                
                // Check for critical performance issues
                if (metrics.frameRate < 15f)
                {
                    Debug.LogWarning("[AdvancedSystemsManager] Critical performance issue detected");
                    TrackSystemEvent("AdvancedSystemsManager", "critical_performance_warning");
                }
                
                if (metrics.memoryUsage > 500f)
                {
                    Debug.LogWarning("[AdvancedSystemsManager] Critical memory usage detected");
                    TrackSystemEvent("AdvancedSystemsManager", "critical_memory_warning");
                }
            }
        }

        /// <summary>
        /// Mark a system as initialized
        /// </summary>
        private void MarkSystemInitialized(string systemName)
        {
            systemInitializationStatus[systemName] = true;
            initializedSystems.Add(systemName);
            
            Debug.Log($"[AdvancedSystemsManager] ✅ {systemName} initialized");
            OnSystemInitialized?.Invoke(systemName);
        }

        /// <summary>
        /// Mark a system as failed
        /// </summary>
        private void MarkSystemFailed(string systemName)
        {
            systemInitializationStatus[systemName] = false;
            failedSystems.Add(systemName);
            
            Debug.LogError($"[AdvancedSystemsManager] ❌ {systemName} failed to initialize");
            OnSystemFailed?.Invoke(systemName);
        }

        /// <summary>
        /// Track system events through analytics
        /// </summary>
        private void TrackSystemEvent(string systemName, string eventType, Dictionary<string, object> parameters = null)
        {
            if (analyticsSystem != null)
            {
                var eventParams = parameters ?? new Dictionary<string, object>();
                eventParams["system"] = systemName;
                eventParams["event_type"] = eventType;
                
                analyticsSystem.TrackGameplayEvent($"system_{eventType}", eventParams);
            }
        }

        /// <summary>
        /// Event handlers for system integration
        /// </summary>
        private void OnPerformanceMetricsUpdated(Performance.PerformanceMetrics metrics)
        {
            // Additional performance tracking logic
        }

        private void OnQualityLevelChanged(Performance.QualityLevel newLevel)
        {
            TrackSystemEvent("PerformanceSystem", "quality_level_changed", new Dictionary<string, object>
            {
                ["new_level"] = newLevel.ToString()
            });
        }

        private void OnCoinPickup(int coins)
        {
            TrackSystemEvent("CryptoCoinSystem", "coin_pickup", new Dictionary<string, object>
            {
                ["coins"] = coins
            });
        }

        private void OnCoinsScored(int playerId, int coins)
        {
            TrackSystemEvent("CryptoCoinSystem", "coins_scored", new Dictionary<string, object>
            {
                ["player_id"] = playerId,
                ["coins"] = coins
            });
        }

        private void OnPlayerKill(int coins)
        {
            TrackSystemEvent("CryptoCoinSystem", "player_kill", new Dictionary<string, object>
            {
                ["coins_earned"] = coins
            });
        }

        /// <summary>
        /// Public API
        /// </summary>
        public bool AreAllSystemsInitialized() => allSystemsInitialized;
        
        public bool IsSystemInitialized(string systemName)
        {
            return systemInitializationStatus.TryGetValue(systemName, out bool status) && status;
        }
        
        public List<string> GetInitializedSystems() => new List<string>(initializedSystems);
        
        public List<string> GetFailedSystems() => new List<string>(failedSystems);
        
        public float GetInitializationTime() => initializationComplete ? 
            Time.time - initializationStartTime : -1f;
        
        /// <summary>
        /// Force reinitialize a specific system
        /// </summary>
        public void ReinitializeSystem(string systemName)
        {
            Debug.Log($"[AdvancedSystemsManager] Reinitializing {systemName}...");
            
            switch (systemName)
            {
                case "PerformanceSystem":
                    StartCoroutine(InitializePerformanceSystem());
                    break;
                case "AnalyticsSystem":
                    StartCoroutine(InitializeAnalyticsSystem());
                    break;
                case "UISystem":
                    StartCoroutine(InitializeUISystem());
                    break;
                // Add other systems as needed
            }
        }
        
        /// <summary>
        /// Get comprehensive system status report
        /// </summary>
        public SystemStatusReport GetSystemStatusReport()
        {
            return new SystemStatusReport
            {
                allSystemsInitialized = allSystemsInitialized,
                systemsConnected = systemsConnected,
                initializationComplete = initializationComplete,
                initializationTime = GetInitializationTime(),
                initializedSystems = GetInitializedSystems(),
                failedSystems = GetFailedSystems(),
                systemCount = systemInitializationStatus.Count
            };
        }
    }

    /// <summary>
    /// System status report data structure
    /// </summary>
    [System.Serializable]
    public struct SystemStatusReport
    {
        public bool allSystemsInitialized;
        public bool systemsConnected;
        public bool initializationComplete;
        public float initializationTime;
        public List<string> initializedSystems;
        public List<string> failedSystems;
        public int systemCount;
    }
}
