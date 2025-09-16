using System;
using System.Collections.Generic;
using MOBA.Debugging;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Generic Object Pool implementation based on Game Programming Patterns
    /// Manages reusable objects to prevent frequent instantiation/destruction
    /// Enhanced with proper disposal and memory management
    /// </summary>
    /// <typeparam name="T">Type of object to pool, must be a Component</typeparam>
    public class ObjectPool<T> : IDisposable where T : Component
    {
        private readonly UnifiedObjectPool.ComponentPool<T> componentPool;
        private readonly string poolName;
        private bool disposed = false;

        private GameDebugContext DebugContext => new GameDebugContext(
            GameDebugCategory.Pooling,
            GameDebugSystemTag.Pooling,
            GameDebugMechanicTag.Pooling,
            subsystem: typeof(T).Name);

        /// <summary>
        /// Creates a new object pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="initialSize">Initial number of objects to create</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
        {
            if (prefab == null)
            {
                GameDebug.LogError(DebugContext, "Cannot create pool without a prefab instance.");
                throw new ArgumentNullException(nameof(prefab));
            }

            poolName = $"{typeof(T).Name}_{prefab.gameObject.GetInstanceID()}_{GetHashCode()}";
            componentPool = UnifiedObjectPool.GetComponentPool(poolName, prefab, initialSize, Mathf.Max(initialSize * 4, initialSize + 10), parent);
        }

        /// <summary>
        /// Gets an object from the pool, creating one if necessary
        /// </summary>
        /// <returns>Available object from the pool</returns>
        public T Get()
        {
            if (disposed)
            {
                GameDebug.LogError(DebugContext, "Attempted to fetch from a disposed pool.");
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
            
            return componentPool.Get();
        }

        /// <summary>
        /// Safe get method with Result pattern for error handling
        /// Based on Code Complete defensive programming principles
        /// </summary>
        public bool TryGet(out T obj)
        {
            obj = null;
            
            if (disposed) 
            {
                GameDebug.LogError(DebugContext, "Cannot get object from disposed pool.");
                return false;
            }
            
            obj = componentPool.Get();
            if (obj == null)
            {
                GameDebug.LogError(DebugContext, "Failed to retrieve pooled object instance.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns an object to the pool for reuse
        /// </summary>
        /// <param name="obj">Object to return to the pool</param>
        public void Return(T obj)
        {
            if (disposed || obj == null) return;

            componentPool.Return(obj);
        }

        /// <summary>
        /// Returns all objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            if (disposed)
            {
                return;
            }

            componentPool.ReturnAll();
        }

        /// <summary>
        /// Dispose of the pool and release pooled instances
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;

            UnifiedObjectPool.ClearPool(poolName);
            disposed = true;
        }

        /// <summary>
        /// Gets the total number of objects created
        /// </summary>
        public int TotalCount => componentPool?.GetStats().total ?? 0;

        /// <summary>
        /// Gets the number of available objects in the pool
        /// </summary>
        public int AvailableCount => componentPool?.GetStats().available ?? 0;

        /// <summary>
        /// Gets the number of active objects
        /// </summary>
        public int ActiveCount => componentPool?.GetStats().active ?? 0;
    }
}
