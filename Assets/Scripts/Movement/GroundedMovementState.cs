using UnityEngine;

namespace MOBA.Movement
{
    /// <summary>
    /// Grounded movement state - handles normal ground-based movement and jumping
    /// Reference: Game Programming Patterns Chapter 6 - State Pattern implementation
    /// Manages player movement while on solid ground with full control
    /// </summary>
    public class GroundedMovementState : MovementState
    {
        private float stateEnterTime;
        
        public override void Enter(MovementContext context)
        {
            stateEnterTime = Time.time;
            
            // Reset jump capabilities when landing
            context.CanDoubleJump = true;
            context.HasDoubleJumped = false;
            context.PendingHoldBoost = false;
            context.HoldBoostApplied = false;
            
            // Reset dash if it was active
            if (context.IsDashing)
            {
                context.IsDashing = false;
                
                // Re-enable gravity if dash disabled it
                if (context.Rigidbody != null)
                {
                    context.Rigidbody.useGravity = true;
                }
            }
            
            if (Application.isPlaying)
            {
                Debug.Log("[GroundedMovementState] Player landed - jump capabilities reset");
            }
        }
        
        public override MovementState Update(MovementContext context)
        {
            // Update grounded status
            bool stillGrounded = context.CheckGrounded();
            
            // Transition to airborne if no longer grounded
            if (!stillGrounded)
            {
                return new AirborneMovementState();
            }
            
            // Handle ground movement
            ApplyGroundMovement(context);
            
            // Check for dash input (if dash is available and not already dashing)
            if (ShouldStartDash(context))
            {
                return new DashingMovementState();
            }
            
            // Stay in grounded state
            return null;
        }
        
        public override void Exit(MovementContext context)
        {
            // Record when we left the ground
            context.LastGroundedTime = Time.time;
            
            if (Application.isPlaying)
            {
                Debug.Log("[GroundedMovementState] Leaving ground");
            }
        }
        
        public override bool HandleJumpInput(MovementContext context)
        {
            if (context.Rigidbody == null)
                return false;
                
            // Perform initial jump
            Vector3 jumpForce = Vector3.up * context.JumpForce;
            context.AddForce(jumpForce, ForceMode.Impulse);
            
            // Set up jump state tracking
            context.FirstJumpTime = Time.time;
            context.PendingHoldBoost = true;
            context.HoldBoostApplied = false;
            context.FirstJumpUsedHold = false;
            
            // Publish jump event
            UnifiedEventSystem.PublishLocal(new JumpExecutedEvent(
                context.Transform.gameObject, jumpForce, UnifiedMovementSystem.JumpExecutionResult.Initial));
            
            if (Application.isPlaying)
            {
                Debug.Log($"[GroundedMovementState] Initial jump executed with force: {jumpForce}");
            }
            
            return true;
        }
        
        /// <summary>
        /// Apply movement forces while grounded
        /// </summary>
        private void ApplyGroundMovement(MovementContext context)
        {
            if (context.MovementInput.magnitude <= 0.01f)
                return;
                
            // Calculate movement force based on input and base speed
            Vector3 moveDirection = new Vector3(context.MovementInput.x, 0f, context.MovementInput.z);
            Vector3 targetVelocity = moveDirection * context.BaseMoveSpeed;
            
            // Apply movement while preserving vertical velocity
            Vector3 currentVelocity = context.GetVelocity();
            Vector3 newVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            
            context.SetVelocity(newVelocity);
            
            // Update last valid position for anti-cheat
            if (context.EnableNetworkValidation)
            {
                ValidatePosition(context);
            }
        }
        
        /// <summary>
        /// Check if dash should be initiated
        /// </summary>
        private bool ShouldStartDash(MovementContext context)
        {
            // Check if dash is available (cooldown, not already dashing)
            if (!context.IsDashAvailable() || context.IsDashing)
                return false;
                
            // For now, we'll integrate dash with the existing input system
            // This would typically be triggered by a specific input (like Shift + movement)
            // For demonstration, we'll return false here and let it be handled by input events
            return false;
        }
        
        /// <summary>
        /// Validate current position for network anti-cheat
        /// </summary>
        private void ValidatePosition(MovementContext context)
        {
            Vector3 currentPosition = context.Transform.position;
            Vector3 lastPosition = context.LastValidPosition;
            
            float distance = Vector3.Distance(currentPosition, lastPosition);
            float maxDistance = context.MaxMovementSpeed * Time.deltaTime;
            
            if (distance > maxDistance)
            {
                // Potential teleportation or speed hack
                if (distance > context.TeleportThreshold)
                {
                    Debug.LogWarning($"[GroundedMovementState] Potential teleportation detected: {distance}m movement");
                    
                    // Reset to last valid position
                    context.Transform.position = lastPosition;
                    context.SetVelocity(Vector3.zero);
                    return;
                }
                else
                {
                    Debug.LogWarning($"[GroundedMovementState] High speed movement detected: {distance}m in {Time.deltaTime}s");
                }
            }
            
            // Update last valid position
            context.LastValidPosition = currentPosition;
            context.OnPositionValidated?.Invoke(currentPosition);
        }
        
        public override string GetDebugInfo()
        {
            float timeInState = Time.time - stateEnterTime;
            return $"GroundedMovementState (Time: {timeInState:F1}s)";
        }
    }
}