using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace MOBA.Networking
{
    /// <summary>
    /// Object pool for network objects to reduce instantiation overhead
    /// Implements Flyweight pattern for memory-efficient network object management
    /// </summary>
    public class NetworkObjectPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] public GameObject prefab;
        [SerializeField] public int initialPoolSize = 10;
        [SerializeField] public int maxPoolSize = 50;
        [SerializeField] private bool autoExpand = true;

        private Queue<GameObject> availableObjects = new Queue<GameObject>();
        private HashSet<GameObject> activeObjects = new HashSet<GameObject>();
        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
            PrewarmPool();
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewObject();
            }

            UnityEngine.Debug.Log($"[NetworkObjectPool] Prewarmed pool with {initialPoolSize} objects for {prefab.name}");
        }

        private GameObject CreateNewObject()
        {
            if (availableObjects.Count + activeObjects.Count >= maxPoolSize && !autoExpand)
            {
                UnityEngine.Debug.LogWarning($"[NetworkObjectPool] Pool limit reached for {prefab.name}");
                return null;
            }

            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);

            // Ensure it has NetworkObject component
            var networkObject = obj.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = obj.AddComponent<NetworkObject>();
                UnityEngine.Debug.LogWarning($"[NetworkObjectPool] Added NetworkObject component to {prefab.name}");
            }

            availableObjects.Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public GameObject Get()
        {
            GameObject obj = null;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (autoExpand)
            {
                obj = CreateNewObject();
                if (obj != null)
                {
                    availableObjects.Dequeue(); // Remove from available since we're using it
                }
            }

            if (obj != null)
            {
                obj.SetActive(true);
                activeObjects.Add(obj);

                // Spawn network object if server
                if (networkManager != null && networkManager.IsServer)
                {
                    var networkObject = obj.GetComponent<NetworkObject>();
                    if (networkObject != null && !networkObject.IsSpawned)
                    {
                        networkObject.Spawn();
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null || !activeObjects.Contains(obj))
            {
                UnityEngine.Debug.LogWarning($"[NetworkObjectPool] Attempted to return invalid object to pool");
                return;
            }

            // Despawn network object if server
            if (networkManager != null && networkManager.IsServer)
            {
                var networkObject = obj.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public (int available, int active, int total) GetPoolStats()
        {
            return (availableObjects.Count, activeObjects.Count, availableObjects.Count + activeObjects.Count);
        }

        /// <summary>
        /// Clear all objects from the pool
        /// </summary>
        public void ClearPool()
        {
            // Return all active objects
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    if (networkManager != null && networkManager.IsServer)
                    {
                        var networkObject = obj.GetComponent<NetworkObject>();
                        if (networkObject != null && networkObject.IsSpawned)
                        {
                            networkObject.Despawn();
                        }
                    }
                    Destroy(obj);
                }
            }

            activeObjects.Clear();

            // Destroy all available objects
            foreach (var obj in availableObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            availableObjects.Clear();

            UnityEngine.Debug.Log($"[NetworkObjectPool] Cleared pool for {prefab.name}");
        }

        /// <summary>
        /// Expand pool by specified amount
        /// </summary>
        public void ExpandPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewObject();
            }

            UnityEngine.Debug.Log($"[NetworkObjectPool] Expanded pool by {count} objects for {prefab.name}");
        }

        private void OnDestroy()
        {
            ClearPool();
        }
    }

    /// <summary>
    /// Manager for multiple network object pools
    /// </summary>
    public class NetworkObjectPoolManager : MonoBehaviour
    {
        private static NetworkObjectPoolManager _instance;
        public static NetworkObjectPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkObjectPoolManager");
                    _instance = go.AddComponent<NetworkObjectPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<string, NetworkObjectPool> pools = new Dictionary<string, NetworkObjectPool>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Create a new object pool
        /// </summary>
        public void CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            if (pools.ContainsKey(poolName))
            {
                UnityEngine.Debug.LogWarning($"[NetworkObjectPoolManager] Pool {poolName} already exists");
                return;
            }

            var poolObject = new GameObject($"{poolName}_Pool");
            poolObject.transform.SetParent(transform);

            var pool = poolObject.AddComponent<NetworkObjectPool>();
            pool.prefab = prefab;
            pool.initialPoolSize = initialSize;
            pool.maxPoolSize = maxSize;

            pools[poolName] = pool;

            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] Created pool {poolName} with {initialSize} objects");
        }

        /// <summary>
        /// Get an object from the specified pool
        /// </summary>
        public GameObject GetFromPool(string poolName)
        {
            if (!pools.ContainsKey(poolName))
            {
                UnityEngine.Debug.LogError($"[NetworkObjectPoolManager] Pool {poolName} does not exist");
                return null;
            }

            return pools[poolName].Get();
        }

        /// <summary>
        /// Return an object to the specified pool
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (!pools.ContainsKey(poolName))
            {
                UnityEngine.Debug.LogError($"[NetworkObjectPoolManager] Pool {poolName} does not exist");
                return;
            }

            pools[poolName].Return(obj);
        }

        /// <summary>
        /// Get statistics for all pools
        /// </summary>
        public Dictionary<string, (int available, int active, int total)> GetAllPoolStats()
        {
            var stats = new Dictionary<string, (int, int, int)>();

            foreach (var kvp in pools)
            {
                stats[kvp.Key] = kvp.Value.GetPoolStats();
            }

            return stats;
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                pool.ClearPool();
            }

            pools.Clear();
            UnityEngine.Debug.Log("[NetworkObjectPoolManager] Cleared all pools");
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}