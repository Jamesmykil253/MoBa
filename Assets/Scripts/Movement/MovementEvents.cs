using UnityEngine;
using MOBA; // For ILocalEvent interface

namespace MOBA.Movement
{
    /// <summary>
    /// Event raised when movement input changes
    /// </summary>
    public class MovementInputChangedEvent : ILocalEvent
    {
        public GameObject Actor { get; }
        public Vector3 PreviousInput { get; }
        public Vector3 NewInput { get; }
        
        public MovementInputChangedEvent(GameObject actor, Vector3 previousInput, Vector3 newInput)
        {
            Actor = actor;
            PreviousInput = previousInput;
            NewInput = newInput;
        }
    }
    
    /// <summary>
    /// Event raised when a jump is executed
    /// </summary>
    public class JumpExecutedEvent : ILocalEvent
    {
        public GameObject Actor { get; }
        public Vector3 JumpForce { get; }
        public UnifiedMovementSystem.JumpExecutionResult JumpType { get; }
        
        public JumpExecutedEvent(GameObject actor, Vector3 jumpForce, UnifiedMovementSystem.JumpExecutionResult jumpType)
        {
            Actor = actor;
            JumpForce = jumpForce;
            JumpType = jumpType;
        }
    }
    
    /// <summary>
    /// Event raised when a dash is executed
    /// </summary>
    public class DashExecutedEvent : ILocalEvent
    {
        public GameObject Actor { get; }
        public Vector3 DashDirection { get; }
        public float DashForce { get; }
        
        public DashExecutedEvent(GameObject actor, Vector3 dashDirection, float dashForce)
        {
            Actor = actor;
            DashDirection = dashDirection;
            DashForce = dashForce;
        }
    }
    
    /// <summary>
    /// Event raised when a dash completes
    /// </summary>
    public class DashCompletedEvent : ILocalEvent
    {
        public GameObject Actor { get; }
        public float DashDuration { get; }
        
        public DashCompletedEvent(GameObject actor, float dashDuration)
        {
            Actor = actor;
            DashDuration = dashDuration;
        }
    }
    
    /// <summary>
    /// Event raised when movement state changes
    /// </summary>
    public class MovementStateChangedEvent : ILocalEvent
    {
        public GameObject Actor { get; }
        public MovementState PreviousState { get; }
        public MovementState NewState { get; }
        
        public MovementStateChangedEvent(GameObject actor, MovementState previousState, MovementState newState)
        {
            Actor = actor;
            PreviousState = previousState;
            NewState = newState;
        }
    }
    
    /// <summary>
    /// Event raised when grounded state changes
    /// </summary>
    public class GroundedStateChangedEvent : ILocalEvent
    {
        public GameObject Actor { get; }
        public bool IsGrounded { get; }
        public float GroundedTime { get; }
        
        public GroundedStateChangedEvent(GameObject actor, bool isGrounded, float groundedTime)
        {
            Actor = actor;
            IsGrounded = isGrounded;
            GroundedTime = groundedTime;
        }
    }
}