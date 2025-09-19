using System;
using System.Collections.Generic;

namespace MOBA.Services
{
    /// <summary>
    /// Lightweight dependency injection container for runtime services.
    /// Provides registration and resolution helpers for gameplay systems.
    /// </summary>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly object gate = new object();

        /// <summary>
        /// Register a service instance for the specified contract.
        /// </summary>
        public static void Register<TService>(TService instance, bool overwrite = true) where TService : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var type = typeof(TService);
            lock (gate)
            {
                if (!overwrite && services.ContainsKey(type))
                {
                    return;
                }

                services[type] = instance;
            }
        }

        /// <summary>
        /// Attempt to resolve a service; returns null when not found.
        /// </summary>
        public static TService Resolve<TService>() where TService : class
        {
            lock (gate)
            {
                services.TryGetValue(typeof(TService), out var instance);
                return instance as TService;
            }
        }

        public static bool TryResolve<TService>(out TService service) where TService : class
        {
            service = Resolve<TService>();
            return service != null;
        }

        /// <summary>
        /// Remove all registered services (primarily for test isolation).
        /// </summary>
        public static void Clear()
        {
            lock (gate)
            {
                services.Clear();
            }
        }
    }
}
