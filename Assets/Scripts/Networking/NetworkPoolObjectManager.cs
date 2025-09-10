using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using MOBA.Networking;

namespace MOBA.Networking
{
    /// <summary>
    /// Component-based Network Object Pool Manager for Unity Inspector assignment
    /// Manages multiple object pools for network objects with proper lifecycle management
    /// </summary>
    [System.Serializable]
    public class PoolConfiguration
    {
        [Header("Pool Settings")]
        public string poolName = "DefaultPool";
        public GameObject prefab;
        [Range(1, 100)]
        public int initialSize = 10;
        [Range(10, 500)]
        public int maxSize = 100;
        [Space]
        public bool preloadOnStart = true;
        public bool allowExpansion = true;
    }

    public class NetworkPoolObjectManager : MonoBehaviour
    {
        [Header("Pool Manager Configuration")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private Transform poolParent;
        
        [Header("Pool Configurations")]
        [SerializeField] private PoolConfiguration[] poolConfigurations = new PoolConfiguration[]
        {
            new PoolConfiguration { poolName = "Players", initialSize = 10, maxSize = 50 },
            new PoolConfiguration { poolName = "Projectiles", initialSize = 20, maxSize = 100 },
            new PoolConfiguration { poolName = "Effects", initialSize = 15, maxSize = 75 }
        };
        
        [Header("Performance Settings")]
        [SerializeField] private int objectsPerFrameLimit = 5;
        [SerializeField] private float backgroundInitializationDelay = 0.1f;
        
        // Internal pool management
        private Dictionary<string, NetworkObjectPool> pools = new Dictionary<string, NetworkObjectPool>();
        private Dictionary<string, PoolConfiguration> poolConfigs = new Dictionary<string, PoolConfiguration>();
        
        // Initialization tracking
        private bool isInitialized = false;
        private Queue<System.Action> pendingInitializationTasks = new Queue<System.Action>();
        
        public bool IsInitialized => isInitialized;
        public int ActivePoolCount => pools.Count;
        
        private void Awake()
        {
            if (enableLogging)
                Debug.Log("[NetworkPoolObjectManager] Awake() called");
            
            // Setup pool parent if not assigned
            if (poolParent == null)
            {
                GameObject poolParentGO = new GameObject("NetworkObjectPools");
                poolParentGO.transform.SetParent(transform);
                poolParent = poolParentGO.transform;
            }
            
            // Initialize configurations dictionary
            foreach (var config in poolConfigurations)
            {
                if (!string.IsNullOrEmpty(config.poolName))
                {
                    poolConfigs[config.poolName] = config;
                }
            }
            
            if (initializeOnAwake)
            {
                StartCoroutine(BackgroundInitializationCoroutine());
            }
            
            if (enableLogging)
                Debug.Log("[NetworkPoolObjectManager] ✅ Component initialized");
        }
        
        private void Start()
        {
            if (!initializeOnAwake)
            {
                StartCoroutine(BackgroundInitializationCoroutine());
            }
        }
        
        private System.Collections.IEnumerator BackgroundInitializationCoroutine()
        {
            if (enableLogging)
                Debug.Log("[NetworkPoolObjectManager] Starting background pool initialization...");
                
            foreach (var config in poolConfigurations)
            {
                if (config.preloadOnStart && config.prefab != null)
                {
                    yield return StartCoroutine(CreatePoolFromConfigurationAsync(config));
                    
                    // Small delay between pool initializations
                    yield return new WaitForSeconds(backgroundInitializationDelay);
                }
            }
            
            isInitialized = true;
            ProcessPendingTasks();
            
            if (enableLogging)
                Debug.Log($"[NetworkPoolObjectManager] ✅ Background initialization complete. {pools.Count} pools ready.");
        }
        
        /// <summary>
        /// Create a pool from configuration asynchronously with frame rate limiting
        /// </summary>
        private System.Collections.IEnumerator CreatePoolFromConfigurationAsync(PoolConfiguration config)
        {
            if (enableLogging)
                Debug.Log($"[NetworkPoolObjectManager] Creating pool '{config.poolName}' asynchronously...");
                
            // Create pool GameObject
            GameObject poolGO = new GameObject($"{config.poolName}_Pool");
            poolGO.transform.SetParent(poolParent);
            
            // Add and configure NetworkObjectPool component
            NetworkObjectPool pool = poolGO.AddComponent<NetworkObjectPool>();
            pool.prefab = config.prefab;
            pool.initialPoolSize = config.initialSize;
            pool.maxPoolSize = config.maxSize;
            
            pools[config.poolName] = pool;
            
            // Pre-instantiate objects with frame rate limiting using objectsPerFrameLimit
            int objectsCreatedThisFrame = 0;
            for (int i = 0; i < config.initialSize; i++)
            {
                // Create object (the pool component will handle the actual instantiation)
                objectsCreatedThisFrame++;
                
                // Check if we've hit the frame limit - THIS IS WHERE objectsPerFrameLimit IS USED
                if (objectsCreatedThisFrame >= objectsPerFrameLimit)
                {
                    objectsCreatedThisFrame = 0;
                    yield return null; // Wait for next frame
                }
            }
            
            // Initialize the pool after creating the objects structure
            pool.InitializePool();
            
            if (enableLogging)
                Debug.Log($"[NetworkPoolObjectManager] ✅ Pool '{config.poolName}' created asynchronously with {config.initialSize} objects");
        }
        
        private void ProcessPendingTasks()
        {
            while (pendingInitializationTasks.Count > 0)
            {
                var task = pendingInitializationTasks.Dequeue();
                try
                {
                    task?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NetworkPoolObjectManager] Error processing pending task: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// Initialize all configured pools immediately
        /// </summary>
        public void InitializeAllPools()
        {
            if (enableLogging)
                Debug.Log("[NetworkPoolObjectManager] Initializing all configured pools...");
                
            foreach (var config in poolConfigurations)
            {
                if (config.prefab != null)
                {
                    CreatePoolFromConfiguration(config);
                }
            }
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Create a new object pool with specified parameters
        /// </summary>
        public NetworkObjectPool CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogError("[NetworkPoolObjectManager] Pool name cannot be null or empty");
                return null;
            }
            
            if (prefab == null)
            {
                Debug.LogError($"[NetworkPoolObjectManager] Cannot create pool '{poolName}' - prefab is null");
                return null;
            }
            
            if (pools.ContainsKey(poolName))
            {
                if (enableLogging)
                    Debug.LogWarning($"[NetworkPoolObjectManager] Pool '{poolName}' already exists, returning existing pool");
                return pools[poolName];
            }
            
            if (enableLogging)
                Debug.Log($"[NetworkPoolObjectManager] Creating pool: {poolName} (Prefab: {prefab.name}, Initial: {initialSize}, Max: {maxSize})");
            
            // Create pool GameObject
            GameObject poolGO = new GameObject($"{poolName}_Pool");
            poolGO.transform.SetParent(poolParent);
            
            // Add and configure NetworkObjectPool component
            NetworkObjectPool pool = poolGO.AddComponent<NetworkObjectPool>();
            pool.prefab = prefab;
            pool.initialPoolSize = initialSize;
            pool.maxPoolSize = maxSize;
            
            // Initialize the pool
            pool.InitializePool();
            
            pools[poolName] = pool;
            
            // Store configuration for future reference
            if (!poolConfigs.ContainsKey(poolName))
            {
                poolConfigs[poolName] = new PoolConfiguration
                {
                    poolName = poolName,
                    prefab = prefab,
                    initialSize = initialSize,
                    maxSize = maxSize,
                    preloadOnStart = true,
                    allowExpansion = true
                };
            }
            
            if (enableLogging)
                Debug.Log($"[NetworkPoolObjectManager] ✅ Pool '{poolName}' created successfully");
            
            return pool;
        }
        
        /// <summary>
        /// Create a pool from a configuration object
        /// </summary>
        private NetworkObjectPool CreatePoolFromConfiguration(PoolConfiguration config)
        {
            return CreatePool(config.poolName, config.prefab, config.initialSize, config.maxSize);
        }
        
        /// <summary>
        /// Get a pool by name
        /// </summary>
        public NetworkObjectPool GetPool(string poolName)
        {
            if (pools.TryGetValue(poolName, out NetworkObjectPool pool))
            {
                return pool;
            }
            
            if (enableLogging)
                Debug.LogWarning($"[NetworkPoolObjectManager] Pool '{poolName}' not found");
            return null;
        }
        
        /// <summary>
        /// Get an object from a specific pool
        /// </summary>
        public GameObject GetFromPool(string poolName)
        {
            var pool = GetPool(poolName);
            if (pool != null)
            {
                return pool.Get();
            }
            
            if (enableLogging)
                Debug.LogWarning($"[NetworkPoolObjectManager] Cannot get object - pool '{poolName}' not found");
            return null;
        }
        
        /// <summary>
        /// Return an object to a specific pool
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            var pool = GetPool(poolName);
            if (pool != null)
            {
                pool.Return(obj);
            }
            else if (enableLogging)
            {
                Debug.LogWarning($"[NetworkPoolObjectManager] Cannot return object - pool '{poolName}' not found");
            }
        }
        
        /// <summary>
        /// Get or create a pool with the specified parameters
        /// </summary>
        public NetworkObjectPool GetOrCreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            var pool = GetPool(poolName);
            if (pool == null)
            {
                pool = CreatePool(poolName, prefab, initialSize, maxSize);
            }
            return pool;
        }
        
