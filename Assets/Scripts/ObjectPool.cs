using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Generic Object Pool implementation based on Game Programming Patterns
    /// Manages reusable objects to prevent frequent instantiation/destruction
    /// </summary>
    /// <typeparam name="T">Type of object to pool, must be a Component</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly List<T> allObjects = new List<T>();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int initialSize;

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
            T obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool for reuse
        /// </summary>
        /// <param name="obj">Object to return to the pool</param>
        public void Return(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);
            availableObjects.Enqueue(obj);
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
            // Removed automatic Object.Instantiate to prevent automatic loading
            // Use CreateObjectWithInstance() method with manually provided instance instead
            Debug.LogError("[ObjectPool] Automatic instantiation disabled - use CreateObjectWithInstance() instead");
            return null;
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