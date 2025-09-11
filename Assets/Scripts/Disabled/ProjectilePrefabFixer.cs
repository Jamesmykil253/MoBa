using UnityEngine;
using Unity.Netcode;

namespace MOBA
{
    /// <summary>
    /// Runtime component to fix missing script issues on projectile prefabs
    /// This should be added to projectile prefabs to ensure they have all required components
    /// </summary>
    public class ProjectilePrefabFixer : MonoBehaviour
    {
        [Header("Auto-Fix Settings")]
        [SerializeField] private bool enableAutoFix = true;
        [SerializeField] private bool logFixActions = true;
        
        [Header("Default Projectile Settings")]
        [SerializeField] private float defaultSpeed = 15f;
        [SerializeField] private float defaultDamage = 50f;
        [SerializeField] private float defaultLifetime = 3f;
        
        [Header("Physics Settings")]
        [SerializeField] private bool useGravity = false;
        [SerializeField] private bool isTrigger = true;
        [SerializeField] private float colliderRadius = 0.1f;
        
        private void Awake()
        {
            if (enableAutoFix)
            {
                FixAllComponents();
            }
        }
        
        /// <summary>
        /// Fixes all missing components on this projectile
        /// </summary>
        public void FixAllComponents()
        {
            FixProjectileComponent();
            FixPhysicsComponents();
            FixNetworkComponents();
            
            if (logFixActions)
            {
                Debug.Log($"[ProjectilePrefabFixer] ✅ Fixed all components on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Ensures this GameObject has a proper Projectile component
        /// </summary>
        private void FixProjectileComponent()
        {
            Projectile projectileComponent = GetComponent<Projectile>();
            
            if (projectileComponent == null)
            {
                projectileComponent = gameObject.AddComponent<Projectile>();
                
                // Configure the projectile with default settings using reflection
                ConfigureProjectileDefaults(projectileComponent);
                
                if (logFixActions)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Added missing Projectile component to {gameObject.name}");
                }
            }
        }
        
        /// <summary>
        /// Configures projectile default values using reflection
        /// </summary>
        private void ConfigureProjectileDefaults(Projectile projectile)
        {
            if (projectile == null) return;
            
            try
            {
                var projectileType = typeof(Projectile);
                
                // Set default speed if field exists
                var speedField = projectileType.GetField("defaultSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (speedField != null && speedField.FieldType == typeof(float))
                {
                    speedField.SetValue(projectile, defaultSpeed);
                }
                
                // Set default damage if field exists
                var damageField = projectileType.GetField("defaultDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (damageField != null && damageField.FieldType == typeof(float))
                {
                    damageField.SetValue(projectile, defaultDamage);
                }
                
                // Set default lifetime if field exists
                var lifetimeField = projectileType.GetField("defaultLifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (lifetimeField != null && lifetimeField.FieldType == typeof(float))
                {
                    lifetimeField.SetValue(projectile, defaultLifetime);
                }
                
                if (logFixActions)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Configured projectile defaults: Speed={defaultSpeed}, Damage={defaultDamage}, Lifetime={defaultLifetime}");
                }
            }
            catch (System.Exception ex)
            {
                if (logFixActions)
                {
                    Debug.LogWarning($"[ProjectilePrefabFixer] Could not configure projectile defaults: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Ensures the GameObject has required physics components
        /// </summary>
        private void FixPhysicsComponents()
        {
            // Ensure Rigidbody exists
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                if (logFixActions)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Added missing Rigidbody to {gameObject.name}");
                }
            }
            
            // Configure Rigidbody
            rb.useGravity = useGravity;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // Ensure Collider exists
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = isTrigger;
                sphereCollider.radius = colliderRadius;
                
                if (logFixActions)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Added missing SphereCollider to {gameObject.name}");
                }
            }
            else
            {
                // Ensure existing collider is configured correctly
                collider.isTrigger = isTrigger;
                if (collider is SphereCollider sphere)
                {
                    sphere.radius = colliderRadius;
                }
            }
        }
        
        /// <summary>
        /// Ensures the GameObject has required network components
        /// </summary>
        private void FixNetworkComponents()
        {
            // Check if we need NetworkObject (only for network projectiles)
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject == null && gameObject.name.Contains("Network"))
            {
                networkObject = gameObject.AddComponent<NetworkObject>();
                
                // Configure NetworkObject for projectiles
                networkObject.DeferredDespawnTick = 0;
                // Note: Ownership property may vary by Netcode version - using default settings
                networkObject.SynchronizeTransform = true;
                networkObject.SpawnWithObservers = true;
                networkObject.DontDestroyWithOwner = false;
                
                if (logFixActions)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Added missing NetworkObject to {gameObject.name}");
                }
            }
        }
        
        /// <summary>
        /// Static method to fix any projectile GameObject
        /// </summary>
        public static void FixProjectileGameObject(GameObject obj)
        {
            if (obj == null) return;
            
            // Add the fixer component if it doesn't exist
            ProjectilePrefabFixer fixer = obj.GetComponent<ProjectilePrefabFixer>();
            if (fixer == null)
            {
                fixer = obj.AddComponent<ProjectilePrefabFixer>();
            }
            
            // Fix all components
            fixer.FixAllComponents();
        }
        
        /// <summary>
        /// Clean up any duplicate or conflicting components
        /// </summary>
        public void CleanupDuplicateComponents()
        {
            // Remove duplicate ProjectilePrefabFixer components
            ProjectilePrefabFixer[] fixers = GetComponents<ProjectilePrefabFixer>();
            if (fixers.Length > 1)
            {
                for (int i = 1; i < fixers.Length; i++)
                {
                    DestroyImmediate(fixers[i]);
                }
                
                if (logFixActions)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Removed {fixers.Length - 1} duplicate fixer components");
                }
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Editor method to manually trigger fixes
        /// </summary>
        [ContextMenu("Fix All Components")]
        public void EditorFixAllComponents()
        {
            FixAllComponents();
            CleanupDuplicateComponents();
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
        #endif
    }
}
