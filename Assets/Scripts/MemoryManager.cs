using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;

namespace MOBA
{
    /// <summary>
    /// Memory management system for MOBA game optimization
    /// Implements intelligent memory cleanup and monitoring
    /// Based on Game Programming Patterns memory considerations
    /// </summary>
    public class MemoryManager : MonoBehaviour
    {
        [Header("Memory Settings")]
        [SerializeField] private bool enableMemoryManagement = true;
        [SerializeField] private float cleanupInterval = 30f;
        [SerializeField] private int targetMemoryMB = 512;
        [SerializeField] private float memoryThresholdPercent = 0.8f;

        [Header("Asset Management")]
        [SerializeField] private bool unloadUnusedAssets = true;
        [SerializeField] private float assetUnloadDelay = 60f;

        [Header("Pool Management")]
        [SerializeField] private bool autoReturnToPools = true;
        [SerializeField] private float poolCleanupInterval = 10f;

        // Memory tracking
        private float lastCleanupTime;
        private float lastAssetUnloadTime;
        private float lastPoolCleanupTime;
        private Dictionary<string, MemoryPool> memoryPools = new();

        // Asset references for cleanup
        private List<AsyncOperation> pendingOperations = new();
        private HashSet<Object> trackedAssets = new();

        private void Awake()
        {
            InitializeMemoryPools();
        }

        private void InitializeMemoryPools()
        {
            // Register existing pools
            var projectilePool = FindAnyObjectByType<ProjectilePool>();
            if (projectilePool != null)
            {
                memoryPools["Projectiles"] = new MemoryPool("Projectiles", projectilePool);
            }

            var unitFactory = FindAnyObjectByType<UnitFactory>();
            if (unitFactory != null)
            {
                memoryPools["Units"] = new MemoryPool("Units", unitFactory);
            }
        }

        private void Update()
        {
            if (!enableMemoryManagement) return;

            float currentTime = Time.time;

            // Periodic memory cleanup
            if (currentTime - lastCleanupTime >= cleanupInterval)
            {
                PerformMemoryCleanup();
                lastCleanupTime = currentTime;
            }

            // Asset unloading
            if (unloadUnusedAssets && currentTime - lastAssetUnloadTime >= assetUnloadDelay)
            {
                UnloadUnusedAssets();
                lastAssetUnloadTime = currentTime;
            }

            // Pool cleanup
            if (autoReturnToPools && currentTime - lastPoolCleanupTime >= poolCleanupInterval)
            {
                CleanupPools();
                lastPoolCleanupTime = currentTime;
            }

            // Monitor memory usage
            MonitorMemoryUsage();
        }

        private void PerformMemoryCleanup()
        {
            // Force garbage collection if memory usage is high
            long memoryUsage = Profiler.GetTotalAllocatedMemoryLong();
            float memoryMB = memoryUsage / (1024f * 1024f);

            if (memoryMB > targetMemoryMB * memoryThresholdPercent)
            {
                Debug.Log($"[MemoryManager] High memory usage detected: {memoryMB:F1}MB. Triggering cleanup.");

                // Force GC
                System.GC.Collect();
                Resources.UnloadUnusedAssets();

                Debug.Log($"[MemoryManager] Cleanup completed. Memory after cleanup: {Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f):F1}MB");
            }
        }

        private void UnloadUnusedAssets()
        {
            if (pendingOperations.Count > 0)
            {
                // Wait for pending operations to complete
                pendingOperations.RemoveAll(op => op.isDone);
                return;
            }

            var operation = Resources.UnloadUnusedAssets();
            pendingOperations.Add(operation);

            Debug.Log("[MemoryManager] Unloading unused assets...");
        }

        private void CleanupPools()
        {
            foreach (var pool in memoryPools.Values)
            {
                pool.Cleanup();
            }
        }

        private void MonitorMemoryUsage()
        {
            long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
            long monoMemory = Profiler.GetMonoHeapSizeLong();
            long gfxMemory = Profiler.GetAllocatedMemoryForGraphicsDriver();

            float totalMB = totalMemory / (1024f * 1024f);
            float monoMB = monoMemory / (1024f * 1024f);
            float gfxMB = gfxMemory / (1024f * 1024f);

            // Log warnings for high memory usage
            if (totalMB > targetMemoryMB)
            {
                Debug.LogWarning($"[MemoryManager] Total memory usage: {totalMB:F1}MB (target: {targetMemoryMB}MB)");
            }

            if (monoMB > targetMemoryMB * 0.6f)
            {
                Debug.LogWarning($"[MemoryManager] Mono heap size: {monoMB:F1}MB");
            }
        }

        /// <summary>
        /// Registers an object for memory tracking
        /// </summary>
        public void TrackObject(Object obj, string category = "General")
        {
            trackedAssets.Add(obj);

            if (!memoryPools.ContainsKey(category))
            {
                memoryPools[category] = new MemoryPool(category, null);
            }

            memoryPools[category].TrackObject(obj);
        }

        /// <summary>
        /// Unregisters an object from memory tracking
        /// </summary>
        public void UntrackObject(Object obj)
        {
            trackedAssets.Remove(obj);

            foreach (var pool in memoryPools.Values)
            {
                pool.UntrackObject(obj);
            }
        }

