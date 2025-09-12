using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Jumping state - character is actively jumping upward
    /// Handles jump physics, animation, and transitions to falling
    /// </summary>
    public class JumpingState : CharacterStateBase
    {
        private float jumpStartTime;
        private float maxJumpTime = 0.5f; // Maximum time character can be in jump state
        private bool canDoubleJump;
        private Vector3 jumpDirection;

        public JumpingState(UnifiedPlayerController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            jumpStartTime = Time.time;
            canDoubleJump = true;
            jumpDirection = controller.MovementInput;

            // Apply jump force
            if (controller.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, controller.JumpForce, rb.linearVelocity.z);
            }

            // Set jumping animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsCasting", false);
                animator.SetBool("IsStunned", false);
                animator.SetTrigger("Jump");
            }

            // Notify camera of jump
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can adjust for jump anticipation
            }

            Debug.Log("Entered Jumping State");
        }

        protected override void OnUpdate()
        {
            float jumpDuration = Time.time - jumpStartTime;

            // Check if jump time exceeded or if we're now falling
            if (jumpDuration > maxJumpTime)
            {
                // Transition to falling state will be handled by physics system
                return;
            }

            // Handle air control during jump
            HandleAirControl();

            // Check for double jump input
            if (canDoubleJump && Input.GetKeyDown(KeyCode.Space))
            {
                PerformDoubleJump();
            }

            // Check for attack input during jump
            // This will be handled by the input system
        }

        protected override void OnExit()
        {
            canDoubleJump = false;

            // Reset jumping animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsJumping", false);
            }

            Debug.Log("Exited Jumping State");
        }

        private void HandleAirControl()
        {
            // Allow some air control during jump
            Vector3 movementInput = controller.MovementInput;

            if (movementInput != Vector3.zero && controller.TryGetComponent(out Rigidbody rb))
            {
                // Apply air control force
                Vector3 airControlForce = movementInput * 5f; // Reduced control in air
                rb.AddForce(airControlForce, ForceMode.Force);
            }
        }

        private void PerformDoubleJump()
        {
            if (!canDoubleJump) return;

            canDoubleJump = false;

            // Apply double jump force
            if (controller.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, controller.DoubleJumpForce, rb.linearVelocity.z);
            }

            // Trigger double jump animation/effect
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetTrigger("DoubleJump");
            }

            // Play double jump sound/effect
            // This would integrate with the audio system

            Debug.Log("Performed Double Jump");
        }

        public override string GetStateName()
        {
            return "Jumping";
        }
    }
}