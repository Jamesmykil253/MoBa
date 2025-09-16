using UnityEngine;
using MOBA.Debugging;

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

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Projectile,
                GameDebugSystemTag.Projectile,
                mechanic,
                subsystem: nameof(ProjectilePool),
                actor: gameObject != null ? gameObject.name : null);
        }

        private void Awake()
        {
            // Validate prefab assignment
            if (projectilePrefab == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Projectile prefab not assigned; disabling pool.");
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
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                        "Projectile pool initialized via UnifiedObjectPool.",
                        ("InitialSize", initialPoolSize));
                }
                else
                {
                    GameDebug.LogError(BuildContext(GameDebugMechanicTag.Initialization),
                        "Failed to create projectile pool via UnifiedObjectPool.");
                    enabled = false;
                }
            }
            else
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Initialization),
                    "Projectile component missing on prefab after fix attempt; disabling pool.");
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
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Recovery),
                    "Added missing Projectile component to prefab.");
            }

            // Ensure Rigidbody component exists
            Rigidbody rb = projectilePrefab.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectilePrefab.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Recovery),
                    "Added missing Rigidbody component to projectile prefab.");
            }

            // Ensure Collider component exists
            Collider collider = projectilePrefab.GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider sphereCollider = projectilePrefab.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.1f;
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Recovery),
                    "Added missing SphereCollider component to projectile prefab.");
            }
            else
            {
                // Ensure existing collider is a trigger
                collider.isTrigger = true;
            }

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Recovery),
                "Projectile prefab dependencies verified.");
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
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Recovery),
                    "Missing script references detected; attempting cleanup.",
                    ("Count", missingCount),
                    ("Object", obj.name));

                // Use Unity's built-in method to remove missing scripts
                #if UNITY_EDITOR
                int removed = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                if (removed > 0)
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Recovery),
                        "Removed missing script references from projectile prefab.",
                        ("Removed", removed),
                        ("Object", obj.name));
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
            GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Recovery),
                "ReturnAll not implemented in UnifiedObjectPool; manual cleanup required.");
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

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Projectile,
                GameDebugSystemTag.Projectile,
                mechanic,
                subsystem: nameof(Projectile),
                actor: gameObject != null ? gameObject.name : null);
        }

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
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Projectile dealt damage via trigger collision.",
                    ("Target", other.gameObject.name),
                    ("Damage", damage));

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
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Projectile dealt damage via physics collision.",
                    ("Target", collision.gameObject.name),
                    ("Damage", damage));

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
