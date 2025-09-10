using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Moving state - character is actively moving
    /// Handles movement input, animation, and transitions to other states
    /// </summary>
    public class MovingState : CharacterStateBase
    {
        private Vector3 lastPosition;
        private float movementSpeed;
        private const float MIN_MOVEMENT_THRESHOLD = 0.1f;

        public MovingState(MOBACharacterController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            lastPosition = controller.transform.position;
            movementSpeed = 0f;

            // Set moving animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsMoving", true);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsCasting", false);
                animator.SetBool("IsStunned", false);
            }

            // Notify camera of movement state
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can adjust distance based on movement
            }

            Debug.Log("Entered Moving State");
        }

        protected override void OnUpdate()
        {
            // Calculate current movement speed
            Vector3 currentPosition = controller.transform.position;
            movementSpeed = Vector3.Distance(currentPosition, lastPosition) / Time.deltaTime;
            lastPosition = currentPosition;

            // Update animation speed based on movement
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetFloat("MoveSpeed", movementSpeed);
            }

            // Check for state transitions
            CheckForTransitions();

            // Handle movement input
            HandleMovement();
        }

        protected override void OnExit()
        {
            movementSpeed = 0f;

            // Reset movement animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveSpeed", 0f);
            }

            Debug.Log("Exited Moving State");
        }

        private void HandleMovement()
        {
            // Movement is handled by the character controller's physics
            // This state just manages the animation and transitions
            Vector3 movementInput = controller.MovementInput;

            if (movementInput != Vector3.zero)
            {
                // Face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(movementInput);
                controller.transform.rotation = Quaternion.Slerp(
                    controller.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 10f
                );
            }
        }

        private void CheckForTransitions()
        {
            // Check if movement stopped
            if (controller.MovementInput == Vector3.zero && movementSpeed < MIN_MOVEMENT_THRESHOLD)
            {
                // Transition back to idle will be handled by input system
                return;
            }

            // Check for jump input
            // This will be handled by the input system triggering state change

            // Check for attack input
            // This will be handled by the input system triggering state change

            // Check for damage
            var playerController = controller.GetComponent<PlayerController>();
            if (playerController != null && playerController.Health <= 0)
            {
                // Transition to dead state will be handled by damage system
                return;
            }

            // Check for ground status (falling)
            // This will be handled by physics system
        }

        public override string GetStateName()
        {
            return "Moving";
        }
    }
}