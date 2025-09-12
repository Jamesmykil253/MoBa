using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Falling state - character is falling downward
    /// Handles falling physics, animation, and landing detection
    /// </summary>
    public class FallingState : CharacterStateBase
    {
        private float fallStartTime;
        private float maxFallSpeed = 20f; // Terminal velocity
        private bool hasPlayedFallAnimation;

        public FallingState(UnifiedPlayerController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            fallStartTime = Time.time;
            hasPlayedFallAnimation = false;

            // Set falling animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", true);
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsCasting", false);
                animator.SetBool("IsStunned", false);
            }

            // Notify camera of falling state
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can adjust for fall anticipation
            }

            Debug.Log("Entered Falling State");
        }

        protected override void OnUpdate()
        {
            // Handle falling physics
            HandleFallingPhysics();

            // Check for landing (will be handled by physics system)
            CheckForLanding();

            // Play fall animation after a delay
            if (!hasPlayedFallAnimation && (Time.time - fallStartTime) > 0.2f)
            {
                PlayFallAnimation();
            }
        }

        protected override void OnExit()
        {
            // Reset falling animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsFalling", false);
            }

            Debug.Log("Exited Falling State");
        }

        private void HandleFallingPhysics()
        {
            if (controller.TryGetComponent(out Rigidbody rb))
            {
                // Clamp fall speed to prevent excessive velocity
                if (rb.linearVelocity.y < -maxFallSpeed)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFallSpeed, rb.linearVelocity.z);
                }

                // Allow some air control during fall
                Vector3 movementInput = controller.MovementInput;
                if (movementInput != Vector3.zero)
                {
                    Vector3 airControlForce = movementInput * 3f; // Reduced control while falling
                    rb.AddForce(airControlForce, ForceMode.Force);
                }
            }
        }

        private void CheckForLanding()
        {
            // Landing detection will be handled by the physics system
            // When character touches ground, transition will be triggered externally
        }

        private void PlayFallAnimation()
        {
            hasPlayedFallAnimation = true;

            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetTrigger("Fall");
            }

            // Could play fall sound effect here
            Debug.Log("Playing fall animation");
        }

        public override string GetStateName()
        {
            return "Falling";
        }
    }
}