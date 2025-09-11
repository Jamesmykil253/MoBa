using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Production build configuration and optimization settings
    /// Controls debug logging, performance features, and production optimizations
    /// </summary>
    public class ProductionConfig : MonoBehaviour
    {
        [Header("Build Configuration")]
        [SerializeField] private bool isProductionBuild = false;
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool enablePerformanceOptimization = true;
        [SerializeField] private bool enableAutomatedTesting = false;

        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool enableVSync = true;
        [SerializeField] private bool optimizeForMobile = false;

        [Header("Logging Configuration")]
        [SerializeField] private LogLevel productionLogLevel = LogLevel.Warning;
        [SerializeField] private LogLevel developmentLogLevel = LogLevel.Debug;
        [SerializeField] private int maxLogsPerSecond = 30;

        [Header("Network Configuration")]
        [SerializeField] private float networkTickRate = 60f;
        [SerializeField] private bool enableAntiCheat = true;
        [SerializeField] private bool enableLagCompensation = true;

        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
            Debug = 4,
            Verbose = 5
        }

        // Singleton pattern for global access
        private static ProductionConfig _instance;
        public static ProductionConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ProductionConfig>();
                    if (_instance == null)
                    {
                        GameObject configObject = new GameObject("ProductionConfig");
                        _instance = configObject.AddComponent<ProductionConfig>();
                        DontDestroyOnLoad(configObject);
                    }
                }
                return _instance;
            }
        }

        // Public properties for easy access
        public static bool IsProductionBuild => Instance.isProductionBuild;
        public static bool EnableDebugLogging => Instance.enableDebugLogging;
        public static bool EnablePerformanceOptimization => Instance.enablePerformanceOptimization;
        public static bool EnableAutomatedTesting => Instance.enableAutomatedTesting;
        public static int TargetFrameRate => Instance.targetFrameRate;
        public static float NetworkTickRate => Instance.networkTickRate;
        public static bool EnableAntiCheat => Instance.enableAntiCheat;
        public static bool EnableLagCompensation => Instance.enableLagCompensation;
        public static int MaxLogsPerSecond => Instance.maxLogsPerSecond;

        private void Awake()
        {
            // Ensure singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-detect build type
            DetectBuildType();
            
            // Apply configuration
            ApplyConfiguration();
        }

        private void DetectBuildType()
        {
            #if UNITY_EDITOR
            isProductionBuild = false;
            enableDebugLogging = true;
            enableAutomatedTesting = true;
            #elif DEVELOPMENT_BUILD
            isProductionBuild = false;
            enableDebugLogging = true;
            enableAutomatedTesting = false;
            #else
            isProductionBuild = true;
            enableDebugLogging = false;
            enableAutomatedTesting = false;
            #endif
        }

        private void ApplyConfiguration()
        {
            // Set frame rate
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;

            // Configure logging
            if (isProductionBuild)
            {
                Debug.unityLogger.logEnabled = enableDebugLogging;
            }

            // Mobile optimizations
            if (optimizeForMobile)
            {
                ApplyMobileOptimizations();
            }

            LogConfigurationStatus();
        }

        private void ApplyMobileOptimizations()
        {
            // Reduce quality for mobile
            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        }

        private void LogConfigurationStatus()
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[ProductionConfig] Build Type: {(isProductionBuild ? "Production" : "Development")}");
                Debug.Log($"[ProductionConfig] Debug Logging: {enableDebugLogging}");
                Debug.Log($"[ProductionConfig] Performance Optimization: {enablePerformanceOptimization}");
                Debug.Log($"[ProductionConfig] Target FPS: {targetFrameRate}");
                Debug.Log($"[ProductionConfig] Network Tick Rate: {networkTickRate}Hz");
                Debug.Log($"[ProductionConfig] Anti-Cheat: {enableAntiCheat}");
            }
        }

        /// <summary>
        /// Conditional logging based on build configuration
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!EnableDebugLogging) return;

            LogLevel currentLevel = Instance.isProductionBuild ? 
                Instance.productionLogLevel : Instance.developmentLogLevel;

            if (level <= currentLevel)
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Debug.LogError($"[MOBA] {message}");
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning($"[MOBA] {message}");
                        break;
                    default:
                        Debug.Log($"[MOBA] {message}");
                        break;
                }
            }
        }

        /// <summary>
        /// Performance-aware logging with rate limiting
        /// </summary>
        public static void LogThrottled(string message, LogLevel level = LogLevel.Debug)
        {
            // Rate limiting based on maxLogsPerSecond
            int frameInterval = Mathf.Max(1, Mathf.RoundToInt(Application.targetFrameRate / (float)MaxLogsPerSecond));
            if (Time.frameCount % frameInterval == 0)
            {
                Log(message, level);
            }
        }

        /// <summary>
        /// Enable/disable production mode at runtime
        /// </summary>
        public void SetProductionMode(bool isProduction)
        {
            isProductionBuild = isProduction;
            enableDebugLogging = !isProduction;
            ApplyConfiguration();
        }

        /// <summary>
        /// Get current configuration as string
        /// </summary>
        public string GetConfigurationSummary()
        {
            return $"Build: {(isProductionBuild ? "PROD" : "DEV")}, " +
                   $"Logging: {enableDebugLogging}, " +
                   $"FPS: {targetFrameRate}, " +
                   $"AntiCheat: {enableAntiCheat}";
        }

        // GUI for runtime configuration in editor
        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(Screen.width - 300, Screen.height - 150, 290, 140));
            GUILayout.Label("Production Configuration");
            
            if (GUILayout.Button($"Mode: {(isProductionBuild ? "PRODUCTION" : "DEVELOPMENT")}"))
            {
                SetProductionMode(!isProductionBuild);
            }
            
            if (GUILayout.Button($"Debug Logging: {(enableDebugLogging ? "ON" : "OFF")}"))
            {
                enableDebugLogging = !enableDebugLogging;
                ApplyConfiguration();
            }
            
            if (GUILayout.Button($"Performance Opt: {(enablePerformanceOptimization ? "ON" : "OFF")}"))
            {
                enablePerformanceOptimization = !enablePerformanceOptimization;
            }
            
            GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F1} / {targetFrameRate}");
            GUILayout.EndArea();
        }
    }
}
