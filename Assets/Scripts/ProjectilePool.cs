using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Specialized object pool for projectiles
    /// Manages projectile lifecycle to prevent garbage collection spikes
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] public GameObject projectilePrefab;
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private Transform poolParent;
        [SerializeField] public FlyweightFactory flyweightFactory;

        private ObjectPool<Projectile> projectilePool;

        private void Awake()
        {
            if (poolParent == null)
            {
                poolParent = transform;
            }

            // Find FlyweightFactory if not assigned
            if (flyweightFactory == null)
            {
                flyweightFactory = FindAnyObjectByType<FlyweightFactory>();
                if (flyweightFactory == null)
                {
                    Debug.LogWarning("FlyweightFactory not found. Creating one...");
                    var factoryObj = new GameObject("FlyweightFactory");
                    flyweightFactory = factoryObj.AddComponent<FlyweightFactory>();
                }
            }

            // Create the pool with projectile component
            var projectileComponent = projectilePrefab.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                projectilePool = new ObjectPool<Projectile>(projectileComponent, initialPoolSize, poolParent);
                Debug.Log("Projectile pool initialized successfully");
            }
            else
            {
                // Try to add Projectile component to prefab if missing
                projectileComponent = projectilePrefab.AddComponent<Projectile>();
                projectilePool = new ObjectPool<Projectile>(projectileComponent, initialPoolSize, poolParent);
                Debug.LogWarning("Projectile component was missing from prefab and has been added automatically");
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
        /// Spawns a projectile using flyweight data
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="direction">Movement direction</param>
        /// <param name="flyweightName">Name of the flyweight to use</param>
        /// <returns>The spawned projectile</returns>
        public Projectile SpawnProjectileWithFlyweight(Vector3 position, Vector3 direction, string flyweightName)
        {
            if (flyweightFactory == null)
            {
                Debug.LogWarning("FlyweightFactory not available, using default projectile");
                return SpawnProjectile(position, direction, 10f, 50f);
            }

            // Get flyweight data
            var flyweight = flyweightFactory.GetFlyweight(flyweightName);
            if (flyweight == null)
            {
                Debug.LogWarning($"Flyweight '{flyweightName}' not found, using default");
                return SpawnProjectile(position, direction, 10f, 50f);
            }

            // Spawn projectile with flyweight data
            Projectile projectile = projectilePool.Get();
            projectile.transform.position = position;
            projectile.Initialize(direction, flyweight.speed, flyweight.damage, flyweight.lifetime, this);

            // Apply flyweight visual properties for 3D
            if (projectile.TryGetComponent(out MeshRenderer meshRenderer) && flyweight.sprite != null)
            {
                // For 3D, we might need to apply material properties instead of sprite
                // This is a placeholder - you may need to adjust based on your 3D projectile setup
                Debug.Log("3D projectile visual properties not fully implemented yet");
            }

            return projectile;
        }

        /// <summary>
        /// Get available flyweight names from the factory
        /// </summary>
        public System.Collections.Generic.List<string> GetAvailableFlyweightNames()
        {
            return flyweightFactory?.GetAvailableFlyweightNames() ?? new System.Collections.Generic.List<string>();
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
            projectilePool.ReturnAll();
        }

        private void OnGUI()
        {
            if (projectilePool != null)
            {
                GUI.Label(new Rect(10, 130, 300, 20), $"Projectile Pool - Total: {projectilePool.TotalCount}, Available: {projectilePool.AvailableCount}, Active: {projectilePool.ActiveCount}");
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
        private ProjectilePool pool;

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
        public void Initialize(Vector3 direction, float speed, float damage, float lifetime, ProjectilePool pool)
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

                // Publish damage event
                EventBus.Publish(new DamageDealtEvent(
                    gameObject, // attacker (projectile)
                    other.gameObject, // defender
                    DamageResult.Create(
                        damage,
                        damage,
                        DamageType.Physical,
                        false,
                        0f,
                        0f,
                        0f
                    ),
                    null, // ability data
                    transform.position // attack position
                ));

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

                // Publish damage event
                EventBus.Publish(new DamageDealtEvent(
                    gameObject,
                    collision.gameObject,
                    DamageResult.Create(
                        damage,
                        damage,
                        DamageType.Physical,
                        false,
                        0f,
                        0f,
                        0f
                    ),
                    null,
                    collision.contacts[0].point
                ));

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

    /// <summary>
    /// Interface for objects that can take damage
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}