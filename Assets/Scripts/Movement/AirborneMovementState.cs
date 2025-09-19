using UnityEngine;

namespace MOBA.Movement
{
    /// <summary>
    /// Airborne movement state - handles air movement, double jumping, and landing detection
    /// Reference: Game Programming Patterns Chapter 6 - State Pattern implementation
    /// Manages player movement while in the air with limited control and jump mechanics
    /// </summary>
    public class AirborneMovementState : MovementState
    {
        private float stateEnterTime;
        private Vector3 initialVelocity;
        private bool apexBoostAvailable;
        
        public override void Enter(MovementContext context)
        {
            stateEnterTime = Time.time;
            initialVelocity = context.GetVelocity();
            apexBoostAvailable = context.PendingHoldBoost && !context.HoldBoostApplied;
            
            // Record airborne state start
            context.AirborneStartTime = Time.time;
            context.AirborneStartVelocity = initialVelocity;
            
            if (Application.isPlaying)
            {
                Debug.Log($"[AirborneMovementState] Entered airborne state with velocity: {initialVelocity}");
            }
        }
        
        public override MovementState Update(MovementContext context)
        {
            // Check if we've landed
            bool isGrounded = context.CheckGrounded();
            if (isGrounded)
            {
                return new GroundedMovementState();
            }
            
            // Apply air movement (limited control)
            ApplyAirMovement(context);
            
            // Handle apex boost if available
            HandleApexBoost(context);
            
            // Check for dash input while airborne
            if (ShouldStartAirDash(context))
            {
                return new DashingMovementState();
            }
            
            // Stay in airborne state
            return null;
        }
        
        public override void Exit(MovementContext context)
        {
            // Clear airborne-specific state
            context.PendingHoldBoost = false;
            
            if (Application.isPlaying)
            {
                float airTime = Time.time - stateEnterTime;
                Debug.Log($"[AirborneMovementState] Exiting after {airTime:F2}s in air");
            }
        }
        
