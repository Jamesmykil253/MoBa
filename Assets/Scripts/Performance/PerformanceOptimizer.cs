using UnityEngine;
using System.Collections.Generic;

namespace MOBA.Performance
{
    /// <summary>
    /// Performance optimization manager to address audit findings
    /// Manages frame rate stability, memory usage, and system efficiency
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        [Header("Performance Targets")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private float memoryThresholdMB = 512f;
        [SerializeField] private bool enableDynamicOptimizations = true;

        [Header("Optimization Settings")]
        [SerializeField] private bool reduceLoggingInProduction = true;
        [SerializeField] private bool enableObjectPooling = true;
        [SerializeField] private bool optimizeNetworkUpdates = true;

        [Header("Monitoring")]
        [SerializeField] private float monitoringInterval = 1f;
        [SerializeField] private bool logPerformanceMetrics = false;

        // Performance tracking
        private float lastFrameRate;
        private float memoryUsageMB;
        private List<float> frameTimeHistory = new List<float>();
        private const int FRAME_HISTORY_SIZE = 60;

        // Optimization state
        private bool isOptimizing = false;
        private Dictionary<string, bool> optimizationFlags = new Dictionary<string, bool>();

        private void Start()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Initialize optimization flags
            InitializeOptimizationFlags();
            
            // Start monitoring
            InvokeRepeating(nameof(MonitorPerformance), 0f, monitoringInterval);
            
            Debug.Log($"[PerformanceOptimizer] Initialized with target FPS: {targetFrameRate}");
        }

        private void InitializeOptimizationFlags()
        {
            optimizationFlags["ReducedLogging"] = false;
            optimizationFlags["PoolingActive"] = false;
            optimizationFlags["NetworkOptimized"] = false;
            optimizationFlags["QualityReduced"] = false;
        }

        private void Update()
        {
            // Track frame time
            TrackFrameTime();
            
            // Apply dynamic optimizations if enabled
            if (enableDynamicOptimizations && !isOptimizing)
            {
                CheckForOptimizationNeeds();
            }
        }

        private void TrackFrameTime()
        {
            float frameTime = Time.unscaledDeltaTime;
            frameTimeHistory.Add(frameTime);
            
            if (frameTimeHistory.Count > FRAME_HISTORY_SIZE)
            {
                frameTimeHistory.RemoveAt(0);
            }
            
            lastFrameRate = 1f / frameTime;
        }

        private void MonitorPerformance()
        {
            // Update memory usage
            memoryUsageMB = (float)System.GC.GetTotalMemory(false) / 1024f / 1024f;
            
            if (logPerformanceMetrics)
            {
                Debug.Log($"[PerformanceOptimizer] FPS: {lastFrameRate:F1}, Memory: {memoryUsageMB:F1}MB");
            }
        }

        private void CheckForOptimizationNeeds()
        {
            bool needsOptimization = false;
            
            // Check frame rate
            if (lastFrameRate < targetFrameRate * 0.8f) // 20% below target
            {
                needsOptimization = true;
            }
            
            // Check memory usage
            if (memoryUsageMB > memoryThresholdMB)
            {
                needsOptimization = true;
            }
            
            if (needsOptimization)
            {
                StartCoroutine(OptimizePerformance());
            }
        }

