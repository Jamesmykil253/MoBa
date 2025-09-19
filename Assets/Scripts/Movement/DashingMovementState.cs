using UnityEngine;

namespace MOBA.Movement
{
    /// <summary>
    /// Dashing movement state - handles dash ability with special movement mechanics
    /// Reference: Game Programming Patterns Chapter 6 - State Pattern implementation
    /// Manages high-speed dash movement with optional gravity ignore and collision handling
    /// </summary>
    public class DashingMovementState : MovementState
    {
        private float stateEnterTime;
        private Vector3 dashDirection;
        private bool gravityWasEnabled;
        private Vector3 originalVelocity;
        
        public override void Enter(MovementContext context)
        {
            stateEnterTime = Time.time;
            originalVelocity = context.GetVelocity();
            
            // Determine dash direction from current input or velocity
            dashDirection = GetDashDirection(context);
            
            // Record dash state
            context.IsDashing = true;
            context.DashStartTime = Time.time;
            context.DashDirection = dashDirection;
            context.LastDashTime = Time.time;
            
            // Handle gravity during dash
            if (context.DashIgnoresGravity && context.Rigidbody != null)
            {
                gravityWasEnabled = context.Rigidbody.useGravity;
                context.Rigidbody.useGravity = false;
            }
            
            // Apply initial dash force
            ApplyDashForce(context);
            
            if (Application.isPlaying)
            {
                Debug.Log($"[DashingMovementState] Started dash in direction: {dashDirection} with force: {context.DashForce}");
            }
            
            // Publish dash event
            UnifiedEventSystem.PublishLocal(new DashExecutedEvent(
                context.Transform.gameObject, dashDirection, context.DashForce));
        }
        
        public override MovementState Update(MovementContext context)
        {
            float dashElapsedTime = Time.time - context.DashStartTime;
            
            // Check if dash duration has expired
            if (dashElapsedTime >= context.DashDuration)
            {
                return EndDash(context);
            }
            
            // Maintain dash velocity (prevent other forces from interfering)
            MaintainDashMovement(context);
            
            // Check for early dash termination conditions
            if (ShouldTerminateDash(context))
            {
                return EndDash(context);
            }
            
            // Continue dashing
            return null;
        }
        
        public override void Exit(MovementContext context)
        {
            // Restore gravity if it was disabled
            if (context.DashIgnoresGravity && context.Rigidbody != null)
            {
                context.Rigidbody.useGravity = gravityWasEnabled;
            }
            
            // Clear dash state
            context.IsDashing = false;
            
            // Apply post-dash velocity adjustment
            ApplyPostDashVelocity(context);
            
            float dashDuration = Time.time - stateEnterTime;
            if (Application.isPlaying)
            {
                Debug.Log($"[DashingMovementState] Dash completed after {dashDuration:F2}s");
            }
            
            // Publish dash end event
            UnifiedEventSystem.PublishLocal(new DashCompletedEvent(
                context.Transform.gameObject, dashDuration));
        }
        
        public override bool HandleJumpInput(MovementContext context)
        {
            // Prevent jumping during dash to maintain dash integrity
            return false;
        }
        
        public override void HandleInput(MovementContext context, Vector3 input)
        {
            // During dash, ignore movement input to maintain dash direction
            // Input will be processed after dash completes
        }
        
        /// <summary>
        /// Determine the direction for the dash based on input and current state
        /// </summary>
        private Vector3 GetDashDirection(MovementContext context)
        {
            Vector3 direction = Vector3.zero;
            
            // Priority 1: Current movement input
            if (context.MovementInput.magnitude > 0.1f)
            {
                direction = new Vector3(context.MovementInput.x, 0f, context.MovementInput.z).normalized;
            }
            // Priority 2: Current velocity direction (if moving)
            else if (originalVelocity.magnitude > 0.1f)
            {
                Vector3 horizontalVelocity = new Vector3(originalVelocity.x, 0f, originalVelocity.z);
                if (horizontalVelocity.magnitude > 0.1f)
                {
                    direction = horizontalVelocity.normalized;
                }
            }
            // Priority 3: Forward direction of transform
            else
            {
                direction = context.Transform.forward;
            }
            
            // Ensure we have a valid direction
            if (direction.magnitude < 0.1f)
            {
                direction = Vector3.forward;
            }
            
            return direction.normalized;
        }
        
        /// <summary>
        /// Apply the initial dash force
        /// </summary>
        private void ApplyDashForce(MovementContext context)
        {
            if (context.Rigidbody == null)
                return;
                
            // Clear existing velocity for clean dash
            if (context.DashIgnoresGravity)
            {
                context.SetVelocity(Vector3.zero);
            }
            else
            {
                // Preserve vertical velocity for ground dashes
                Vector3 currentVelocity = context.GetVelocity();
                context.SetVelocity(new Vector3(0f, currentVelocity.y, 0f));
            }
            
            // Apply dash force in the calculated direction
            Vector3 dashForceVector = dashDirection * context.DashForce;
            context.AddForce(dashForceVector, ForceMode.VelocityChange);
        }
        
