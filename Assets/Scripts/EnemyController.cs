using UnityEngine;
using MOBA.Networking;
using MOBA.Debugging;
using MOBA.Effects;

namespace MOBA
{
    /// <summary>
    /// Basic enemy controller that implements IDamageable interface
    /// Designed to work with the existing MOBA architecture
    /// Uses similar patterns to PlayerController for consistency
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Enemy Stats")]
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float damage = 50f;
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float moveSpeed = 200f;

        [Header("AI Behavior")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float chaseRange = 12f;
        [SerializeField] private LayerMask targetLayerMask = -1;

        [Header("Visual Settings")]
        [SerializeField] private Color enemyColor = Color.red;

        [Header("Rewards")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private Transform coinSpawnPoint;
        [SerializeField] private int coinScoreValue = 10;
        [SerializeField] private float coinLaunchForce = 2f;

        // Component references
        private Rigidbody rb;
        private Collider col;
        private Renderer meshRenderer;

        // AI State
        private Transform target;
        private float lastAttackTime;
        private Vector3 originalPosition;
        private bool isChasing;

        // State tracking
        private bool isDead;

        [Header("Debug")]
        [SerializeField] private bool logDebugMessages = true;
        private bool isInitialized;
        private bool isReturning;
        private Transform lastLoggedTarget;

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Enemy,
                GameDebugSystemTag.Enemy,
                mechanic,
                subsystem: nameof(EnemyController),
                actor: gameObject != null ? gameObject.name : null);
        }

        private void Awake()
        {
            if (!logDebugMessages)
            {
                logDebugMessages = true;
            }
            Log(GameDebugMechanicTag.Initialization, "Awake called - awaiting manual initialization.");
        }

        /// <summary>
        /// Manual initialization - call this explicitly for MOBA best practices
        /// </summary>
        public void ManualInitialize()
        {
            if (isInitialized)
            {
                return;
            }
            // Get required components
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            meshRenderer = GetComponent<Renderer>();

            // Initialize
            InitializeEnemy();
            Log(GameDebugMechanicTag.Initialization, "Manual initialization completed.");
        }

        private void InitializeEnemy()
        {
            currentHealth = maxHealth;
            originalPosition = transform.position;
            isDead = false;
            isInitialized = true;

            // Set enemy color
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = enemyColor;
            }

