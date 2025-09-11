using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Dead state - character has died and is in death animation
    /// Handles death effects, respawn timing, and final cleanup
    /// </summary>
    public class DeadState : CharacterStateBase
    {
        private float deathStartTime;
        private float deathDuration = 3f; // Time before respawn
        private bool hasPlayedDeathEffect;
        private bool respawnTriggered;

        public DeadState(MOBACharacterController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            deathStartTime = Time.time;
            hasPlayedDeathEffect = false;
            respawnTriggered = false;

            // Disable physics and movement
            if (controller.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true; // Disable physics during death
            }

            // Disable collider to prevent interactions
            if (controller.TryGetComponent(out Collider collider))
            {
                collider.enabled = false;
            }

            // Set death animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsDead", true);
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsCasting", false);
                animator.SetBool("IsStunned", false);
                animator.SetTrigger("Death");
            }

            // Play death effect
            PlayDeathEffect();

            // Notify camera of death
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can focus on death or show dramatic effect
            }

            Debug.Log("Entered Dead State");
        }

        protected override void OnUpdate()
        {
            float deathProgress = (Time.time - deathStartTime) / deathDuration;

            // Handle death sequence
            if (deathProgress < 0.5f)
            {
                // Death animation phase
                UpdateDeathAnimation(deathProgress);
            }
            else if (deathProgress < 0.8f)
            {
                // Death effect phase
                UpdateDeathEffects(deathProgress);
            }
            else if (!respawnTriggered)
            {
                // Trigger respawn
                TriggerRespawn();
            }
        }

        protected override void OnExit()
        {
            // Reset death animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsDead", false);
            }

            // Re-enable physics and collider
            if (controller.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
            }

            if (controller.TryGetComponent(out Collider collider))
            {
                collider.enabled = true;
            }

            Debug.Log("Exited Dead State");
        }

        private void PlayDeathEffect()
        {
            if (hasPlayedDeathEffect) return;
            hasPlayedDeathEffect = true;

            // Create death particle effect
            GameObject deathEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            deathEffect.transform.position = controller.transform.position + Vector3.up;
            deathEffect.transform.localScale = Vector3.one * 2f;

            var renderer = deathEffect.GetComponent<Renderer>();
            renderer.material.color = Color.red;

            // Add expanding effect
            var expander = deathEffect.AddComponent<DeathEffect>();
            expander.expansionSpeed = 5f;

            Object.Destroy(deathEffect, 2f);

            // Play death sound
            // This would integrate with audio system

            Debug.Log("Death effect played");
        }

        private void UpdateDeathAnimation(float progress)
        {
            // Update death animation based on progress
            // Could include falling, fading, etc.

            if (controller.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                // Fade out during death
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0.3f, progress * 2f);
                spriteRenderer.color = color;
            }
        }

        private void UpdateDeathEffects(float progress)
        {
            // Update death effects (particles, screen effects, etc.)
            // This could include camera shake, slow motion, etc.
        }

        private void TriggerRespawn()
        {
            respawnTriggered = true;

            // Create respawn effect
            GameObject respawnEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            respawnEffect.transform.position = controller.transform.position + Vector3.down * 2f;
            respawnEffect.transform.localScale = Vector3.zero;

            var renderer = respawnEffect.GetComponent<Renderer>();
            renderer.material.color = Color.green;

            // Add respawn effect
            var respawnEffectComponent = respawnEffect.AddComponent<RespawnEffect>();
            respawnEffectComponent.targetScale = Vector3.one * 3f;

            Object.Destroy(respawnEffect, 1f);

            // Reset character properties
            ResetCharacter();

            // Transition to appropriate state
            // This will be handled by the respawn system

            Debug.Log("Respawn triggered");
        }

        private void ResetCharacter()
        {
            // Reset position (would use spawn point in real implementation)
            controller.transform.position = Vector3.zero;

            // Reset visual effects
            if (controller.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }

            // FIXED: Actually reset health to max when respawning
            var playerController = controller.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Reset health to max using reflection to access private fields
                var currentHealthField = typeof(PlayerController).GetField("currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var maxHealthField = typeof(PlayerController).GetField("maxHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (currentHealthField != null && maxHealthField != null)
                {
                    float maxHealth = (float)maxHealthField.GetValue(playerController);
                    currentHealthField.SetValue(playerController, maxHealth);
                    Debug.Log($"[DeadState] Health reset to {maxHealth}");
                }
                else
                {
                    // Fallback: call respawn method if available
                    var respawnMethod = typeof(PlayerController).GetMethod("Respawn", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (respawnMethod != null)
                    {
                        respawnMethod.Invoke(playerController, null);
                        Debug.Log("[DeadState] Called Respawn method");
                    }
                }
            }
        }

        public override string GetStateName()
        {
            if (respawnTriggered)
            {
                return "Dead (Respawning)";
            }

            float remainingTime = deathDuration - (Time.time - deathStartTime);
            return $"Dead ({remainingTime:F1}s)";
        }
    }

    /// <summary>
    /// Death effect component for expanding sphere
    /// </summary>
    public class DeathEffect : MonoBehaviour
    {
        public float expansionSpeed = 5f;

        private void Update()
        {
            transform.localScale += Vector3.one * expansionSpeed * Time.deltaTime;

            // Fade out as it expands
            if (TryGetComponent(out Renderer renderer))
            {
                Color color = renderer.material.color;
                color.a = Mathf.Max(0, color.a - Time.deltaTime * 2f);
                renderer.material.color = color;

                // Destroy when fully faded
                if (color.a <= 0.01f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Respawn effect component for growing sphere
    /// </summary>
    public class RespawnEffect : MonoBehaviour
    {
        public Vector3 targetScale = Vector3.one * 3f;
        public float growthSpeed = 5f;

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, growthSpeed * Time.deltaTime);

            // Fade out as it grows
            if (TryGetComponent(out Renderer renderer))
            {
                Color color = renderer.material.color;
                color.a = Mathf.Max(0, color.a - Time.deltaTime * 2f);
                renderer.material.color = color;
            }
        }
    }
}