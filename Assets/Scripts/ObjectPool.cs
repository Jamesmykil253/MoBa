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
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly List<T> allObjects = new List<T>();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int initialSize;
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
            this.prefab = prefab;
            this.initialSize = initialSize;
            this.parent = parent;

            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
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
            
            T obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
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

            try
            {
                if (availableObjects.Count > 0)
                {
                    obj = availableObjects.Dequeue();
                }
                else
                {
                    obj = CreateNewObject();
                }

                if (obj == null)
                {
                    GameDebug.LogError(DebugContext, "Failed to create or retrieve pooled object instance.");
                    return false;
                }

                obj.gameObject.SetActive(true);
                return true;
            }
            catch (System.Exception ex)
            {
                GameDebug.LogException(ex, DebugContext);
                return false;
            }
        }

        /// <summary>
        /// Returns an object to the pool for reuse
        /// </summary>
        /// <param name="obj">Object to return to the pool</param>
        public void Return(T obj)
        {
            if (disposed || obj == null) return;

            obj.gameObject.SetActive(false);
            if (!availableObjects.Contains(obj))
            {
                availableObjects.Enqueue(obj);
            }
        }

        /// <summary>
        /// Returns all objects to the pool
        /// </summary>
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

        /// <summary>
        /// Creates a new object and adds it to the pool
        /// </summary>
        /// <returns>Newly created object</returns>
        private T CreateNewObject()
        {
            if (disposed) return null;
            
            // Create new instance from the prefab
            if (prefab == null)
            {
                GameDebug.LogError(DebugContext, "Prefab reference is null; cannot create new pooled object.");
                return null;
            }

            GameObject newObj = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
            T component = newObj.GetComponent<T>();

            if (component == null)
            {
                GameDebug.LogError(
                    DebugContext,
                    $"Prefab object {prefab.gameObject.name} missing required component {typeof(T).Name}.");
                UnityEngine.Object.Destroy(newObj);
                return null;
            }

            allObjects.Add(component);
            return component;
        }

        /// <summary>
        /// Dispose of all pooled objects and clear collections
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            
            // Destroy all pooled objects
            foreach (var obj in allObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            
            availableObjects.Clear();
            allObjects.Clear();
            disposed = true;
        }

        /// <summary>
        /// Gets the total number of objects created
        /// </summary>
        public int TotalCount => allObjects.Count;

        /// <summary>
        /// Gets the number of available objects in the pool
        /// </summary>
        public int AvailableCount => availableObjects.Count;

        /// <summary>
        /// Gets the number of active objects
        /// </summary>
        public int ActiveCount => TotalCount - AvailableCount;
    }
}
