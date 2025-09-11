using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Enhanced Object Pool implementation with robust error handling
    /// Addresses critical issues found in audit: missing components, null references, memory leaks
    /// </summary>
    /// <typeparam name="T">Type of object to pool, must be a Component</typeparam>
    public class EnhancedObjectPool<T> where T : Component
    {
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly List<T> allObjects = new List<T>();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int initialSize;
        private readonly bool enableValidation;

        // Health metrics for monitoring
        private int creationAttempts = 0;
        private int successfulCreations = 0;
        private int componentFixCount = 0;

        /// <summary>
        /// Creates a new enhanced object pool with validation
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="initialSize">Initial number of objects to create</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="enableValidation">Enable component validation and auto-fixing</param>
        public EnhancedObjectPool(T prefab, int initialSize = 10, Transform parent = null, bool enableValidation = true)
        {
            this.prefab = prefab;
            this.initialSize = initialSize;
            this.parent = parent;
            this.enableValidation = enableValidation;

            // Validate prefab before creating pool
            if (!ValidatePrefab())
            {
                Debug.LogError($"[EnhancedObjectPool] Prefab validation failed for {typeof(T).Name}");
                return;
            }

            // Pre-populate the pool with validation
            for (int i = 0; i < initialSize; i++)
            {
                T newObject = CreateNewObjectSafe();
                if (newObject != null)
                {
                    Return(newObject); // Add to available queue
                }
            }

            Debug.Log($"[EnhancedObjectPool] Initialized {typeof(T).Name} pool: " +
                     $"{successfulCreations}/{creationAttempts} successful, " +
                     $"{componentFixCount} components auto-fixed");
        }

        /// <summary>
        /// Validates the prefab has required components
        /// </summary>
        private bool ValidatePrefab()
        {
            if (prefab == null)
            {
                Debug.LogError($"[EnhancedObjectPool] Prefab is null for {typeof(T).Name}");
                return false;
            }

            if (prefab.gameObject == null)
            {
                Debug.LogError($"[EnhancedObjectPool] Prefab GameObject is null for {typeof(T).Name}");
                return false;
            }

            // Check if prefab has the required component
            T component = prefab.gameObject.GetComponent<T>();
            if (component == null && enableValidation)
            {
                Debug.LogWarning($"[EnhancedObjectPool] Prefab {prefab.name} missing {typeof(T).Name} component - will auto-fix at runtime");
            }

            return true;
        }

        /// <summary>
        /// Gets an object from the pool, creating one if necessary with full validation
        /// </summary>
        /// <returns>Available object from the pool</returns>
        public T Get()
        {
            T obj = null;

            // Try to get from available queue
            while (availableObjects.Count > 0)
            {
                T candidate = availableObjects.Dequeue();
                if (candidate != null && candidate.gameObject != null)
                {
                    obj = candidate;
                    break;
                }
                else
                {
                    Debug.LogWarning($"[EnhancedObjectPool] Removed invalid object from {typeof(T).Name} pool");
                }
            }

            // Create new object if none available
            if (obj == null)
            {
                obj = CreateNewObjectSafe();
            }

            if (obj != null)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }

            Debug.LogError($"[EnhancedObjectPool] Failed to provide {typeof(T).Name} object");
            return null;
        }

        /// <summary>
        /// Returns an object to the pool for reuse with validation
        /// </summary>
        /// <param name="obj">Object to return to the pool</param>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[EnhancedObjectPool] Attempted to return null {typeof(T).Name} object");
                return;
            }

            if (obj.gameObject == null)
            {
                Debug.LogWarning($"[EnhancedObjectPool] Attempted to return {typeof(T).Name} with null GameObject");
                return;
            }

            // Validate object is actually from this pool
            if (!allObjects.Contains(obj))
            {
                Debug.LogWarning($"[EnhancedObjectPool] Attempted to return {typeof(T).Name} object not from this pool");
                return;
            }

            obj.gameObject.SetActive(false);
            
            // Reset object position to parent (cleanup)
            if (parent != null)
            {
                obj.transform.SetParent(parent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
            }

            availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Returns all objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.gameObject != null && obj.gameObject.activeSelf)
                {
                    Return(obj);
                }
            }
        }

        /// <summary>
        /// Creates a new object with comprehensive error handling and auto-fixing
        /// </summary>
        /// <returns>Newly created object or null if failed</returns>
        private T CreateNewObjectSafe()
        {
            creationAttempts++;

            try
            {
                if (prefab == null)
                {
                    Debug.LogError($"[EnhancedObjectPool] Cannot create {typeof(T).Name}: prefab is null");
                    return null;
                }

                // Create instance
                GameObject newObj = Object.Instantiate(prefab.gameObject, parent);
                if (newObj == null)
                {
                    Debug.LogError($"[EnhancedObjectPool] Failed to instantiate {typeof(T).Name} prefab");
                    return null;
                }

                // Try to get required component
                T component = newObj.GetComponent<T>();

                // Auto-fix missing component if validation enabled
                if (component == null && enableValidation)
                {
                    Debug.LogWarning($"[EnhancedObjectPool] Auto-fixing missing {typeof(T).Name} component on {newObj.name}");
                    
                    component = newObj.AddComponent<T>();
                    componentFixCount++;

                    // For Projectile specifically, ensure physics components
                    if (typeof(T) == typeof(Projectile))
                    {
                        EnsureProjectilePhysics(newObj);
                    }
                }

                if (component == null)
                {
                    Debug.LogError($"[EnhancedObjectPool] Failed to get or create {typeof(T).Name} component on {newObj.name}");
                    Object.Destroy(newObj);
                    return null;
                }

                // Configure new object
                newObj.SetActive(false);
                allObjects.Add(component);
                successfulCreations++;

                return component;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnhancedObjectPool] Exception creating {typeof(T).Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ensures projectile objects have required physics components
        /// </summary>
        private void EnsureProjectilePhysics(GameObject obj)
        {
            // Add Rigidbody if missing
            if (obj.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = obj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log($"[EnhancedObjectPool] Added Rigidbody to {obj.name}");
            }

            // Add Collider if missing
            if (obj.GetComponent<Collider>() == null)
            {
                SphereCollider collider = obj.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.1f;
                Debug.Log($"[EnhancedObjectPool] Added SphereCollider to {obj.name}");
            }
        }

        /// <summary>
        /// Validates pool health and removes invalid objects
        /// </summary>
        public void ValidatePoolHealth()
        {
            List<T> validObjects = new List<T>();
            int removedCount = 0;

            foreach (var obj in allObjects)
            {
                if (obj != null && obj.gameObject != null)
                {
                    validObjects.Add(obj);
                }
                else
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                allObjects.Clear();
                allObjects.AddRange(validObjects);
                
                // Clean available queue
                Queue<T> cleanQueue = new Queue<T>();
                while (availableObjects.Count > 0)
                {
                    T obj = availableObjects.Dequeue();
                    if (obj != null && obj.gameObject != null)
                    {
                        cleanQueue.Enqueue(obj);
                    }
                }
                availableObjects.Clear();
                while (cleanQueue.Count > 0)
                {
                    availableObjects.Enqueue(cleanQueue.Dequeue());
                }

                Debug.LogWarning($"[EnhancedObjectPool] Removed {removedCount} invalid {typeof(T).Name} objects from pool");
            }
        }

        /// <summary>
        /// Gets pool health statistics
        /// </summary>
        public PoolHealthStats GetHealthStats()
        {
            return new PoolHealthStats
            {
                TotalObjects = allObjects.Count,
                AvailableObjects = availableObjects.Count,
                ActiveObjects = allObjects.Count - availableObjects.Count,
                CreationAttempts = creationAttempts,
                SuccessfulCreations = successfulCreations,
                ComponentFixCount = componentFixCount,
                SuccessRate = creationAttempts > 0 ? (float)successfulCreations / creationAttempts : 0f
            };
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

    /// <summary>
    /// Health statistics for object pool monitoring
    /// </summary>
    public struct PoolHealthStats
    {
        public int TotalObjects;
        public int AvailableObjects;
        public int ActiveObjects;
        public int CreationAttempts;
        public int SuccessfulCreations;
        public int ComponentFixCount;
        public float SuccessRate;

        public override string ToString()
        {
            return $"Pool Health: {ActiveObjects}/{TotalObjects} active, " +
                   $"{SuccessRate:P1} success rate, " +
                   $"{ComponentFixCount} fixes applied";
        }
    }
}