        /// <summary>
        /// Maintain consistent dash movement throughout the dash duration
        /// </summary>
        private void MaintainDashMovement(MovementContext context)
        {
            if (context.Rigidbody == null)
                return;
                
            // Calculate expected dash velocity
            Vector3 expectedVelocity = dashDirection * context.DashForce;
            
            // If gravity is ignored, maintain exact dash velocity
            if (context.DashIgnoresGravity)
            {
                context.SetVelocity(expectedVelocity);
            }
            else
            {
                // Maintain horizontal dash velocity while allowing vertical changes
                Vector3 currentVelocity = context.GetVelocity();
                Vector3 maintainedVelocity = new Vector3(expectedVelocity.x, currentVelocity.y, expectedVelocity.z);
                context.SetVelocity(maintainedVelocity);
            }
            
            // Validate position for anti-cheat
            if (context.EnableNetworkValidation)
            {
                ValidateDashMovement(context);
            }
        }
        
        /// <summary>
        /// Check if dash should be terminated early
        /// </summary>
        private bool ShouldTerminateDash(MovementContext context)
        {
            // Terminate if we hit a wall or obstacle
            if (HasHitObstacle(context))
            {
                if (Application.isPlaying)
                {
                    Debug.Log("[DashingMovementState] Dash terminated due to obstacle collision");
                }
                return true;
            }
            
            // Could add other termination conditions here:
            // - Taking damage
            // - Manual cancellation input
            // - Environmental hazards
            
            return false;
        }
        
        /// <summary>
        /// Check if dash has collided with an obstacle
        /// </summary>
        private bool HasHitObstacle(MovementContext context)
        {
            // Simple implementation: check if velocity has been significantly reduced
            Vector3 currentVelocity = context.GetVelocity();
            float expectedSpeed = context.DashForce;
            float currentSpeed = currentVelocity.magnitude;
            
            // If speed has dropped significantly, we likely hit something
            float speedRatio = currentSpeed / expectedSpeed;
            return speedRatio < 0.5f;
        }
        
        /// <summary>
        /// Determine which state to transition to after dash ends
        /// </summary>
        private MovementState EndDash(MovementContext context)
        {
            // Check if we're grounded or airborne
            bool isGrounded = context.CheckGrounded();
            
            if (isGrounded)
            {
                return new GroundedMovementState();
            }
            else
            {
                return new AirborneMovementState();
            }
        }
        
        /// <summary>
        /// Apply velocity adjustment after dash completes
        /// </summary>
        private void ApplyPostDashVelocity(MovementContext context)
        {
            if (context.Rigidbody == null)
                return;
                
            Vector3 currentVelocity = context.GetVelocity();
            
            // Reduce dash velocity to prevent excessive momentum
            float velocityReduction = 0.6f; // Reduce to 60% of dash speed
            Vector3 reducedVelocity = currentVelocity * velocityReduction;
            
            // If gravity was ignored during dash, restore normal physics
            if (context.DashIgnoresGravity)
            {
                // Maintain some horizontal momentum but allow normal vertical physics
                Vector3 horizontalMomentum = new Vector3(reducedVelocity.x, 0f, reducedVelocity.z);
                context.SetVelocity(horizontalMomentum);
            }
            else
            {
                context.SetVelocity(reducedVelocity);
            }
        }
        
        /// <summary>
        /// Validate dash movement for network anti-cheat
        /// </summary>
        private void ValidateDashMovement(MovementContext context)
        {
            Vector3 currentPosition = context.Transform.position;
            Vector3 lastPosition = context.LastValidPosition;
            
            float distance = Vector3.Distance(currentPosition, lastPosition);
            float expectedMaxDistance = context.DashForce * Time.deltaTime * 1.2f; // Allow 20% tolerance
            
            if (distance > expectedMaxDistance)
            {
                Debug.LogWarning($"[DashingMovementState] Excessive dash movement: {distance}m vs expected {expectedMaxDistance}m");
                
                // Don't reset position during dash unless it's clearly impossible
                if (distance > context.TeleportThreshold)
                {
                    Debug.LogError("[DashingMovementState] Impossible dash distance - potential teleport hack");
                    context.Transform.position = lastPosition;
                    context.SetVelocity(Vector3.zero);
                    return;
                }
            }
            
            // Update last valid position
            context.LastValidPosition = currentPosition;
            context.OnPositionValidated?.Invoke(currentPosition);
        }
        
        public override string GetDebugInfo()
        {
            float timeInState = Time.time - stateEnterTime;
            float remainingTime = Mathf.Max(0f, stateEnterTime + 0.2f - Time.time); // Assuming 0.2s dash duration
            return $"DashingMovementState (Time: {timeInState:F2}s, Remaining: {remainingTime:F2}s, Dir: {dashDirection})";
        }
    }
}