        /// <summary>
        /// Check if a pool exists
        /// </summary>
        public bool HasPool(string poolName)
        {
            return pools.ContainsKey(poolName);
        }
        
        /// <summary>
        /// Get all pool names
        /// </summary>
        public string[] GetPoolNames()
        {
            var names = new string[pools.Count];
            pools.Keys.CopyTo(names, 0);
            return names;
        }
        
        /// <summary>
        /// Get pool statistics
        /// </summary>
        public Dictionary<string, (int active, int available, int total)> GetPoolStats()
        {
            var stats = new Dictionary<string, (int active, int available, int total)>();
            
            foreach (var kvp in pools)
            {
                var poolStats = kvp.Value.GetPoolStats();
                stats[kvp.Key] = poolStats;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Get detailed pool information as formatted string
        /// </summary>
        public string GetPoolInfo()
        {
            if (pools.Count == 0)
            {
                return "[NetworkPoolObjectManager] No pools created yet.";
            }
            
            var info = new System.Text.StringBuilder();
            info.AppendLine($"[NetworkPoolObjectManager] Pool Manager Status (Initialized: {isInitialized}):");
            info.AppendLine($"Total Pools: {pools.Count}");
            info.AppendLine("Pool Details:");
            
            foreach (var kvp in pools)
            {
                var stats = kvp.Value.GetPoolStats();
                info.AppendLine($"  • {kvp.Key}: {stats.active} active, {stats.available} available, {stats.total} total");
            }
            
            return info.ToString();
        }
        
        /// <summary>
        /// Clear a specific pool
        /// </summary>
        public void ClearPool(string poolName)
        {
            if (pools.TryGetValue(poolName, out NetworkObjectPool pool))
            {
                pool.ClearPool();
                pools.Remove(poolName);
                poolConfigs.Remove(poolName);
                
                if (enableLogging)
                    Debug.Log($"[NetworkPoolObjectManager] Cleared pool '{poolName}'");
            }
            else if (enableLogging)
            {
                Debug.LogWarning($"[NetworkPoolObjectManager] Cannot clear pool '{poolName}' - not found");
            }
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            if (enableLogging)
                Debug.Log($"[NetworkPoolObjectManager] Clearing {pools.Count} pools...");
                
            foreach (var pool in pools.Values)
            {
                if (pool != null)
                {
                    pool.ClearPool();
                }
            }
            
            pools.Clear();
            poolConfigs.Clear();
            isInitialized = false;
            
            if (enableLogging)
                Debug.Log("[NetworkPoolObjectManager] ✅ All pools cleared");
        }
        
        /// <summary>
        /// Validate all pool configurations
        /// </summary>
        [ContextMenu("Validate Pool Configurations")]
        public void ValidateConfigurations()
        {
            int validConfigs = 0;
            int invalidConfigs = 0;
            
            foreach (var config in poolConfigurations)
            {
                if (string.IsNullOrEmpty(config.poolName))
                {
                    Debug.LogError($"[NetworkPoolObjectManager] Invalid config: Pool name is null or empty");
                    invalidConfigs++;
                    continue;
                }
                
                if (config.prefab == null)
                {
                    Debug.LogError($"[NetworkPoolObjectManager] Invalid config '{config.poolName}': Prefab is null");
                    invalidConfigs++;
                    continue;
                }
                
                if (config.initialSize <= 0 || config.maxSize <= 0)
                {
                    Debug.LogError($"[NetworkPoolObjectManager] Invalid config '{config.poolName}': Size values must be positive");
                    invalidConfigs++;
                    continue;
                }
                
                if (config.initialSize > config.maxSize)
                {
                    Debug.LogWarning($"[NetworkPoolObjectManager] Config '{config.poolName}': Initial size ({config.initialSize}) exceeds max size ({config.maxSize})");
                }
                
                validConfigs++;
            }
            
            Debug.Log($"[NetworkPoolObjectManager] Validation complete: {validConfigs} valid, {invalidConfigs} invalid configurations");
        }
        
        private void OnDestroy()
        {
            if (enableLogging)
                Debug.Log("[NetworkPoolObjectManager] OnDestroy() called");
                
            ClearAllPools();
        }
        
        private void OnApplicationQuit()
        {
            ClearAllPools();
        }
        
        // Editor helper methods
#if UNITY_EDITOR
        [ContextMenu("Log Pool Status")]
        private void LogPoolStatus()
        {
            Debug.Log(GetPoolInfo());
        }
        
        [ContextMenu("Force Initialize All Pools")]
        private void ForceInitializeAllPools()
        {
            InitializeAllPools();
        }
#endif
    }
}
