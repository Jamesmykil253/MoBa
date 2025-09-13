using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple animation controller
    /// </summary>
    [System.Serializable]
    public class CharacterAnimationController
    {
        [Header("Animation Parameters")]
        [SerializeField] private string groundedParam = "IsGrounded";
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private string jumpParam = "Jump";
        [SerializeField] private string attackParam = "Attack";

        /// <summary>
        /// Update character animations
        /// </summary>
        public void UpdateCharacterAnimations(
            Animator animator, 
            SpriteRenderer spriteRenderer,
            bool isGrounded,
            Vector3 movementInput,
            bool isAttacking = false)
        {
            if (animator == null) return;

            // Basic animation parameters
            animator.SetBool(groundedParam, isGrounded);
            animator.SetFloat(moveSpeedParam, movementInput.magnitude);
            animator.SetBool(attackParam, isAttacking);

            // Sprite flipping
            if (spriteRenderer != null && movementInput.x != 0)
            {
                spriteRenderer.flipX = movementInput.x < 0;
            }
        }

        /// <summary>
        /// Trigger jump animation
        /// </summary>
        public void TriggerJump(Animator animator)
        {
            if (animator != null)
            {
                animator.SetTrigger(jumpParam);
            }
        }

        /// <summary>
        /// Trigger attack animation
        /// </summary>
        public void TriggerAttack(Animator animator)
        {
            if (animator != null)
            {
                animator.SetTrigger(attackParam);
            }
        }
    }
}
