using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Shared animation controller to eliminate DRY violations
    /// Replaces duplicate animation code in PlayerController and NetworkPlayerController
    /// Based on Pragmatic Programmer DRY principle
    /// </summary>
    [System.Serializable]
    public class CharacterAnimationController
    {
        [Header("Animation Parameters")]
        [SerializeField] private string groundedParam = "IsGrounded";
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private string verticalVelocityParam = "VerticalVelocity";
        [SerializeField] private string jumpParam = "Jump";
        [SerializeField] private string attackParam = "Attack";
        [SerializeField] private string deadParam = "IsDead";

        [Header("Settings")]
        [SerializeField] private bool enableSpriteFlipping = true;
        [SerializeField] private float moveSpeedMultiplier = 1f;

        /// <summary>
        /// Update character animations with all common parameters
        /// Eliminates code duplication between controllers
        /// </summary>
        public void UpdateCharacterAnimations(
            Animator animator, 
            SpriteRenderer spriteRenderer,
            bool isGrounded, 
            Vector3 movementInput, 
            Rigidbody rb)
        {
            if (animator == null) 
            {
                Logger.LogWarning("Animator is null, cannot update animations");
                return;
            }

            try
            {
                // Ground state
                animator.SetBool(groundedParam, isGrounded);

                // Movement speed
                float moveSpeed = Mathf.Abs(movementInput.x) * moveSpeedMultiplier;
                animator.SetFloat(moveSpeedParam, moveSpeed);

                // Vertical velocity
                if (rb != null)
                {
                    animator.SetFloat(verticalVelocityParam, rb.linearVelocity.y);
                }

                // Sprite flipping
                if (enableSpriteFlipping && spriteRenderer != null && movementInput.x != 0)
                {
                    spriteRenderer.flipX = movementInput.x < 0;
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error updating animations: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger jump animation
        /// </summary>
        public void TriggerJump(Animator animator)
        {
            if (animator == null) return;
            
            try
            {
                animator.SetTrigger(jumpParam);
                Logger.LogDebug("Jump animation triggered");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error triggering jump animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger attack animation
        /// </summary>
        public void TriggerAttack(Animator animator)
        {
            if (animator == null) return;
            
            try
            {
                animator.SetTrigger(attackParam);
                Logger.LogDebug("Attack animation triggered");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error triggering attack animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Set death state
        /// </summary>
        public void SetDeathState(Animator animator, bool isDead)
        {
            if (animator == null) return;
            
            try
            {
                animator.SetBool(deadParam, isDead);
                Logger.LogDebug($"Death state set to: {isDead}");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error setting death state: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate animator has required parameters
        /// Defensive programming to catch missing animation parameters
        /// </summary>
        public Result ValidateAnimatorParameters(Animator animator)
        {
            if (animator == null)
                return Result.Failure("Animator is null");

            try
            {
                // Check for required parameters
                string[] requiredParams = { groundedParam, moveSpeedParam, verticalVelocityParam };
                
                foreach (string param in requiredParams)
                {
                    bool hasParam = false;
                    foreach (AnimatorControllerParameter parameter in animator.parameters)
                    {
                        if (parameter.name == param)
                        {
                            hasParam = true;
                            break;
                        }
                    }
                    
                    if (!hasParam)
                    {
                        return Result.Failure($"Missing required animation parameter: {param}");
                    }
                }

                Logger.LogDebug("All required animation parameters found");
                return Result.Success();
            }
            catch (System.Exception ex)
            {
                return Result.Failure($"Error validating animator parameters: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset all animation parameters to default state
        /// </summary>
        public void ResetAnimationState(Animator animator)
        {
            if (animator == null) return;

            try
            {
                animator.SetBool(groundedParam, true);
                animator.SetFloat(moveSpeedParam, 0f);
                animator.SetFloat(verticalVelocityParam, 0f);
                animator.SetBool(deadParam, false);
                
                Logger.LogDebug("Animation state reset to defaults");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error resetting animation state: {ex.Message}");
            }
        }
    }
}
