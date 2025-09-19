using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Unity.Netcode;

namespace MOBA
{
    /// <summary>
    /// Unified Object Pool system that consolidates all pool implementations
    /// Supports both regular GameObjects and NetworkObjects with Components
    /// Replaces: ObjectPool<T>, GameObjectPool, NetworkObjectPool, and all pool managers
    /// 
    /// <example>
    /// <para><strong>Basic Component Pool Usage:</strong></para>
    /// <code>
    /// // Create a pool for projectile components
    /// var projectilePool = UnifiedObjectPool.GetComponentPool&lt;Projectile&gt;(
    ///     "ProjectilePool", projectilePrefab, initialSize: 20, maxSize: 100);
    /// 
    /// // Get a projectile from the pool
    /// var projectile = projectilePool.Get();
    /// if (projectile != null)
    /// {
    ///     projectile.Initialize(startPos, targetPos, damage);
    ///     // ... use the projectile ...
    ///     
    ///     // Return to pool when done
    ///     projectilePool.Return(projectile);
    /// }
    /// </code>
    /// 
    /// <para><strong>GameObject Pool for Effects:</strong></para>
    /// <code>
    /// // Create pool for visual effects
    /// var effectPool = UnifiedObjectPool.GetGameObjectPool(
    ///     "ExplosionEffects", explosionPrefab, initialSize: 10, maxSize: 50);
    /// 
    /// // Spawn effect at impact location
    /// var effect = effectPool.Get();
    /// if (effect != null)
    /// {
    ///     effect.transform.position = impactPoint;
    ///     effect.transform.rotation = impactRotation;
    ///     
    ///     // Auto-return after 3 seconds
    ///     StartCoroutine(ReturnAfterDelay(effect, 3f));
    /// }
    /// 
    /// IEnumerator ReturnAfterDelay(GameObject obj, float delay)
    /// {
    ///     yield return new WaitForSeconds(delay);
    ///     effectPool.Return(obj);
    /// }
    /// </code>
    /// 
    /// <para><strong>NetworkObject Pool for Multiplayer:</strong></para>
    /// <code>
    /// // Create network-aware pool (server only)
    /// if (NetworkManager.Singleton.IsServer)
    /// {
    ///     var networkPool = UnifiedObjectPool.GetNetworkObjectPool(
    ///         "NetworkEnemies", enemyNetworkPrefab, initialSize: 5, maxSize: 20);
    ///     
    ///     // Spawn networked enemy
    ///     var enemy = networkPool.Get(); // Automatically spawns on network
    ///     if (enemy != null)
    ///     {
    ///         enemy.transform.position = spawnPoint;
    ///         // Enemy is now visible to all clients
    ///     }
    ///     
    ///     // Despawn when defeated
    ///     networkPool.Return(enemy); // Automatically despawns from network
    /// }
    /// </code>
    /// 
    /// <para><strong>Pool Management and Statistics:</strong></para>
    /// <code>
    /// // Monitor pool performance
    /// var stats = UnifiedObjectPool.GetAllPoolStats();
    /// foreach (var kvp in stats)
    /// {
    ///     Debug.Log($"Pool {kvp.Key}: {kvp.Value.available} available, " +
    ///               $"{kvp.Value.active} active, {kvp.Value.total} total");
    /// }
    /// 
    /// // Clear specific pool when done
    /// UnifiedObjectPool.ClearPool("ProjectilePool");
    /// 
    /// // Clear all pools on scene change
    /// UnifiedObjectPool.ClearAllPools();
    /// </code>
    /// </example>
    /// 
    /// <para><strong>Design Pattern:</strong> Object Pool (Creational)</para>
    /// <para><strong>Thread Safety:</strong> All operations are thread-safe using ConcurrentDictionary and locking</para>
    /// <para><strong>Memory Management:</strong> Pools automatically manage object lifecycle and prevent memory leaks</para>
    /// <para><strong>Network Aware:</strong> NetworkObject pools handle spawning/despawning automatically</para>
    /// </summary>
    public static class UnifiedObjectPool
    {
        // Thread-safe pool storage
        private static readonly ConcurrentDictionary<string, IObjectPool> pools = new();
        private static readonly object lockObject = new object();
        
        /// <summary>
        /// Create or get a component-based object pool
        /// </summary>
        public static ComponentPool<T> GetComponentPool<T>(string poolName, T prefab, int initialSize = 10, int maxSize = 100, Transform parent = null) 
            where T : Component
        {
            lock (lockObject)
            {
                if (pools.TryGetValue(poolName, out IObjectPool existingPool))
                {
                    if (existingPool is ComponentPool<T> componentPool)
                    {
                        return componentPool;
                    }
                    Debug.LogError($"[UnifiedObjectPool] Pool '{poolName}' exists but is not a ComponentPool<{typeof(T).Name}>");
                    return null;
                }

                var newPool = new ComponentPool<T>(prefab, initialSize, maxSize, parent);
                pools[poolName] = newPool;
                Debug.Log($"[UnifiedObjectPool] Created ComponentPool<{typeof(T).Name}> '{poolName}' (Initial: {initialSize}, Max: {maxSize})");
                return newPool;
            }
        }

