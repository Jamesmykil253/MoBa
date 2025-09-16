using UnityEngine;
using MOBA.Networking;

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
        private bool isInitialized;

        private void Awake()
        {
            // REMOVED: No auto-initialization - manual setup required for MOBA best practices
            UnityEngine.Debug.Log("[EnemyController] Awake - Manual initialization required");
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
            UnityEngine.Debug.Log("[EnemyController] Manual initialization completed");
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

            Debug.Log($"[EnemyController] Enemy initialized at {transform.position} with {maxHealth} health");
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

            target = closestTarget;
        }

        private void ChaseTarget()
        {
            if (target == null) return;

            isChasing = true;
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // Keep on ground

            // Use faster speed when chasing
            float currentSpeed = isChasing ? moveSpeed * 1.2f : moveSpeed;

            // FIXED: Remove Time.deltaTime from velocity calculation - velocity is already per-second
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.x = direction.x * currentSpeed;
            currentVelocity.z = direction.z * currentSpeed;
            rb.linearVelocity = currentVelocity;

            // Look at target
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }

        private void ReturnToOrigin()
        {
            float distanceToOrigin = Vector3.Distance(transform.position, originalPosition);
            
            if (distanceToOrigin > 1f)
            {
                isChasing = false;
                Vector3 direction = (originalPosition - transform.position).normalized;
                direction.y = 0;

                // Use slower speed when returning (not chasing)
                float returnSpeed = isChasing ? moveSpeed : moveSpeed * 0.5f;
                // FIXED: Remove Time.deltaTime from velocity calculation
                Vector3 currentVelocity = rb.linearVelocity;
                currentVelocity.x = direction.x * returnSpeed;
                currentVelocity.z = direction.z * returnSpeed;
                rb.linearVelocity = currentVelocity;
            }
            else
            {
                // Stop moving when close to origin
                isChasing = false;
                Vector3 currentVelocity = rb.linearVelocity;
                currentVelocity.x = 0f;
                currentVelocity.z = 0f;
                rb.linearVelocity = currentVelocity;
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
                        Debug.Log($"[EnemyController] Enemy attacked {target.name} for {damage} damage");

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
            
            // Simple attack effect - colored sphere
            GameObject attackEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            attackEffect.transform.position = transform.position + transform.forward * 1.5f;
            attackEffect.transform.localScale = Vector3.one * 0.5f;
            
            var renderer = attackEffect.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = effectColor;
            }

            // Remove collider so it doesn't interfere
            Destroy(attackEffect.GetComponent<Collider>());
            
            // Destroy effect after short time
            Destroy(attackEffect, 0.3f);
        }

        public void TakeDamage(float damage)
        {
            if (!isInitialized) return;
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"[EnemyController] Enemy took {damage} damage. Health: {currentHealth}/{maxHealth}");

            // Create damage effect
            CreateDamageEffect();

            if (currentHealth <= 0)
            {
                Die();
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
            Debug.Log("[EnemyController] Enemy died!");

            // Stop movement
            rb.linearVelocity = Vector3.zero;

            // Create death effect
            CreateDeathEffect();

            // Disable enemy after short delay
            Invoke(nameof(DisableEnemy), 2f);
        }

        private void CreateDeathEffect()
        {
            // Create explosion-like effect
            for (int i = 0; i < 5; i++)
            {
                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                particle.transform.position = transform.position + Random.insideUnitSphere * 2f;
                particle.transform.localScale = Vector3.one * 0.2f;
                
                var renderer = particle.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.yellow;
                }

                // Add some physics
                var particleRb = particle.GetComponent<Rigidbody>();
                if (particleRb != null)
                {
                    particleRb.AddForce(Random.insideUnitSphere * 300f);
                }

                Destroy(particle, 3f);
            }
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
            
            Debug.Log("[EnemyController] Enemy respawned");
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
    }
}