            Log(GameDebugMechanicTag.Initialization,
                "Enemy initialized with starting stats.",
                ("Position", transform.position),
                ("Health", maxHealth));
        }

        private void Update()
        {
            if (!isInitialized || isDead) return;

            // Find targets
            FindTarget();

            // Update behavior based on target
            if (target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                
                if (distanceToTarget <= attackRange)
                {
                    // Attack if in range
                    TryAttack();
                }
                else if (distanceToTarget <= chaseRange)
                {
                    // Chase target
                    ChaseTarget();
                }
                else
                {
                    // Return to original position
                    ReturnToOrigin();
                }
            }
            else
            {
                // No target, return to origin
                ReturnToOrigin();
            }
        }

        private void FindTarget()
        {
            // Look for targets within detection range
            Collider[] potentialTargets = Physics.OverlapSphere(transform.position, detectionRange, targetLayerMask);
            
            float closestDistance = float.MaxValue;
            Transform closestTarget = null;

            foreach (var collider in potentialTargets)
            {
                // Look for player controllers or other damageable objects
                if (collider.GetComponent<SimplePlayerController>() != null)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = collider.transform;
                    }
                }
            }

            if (closestTarget != lastLoggedTarget)
            {
                if (closestTarget != null)
                {
                    Log(GameDebugMechanicTag.Targeting,
                        "Acquired new target.",
                        ("Target", closestTarget.name),
                        ("Distance", closestDistance));
                }
                else if (lastLoggedTarget != null)
                {
                    Log(GameDebugMechanicTag.Targeting,
                        "Lost current target.");
                }
                lastLoggedTarget = closestTarget;
            }

            target = closestTarget;
        }

        private void ChaseTarget()
        {
            if (target == null) return;

            if (!isChasing)
            {
                Log(GameDebugMechanicTag.StateChange,
                    "Entered chase state.",
                    ("Target", target.name));
            }

            isChasing = true;
            isReturning = false;

            Vector3 direction = (target.position - transform.position);
            direction.y = 0f;
            direction.Normalize();

            float clampedSpeed = Mathf.Clamp(moveSpeed, 0f, 10f);
            float currentSpeed = clampedSpeed * 1.1f;

            Vector3 desiredVelocity = direction * currentSpeed;
            desiredVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, Time.deltaTime * 5f);

            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }

        private void ReturnToOrigin()
        {
            float distanceToOrigin = Vector3.Distance(transform.position, originalPosition);

            if (distanceToOrigin > 1f)
            {
                if (isChasing)
                {
                    Log(GameDebugMechanicTag.StateChange,
                        "Lost target; returning to origin.",
                        ("Distance", distanceToOrigin));
                }

                isChasing = false;
                Vector3 direction = (originalPosition - transform.position);
                direction.y = 0f;
                direction.Normalize();

                float clampedSpeed = Mathf.Clamp(moveSpeed, 0f, 10f) * 0.7f;
                Vector3 desiredVelocity = direction * clampedSpeed;
                desiredVelocity.y = rb.linearVelocity.y;

                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, Time.deltaTime * 4f);

                if (!isReturning)
                {
                    Log(GameDebugMechanicTag.StateChange,
                        "Navigating back to origin.");
                    isReturning = true;
                }
            }
            else
            {
                isChasing = false;
                rb.linearVelocity = Vector3.zero;
                if (isReturning)
                {
                    Log(GameDebugMechanicTag.StateChange,
                        "Reached origin and idling.");
                    isReturning = false;
                }
            }
        }

        private void TryAttack()
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        private void PerformAttack()
        {
            if (target == null) return;

            // Check if target is still in range
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget <= attackRange)
            {
                // Line-of-sight check: raycast from enemy to target, ignore self
                Vector3 direction = (target.position - transform.position).normalized;
                if (!Physics.Raycast(transform.position, direction, distanceToTarget, ~LayerMask.GetMask("Enemy")))
                {
                    // Deal damage to target
                    var damageable = target.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damage);
                        Log(GameDebugMechanicTag.Combat,
                            "Enemy attack executed.",
                            ("Target", target.name),
                            ("Damage", damage));

                        // Create attack effect
                        CreateAttackEffect();
                    }
                }
            }
        }

        private void CreateAttackEffect()
        {
            // Different effect color based on state
            Color effectColor = isChasing ? Color.red : new Color(1f, 0.5f, 0f); // Red when chasing, orange when not
            
            EffectPoolService.SpawnSphereEffect(transform.position + transform.forward * 1.5f, effectColor, 0.5f, 0.4f, transform.root);
        }

        public void TakeDamage(float damage)
        {
            if (!isInitialized) return;
            if (isDead) return;

            currentHealth -= damage;
            Log(GameDebugMechanicTag.Damage,
                "Enemy took damage.",
                ("Damage", damage),
                ("CurrentHealth", currentHealth),
                ("MaxHealth", maxHealth));

            // Create damage effect
            CreateDamageEffect();

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (currentHealth <= maxHealth * 0.25f)
            {
                Log(GameDebugMechanicTag.StateChange,
                    "Enemy health critical.",
                    ("CurrentHealth", currentHealth));
            }
        }

        // IDamageable interface implementation
        public float GetHealth() => currentHealth;
        public bool IsDead() => isDead;

        private void CreateDamageEffect()
        {
            // Flash red briefly
            if (meshRenderer != null)
            {
                StartCoroutine(FlashRed());
            }
        }

        private System.Collections.IEnumerator FlashRed()
        {
            Color originalColor = meshRenderer.material.color;
            meshRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            meshRenderer.material.color = originalColor;
        }

        private void Die()
        {
            isDead = true;
            Log(GameDebugMechanicTag.StateChange, "Enemy died and will trigger death effects.");

            // Stop movement
            rb.linearVelocity = Vector3.zero;

            // Create death effect
            CreateDeathEffect();

            SpawnCoinReward();

            // Disable enemy after short delay
            Invoke(nameof(DisableEnemy), 2f);
        }

        private void SpawnCoinReward()
        {
            if (coinPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = coinSpawnPoint != null ? coinSpawnPoint.position : transform.position + Vector3.up * 0.5f;
            var coinInstance = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            if (coinInstance.TryGetComponent<Rigidbody>(out var coinRb))
            {
                Vector3 launchDirection = Vector3.up + Random.insideUnitSphere * 0.25f;
                coinRb.AddForce(launchDirection.normalized * Mathf.Max(0f, coinLaunchForce), ForceMode.Impulse);
            }

            if (coinInstance.TryGetComponent<CoinPickup>(out var coinPickup))
            {
                coinPickup.Initialize(Mathf.Max(1, coinScoreValue));
            }

            Log(GameDebugMechanicTag.Score,
                "Dropped coin reward.",
                ("ScoreValue", coinScoreValue));
        }

        private void CreateDeathEffect()
        {
            EffectPoolService.SpawnSphereBurst(transform.position, Color.yellow, 2f, 8, 2f, 0.25f, transform.root);
        }

        private void DisableEnemy()
        {
            // In a real game, this would return the enemy to an object pool
            gameObject.SetActive(false);
        }

        public void Respawn()
        {
            if (!isInitialized)
            {
                ManualInitialize();
            }
            // Reset enemy state
            isDead = false;
            currentHealth = maxHealth;
            transform.position = originalPosition;
            target = null;
            isChasing = false;
            
            gameObject.SetActive(true);
            
            Log(GameDebugMechanicTag.Spawning, "Enemy respawned with reset state.");
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw chase range
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            // Draw line to target
            if (target != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, target.position);
            }

            // Draw line to origin
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, originalPosition);
            }
        }

        private void Log(GameDebugMechanicTag mechanic, string message, params (string Key, object Value)[] details)
        {
            if (!logDebugMessages)
            {
                return;
            }

            GameDebug.Log(BuildContext(mechanic), message, details);
        }
    }
}
