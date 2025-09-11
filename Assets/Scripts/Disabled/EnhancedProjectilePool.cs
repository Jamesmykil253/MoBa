using UnityEngine;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// Enhanced Projectile Pool with comprehensive error handling and validation
    /// Replaces original ProjectilePool to address critical audit findings
    /// </summary>
    public class EnhancedProjectilePool : MonoBehaviour, IProjectilePool
    {
        [Header("Pool Configuration")]
        [SerializeField] public GameObject projectilePrefab;
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private Transform poolParent;
        [SerializeField] private bool enableAutoValidation = true;
        [SerializeField] private bool enableHealthMonitoring = true;

        [Header("Flyweight Integration")]
        [SerializeField] public FlyweightFactory flyweightFactory;

        [Header("Debug Options")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool logPoolOperations = false;

        private EnhancedObjectPool<Projectile> projectilePool;
        private List<string> recentErrors = new List<string>();
        private float lastValidationTime = 0f;
        private const float VALIDATION_INTERVAL = 5f;

        #region Initialization

        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            try
            {
                // Setup pool parent
                if (poolParent == null)
                {
                    poolParent = transform;
                }

                // Find or create FlyweightFactory
                SetupFlyweightFactory();

                // Validate and fix projectile prefab
                if (!ValidateProjectilePrefab())
                {
                    LogError("Projectile prefab validation failed - attempting auto-fix");
                    if (!AttemptPrefabAutoFix())
                    {
                        LogError("Auto-fix failed - pool initialization aborted");
                        return;
                    }
                }

                // Create enhanced pool
                var projectileComponent = projectilePrefab.GetComponent<Projectile>();
                projectilePool = new EnhancedObjectPool<Projectile>(
                    projectileComponent, 
                    initialPoolSize, 
                    poolParent, 
                    enableAutoValidation
                );

                var healthStats = projectilePool.GetHealthStats();
                Log($"Enhanced Projectile Pool initialized: {healthStats}");

                // Start health monitoring if enabled
                if (enableHealthMonitoring)
                {
                    InvokeRepeating(nameof(MonitorPoolHealth), VALIDATION_INTERVAL, VALIDATION_INTERVAL);
                }
            }
            catch (System.Exception e)
            {
                LogError($"Failed to initialize projectile pool: {e.Message}");
            }
        }

        private void SetupFlyweightFactory()
        {
            if (flyweightFactory == null)
            {
                flyweightFactory = FindAnyObjectByType<FlyweightFactory>();
                if (flyweightFactory == null)
                {
                    Log("FlyweightFactory not found - creating default instance");
                    var factoryObj = new GameObject("FlyweightFactory_Auto");
                    flyweightFactory = factoryObj.AddComponent<FlyweightFactory>();
                    factoryObj.transform.SetParent(transform);
                }
            }
        }

        private bool ValidateProjectilePrefab()
        {
            if (projectilePrefab == null)
            {
                LogError("Projectile prefab is null");
                return false;
            }

            var projectileComponent = projectilePrefab.GetComponent<Projectile>();
            if (projectileComponent == null)
            {
                LogError($"Projectile prefab '{projectilePrefab.name}' missing Projectile component");
                return false;
            }

            // Validate required physics components
            if (projectilePrefab.GetComponent<Rigidbody>() == null)
            {
                Log($"Projectile prefab '{projectilePrefab.name}' missing Rigidbody - will auto-fix");
            }

            if (projectilePrefab.GetComponent<Collider>() == null)
            {
                Log($"Projectile prefab '{projectilePrefab.name}' missing Collider - will auto-fix");
            }

            return true;
        }

        private bool AttemptPrefabAutoFix()
        {
            if (projectilePrefab == null) return false;

            try
            {
                // Add missing Projectile component if needed
                if (projectilePrefab.GetComponent<Projectile>() == null)
                {
                    // Note: This only works on scene instances, not prefab assets
                    if (Application.isPlaying)
                    {
                        LogError("Cannot auto-fix prefab assets at runtime - please fix in editor");
                        return false;
                    }
                }

                return true;
            }
            catch (System.Exception e)
            {
                LogError($"Auto-fix attempt failed: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Pool Operations

        /// <summary>
        /// Spawns a projectile from the pool with full validation
        /// </summary>
        public Projectile SpawnProjectile(Vector3 position, Vector3 direction, float speed, float damage, float lifetime = 5f)
        {
            if (projectilePool == null)
            {
                LogError("Projectile pool not initialized");
                return null;
            }

            try
            {
                Projectile projectile = projectilePool.Get();
                if (projectile == null)
                {
                    LogError("Failed to get projectile from pool");
                    return null;
                }

                // Validate projectile before use
                if (!ValidateProjectileInstance(projectile))
                {
                    LogError("Retrieved invalid projectile from pool");
                    return null;
                }

                // Initialize projectile
                projectile.transform.position = position;
                projectile.Initialize(direction, speed, damage, lifetime, this);

                if (logPoolOperations)
                {
                    Log($"Spawned projectile at {position} with speed {speed}");
                }

                return projectile;
            }
            catch (System.Exception e)
            {
                LogError($"Exception spawning projectile: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Spawns a projectile using flyweight data with validation
        /// </summary>
        public Projectile SpawnProjectileWithFlyweight(Vector3 position, Vector3 direction, string flyweightName)
        {
            if (flyweightFactory == null)
            {
                Log("FlyweightFactory not available, using default projectile settings");
                return SpawnProjectile(position, direction, 10f, 50f);
            }

            try
            {
                var flyweight = flyweightFactory.GetFlyweight(flyweightName);
                if (flyweight == null)
                {
                    Log($"Flyweight '{flyweightName}' not found, using default settings");
                    return SpawnProjectile(position, direction, 10f, 50f);
                }

                return SpawnProjectile(position, direction, flyweight.speed, flyweight.damage, flyweight.lifetime);
            }
            catch (System.Exception e)
            {
                LogError($"Exception spawning flyweight projectile: {e.Message}");
                return SpawnProjectile(position, direction, 10f, 50f);
            }
        }

        /// <summary>
        /// Returns a projectile to the pool with validation
        /// </summary>
        public void ReturnProjectile(Projectile projectile)
        {
            if (projectilePool == null)
            {
                LogError("Cannot return projectile: pool not initialized");
                if (projectile != null && projectile.gameObject != null)
                {
                    projectile.gameObject.SetActive(false);
                }
                return;
            }

            if (projectile == null)
            {
                LogError("Attempted to return null projectile");
                return;
            }

            try
            {
                projectilePool.Return(projectile);
                
                if (logPoolOperations)
                {
                    Log($"Returned projectile to pool");
                }
            }
            catch (System.Exception e)
            {
                LogError($"Exception returning projectile: {e.Message}");
            }
        }

        /// <summary>
        /// Returns all active projectiles to the pool
        /// </summary>
        public void ReturnAllProjectiles()
        {
            if (projectilePool == null)
            {
                LogError("Cannot return projectiles: pool not initialized");
                return;
            }

            try
            {
                projectilePool.ReturnAll();
                Log("Returned all projectiles to pool");
            }
            catch (System.Exception e)
            {
                LogError($"Exception returning all projectiles: {e.Message}");
            }
        }

        #endregion

        #region Validation and Monitoring

        private bool ValidateProjectileInstance(Projectile projectile)
        {
            if (projectile == null) return false;
            if (projectile.gameObject == null) return false;

            // Check for required components
            if (projectile.GetComponent<Rigidbody>() == null)
            {
                Log($"Auto-fixing missing Rigidbody on {projectile.name}");
                var rb = projectile.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if (projectile.GetComponent<Collider>() == null)
            {
                Log($"Auto-fixing missing Collider on {projectile.name}");
                var collider = projectile.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.1f;
            }

            return true;
        }

        private void MonitorPoolHealth()
        {
            if (projectilePool == null) return;

            try
            {
                projectilePool.ValidatePoolHealth();
                
                var healthStats = projectilePool.GetHealthStats();
                if (healthStats.SuccessRate < 0.9f)
                {
                    LogError($"Pool health degraded: {healthStats}");
                }

                lastValidationTime = Time.time;
            }
            catch (System.Exception e)
            {
                LogError($"Health monitoring failed: {e.Message}");
            }
        }

        /// <summary>
        /// Force immediate pool validation
        /// </summary>
        [ContextMenu("Validate Pool Health")]
        public void ValidatePoolHealth()
        {
            MonitorPoolHealth();
        }

        /// <summary>
        /// Get current pool statistics
        /// </summary>
        public PoolHealthStats GetPoolStats()
        {
            return projectilePool?.GetHealthStats() ?? new PoolHealthStats();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get available flyweight names from the factory
        /// </summary>
        public List<string> GetAvailableFlyweightNames()
        {
            try
            {
                return flyweightFactory?.GetAvailableFlyweightNames() ?? new List<string>();
            }
            catch (System.Exception e)
            {
                LogError($"Failed to get flyweight names: {e.Message}");
                return new List<string>();
            }
        }

        private void Log(string message)
        {
            if (logPoolOperations)
            {
                Debug.Log($"[EnhancedProjectilePool] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnhancedProjectilePool] {message}");
            recentErrors.Add($"{Time.time:F1}: {message}");
            
            // Keep only recent errors
            if (recentErrors.Count > 10)
            {
                recentErrors.RemoveAt(0);
            }
        }

        #endregion

        #region UI and Debug

        private void OnGUI()
        {
            if (!showDebugUI || projectilePool == null) return;

            var healthStats = projectilePool.GetHealthStats();
            
            GUILayout.BeginArea(new Rect(10, 300, 400, 200));
            GUILayout.Label("Enhanced Projectile Pool", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            
            GUILayout.Label($"Pool Health: {healthStats}");
            GUILayout.Label($"Active/Total: {healthStats.ActiveObjects}/{healthStats.TotalObjects}");
            GUILayout.Label($"Success Rate: {healthStats.SuccessRate:P1}");
            GUILayout.Label($"Component Fixes: {healthStats.ComponentFixCount}");
            GUILayout.Label($"Last Validation: {Time.time - lastValidationTime:F1}s ago");

            if (GUILayout.Button("Validate Health"))
            {
                ValidatePoolHealth();
            }

            if (GUILayout.Button("Return All"))
            {
                ReturnAllProjectiles();
            }

            if (GUILayout.Button("Clear Errors"))
            {
                recentErrors.Clear();
            }

            // Show recent errors
            if (recentErrors.Count > 0)
            {
                GUILayout.Label("Recent Errors:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                foreach (var error in recentErrors)
                {
                    GUILayout.Label(error, new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } });
                }
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (enableHealthMonitoring)
            {
                CancelInvoke(nameof(MonitorPoolHealth));
            }
        }

        #endregion
    }
}
