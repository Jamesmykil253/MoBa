using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Stunned state - character is temporarily incapacitated
    /// Handles stun duration, visual effects, and recovery
    /// </summary>
    public class StunnedState : CharacterStateBase
    {
        private float stunDuration = 2f;
        private float stunStartTime;
        private bool hasPlayedStunEffect;

        public StunnedState(MOBACharacterController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            stunStartTime = Time.time;
            hasPlayedStunEffect = false;

            // Disable movement
            if (controller.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
            }

            // Set stunned animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsStunned", true);
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsCasting", false);
                animator.SetTrigger("Stunned");
            }

            // Play stun effect
            PlayStunEffect();

            // Notify camera of stun
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can add shake effect or focus on stunned character
            }

            Debug.Log($"Entered Stunned State (Duration: {stunDuration}s)");
        }

        protected override void OnUpdate()
        {
            float stunProgress = (Time.time - stunStartTime) / stunDuration;

            // Update stun visual effects
            UpdateStunEffects(stunProgress);

            // Check for stun recovery
            if (stunProgress >= 1.0f)
            {
                RecoverFromStun();
            }
        }

        protected override void OnExit()
        {
            // Reset stunned animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsStunned", false);
            }

            // Clean up stun effects
            CleanupStunEffects();

            Debug.Log("Exited Stunned State");
        }

        private void PlayStunEffect()
        {
            if (hasPlayedStunEffect) return;
            hasPlayedStunEffect = true;

            // Create stun particle effect
            GameObject stunEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stunEffect.transform.position = controller.transform.position + Vector3.up * 2f;
            stunEffect.transform.localScale = Vector3.one * 0.3f;

            var renderer = stunEffect.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;

            // Add rotation animation
            var rotator = stunEffect.AddComponent<Rotator>();
            rotator.rotationSpeed = 360f;

            Object.Destroy(stunEffect, stunDuration);

            // Play stun sound
            // This would integrate with audio system

            Debug.Log("Stun effect played");
        }

        private void UpdateStunEffects(float progress)
        {
            // Update visual effects based on stun progress
            // Could flash the character, show particle effects, etc.

            // Example: Flash the character sprite
            if (controller.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                float flashSpeed = 10f;
                float alpha = (Mathf.Sin(progress * flashSpeed * Mathf.PI * 2) + 1) / 2;
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0.3f, 1f, alpha);
                spriteRenderer.color = color;
            }
        }

        private void RecoverFromStun()
        {
            // Play recovery effect
            GameObject recoveryEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            recoveryEffect.transform.position = controller.transform.position + Vector3.up;
            recoveryEffect.transform.localScale = Vector3.one * 1.5f;

            var renderer = recoveryEffect.GetComponent<Renderer>();
            renderer.material.color = Color.green;

            Object.Destroy(recoveryEffect, 0.5f);

            // Transition to appropriate state
            // This will be handled by the state machine based on current conditions
            Debug.Log("Recovering from stun");
        }

        private void CleanupStunEffects()
        {
            // Reset visual effects
            if (controller.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }

        public override string GetStateName()
        {
            float remainingTime = stunDuration - (Time.time - stunStartTime);
            return $"Stunned ({remainingTime:F1}s)";
        }
    }

    /// <summary>
    /// Simple rotation component for stun effects
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        public float rotationSpeed = 360f;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}