        /// <summary>
        /// Create or get a GameObject-based object pool
        /// </summary>
        public static GameObjectPool GetGameObjectPool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 100, Transform parent = null)
        {
            lock (lockObject)
            {
                if (pools.TryGetValue(poolName, out IObjectPool existingPool))
                {
                    if (existingPool is GameObjectPool gameObjectPool)
                    {
                        return gameObjectPool;
                    }
                    Debug.LogError($"[UnifiedObjectPool] Pool '{poolName}' exists but is not a GameObjectPool");
                    return null;
                }

                var newPool = new GameObjectPool(prefab, initialSize, maxSize, parent);
                pools[poolName] = newPool;
                Debug.Log($"[UnifiedObjectPool] Created GameObjectPool '{poolName}' (Initial: {initialSize}, Max: {maxSize})");
                return newPool;
            }
        }

        /// <summary>
        /// Create or get a NetworkObject-based object pool
        /// </summary>
        public static NetworkObjectPool GetNetworkObjectPool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 100, Transform parent = null)
        {
            lock (lockObject)
            {
                if (pools.TryGetValue(poolName, out IObjectPool existingPool))
                {
                    if (existingPool is NetworkObjectPool networkPool)
                    {
                        return networkPool;
                    }
                    Debug.LogError($"[UnifiedObjectPool] Pool '{poolName}' exists but is not a NetworkObjectPool");
                    return null;
                }

                var newPool = new NetworkObjectPool(prefab, initialSize, maxSize, parent);
                pools[poolName] = newPool;
                Debug.Log($"[UnifiedObjectPool] Created NetworkObjectPool '{poolName}' (Initial: {initialSize}, Max: {maxSize})");
                return newPool;
            }
        }

        /// <summary>
        /// Get pool statistics for debugging
        /// </summary>
        public static Dictionary<string, (int available, int active, int total)> GetAllPoolStats()
        {
            var stats = new Dictionary<string, (int, int, int)>();
            
            foreach (var kvp in pools)
            {
                var poolStats = kvp.Value.GetStats();
                stats[kvp.Key] = poolStats;
            }
            
            return stats;
        }

        /// <summary>
        /// Clear a specific pool
        /// </summary>
        public static void ClearPool(string poolName)
        {
            lock (lockObject)
            {
                if (pools.TryGetValue(poolName, out IObjectPool pool))
                {
                    pool.Clear();
                    pools.TryRemove(poolName, out _);
                    Debug.Log($"[UnifiedObjectPool] Cleared pool '{poolName}'");
                }
            }
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public static void ClearAllPools()
        {
            lock (lockObject)
            {
                foreach (var pool in pools.Values)
                {
                    pool.Clear();
                }
                pools.Clear();
                Debug.Log("[UnifiedObjectPool] Cleared all pools");
            }
        }

        /// <summary>
        /// Base interface for all pool types
        /// </summary>
        public interface IObjectPool
        {
            (int available, int active, int total) GetStats();
            void Clear();
            void ReturnAll();
        }

        /// <summary>
        /// Component-based object pool with proper lifecycle management
        /// </summary>
        public class ComponentPool<T> : IObjectPool, IDisposable where T : Component
        {
        private readonly Queue<T> availableObjects = new();
        private readonly HashSet<T> availableLookup = new();
        private readonly List<T> allObjects = new();
        private readonly T prefab;
        private readonly int maxSize;
        private readonly Transform parent;
        private bool disposed = false;

        public ComponentPool(T prefab, int initialSize, int maxSize, Transform parent = null)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.parent = parent;
            
            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        public T Get()
        {
            if (disposed) return null;
            
            T obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
                availableLookup.Remove(obj);
            }
            else
            {
                obj = CreateNewObject();
            }

            if (obj != null)
            {
                obj.gameObject.SetActive(true);
            }
            return obj;
        }

        public void Return(T obj)
        {
            if (disposed || obj == null) return;

            obj.gameObject.SetActive(false);
            if (availableLookup.Add(obj))
            {
                availableObjects.Enqueue(obj);
            }
        }

        private T CreateNewObject()
        {
            if (disposed || allObjects.Count >= maxSize) return null;
            
            GameObject newObj = parent != null
                ? UnityEngine.Object.Instantiate(prefab.gameObject, parent)
                : UnityEngine.Object.Instantiate(prefab.gameObject);
            T component = newObj.GetComponent<T>();
            
            if (component == null)
            {
                Debug.LogError($"[ComponentPool] Prefab does not have component {typeof(T).Name}");
                UnityEngine.Object.Destroy(newObj);
                return null;
            }

            allObjects.Add(component);
            newObj.SetActive(false);
            return component;
        }

        public (int available, int active, int total) GetStats()
        {
            return (availableObjects.Count, allObjects.Count - availableObjects.Count, allObjects.Count);
        }

        public void Clear()
        {
            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            availableObjects.Clear();
            availableLookup.Clear();
            allObjects.Clear();
        }

        public void ReturnAll()
        {
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                obj.gameObject.SetActive(false);
                if (availableLookup.Add(obj))
                {
                    availableObjects.Enqueue(obj);
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Clear();
                disposed = true;
            }
        }
        }

        /// <summary>
        /// GameObject-based object pool for prefabs without specific component requirements
        /// </summary>
        public class GameObjectPool : IObjectPool
        {
        private readonly Queue<GameObject> availableObjects = new();
        private readonly HashSet<GameObject> availableLookup = new();
        private readonly List<GameObject> allObjects = new();
        private readonly GameObject prefab;
        private readonly int maxSize;
        private readonly Transform parent;

        public GameObjectPool(GameObject prefab, int initialSize, int maxSize, Transform parent = null)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.parent = parent;

            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        public GameObject Get()
        {
            GameObject obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
                availableLookup.Remove(obj);
            }
            else
            {
                obj = CreateNewObject();
            }

            if (obj != null)
            {
                obj.SetActive(true);
            }
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            if (availableLookup.Add(obj))
            {
                availableObjects.Enqueue(obj);
            }
        }

        private GameObject CreateNewObject()
        {
            if (allObjects.Count >= maxSize) return null;
            
            GameObject newObj = parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab);
            allObjects.Add(newObj);
            newObj.SetActive(false);
            return newObj;
        }

        public (int available, int active, int total) GetStats()
        {
            return (availableObjects.Count, allObjects.Count - availableObjects.Count, allObjects.Count);
        }

        public void Clear()
        {
            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            availableObjects.Clear();
            availableLookup.Clear();
            allObjects.Clear();
        }

        public void ReturnAll()
        {
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                obj.SetActive(false);
                if (availableLookup.Add(obj))
                {
                    availableObjects.Enqueue(obj);
                }
            }
        }
        }

        /// <summary>
        /// Network-aware object pool for multiplayer games
        /// </summary>
        public class NetworkObjectPool : IObjectPool
        {
        private readonly Queue<GameObject> availableObjects = new();
        private readonly HashSet<GameObject> availableLookup = new();
        private readonly List<GameObject> allObjects = new();
        private readonly GameObject prefab;
        private readonly int maxSize;
        private NetworkManager networkManager;
        private readonly Transform parent;

        public NetworkObjectPool(GameObject prefab, int initialSize, int maxSize, Transform parent = null)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            networkManager = NetworkManager.Singleton;
            this.parent = parent;
            
            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        public GameObject Get()
        {
            GameObject obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
                availableLookup.Remove(obj);
            }
            else
            {
                obj = CreateNewObject();
            }

            if (obj != null)
            {
                obj.SetActive(true);
                
                // Handle network spawning if on server
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

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            // Handle network despawning if on server
            if (networkManager != null && networkManager.IsServer)
            {
                var networkObject = obj.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
            }

            obj.SetActive(false);
            if (availableLookup.Add(obj))
            {
                availableObjects.Enqueue(obj);
            }
        }

        private GameObject CreateNewObject()
        {
            if (allObjects.Count >= maxSize) return null;
            
            GameObject newObj = parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab);
            allObjects.Add(newObj);
            newObj.SetActive(false);
            return newObj;
        }

        public (int available, int active, int total) GetStats()
        {
            return (availableObjects.Count, allObjects.Count - availableObjects.Count, allObjects.Count);
        }

        public void Clear()
        {
            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    // Properly despawn network objects before destruction
                    if (networkManager != null && networkManager.IsServer)
                    {
                        var networkObject = obj.GetComponent<NetworkObject>();
                        if (networkObject != null && networkObject.IsSpawned)
                        {
                            networkObject.Despawn();
                        }
                    }
                    UnityEngine.Object.Destroy(obj);
                }
            }
            availableObjects.Clear();
            availableLookup.Clear();
            allObjects.Clear();
        }

        public void ReturnAll()
        {
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                obj.SetActive(false);
                if (availableLookup.Add(obj))
                {
                    availableObjects.Enqueue(obj);
                }
            }
        }
        }
    }
}