        /// <summary>
        /// Gets memory usage statistics
        /// </summary>
        public Dictionary<string, MemoryStats> GetMemoryStats()
        {
            var stats = new Dictionary<string, MemoryStats>();

            foreach (var kvp in memoryPools)
            {
                stats[kvp.Key] = kvp.Value.GetStats();
            }

            // Add global stats
            stats["Global"] = new MemoryStats
            {
                TotalMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                MonoHeapMB = Profiler.GetMonoHeapSizeLong() / (1024f * 1024f),
                GfxMemoryMB = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f),
                TrackedObjects = trackedAssets.Count
            };

            return stats;
        }

        /// <summary>
        /// Forces immediate memory cleanup
        /// </summary>
        public void ForceCleanup()
        {
            Debug.Log("[MemoryManager] Forcing immediate memory cleanup...");

            System.GC.Collect();
            Resources.UnloadUnusedAssets();

            foreach (var pool in memoryPools.Values)
            {
                pool.ForceCleanup();
            }

            Debug.Log("[MemoryManager] Forced cleanup completed.");
        }

        /// <summary>
        /// Registers a custom memory pool
        /// </summary>
        public void RegisterPool(string name, System.Action cleanupAction)
        {
            memoryPools[name] = new MemoryPool(name, null, cleanupAction);
        }

        private void OnGUI()
        {
            if (!enableMemoryManagement || !Application.isEditor) return;

            GUI.Label(new Rect(10, 200, 300, 20), "=== Memory Manager ===");

            int y = 220;
            var stats = GetMemoryStats();

            foreach (var kvp in stats)
            {
                var stat = kvp.Value;
                GUI.Label(new Rect(10, y, 400, 20),
                    $"{kvp.Key}: {stat.TotalMemoryMB:F1}MB, Objects: {stat.TrackedObjects}");
                y += 20;
            }
        }

        private void OnApplicationQuit()
        {
            // Final cleanup
            ForceCleanup();
        }

        /// <summary>
        /// Memory pool data structure
        /// </summary>
        private class MemoryPool
        {
            public string Name { get; }
            private System.Action cleanupAction;
            private HashSet<Object> trackedObjects = new();
            private object poolReference; // Weak reference to actual pool

            public MemoryPool(string name, object poolRef, System.Action cleanup = null)
            {
                Name = name;
                poolReference = poolRef;
                cleanupAction = cleanup;
            }

            public void TrackObject(Object obj)
            {
                trackedObjects.Add(obj);
            }

            public void UntrackObject(Object obj)
            {
                trackedObjects.Remove(obj);
            }

            public void Cleanup()
            {
                cleanupAction?.Invoke();

                // Clean up destroyed objects
                trackedObjects.RemoveWhere(obj => obj == null);
            }

            public void ForceCleanup()
            {
                Cleanup();

                // Additional forced cleanup for specific pool types
                if (poolReference is ProjectilePool projectilePool)
                {
                    projectilePool.ReturnAllProjectiles();
                }
                else if (poolReference is UnitFactory unitFactory)
                {
                    // Unit factory cleanup would be implemented here
                }
            }

            public MemoryStats GetStats()
            {
                return new MemoryStats
                {
                    TotalMemoryMB = 0f, // Would need more detailed tracking
                    MonoHeapMB = 0f,
                    GfxMemoryMB = 0f,
                    TrackedObjects = trackedObjects.Count
                };
            }
        }

        /// <summary>
        /// Memory statistics structure
        /// </summary>
        public struct MemoryStats
        {
            public float TotalMemoryMB;
            public float MonoHeapMB;
            public float GfxMemoryMB;
            public int TrackedObjects;
        }
    }

    /// <summary>
    /// Memory-aware object pool base class
    /// </summary>
    public class MemoryAwarePool<T> where T : Component
    {
        private readonly Queue<T> availableObjects = new();
        private readonly List<T> allObjects = new();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int maxSize;

        public MemoryAwarePool(T prefab, int maxSize = 100, Transform parent = null)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.parent = parent;
        }

        public T Get()
        {
            T obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (allObjects.Count < maxSize)
            {
                // Removed automatic Object.Instantiate to prevent automatic loading
                // Use GetWithInstance() method with manually provided instance instead
                Debug.LogError($"[MemoryAwarePool] Automatic instantiation disabled for {typeof(T).Name} - use GetWithInstance() instead");
                return null;
            }
            else
            {
                // Pool exhausted, return first available or null
                Debug.LogWarning($"[MemoryAwarePool] Pool exhausted for {typeof(T).Name}");
                return null;
            }

            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null || !allObjects.Contains(obj)) return;

            obj.gameObject.SetActive(false);
            availableObjects.Enqueue(obj);
        }

        public void ReturnAll()
        {
            foreach (var obj in allObjects)
            {
                if (obj.gameObject.activeSelf)
                {
                    Return(obj);
                }
            }
        }

        public int ActiveCount => allObjects.Count - availableObjects.Count;
        public int TotalCount => allObjects.Count;
        public int AvailableCount => availableObjects.Count;
    }
}