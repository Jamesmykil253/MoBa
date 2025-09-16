using UnityEngine;
using MOBA.Debugging;
using MOBA.Effects;

namespace MOBA
{
    /// <summary>
    /// Simple health component that implements IDamageable interface
    /// Used for test targets and other damageable objects in the scene
    /// </summary>
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Visual Settings")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.red;

        // Component references
        private Renderer meshRenderer;
        private Color originalColor;

        private GameDebugContext GetContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Health,
                GameDebugSystemTag.Health,
                mechanic,
                subsystem: nameof(HealthComponent),
                actor: gameObject != null ? gameObject.name : null);
        }

        // Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnDeath;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;

        // IDamageable interface implementation
        public float GetHealth() => currentHealth;
        public bool IsDead() => currentHealth <= 0f;

        private void Awake()
        {
            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        private void Start()
        {
            // Initialize health
            currentHealth = maxHealth;
            UpdateVisuals();
        }

        public void TakeDamage(float damage)
        {
            if (IsDead()) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            
            GameDebug.Log(
                GetContext(GameDebugMechanicTag.Damage),
                "Damage applied to health component.",
                ("Damage", damage),
                ("Current", currentHealth),
                ("Max", maxHealth));

            // Update visuals
            UpdateVisuals();

            // Create damage effect
            CreateDamageEffect();

            // Trigger events
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Check for death
            if (IsDead())
            {
                HandleDeath();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead()) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            
            GameDebug.Log(
                GetContext(GameDebugMechanicTag.Healing),
                "Healing applied to health component.",
                ("Amount", amount),
                ("Current", currentHealth),
                ("Max", maxHealth));

            UpdateVisuals();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateVisuals();
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            UpdateVisuals();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void UpdateVisuals()
        {
            if (meshRenderer == null) return;

            // Color based on health percentage
            float healthPercent = HealthPercentage;
            Color targetColor = Color.Lerp(damagedColor, healthyColor, healthPercent);
            
            // Blend with original color
            meshRenderer.material.color = Color.Lerp(originalColor, targetColor, 0.7f);
        }

        private void CreateDamageEffect()
        {
            // Flash effect
            StartCoroutine(FlashEffect());
        }

        private System.Collections.IEnumerator FlashEffect()
        {
            if (meshRenderer == null) yield break;

            Color originalMaterialColor = meshRenderer.material.color;
            
            // Flash white
            meshRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            
            // Return to health-based color
            meshRenderer.material.color = originalMaterialColor;
        }

        private void HandleDeath()
        {
            GameDebug.Log(
                GetContext(GameDebugMechanicTag.StateChange),
                "Health component reached zero and triggered death state.");

            OnDeath?.Invoke();

            // Create death effect
            CreateDeathEffect();

            // In a real game, you might disable the GameObject or trigger respawn
            // For now, just change color to indicate death
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.black;
            }
        }

        private void CreateDeathEffect()
        {
            EffectPoolService.SpawnSphereBurst(transform.position, Color.yellow, 1f, 6, 1.5f, 0.2f, transform.root);
        }

        private void OnDestroy()
        {
            // Clean up events to prevent memory leaks
            OnHealthChanged = null;
            OnDeath = null;
        }

        // Gizmos for debugging
        private void OnDrawGizmosSelected()
        {
            if (showHealthBar && Application.isPlaying)
            {
                // Draw health bar above object
                Vector3 barPosition = transform.position + Vector3.up * 3f;
                Vector3 barSize = new Vector3(2f, 0.2f, 0f);
                
                // Background
                Gizmos.color = Color.black;
                Gizmos.DrawCube(barPosition, barSize);
                
                // Health fill
                Gizmos.color = Color.Lerp(Color.red, Color.green, HealthPercentage);
                Vector3 fillSize = new Vector3(barSize.x * HealthPercentage, barSize.y, barSize.z);
                Vector3 fillPosition = barPosition - Vector3.right * (barSize.x - fillSize.x) * 0.5f;
                Gizmos.DrawCube(fillPosition, fillSize);
            }
        }
    }
}
