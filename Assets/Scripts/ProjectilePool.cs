using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Interface for projectile pool operations
    /// </summary>
    public interface IProjectilePool
    {
        Projectile SpawnProjectile(Vector3 position, Vector3 direction, float speed, float damage, float lifetime = 5f);
        void ReturnProjectile(Projectile projectile);
        void ReturnAllProjectiles();
    }

    /// <summary>
    /// Specialized object pool for projectiles
    /// Manages projectile lifecycle to prevent garbage collection spikes
    /// </summary>
    public class ProjectilePool : MonoBehaviour, IProjectilePool
    {
        [SerializeField] public GameObject projectilePrefab;
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private Transform poolParent;

        private ComponentPool<Projectile> projectilePool;

        private void Awake()
        {
            // Validate prefab assignment
            if (projectilePrefab == null)
            {
                Debug.LogError("[ProjectilePool] Projectile prefab is not assigned! Cannot initialize pool.");
                enabled = false;
                return;
            }

            if (poolParent == null)
            {
                poolParent = transform;
            }

            // Fix the prefab before creating the pool
            FixProjectilePrefab();

            // Create the pool using UnifiedObjectPool system
            var projectileComponent = projectilePrefab.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                projectilePool = UnifiedObjectPool.GetComponentPool<Projectile>("ProjectilePool", projectileComponent, initialPoolSize, 100);
                if (projectilePool != null)
                {
                    Debug.Log("[ProjectilePool] ✅ Projectile pool initialized successfully with UnifiedObjectPool");
                }
                else
                {
                    Debug.LogError("[ProjectilePool] ❌ Failed to create projectile pool with UnifiedObjectPool");
                    enabled = false;
                }
            }
            else
            {
                Debug.LogError("[ProjectilePool] ❌ Failed to initialize projectile pool - Projectile component still missing after fix attempt");
                enabled = false;
            }
        }

        /// <summary>
        /// Fixes the projectile prefab to ensure it has all required components
        /// </summary>
        private void FixProjectilePrefab()
        {
            if (projectilePrefab == null) return;

            // Clean any missing script references first
            CleanMissingScripts(projectilePrefab);

            // Ensure Projectile component exists
            if (projectilePrefab.GetComponent<Projectile>() == null)
            {
                projectilePrefab.AddComponent<Projectile>();
                Debug.Log("[ProjectilePool] Added missing Projectile component");
            }

            // Ensure Rigidbody component exists
            Rigidbody rb = projectilePrefab.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectilePrefab.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("[ProjectilePool] Added missing Rigidbody component");
            }

            // Ensure Collider component exists
            Collider collider = projectilePrefab.GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider sphereCollider = projectilePrefab.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.1f;
                Debug.Log("[ProjectilePool] Added missing SphereCollider component");
            }
            else
            {
                // Ensure existing collider is a trigger
                collider.isTrigger = true;
            }

            Debug.Log("[ProjectilePool] Projectile prefab has been fixed and is ready for use");
        }

        /// <summary>
        /// Cleans missing script references from a GameObject
        /// </summary>
        private void CleanMissingScripts(GameObject obj)
        {
            if (obj == null) return;

            Component[] components = obj.GetComponents<Component>();
            int missingCount = 0;
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                }
            }

            if (missingCount > 0)
            {
                Debug.LogWarning($"[ProjectilePool] Found {missingCount} missing script references on {obj.name}, attempting to clean...");
                
                // Use Unity's built-in method to remove missing scripts
                #if UNITY_EDITOR
                int removed = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                if (removed > 0)
                {
                    Debug.Log($"[ProjectilePool] ✅ Removed {removed} missing script references from {obj.name}");
                }
                #endif
            }
        }

        /// <summary>
        /// Spawns a projectile from the pool
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="direction">Movement direction</param>
        /// <param name="speed">Projectile speed</param>
        /// <param name="damage">Projectile damage</param>
        /// <param name="lifetime">How long before auto-return to pool</param>
        /// <returns>The spawned projectile</returns>
        public Projectile SpawnProjectile(Vector3 position, Vector3 direction, float speed, float damage, float lifetime = 5f)
        {
            Projectile projectile = projectilePool.Get();
            projectile.transform.position = position;
            projectile.Initialize(direction, speed, damage, lifetime, this);
            return projectile;
        }

        /// <summary>
        /// Returns a projectile to the pool
        /// </summary>
        /// <param name="projectile">Projectile to return</param>
        public void ReturnProjectile(Projectile projectile)
        {
            projectilePool.Return(projectile);
        }

        /// <summary>
        /// Returns all active projectiles to the pool
        /// </summary>
        public void ReturnAllProjectiles()
        {
            // UnifiedObjectPool doesn't have ReturnAll method, 
            // but we could implement it or just clear the specific pool
            Debug.Log("[ProjectilePool] ReturnAll not implemented in UnifiedObjectPool - consider manual cleanup");
        }

        private void OnGUI()
        {
            if (projectilePool != null)
            {
                var stats = projectilePool.GetStats();
                GUI.Label(new Rect(10, 130, 300, 20), $"Projectile Pool - Total: {stats.total}, Available: {stats.available}, Active: {stats.active}");
            }
        }
    }

    /// <summary>
    /// Projectile component that works with the object pool
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float defaultSpeed = 10f;
        [SerializeField] private float defaultDamage = 50f;
        [SerializeField] private float defaultLifetime = 3f;

        // Note: These fields are used in the Initialize method and serve as fallbacks
        // They can be modified in the Inspector for different projectile types

        private Vector3 direction;
        private float speed;
        private float damage;
        private float lifetime;
        private float age;
        private IProjectilePool pool;

        private void Update()
        {
            // Move the projectile
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            // Update age and check lifetime
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// Initializes the projectile with movement and damage parameters
        /// </summary>
        public void Initialize(Vector3 direction, float speed, float damage, float lifetime, IProjectilePool pool)
        {
            this.direction = direction.normalized;
            this.speed = speed > 0 ? speed : defaultSpeed; // Use default if not provided
            this.damage = damage > 0 ? damage : defaultDamage; // Use default if not provided
            this.lifetime = lifetime > 0 ? lifetime : defaultLifetime; // Use default if not provided
            this.pool = pool;
            this.age = 0f;

            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Handle collision with target
            var target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);

                // Simple damage logging instead of complex event system
                Debug.Log($"Projectile dealt {damage} damage to {other.gameObject.name}");

                ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Handle 3D collision as fallback
            var target = collision.gameObject.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);

                // Simple damage logging instead of complex event system
                Debug.Log($"Projectile dealt {damage} damage to {collision.gameObject.name}");

                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (pool != null)
            {
                pool.ReturnProjectile(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}