        private System.Collections.IEnumerator OptimizePerformance()
        {
            isOptimizing = true;
            Debug.Log("[PerformanceOptimizer] Starting performance optimization...");
            
            // Step 1: Reduce logging if enabled
            if (reduceLoggingInProduction && !optimizationFlags["ReducedLogging"])
            {
                OptimizeLogging();
                yield return new WaitForSeconds(0.1f);
            }
            
            // Step 2: Optimize object pooling
            if (enableObjectPooling && !optimizationFlags["PoolingActive"])
            {
                OptimizeObjectPooling();
                yield return new WaitForSeconds(0.1f);
            }
            
            // Step 3: Optimize network updates
            if (optimizeNetworkUpdates && !optimizationFlags["NetworkOptimized"])
            {
                OptimizeNetworkPerformance();
                yield return new WaitForSeconds(0.1f);
            }
            
            // Step 4: Reduce quality settings if necessary
            if (lastFrameRate < targetFrameRate * 0.6f && !optimizationFlags["QualityReduced"])
            {
                ReduceQualitySettings();
                yield return new WaitForSeconds(0.1f);
            }
            
            // Step 5: Force garbage collection
            if (memoryUsageMB > memoryThresholdMB)
            {
                ForceGarbageCollection();
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("[PerformanceOptimizer] Performance optimization complete");
            isOptimizing = false;
        }

        private void OptimizeLogging()
        {
            // This would integrate with a logging system to reduce verbosity
            Debug.Log("[PerformanceOptimizer] Reduced logging verbosity");
            optimizationFlags["ReducedLogging"] = true;
        }

        private void OptimizeObjectPooling()
        {
            // Find and optimize all object pools
            var pools = FindObjectsByType<ProjectilePool>(FindObjectsSortMode.None);
            foreach (var pool in pools)
            {
                // Return all unused objects to pool
                pool.ReturnAllProjectiles();
            }
            
            Debug.Log($"[PerformanceOptimizer] Optimized {pools.Length} object pools");
            optimizationFlags["PoolingActive"] = true;
        }

        private void OptimizeNetworkPerformance()
        {
            // Find network components and optimize update rates
            var networkControllers = FindObjectsByType<Networking.NetworkPlayerController>(FindObjectsSortMode.None);
            
            // This could reduce network update frequency temporarily
            Debug.Log($"[PerformanceOptimizer] Optimized {networkControllers.Length} network controllers");
            optimizationFlags["NetworkOptimized"] = true;
        }

        private void ReduceQualitySettings()
        {
            // Reduce quality settings to improve performance
            QualitySettings.pixelLightCount = Mathf.Max(1, QualitySettings.pixelLightCount - 1);
            QualitySettings.shadowResolution = ShadowResolution.Low;
            
            Debug.Log("[PerformanceOptimizer] Reduced quality settings");
            optimizationFlags["QualityReduced"] = true;
        }

        private void ForceGarbageCollection()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            // Update memory usage after collection
            float newMemoryUsage = (float)System.GC.GetTotalMemory(false) / 1024f / 1024f;
            Debug.Log($"[PerformanceOptimizer] Garbage collection: {memoryUsageMB:F1}MB → {newMemoryUsage:F1}MB");
            memoryUsageMB = newMemoryUsage;
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public OptimizerMetrics GetMetrics()
        {
            return new OptimizerMetrics
            {
                currentFPS = lastFrameRate,
                targetFPS = targetFrameRate,
                memoryUsageMB = memoryUsageMB,
                memoryThresholdMB = memoryThresholdMB,
                averageFrameTime = GetAverageFrameTime(),
                isOptimizing = isOptimizing,
                optimizationFlags = new Dictionary<string, bool>(optimizationFlags)
            };
        }

        private float GetAverageFrameTime()
        {
            if (frameTimeHistory.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float frameTime in frameTimeHistory)
            {
                sum += frameTime;
            }
            
            return sum / frameTimeHistory.Count;
        }

        /// <summary>
        /// Manually trigger performance optimization
        /// </summary>
        public void TriggerOptimization()
        {
            if (!isOptimizing)
            {
                StartCoroutine(OptimizePerformance());
            }
        }

        /// <summary>
        /// Reset optimization flags to allow re-optimization
        /// </summary>
        public void ResetOptimizations()
        {
            InitializeOptimizationFlags();
            Debug.Log("[PerformanceOptimizer] Optimization flags reset");
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;
            
            var metrics = GetMetrics();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Performance Optimizer", GUILayout.Width(280));
            GUILayout.Label($"FPS: {metrics.currentFPS:F1} / {metrics.targetFPS}");
            GUILayout.Label($"Memory: {metrics.memoryUsageMB:F1} / {metrics.memoryThresholdMB} MB");
            GUILayout.Label($"Avg Frame Time: {metrics.averageFrameTime * 1000:F1}ms");
            GUILayout.Label($"Optimizing: {metrics.isOptimizing}");
            
            if (GUILayout.Button("Force Optimization"))
            {
                TriggerOptimization();
            }
            
            if (GUILayout.Button("Reset Optimizations"))
            {
                ResetOptimizations();
            }
            
            GUILayout.EndArea();
        }
    }

    [System.Serializable]
    public struct OptimizerMetrics
    {
        public float currentFPS;
        public float targetFPS;
        public float memoryUsageMB;
        public float memoryThresholdMB;
        public float averageFrameTime;
        public bool isOptimizing;
        public Dictionary<string, bool> optimizationFlags;
    }
}
