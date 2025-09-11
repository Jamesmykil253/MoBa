using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Runtime Projectile Prefab Fixer - addresses missing script components in projectile prefabs
    /// This component automatically fixes missing Projectile components when they are instantiated
    /// </summary>
    public class RuntimeProjectileFixer : MonoBehaviour
    {
        [Header("Auto-Fix Settings")]
        [SerializeField] private bool enableAutoFix = true;
        [SerializeField] private bool logFixActions = true;
        
        [Header("Default Projectile Settings")]
        [SerializeField] private float defaultSpeed = 15f;
        [SerializeField] private float defaultDamage = 50f;
        [SerializeField] private float defaultLifetime = 3f;
        
        private void Awake()
        {
            if (enableAutoFix)
            {
                FixProjectileComponent();
            }
        }
        
        /// <summary>
        /// Ensures this GameObject has a proper Projectile component
        /// </summary>
        private void FixProjectileComponent()
        {
            // Check if we already have a Projectile component
            Projectile projectileComponent = GetComponent<Projectile>();
            
            if (projectileComponent == null)
            {
                // Add the missing Projectile component
                projectileComponent = gameObject.AddComponent<Projectile>();
                
                if (logFixActions)
                {
                    Debug.Log($"[RuntimeProjectileFixer] Added missing Projectile component to {gameObject.name}");
                }
            }
            
            // Ensure we have required physics components
            EnsurePhysicsComponents();
        }
        
        /// <summary>
        /// Ensures the GameObject has required physics components
        /// </summary>
        private void EnsurePhysicsComponents()
        {
            // Ensure Rigidbody exists
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false; // Projectiles typically don't use gravity
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent unwanted rotation
                
                if (logFixActions)
                {
                    Debug.Log($"[RuntimeProjectileFixer] Added missing Rigidbody to {gameObject.name}");
                }
            }
            
            // Ensure Collider exists
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                // Add a sphere collider as default
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true; // Projectiles should be triggers
                sphereCollider.radius = 0.1f; // Small radius for projectiles
                
                if (logFixActions)
                {
                    Debug.Log($"[RuntimeProjectileFixer] Added missing SphereCollider to {gameObject.name}");
                }
            }
            else
            {
                // Ensure existing collider is a trigger
                collider.isTrigger = true;
            }
        }
        
        /// <summary>
        /// Configure the projectile with default settings
        /// </summary>
        public void ConfigureProjectile()
        {
            Projectile projectile = GetComponent<Projectile>();
            if (projectile != null)
            {
                // Use reflection to set default values if they're not already set
                var projectileType = typeof(Projectile);
                
                // Set default speed if not configured
                var speedField = projectileType.GetField("defaultSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (speedField != null)
                {
                    float currentSpeed = (float)speedField.GetValue(projectile);
                    if (currentSpeed <= 0)
                    {
                        speedField.SetValue(projectile, defaultSpeed);
                    }
                }
                
                // Set default damage if not configured
                var damageField = projectileType.GetField("defaultDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (damageField != null)
                {
                    float currentDamage = (float)damageField.GetValue(projectile);
                    if (currentDamage <= 0)
                    {
                        damageField.SetValue(projectile, defaultDamage);
                    }
                }
                
                // Set default lifetime if not configured
                var lifetimeField = projectileType.GetField("defaultLifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (lifetimeField != null)
                {
                    float currentLifetime = (float)lifetimeField.GetValue(projectile);
                    if (currentLifetime <= 0)
                    {
                        lifetimeField.SetValue(projectile, defaultLifetime);
                    }
                }
                
                if (logFixActions)
                {
                    Debug.Log($"[RuntimeProjectileFixer] Configured projectile defaults for {gameObject.name}");
                }
            }
        }
        
        /// <summary>
        /// Static method to fix any GameObject that should be a projectile
        /// </summary>
        public static void FixProjectileGameObject(GameObject obj)
        {
            if (obj == null) return;
            
            // Add the runtime fixer component if it doesn't exist
            RuntimeProjectileFixer fixer = obj.GetComponent<RuntimeProjectileFixer>();
            if (fixer == null)
            {
                fixer = obj.AddComponent<RuntimeProjectileFixer>();
            }
            
            // Configure the projectile
            fixer.ConfigureProjectile();
        }
    }
}
