using System.Collections.Generic;
using UnityEngine;
using MOBA.Networking;

namespace MOBA.Networking
{
    /// <summary>
    /// Component-based Network Object Pool Manager that can be assigned in the inspector
    /// </summary>
    public class NetworkObjectPoolManagerComponent : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private bool enableLogging = true;
        
        [Header("Prefab Pools")]
        [SerializeField] private PoolConfiguration[] poolConfigurations;
        
        [System.Serializable]
        public class PoolConfiguration
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 100;
        }
        
        private Dictionary<string, NetworkObjectPool> pools = new Dictionary<string, NetworkObjectPool>();
        private static NetworkObjectPoolManagerComponent _instance;
        
        public static NetworkObjectPoolManagerComponent Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<NetworkObjectPoolManagerComponent>();
                    if (_instance == null)
                    {
                        Debug.LogError("[NetworkObjectPoolManagerComponent] No instance found in scene! Please add the component to a GameObject.");
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (enableLogging)
                Debug.Log("[NetworkObjectPoolManagerComponent] Awake() called");
                
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                if (enableLogging)
                    Debug.Log("[NetworkObjectPoolManagerComponent] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            if (enableLogging)
                Debug.Log("[NetworkObjectPoolManagerComponent] ✅ Component instance established");
        }
        
        private void Start()
        {
            // Initialize configured pools
            if (poolConfigurations != null)
            {
                foreach (var config in poolConfigurations)
                {
                    if (config.prefab != null && !string.IsNullOrEmpty(config.poolName))
                    {
                        CreatePool(config.poolName, config.prefab, config.initialSize, config.maxSize);
                    }
                }
            }
        }
        
        /// <summary>
        /// Create a new object pool
        /// </summary>
        public NetworkObjectPool CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            if (enableLogging)
                Debug.Log($"[NetworkObjectPoolManagerComponent] CreatePool() called - Name: {poolName}, Prefab: {prefab?.name}, Initial: {initialSize}, Max: {maxSize}");
            
            if (prefab == null)
            {
                Debug.LogError($"[NetworkObjectPoolManagerComponent] Cannot create pool '{poolName}' - prefab is null");
                return null;
            }
            
            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[NetworkObjectPoolManagerComponent] Pool '{poolName}' already exists");
                return pools[poolName];
            }
            
            // Create pool GameObject as child
            if (enableLogging)
                Debug.Log($"[NetworkObjectPoolManagerComponent] Creating pool GameObject: {poolName}_Pool");
            GameObject poolGO = new GameObject($"{poolName}_Pool");
            poolGO.transform.SetParent(transform);
            
            // Add pool component
            if (enableLogging)
                Debug.Log($"[NetworkObjectPoolManagerComponent] Adding NetworkObjectPool component to {poolName}_Pool");
            NetworkObjectPool pool = poolGO.AddComponent<NetworkObjectPool>();
            
            // Configure pool
            if (enableLogging)
                Debug.Log($"[NetworkObjectPoolManagerComponent] Assigning prefab {prefab.name} to pool component");
            pool.AssignPrefab(prefab);
            
            // Initialize manually to avoid timing issues
            if (enableLogging)
                Debug.Log($"[NetworkObjectPoolManagerComponent] Manually initializing pool to avoid Awake timing issue");
            pool.InitializePool();
            
            pools[poolName] = pool;
            
            if (enableLogging)
                Debug.Log($"[NetworkObjectPoolManagerComponent] ✅ Created pool {poolName} with {initialSize} objects");
            
            return pool;
        }
        
        /// <summary>
        /// Get a pool by name
        /// </summary>
        public NetworkObjectPool GetPool(string poolName)
        {
            pools.TryGetValue(poolName, out NetworkObjectPool pool);
            if (pool == null && enableLogging)
            {
                Debug.LogWarning($"[NetworkObjectPoolManagerComponent] Pool '{poolName}' not found");
            }
            return pool;
        }
        
        /// <summary>
        /// Get an object from a specific pool
        /// </summary>
        public GameObject GetFromPool(string poolName)
        {
            var pool = GetPool(poolName);
            return pool?.Get();
        }
        
        /// <summary>
        /// Return an object to a specific pool
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            var pool = GetPool(poolName);
            pool?.Return(obj);
        }
        
        /// <summary>
        /// Get all active pools
        /// </summary>
        public Dictionary<string, NetworkObjectPool> GetAllPools()
        {
            return new Dictionary<string, NetworkObjectPool>(pools);
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            if (enableLogging)
                Debug.Log("[NetworkObjectPoolManagerComponent] Clearing all pools");
                
            foreach (var pool in pools.Values)
            {
                if (pool != null)
                {
                    pool.ClearPool();
                }
            }
            pools.Clear();
            
            if (enableLogging)
                Debug.Log("[NetworkObjectPoolManagerComponent] ✅ All pools cleared");
        }
        
        /// <summary>
        /// Get pool statistics
        /// </summary>
        public string GetPoolStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== Pool Statistics ===");
            
            foreach (var kvp in pools)
            {
                var pool = kvp.Value;
                if (pool != null)
                {
                    stats.AppendLine($"{kvp.Key}: Active={pool.ActiveCount}, Available={pool.AvailableCount}");
                }
            }
            
            return stats.ToString();
        }
        
        private void OnDestroy()
        {
            if (enableLogging)
                Debug.Log("[NetworkObjectPoolManagerComponent] OnDestroy() called");
            ClearAllPools();
        }
        
        private void OnApplicationQuit()
        {
            ClearAllPools();
        }
    }
}
