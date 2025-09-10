using UnityEngine;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// Flyweight Factory for managing shared flyweight instances
    /// Implements the Flyweight Pattern for efficient resource sharing
    /// Based on Game Programming Patterns - Flyweight Pattern
    /// </summary>
    public class FlyweightFactory : MonoBehaviour
    {
        [Header("Projectile Flyweights")]
        [SerializeField] private List<ProjectileFlyweight> projectileFlyweights = new List<ProjectileFlyweight>();

        [Header("Factory Settings")]
        [SerializeField] private bool createDefaultsIfMissing = true;
        [SerializeField] private int maxCachedFlyweights = 50;

        // Internal storage
        private Dictionary<string, ProjectileFlyweight> flyweightCache = new Dictionary<string, ProjectileFlyweight>();
        private Dictionary<string, int> usageCount = new Dictionary<string, int>();

        // Removed automatic Awake() method to prevent automatic loading
        // Use InitializeFactory() method manually instead

        private void InitializeFactory()
        {
            // Load existing flyweights
            foreach (var flyweight in projectileFlyweights)
            {
                if (flyweight != null)
                {
                    string key = flyweight.name;
                    flyweightCache[key] = flyweight;
                    usageCount[key] = 0;
                }
            }

            // Create default flyweights if enabled and missing
            if (createDefaultsIfMissing)
            {
                CreateDefaultFlyweights();
            }

            Debug.Log($"FlyweightFactory initialized with {flyweightCache.Count} flyweights");
        }

        /// <summary>
        /// Get or create a flyweight by name
        /// </summary>
        public ProjectileFlyweight GetFlyweight(string name)
        {
            if (flyweightCache.TryGetValue(name, out ProjectileFlyweight flyweight))
            {
                usageCount[name]++;
                return flyweight;
            }

            // Try to find in Resources
            ProjectileFlyweight loadedFlyweight = Resources.Load<ProjectileFlyweight>(name);
            if (loadedFlyweight != null)
            {
                flyweightCache[name] = loadedFlyweight;
                usageCount[name] = 1;
                return loadedFlyweight;
            }

            // Create default if allowed
            if (createDefaultsIfMissing)
            {
                ProjectileFlyweight defaultFlyweight = CreateDefaultFlyweight(name);
                flyweightCache[name] = defaultFlyweight;
                usageCount[name] = 1;
                return defaultFlyweight;
            }

            Debug.LogWarning($"Flyweight '{name}' not found and creation disabled");
            return null;
        }

        /// <summary>
        /// Return a flyweight (decrease usage count)
        /// </summary>
        public void ReturnFlyweight(string name)
        {
            if (usageCount.ContainsKey(name))
            {
                usageCount[name] = Mathf.Max(0, usageCount[name] - 1);

                // Optional: Clean up unused flyweights after some time
                if (usageCount[name] == 0 && flyweightCache.Count > maxCachedFlyweights)
                {
                    // Could implement LRU eviction here
                }
            }
        }

        /// <summary>
        /// Get usage statistics
        /// </summary>
        public Dictionary<string, int> GetUsageStats()
        {
            return new Dictionary<string, int>(usageCount);
        }

        /// <summary>
        /// Get all available flyweight names
        /// </summary>
        public List<string> GetAvailableFlyweightNames()
        {
            return new List<string>(flyweightCache.Keys);
        }

        /// <summary>
        /// Add a custom flyweight at runtime
        /// </summary>
        public void AddFlyweight(string name, ProjectileFlyweight flyweight)
        {
            if (flyweight != null && !flyweightCache.ContainsKey(name))
            {
                flyweightCache[name] = flyweight;
                usageCount[name] = 0;
                Debug.Log($"Added custom flyweight: {name}");
            }
        }

        /// <summary>
        /// Remove a flyweight
        /// </summary>
        public void RemoveFlyweight(string name)
        {
            if (flyweightCache.ContainsKey(name))
            {
                flyweightCache.Remove(name);
                usageCount.Remove(name);
                Debug.Log($"Removed flyweight: {name}");
            }
        }

        /// <summary>
        /// Clear all cached flyweights
        /// </summary>
        public void ClearCache()
        {
            flyweightCache.Clear();
            usageCount.Clear();
            Debug.Log("Flyweight cache cleared");
        }

        /// <summary>
        /// Create a modified version of an existing flyweight
        /// </summary>
        /// <param name="baseName">Name of the base flyweight</param>
        /// <param name="modifier">Action to modify the flyweight</param>
        /// <returns>A new modified flyweight instance</returns>
        public ProjectileFlyweight CreateModifiedFlyweight(string baseName, System.Action<ProjectileFlyweight> modifier)
        {
            // Get the base flyweight
            var baseFlyweight = GetFlyweight(baseName);
            if (baseFlyweight == null)
            {
                Debug.LogWarning($"Cannot create modified flyweight: base '{baseName}' not found");
                return null;
            }

            // Create a clone of the base flyweight
            var modifiedFlyweight = baseFlyweight.Clone();

            // Apply modifications
            if (modifier != null)
            {
                modifier(modifiedFlyweight);
            }

            // Generate a unique name for the modified flyweight
            string modifiedName = $"{baseName}_Modified_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

            // Add to cache (but don't count it in usage since it's a one-off)
            flyweightCache[modifiedName] = modifiedFlyweight;
            usageCount[modifiedName] = 0;

            Debug.Log($"Created modified flyweight: {modifiedName} based on {baseName}");
            return modifiedFlyweight;
        }

        /// <summary>
        /// Create a modified flyweight with a specific name
        /// </summary>
        /// <param name="baseName">Name of the base flyweight</param>
        /// <param name="newName">Name for the new modified flyweight</param>
        /// <param name="modifier">Action to modify the flyweight</param>
        /// <returns>A new modified flyweight instance</returns>
        public ProjectileFlyweight CreateModifiedFlyweight(string baseName, string newName, System.Action<ProjectileFlyweight> modifier)
        {
            // Get the base flyweight
            var baseFlyweight = GetFlyweight(baseName);
            if (baseFlyweight == null)
            {
                Debug.LogWarning($"Cannot create modified flyweight: base '{baseName}' not found");
                return null;
            }

            // Check if the new name already exists
            if (flyweightCache.ContainsKey(newName))
            {
                Debug.LogWarning($"Modified flyweight name '{newName}' already exists, using base name");
                return CreateModifiedFlyweight(baseName, modifier);
            }

            // Create a clone of the base flyweight
            var modifiedFlyweight = baseFlyweight.Clone();

            // Apply modifications
            if (modifier != null)
            {
                modifier(modifiedFlyweight);
            }

            // Add to cache
            flyweightCache[newName] = modifiedFlyweight;
            usageCount[newName] = 0;

            Debug.Log($"Created modified flyweight: {newName} based on {baseName}");
            return modifiedFlyweight;
        }

        /// <summary>
        /// Get memory usage estimate
        /// </summary>
        public int GetMemoryUsageEstimate()
        {
            // Rough estimate: each flyweight ~ 1KB
            return flyweightCache.Count * 1024;
        }

        private void CreateDefaultFlyweights()
        {
            // Create basic projectile types
            string[] defaultTypes = { "BasicProjectile", "FastProjectile", "HeavyProjectile", "HomingProjectile" };

            foreach (string type in defaultTypes)
            {
                if (!flyweightCache.ContainsKey(type))
                {
                    ProjectileFlyweight flyweight = CreateDefaultFlyweight(type);
                    flyweightCache[type] = flyweight;
                    usageCount[type] = 0;
                }
            }
        }

        private ProjectileFlyweight CreateDefaultFlyweight(string name)
        {
            // Create a new flyweight asset
            ProjectileFlyweight flyweight = ScriptableObject.CreateInstance<ProjectileFlyweight>();
            flyweight.name = name;

            // Set default values based on type
            switch (name)
            {
                case "BasicProjectile":
                    flyweight.speed = 10f;
                    flyweight.damage = 50f;
                    flyweight.lifetime = 3f;
                    flyweight.size = 1f;
                    flyweight.critChance = 0.1f;
                    flyweight.critMultiplier = 1.5f;
                    break;

                case "FastProjectile":
                    flyweight.speed = 20f;
                    flyweight.damage = 30f;
                    flyweight.lifetime = 2f;
                    flyweight.size = 0.8f;
                    flyweight.critChance = 0.05f;
                    flyweight.critMultiplier = 1.3f;
                    break;

                case "HeavyProjectile":
                    flyweight.speed = 8f;
                    flyweight.damage = 100f;
                    flyweight.lifetime = 4f;
                    flyweight.size = 1.5f;
                    flyweight.critChance = 0.15f;
                    flyweight.critMultiplier = 1.8f;
                    break;

                case "HomingProjectile":
                    flyweight.speed = 12f;
                    flyweight.damage = 40f;
                    flyweight.lifetime = 5f;
                    flyweight.size = 1f;
                    flyweight.homing = true;
                    flyweight.turnSpeed = 180f;
                    flyweight.critChance = 0.08f;
                    flyweight.critMultiplier = 1.4f;
                    break;

                default:
                    // Generic defaults
                    flyweight.speed = 10f;
                    flyweight.damage = 50f;
                    flyweight.lifetime = 3f;
                    flyweight.size = 1f;
                    flyweight.critChance = 0.1f;
                    flyweight.critMultiplier = 1.5f;
                    break;
            }

            return flyweight;
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUI.Label(new Rect(10, 150, 300, 20), $"FlyweightFactory - Cached: {flyweightCache.Count}, Memory: {GetMemoryUsageEstimate()} bytes");

            int y = 170;
            foreach (var kvp in usageCount)
            {
                if (kvp.Value > 0)
                {
                    GUI.Label(new Rect(10, y, 300, 20), $"{kvp.Key}: {kvp.Value} uses");
                    y += 20;
                }
            }
        }
    }
}