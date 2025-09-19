using UnityEngine;

namespace MOBA.Movement
{
    /// <summary>
    /// Abstract base class for movement states implementing the State Pattern
    /// Reference: Game Programming Patterns Chapter 6, Design Patterns (GOF) Chapter 5
    /// Provides explicit state management for complex movement behaviors
    /// </summary>
    public abstract class MovementState
    {
        /// <summary>
        /// Called when entering this movement state
        /// </summary>
        /// <param name="context">Movement context containing all necessary components and data</param>
        public abstract void Enter(MovementContext context);
        
        /// <summary>
        /// Called every frame while in this movement state
        /// </summary>
        /// <param name="context">Movement context containing all necessary components and data</param>
        /// <returns>The next state to transition to, or null to remain in current state</returns>
        public abstract MovementState Update(MovementContext context);
        
        /// <summary>
        /// Called when exiting this movement state
        /// </summary>
        /// <param name="context">Movement context containing all necessary components and data</param>
        public abstract void Exit(MovementContext context);
        
        /// <summary>
        /// Handle input events in this state
        /// </summary>
        /// <param name="context">Movement context</param>
        /// <param name="input">Input vector</param>
        public virtual void HandleInput(MovementContext context, Vector3 input)
        {
            // Default implementation: update movement input
            context.MovementInput = input;
        }
        
        /// <summary>
        /// Handle jump input in this state
        /// </summary>
        /// <param name="context">Movement context</param>
        /// <returns>True if jump was handled, false otherwise</returns>
        public virtual bool HandleJumpInput(MovementContext context)
        {
            // Default implementation: no jump handling
            return false;
        }
        
        /// <summary>
        /// Check if this state can transition to another state
        /// </summary>
        /// <param name="targetState">The state to transition to</param>
        /// <returns>True if transition is allowed, false otherwise</returns>
        public virtual bool CanTransitionTo(MovementState targetState)
        {
            // Default implementation: allow all transitions
            return true;
        }
        
        /// <summary>
        /// Get debug information about this state
        /// </summary>
        /// <returns>String representation for debugging</returns>
        public virtual string GetDebugInfo()
        {
            return GetType().Name;
        }
    }
}