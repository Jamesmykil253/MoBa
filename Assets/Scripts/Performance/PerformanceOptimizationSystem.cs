using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace MOBA.Performance
{
    /// <summary>
    /// Advanced performance optimization and monitoring system for Unity MOBA project
    /// Implements automatic quality adjustment, object pooling, and performance monitoring
    /// Based on Game Programming Patterns and Clean Code principles
    /// Updated to fix compilation warnings
    /// </summary>
    public class PerformanceOptimizationSystem : MonoBehaviour
    {
        [Header("Performance Targets")]
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float minimumFrameRate = 30f;
        [SerializeField] private float maxMemoryUsageMB = 400f;
        [SerializeField] private float maxCpuUsagePercent = 70f;
        
        [Header("Quality Settings")]
        [SerializeField] private bool enableAdaptiveQuality = true;
        [SerializeField] private bool enableLODOptimization = true;
        [SerializeField] private bool enableBatching = true;
        [SerializeField] private bool enableOcclusion = true;
        
        [Header("Object Pooling")]
        [SerializeField] private int maxPooledObjects = 1000;
        [SerializeField] private int damageNumberPoolSize = 50;
        [SerializeField] private int projectilePoolSize = 200;
        [SerializeField] private int particlePoolSize = 100;
        
        [Header("Performance Monitoring")]
        [SerializeField] private float monitoringInterval = 1f;
        [SerializeField] private bool logPerformanceMetrics = true;
        [SerializeField] private bool enableAutoOptimization = true;
        
        // Performance metrics
        private PerformanceMetrics currentMetrics;
        private Queue<PerformanceMetrics> metricsHistory = new Queue<PerformanceMetrics>();
        private const int maxMetricsHistory = 60; // 1 minute at 1Hz
        
        // Quality level management
        private QualityLevel currentQualityLevel = QualityLevel.High;
        private float lastQualityAdjustment = 0f;
        private const float qualityAdjustmentCooldown = 5f;
        
        // Object pools
        private Dictionary<string, ObjectPool> objectPools = new Dictionary<string, ObjectPool>();
        
        // LOD system
        private List<LODGroup> managedLODGroups = new List<LODGroup>();
        private float lodBias = 1f;
        
        // Batching system
        private Dictionary<string, List<Renderer>> batchableRenderers = new Dictionary<string, List<Renderer>>();
        
        // Job system for performance calculations
        private JobHandle performanceJobHandle;
        private NativeArray<float> frameTimeBuffer;
        private bool isJobScheduled = false;
        
        // Event system
        public static event System.Action<PerformanceMetrics> OnPerformanceMetricsUpdated;
        public static event System.Action<QualityLevel> OnQualityLevelChanged;
        public static event System.Action<string> OnPerformanceWarning;

        private void Awake()
        {
            InitializePerformanceSystem();
            InitializeObjectPools();
            InitializeLODSystem();
            InitializeBatchingSystem();
        }

        private void Start()
        {
            // Start performance monitoring
            InvokeRepeating(nameof(UpdatePerformanceMetrics), monitoringInterval, monitoringInterval);
            
            // Apply initial quality settings
            ApplyQualitySettings();
        }

        private void Update()
        {
            // Complete performance calculation jobs
            if (isJobScheduled && performanceJobHandle.IsCompleted)
            {
                CompletePerformanceJob();
            }
            
            // Update LOD system
            if (enableLODOptimization)
            {
                UpdateLODSystem();
            }
            
            // Update batching system
            if (enableBatching)
            {
                UpdateBatchingSystem();
            }
        }

        private void OnDestroy()
        {
            // Complete any pending jobs
            if (isJobScheduled)
            {
                performanceJobHandle.Complete();
            }
            
            // Dispose native arrays
            if (frameTimeBuffer.IsCreated)
            {
                frameTimeBuffer.Dispose();
            }
            
            // Cleanup object pools
            CleanupObjectPools();
        }

        /// <summary>
        /// Initialize the performance optimization system
        /// </summary>
        private void InitializePerformanceSystem()
        {
            // Initialize frame time buffer for job system
            frameTimeBuffer = new NativeArray<float>(60, Allocator.Persistent);
            
            // Set target frame rate
            Application.targetFrameRate = (int)targetFrameRate;
            
            // Initialize performance metrics
            currentMetrics = new PerformanceMetrics();
            
            Debug.Log("[PerformanceOptimizationSystem] System initialized");
        }

        /// <summary>
        /// Initialize object pools for performance optimization
        /// </summary>
        private void InitializeObjectPools()
        {
            // Create damage number pool
            CreateObjectPool("DamageNumbers", "UI/DamageNumber", damageNumberPoolSize);
            
            // Create projectile pool
            CreateObjectPool("Projectiles", "Projectiles/BasicProjectile", projectilePoolSize);
            
            // Create particle pool
            CreateObjectPool("Particles", "Particles/BasicParticle", particlePoolSize);
            
            Debug.Log($"[PerformanceOptimizationSystem] Initialized {objectPools.Count} object pools");
        }

        /// <summary>
        /// Initialize LOD (Level of Detail) optimization system
        /// </summary>
        private void InitializeLODSystem()
        {
            if (!enableLODOptimization) return;
            
            // Find all LOD groups in the scene
            LODGroup[] lodGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            managedLODGroups.AddRange(lodGroups);
            
            // Set initial LOD bias
            QualitySettings.lodBias = lodBias;
            
            Debug.Log($"[PerformanceOptimizationSystem] Managing {managedLODGroups.Count} LOD groups");
        }

        /// <summary>
        /// Get estimated draw calls (fallback for UnityStats)
        /// </summary>
        private int GetDrawCalls()
        {
            // Estimate based on visible renderers
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            return renderers?.Length ?? 0;
        }

        /// <summary>
        /// Get estimated triangle count (fallback for UnityStats)
        /// </summary>
        private int GetTriangleCount()
        {
            // Estimate based on mesh renderers
            var meshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int triangles = 0;
            foreach (var renderer in meshRenderers)
            {
                if (renderer.gameObject.activeInHierarchy)
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter?.mesh != null)
                    {
                        triangles += meshFilter.mesh.triangles.Length / 3;
                    }
                }
            }
            return triangles;
        }

        /// <summary>
        /// Get estimated vertex count (fallback for UnityStats)
        /// </summary>
        private int GetVertexCount()
        {
            // Estimate based on mesh renderers
            var meshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int vertices = 0;
            foreach (var renderer in meshRenderers)
            {
                if (renderer.gameObject.activeInHierarchy)
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter?.mesh != null)
                    {
                        vertices += meshFilter.mesh.vertexCount;
                    }
                }
            }
            return vertices;
        }

        /// <summary>
        /// Initialize dynamic batching system
        /// </summary>
        private void InitializeBatchingSystem()
        {
            if (!enableBatching) return;
            
            // Find all renderers that can be batched
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in renderers)
            {
                if (CanBeBatched(renderer))
                {
                    string materialKey = GetMaterialKey(renderer);
                    if (!batchableRenderers.ContainsKey(materialKey))
                    {
                        batchableRenderers[materialKey] = new List<Renderer>();
                    }
                    batchableRenderers[materialKey].Add(renderer);
                }
            }
            
            Debug.Log($"[PerformanceOptimizationSystem] Found {batchableRenderers.Count} batchable material groups");
        }

        /// <summary>
        /// Update performance metrics and trigger optimizations
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            // Schedule performance calculation job
            if (!isJobScheduled)
            {
                SchedulePerformanceJob();
            }
            
            // Calculate current metrics
            CalculatePerformanceMetrics();
            
            // Add to history
            metricsHistory.Enqueue(currentMetrics);
            if (metricsHistory.Count > maxMetricsHistory)
            {
                metricsHistory.Dequeue();
            }
            
            // Log metrics if enabled
            if (logPerformanceMetrics)
            {
                LogPerformanceMetrics();
            }
            
            // Trigger automatic optimization
            if (enableAutoOptimization)
            {
                OptimizePerformance();
            }
            
            // Fire performance update event
            OnPerformanceMetricsUpdated?.Invoke(currentMetrics);
        }

        /// <summary>
        /// Calculate current performance metrics
        /// </summary>
        private void CalculatePerformanceMetrics()
        {
            currentMetrics = new PerformanceMetrics
            {
                timestamp = Time.time,
                frameRate = 1f / Time.deltaTime,
                memoryUsage = (float)System.GC.GetTotalMemory(false) / (1024 * 1024), // MB
                drawCalls = GetDrawCalls(),
                triangles = GetTriangleCount(),
                vertices = GetVertexCount(),
                cpuUsage = CalculateCPUUsage(),
                gpuUsage = CalculateGPUUsage(),
                qualityLevel = currentQualityLevel
            };
        }

        /// <summary>
        /// Schedule performance calculation job for heavy computations
        /// </summary>
        private void SchedulePerformanceJob()
        {
            var job = new PerformanceCalculationJob
            {
                frameTimeBuffer = frameTimeBuffer,
                deltaTime = Time.deltaTime
            };
            
            performanceJobHandle = job.Schedule();
            isJobScheduled = true;
        }

        /// <summary>
        /// Complete performance calculation job
        /// </summary>
        private void CompletePerformanceJob()
        {
            performanceJobHandle.Complete();
            isJobScheduled = false;
            
            // Process job results here if needed
        }

        /// <summary>
        /// Optimize performance based on current metrics
        /// </summary>
        private void OptimizePerformance()
        {
            bool needsOptimization = false;
            string optimizationReason = "";
            
            // Check frame rate
            if (currentMetrics.frameRate < minimumFrameRate)
            {
                needsOptimization = true;
                optimizationReason = "Low frame rate";
            }
            
            // Check memory usage
            if (currentMetrics.memoryUsage > maxMemoryUsageMB)
            {
                needsOptimization = true;
                optimizationReason = "High memory usage";
                OnPerformanceWarning?.Invoke($"Memory usage exceeded {maxMemoryUsageMB}MB: {currentMetrics.memoryUsage:F1}MB");
            }
            
            // Check CPU usage
            if (currentMetrics.cpuUsage > maxCpuUsagePercent)
            {
                needsOptimization = true;
                optimizationReason = "High CPU usage";
                OnPerformanceWarning?.Invoke($"CPU usage exceeded {maxCpuUsagePercent}%: {currentMetrics.cpuUsage:F1}%");
            }
            
            // Only adjust quality if adaptive quality is enabled
            if (enableAdaptiveQuality && needsOptimization && Time.time - lastQualityAdjustment > qualityAdjustmentCooldown)
            {
                ReduceQualityLevel(optimizationReason);
            }
            else if (enableAdaptiveQuality && currentMetrics.frameRate > targetFrameRate && 
                     currentMetrics.memoryUsage < maxMemoryUsageMB * 0.7f &&
                     currentQualityLevel < QualityLevel.Ultra)
            {
                IncreaseQualityLevel("Good performance");
            }
        }

        /// <summary>
        /// Reduce quality level for better performance
        /// </summary>
        private void ReduceQualityLevel(string reason)
        {
            QualityLevel newLevel = currentQualityLevel;
            
            switch (currentQualityLevel)
            {
                case QualityLevel.Ultra:
                    newLevel = QualityLevel.High;
                    break;
                case QualityLevel.High:
                    newLevel = QualityLevel.Medium;
                    break;
                case QualityLevel.Medium:
                    newLevel = QualityLevel.Low;
                    break;
                case QualityLevel.Low:
                    // Already at lowest quality
                    return;
            }
            
            SetQualityLevel(newLevel, reason);
        }

        /// <summary>
        /// Increase quality level when performance allows
        /// </summary>
        private void IncreaseQualityLevel(string reason)
        {
            QualityLevel newLevel = currentQualityLevel;
            
            switch (currentQualityLevel)
            {
                case QualityLevel.Low:
                    newLevel = QualityLevel.Medium;
                    break;
                case QualityLevel.Medium:
                    newLevel = QualityLevel.High;
                    break;
                case QualityLevel.High:
                    newLevel = QualityLevel.Ultra;
                    break;
                case QualityLevel.Ultra:
                    // Already at highest quality
                    return;
            }
            
            SetQualityLevel(newLevel, reason);
        }

        /// <summary>
        /// Set quality level and apply settings
        /// </summary>
        private void SetQualityLevel(QualityLevel level, string reason)
        {
            currentQualityLevel = level;
            lastQualityAdjustment = Time.time;
            
            ApplyQualitySettings();
            
            Debug.Log($"[PerformanceOptimizationSystem] Quality level changed to {level} ({reason})");
            OnQualityLevelChanged?.Invoke(level);
        }

        /// <summary>
        /// Apply quality settings based on current level
        /// </summary>
        private void ApplyQualitySettings()
        {
            // Apply occlusion culling setting
            Camera.main.useOcclusionCulling = enableOcclusion;
            
            switch (currentQualityLevel)
            {
                case QualityLevel.Low:
                    QualitySettings.SetQualityLevel(0);
                    lodBias = 0.5f;
                    QualitySettings.shadowDistance = 20f;
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                    
                case QualityLevel.Medium:
                    QualitySettings.SetQualityLevel(2);
                    lodBias = 0.7f;
                    QualitySettings.shadowDistance = 50f;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    break;
                    
                case QualityLevel.High:
                    QualitySettings.SetQualityLevel(4);
                    lodBias = 1f;
                    QualitySettings.shadowDistance = 100f;
                    QualitySettings.shadows = ShadowQuality.All;
                    break;
                    
                case QualityLevel.Ultra:
                    QualitySettings.SetQualityLevel(5);
                    lodBias = 1.5f;
                    QualitySettings.shadowDistance = 150f;
                    QualitySettings.shadows = ShadowQuality.All;
                    break;
            }
            
            QualitySettings.lodBias = lodBias;
        }

        /// <summary>
        /// Update LOD system based on performance
        /// </summary>
        private void UpdateLODSystem()
        {
            // Adjust LOD bias based on performance
            foreach (var lodGroup in managedLODGroups)
            {
                if (lodGroup != null)
                {
                    // Adjust LOD distances based on current performance
                    AdjustLODDistances(lodGroup);
                }
            }
        }

        /// <summary>
        /// Update batching system
        /// </summary>
        private void UpdateBatchingSystem()
        {
            // Enable/disable batching based on performance
            foreach (var rendererGroup in batchableRenderers.Values)
            {
                foreach (var renderer in rendererGroup)
                {
                    if (renderer != null)
                    {
                        // Apply batching optimizations
                        OptimizeRenderer(renderer);
                    }
                }
            }
        }

        /// <summary>
        /// Create an object pool with specified parameters
        /// </summary>
        private void CreateObjectPool(string poolName, string prefabPath, int poolSize)
        {
            // Respect maxPooledObjects limit
            poolSize = Mathf.Min(poolSize, maxPooledObjects);
            
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[PerformanceOptimizationSystem] Could not load prefab: {prefabPath}");
                return;
            }
            
            ObjectPool pool = new ObjectPool(prefab, poolSize, transform);
            objectPools[poolName] = pool;
        }

        /// <summary>
        /// Get object from pool
        /// </summary>
        public GameObject GetPooledObject(string poolName)
        {
            if (objectPools.TryGetValue(poolName, out ObjectPool pool))
            {
                return pool.GetObject();
            }
            
            Debug.LogWarning($"[PerformanceOptimizationSystem] Pool not found: {poolName}");
            return null;
        }

        /// <summary>
        /// Return object to pool
        /// </summary>
        public void ReturnPooledObject(string poolName, GameObject obj)
        {
            if (objectPools.TryGetValue(poolName, out ObjectPool pool))
            {
                pool.ReturnObject(obj);
            }
        }

        /// <summary>
        /// Utility methods
        /// </summary>
        private float CalculateCPUUsage()
        {
            // Simplified CPU usage calculation
            return Mathf.Clamp01(1f / Time.deltaTime / targetFrameRate) * 100f;
        }

        private float CalculateGPUUsage()
        {
            // Placeholder - actual GPU usage would require platform-specific implementation
            return Mathf.Clamp01((float)GetDrawCalls() / 1000f) * 100f;
        }

        private void AdjustLODDistances(LODGroup lodGroup)
        {
            LOD[] lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                lods[i].screenRelativeTransitionHeight *= lodBias;
            }
            lodGroup.SetLODs(lods);
        }

        private bool CanBeBatched(Renderer renderer)
        {
            // Check if renderer can be batched
            return renderer.sharedMaterial != null && 
                   renderer.gameObject.isStatic &&
                   renderer is MeshRenderer;
        }

        private string GetMaterialKey(Renderer renderer)
        {
            return renderer.sharedMaterial?.name ?? "default";
        }

        private void OptimizeRenderer(Renderer renderer)
        {
            // Apply renderer optimizations based on quality level
            switch (currentQualityLevel)
            {
                case QualityLevel.Low:
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    break;
                case QualityLevel.Medium:
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    break;
            }
        }

        private void LogPerformanceMetrics()
        {
            Debug.Log($"[Performance] FPS: {currentMetrics.frameRate:F1}, " +
                     $"Memory: {currentMetrics.memoryUsage:F1}MB, " +
                     $"Draw Calls: {currentMetrics.drawCalls}, " +
                     $"Quality: {currentQualityLevel}");
        }

        private void CleanupObjectPools()
        {
            foreach (var pool in objectPools.Values)
            {
                pool.Cleanup();
            }
            objectPools.Clear();
        }

        /// <summary>
        /// Public API
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics() => currentMetrics;
        
        public QualityLevel GetCurrentQualityLevel() => currentQualityLevel;
        
        public void ForceQualityLevel(QualityLevel level)
        {
            SetQualityLevel(level, "Manual override");
        }
        
        public PerformanceMetrics[] GetMetricsHistory()
        {
            return metricsHistory.ToArray();
        }
    }

    /// <summary>
    /// Performance calculation job for expensive operations
    /// </summary>
    public struct PerformanceCalculationJob : IJob
    {
        public NativeArray<float> frameTimeBuffer;
        public float deltaTime;
        
        public void Execute()
        {
            // Perform expensive performance calculations here
            // This runs on a worker thread to avoid blocking the main thread
            for (int i = frameTimeBuffer.Length - 1; i > 0; i--)
            {
                frameTimeBuffer[i] = frameTimeBuffer[i - 1];
            }
            frameTimeBuffer[0] = deltaTime;
        }
    }

    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    [System.Serializable]
    public struct PerformanceMetrics
    {
        public float timestamp;
        public float frameRate;
        public float memoryUsage;
        public int drawCalls;
        public int triangles;
        public int vertices;
        public float cpuUsage;
        public float gpuUsage;
        public QualityLevel qualityLevel;
    }

    /// <summary>
    /// Quality level enumeration
    /// </summary>
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// Simple object pool implementation
    /// </summary>
    public class ObjectPool
    {
        private Queue<GameObject> pool = new Queue<GameObject>();
        private GameObject prefab;
        private Transform parent;
        private int maxSize;

        public ObjectPool(GameObject prefab, int maxSize, Transform parent)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.parent = parent;
            
            // Pre-populate pool
            for (int i = 0; i < maxSize; i++)
            {
                GameObject obj = Object.Instantiate(prefab, parent);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public GameObject GetObject()
        {
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            
            // Pool exhausted, create new object
            return Object.Instantiate(prefab, parent);
        }

        public void ReturnObject(GameObject obj)
        {
            if (pool.Count < maxSize)
            {
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
        }

        public void Cleanup()
        {
            while (pool.Count > 0)
            {
                Object.Destroy(pool.Dequeue());
            }
        }
    }
}