        public override bool HandleJumpInput(MovementContext context)
        {
            // Handle double jump
            if (context.CanDoubleJump && !context.HasDoubleJumped)
            {
                Vector3 currentVelocity = context.GetVelocity();
                float preJumpVelocityY = currentVelocity.y;
                float multiplier = context.DoubleJumpMultiplier;
                
                // Check for apex boost conditions
                if (context.FirstJumpUsedHold && 
                    Mathf.Abs(preJumpVelocityY) <= context.ApexVelocityThreshold && 
                    Time.time - context.FirstJumpTime <= context.ApexTimeWindow)
                {
                    multiplier = context.ApexBoostMultiplier;
                    
                    if (Application.isPlaying)
                    {
                        Debug.Log("[AirborneMovementState] Apex boost double jump executed!");
                    }
                }
                
                // Execute double jump
                Vector3 doubleJumpForce = Vector3.up * context.JumpForce * multiplier;
                
                // Reset vertical velocity before applying double jump
                Vector3 newVelocity = currentVelocity;
                newVelocity.y = 0f;
                context.SetVelocity(newVelocity);
                
                // Apply double jump force
                context.AddForce(doubleJumpForce, ForceMode.Impulse);
                
                // Update jump state
                context.HasDoubleJumped = true;
                context.CanDoubleJump = false;
                context.PendingHoldBoost = false;
                
                // Publish jump event
                UnifiedEventSystem.PublishLocal(new JumpExecutedEvent(
                    context.Transform.gameObject, doubleJumpForce, UnifiedMovementSystem.JumpExecutionResult.Double));
                
                if (Application.isPlaying)
                {
                    Debug.Log($"[AirborneMovementState] Double jump executed with force: {doubleJumpForce} (multiplier: {multiplier})");
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Apply limited movement control while airborne
        /// </summary>
        private void ApplyAirMovement(MovementContext context)
        {
            if (context.MovementInput.magnitude <= 0.01f)
                return;
                
            // Air movement has reduced control (typically 25-50% of ground movement)
            float airControlFactor = 0.3f;
            Vector3 moveDirection = new Vector3(context.MovementInput.x, 0f, context.MovementInput.z);
            Vector3 airForce = moveDirection * context.BaseMoveSpeed * airControlFactor;
            
            // Apply air movement force (preserving existing momentum)
            context.AddForce(airForce, ForceMode.Force);
            
            // Validate air movement for anti-cheat
            if (context.EnableNetworkValidation)
            {
                ValidateAirMovement(context);
            }
        }
        
        /// <summary>
        /// Handle apex boost mechanic for held jump
        /// </summary>
        private void HandleApexBoost(MovementContext context)
        {
            if (!apexBoostAvailable || context.HoldBoostApplied)
                return;
                
            Vector3 currentVelocity = context.GetVelocity();
            
            // Check if we're falling (negative Y velocity) - boost is no longer available
            if (currentVelocity.y <= 0f)
            {
                context.PendingHoldBoost = false;
                apexBoostAvailable = false;
                return;
            }
            
            // Apex boost can be applied during upward movement
            // This would typically be triggered by holding the jump button
            // For now, we'll mark that boost was applied to prevent re-triggering
            if (context.PendingHoldBoost)
            {
                // This would be called by an external system when jump is held
                // For demonstration, we're not automatically applying it here
            }
        }
        
        /// <summary>
        /// Apply hold boost when jump button is held
        /// </summary>
        public bool TryApplyHoldBoost(MovementContext context)
        {
            if (!context.PendingHoldBoost || context.HoldBoostApplied)
                return false;
                
            Vector3 currentVelocity = context.GetVelocity();
            if (currentVelocity.y <= 0f)
            {
                context.PendingHoldBoost = false;
                return false;
            }
            
            // Apply hold boost
            float extraMultiplier = Mathf.Max(0f, context.HoldJumpMultiplier - 1f);
            if (extraMultiplier <= 0f)
            {
                context.PendingHoldBoost = false;
                return false;
            }
            
            Vector3 holdBoostForce = Vector3.up * context.JumpForce * extraMultiplier;
            context.AddForce(holdBoostForce, ForceMode.Impulse);
            
            context.HoldBoostApplied = true;
            context.FirstJumpUsedHold = true;
            context.PendingHoldBoost = false;
            
            if (Application.isPlaying)
            {
                Debug.Log($"[AirborneMovementState] Hold boost applied with force: {holdBoostForce}");
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if air dash should be initiated
        /// </summary>
        private bool ShouldStartAirDash(MovementContext context)
        {
            // Check if dash is available and not already dashing
            if (!context.IsDashAvailable() || context.IsDashing)
                return false;
                
            // Air dash conditions can be more restrictive
            // For example, might require specific input combinations or limited uses
            return false;
        }
        
        /// <summary>
        /// Validate air movement for network anti-cheat
        /// </summary>
        private void ValidateAirMovement(MovementContext context)
        {
            Vector3 currentPosition = context.Transform.position;
            Vector3 lastPosition = context.LastValidPosition;
            Vector3 currentVelocity = context.GetVelocity();
            
            float distance = Vector3.Distance(currentPosition, lastPosition);
            float timeInAir = Time.time - context.AirborneStartTime;
            
            // Check for impossible air movement (flying, extreme speeds)
            float maxAirSpeed = context.MaxMovementSpeed * 1.5f; // Allow some extra for jump momentum
            if (currentVelocity.magnitude > maxAirSpeed)
            {
                Debug.LogWarning($"[AirborneMovementState] Excessive air speed detected: {currentVelocity.magnitude}");
                
                // Clamp velocity to maximum allowed
                Vector3 clampedVelocity = currentVelocity.normalized * maxAirSpeed;
                context.SetVelocity(clampedVelocity);
            }
            
            // Check for unrealistic position changes
            if (distance > context.TeleportThreshold)
            {
                Debug.LogWarning($"[AirborneMovementState] Potential teleportation in air: {distance}m");
                
                // Reset to last valid position
                context.Transform.position = lastPosition;
                context.SetVelocity(Vector3.zero);
                return;
            }
            
            // Update last valid position
            context.LastValidPosition = currentPosition;
            context.OnPositionValidated?.Invoke(currentPosition);
        }
        
        public override string GetDebugInfo()
        {
            float timeInState = Time.time - stateEnterTime;
            string boost = apexBoostAvailable ? " (Boost Available)" : "";
            return $"AirborneMovementState (Time: {timeInState:F1}s{boost})";
        }
    }
}