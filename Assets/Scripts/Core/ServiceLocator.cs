using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple Service Locator pattern implementation for dependency injection
    /// Replaces manual dependency setup throughout codebase
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly object lockObject = new object();

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            lock (lockObject)
            {
                var serviceType = typeof(T);
                if (services.ContainsKey(serviceType))
                {
                    Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} is already registered. Replacing...");
                }
                services[serviceType] = service;
                Debug.Log($"[ServiceLocator] Registered service: {serviceType.Name}");
            }
        }

        /// <summary>
        /// Get a service instance
        /// </summary>
        public static T Get<T>() where T : class
        {
            lock (lockObject)
            {
                var serviceType = typeof(T);
                if (services.TryGetValue(serviceType, out var service))
                {
                    return service as T;
                }
                
                Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} not found. Attempting auto-registration...");
                
                // Try to find the service in the scene
                if (typeof(MonoBehaviour).IsAssignableFrom(serviceType))
                {
                    var foundService = UnityEngine.Object.FindAnyObjectByType(serviceType) as T;
                    if (foundService != null)
                    {
                        Register(foundService);
                        return foundService;
                    }
                }
                
                return null;
            }
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            lock (lockObject)
            {
                return services.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            lock (lockObject)
            {
                var serviceType = typeof(T);
                if (services.Remove(serviceType))
                {
                    Debug.Log($"[ServiceLocator] Unregistered service: {serviceType.Name}");
                }
            }
        }

        /// <summary>
        /// Clear all registered services
        /// </summary>
        public static void Clear()
        {
            lock (lockObject)
            {
                services.Clear();
                Debug.Log("[ServiceLocator] All services cleared");
            }
        }

        /// <summary>
        /// Get all registered service types (for debugging)
        /// </summary>
        public static IEnumerable<Type> GetRegisteredTypes()
        {
            lock (lockObject)
            {
                return new List<Type>(services.Keys);
            }
        }
    }
}
