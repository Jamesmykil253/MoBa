using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Idle state - character is standing still, ready for action
    /// This is the default state when no other actions are being performed
    /// </summary>
    public class IdleState : CharacterStateBase
    {
        private float idleTime;
        private const float IDLE_ANIMATION_THRESHOLD = 3f;

        public IdleState(MOBACharacterController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            idleTime = 0f;

            // Set idle animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsCasting", false);
                animator.SetBool("IsStunned", false);
                animator.SetTrigger("Idle");
            }

            // Notify camera of state change
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can return to default position/behavior
            }

            Debug.Log("Entered Idle State");
        }

        protected override void OnUpdate()
        {
            idleTime += Time.deltaTime;

            // Play idle animation variations after some time
            if (idleTime > IDLE_ANIMATION_THRESHOLD)
            {
                // Could trigger idle animation variations here
            }

            // Check for automatic transitions
            CheckForTransitions();
        }

        protected override void OnExit()
        {
            idleTime = 0f;
            Debug.Log("Exited Idle State");
        }

        private void CheckForTransitions()
        {
            // Check if player started moving
            if (controller.MovementInput != Vector3.zero)
            {
                // Transition will be handled by input system
                return;
            }

            // Check for damage
            var playerController = controller.GetComponent<PlayerController>();
            if (playerController != null && playerController.Health <= 0)
            {
                // Transition to dead state will be handled by damage system
                return;
            }
        }

        public override string GetStateName()
        {
            return "Idle";
        }
    }
}