using UnityEngine;
using Unity.Netcode;

namespace MOBA.Movement
{
    /// <summary>
    /// Movement context containing all necessary components and data for movement states
    /// Implements the Context pattern to provide state classes with required dependencies
    /// Reference: Design Patterns (GOF) Chapter 5 - State Pattern implementation
    /// </summary>
    [System.Serializable]
    public class MovementContext
    {
        #region Component References
        
        public Transform Transform { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public NetworkObject NetworkObject { get; private set; }
        public bool IsNetworked => NetworkObject != null;
        
        #endregion
        
        #region Movement Configuration
        
        [Header("Movement Settings")]
        public float BaseMoveSpeed = 8f;
        public float JumpForce = 8f;
        public float HoldJumpMultiplier = 1.5f;
        public float DoubleJumpMultiplier = 2.0f;
        public float ApexBoostMultiplier = 2.8f;
        public float ApexVelocityThreshold = 1.0f;
        public float ApexTimeWindow = 0.4f;
        public float GroundCheckDistance = 1.1f;
        public LayerMask GroundLayer = 1;
        
        [Header("Network Validation")]
        public bool EnableNetworkValidation = true;
        public float MaxMovementSpeed = 15f;
        public float TeleportThreshold = 20f;
        public float MaxInputMagnitude = 1.1f;
        
        [Header("Dash Settings")]
        public float DashForce = 20f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 3f;
        public bool DashIgnoresGravity = true;
        
        #endregion
        
        #region State Data
        
        public Vector3 MovementInput { get; set; } = Vector3.zero;
        public Vector3 LastValidPosition { get; set; } = Vector3.zero;
        public bool IsGrounded { get; set; } = false;
        public float LastGroundedTime { get; set; } = 0f;
        public bool CanDoubleJump { get; set; } = true;
        public bool HasDoubleJumped { get; set; } = false;
        public bool PendingHoldBoost { get; set; } = false;
        public bool HoldBoostApplied { get; set; } = false;
        public bool FirstJumpUsedHold { get; set; } = false;
        public float FirstJumpTime { get; set; } = 0f;
        public float ValidationSuspendUntil { get; set; } = 0f;
        
        // Dash state
        public bool IsDashing { get; set; } = false;
        public float DashStartTime { get; set; } = 0f;
        public Vector3 DashDirection { get; set; } = Vector3.zero;
        public float LastDashTime { get; set; } = -999f;
        
        // Airborne state
        public float AirborneStartTime { get; set; } = 0f;
        public Vector3 AirborneStartVelocity { get; set; } = Vector3.zero;
        
        #endregion
        
        #region Events
        
        public System.Action<Vector3> OnMovementInputChanged;
        public System.Action<bool> OnGroundedStateChanged;
        public System.Action<Vector3> OnPositionValidated;
        public System.Action<MovementState, MovementState> OnStateChanged;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the movement context with required components
        /// </summary>
        /// <param name="transform">Player transform component</param>
        /// <param name="rigidbody">Player rigidbody component</param>
        /// <param name="networkObject">Network object for multiplayer support (optional)</param>
        public void Initialize(Transform transform, Rigidbody rigidbody, NetworkObject networkObject = null)
        {
            Transform = transform;
            Rigidbody = rigidbody;
            NetworkObject = networkObject;
            
            if (Transform == null || Rigidbody == null)
            {
                Debug.LogError("[MovementContext] Missing required components (Transform or Rigidbody)");
                return;
            }
            
            LastValidPosition = Transform.position;
            ResetToDefaults();
        }
        
        /// <summary>
        /// Reset movement state to default values
        /// </summary>
        public void ResetToDefaults()
        {
            MovementInput = Vector3.zero;
            IsGrounded = false;
            LastGroundedTime = 0f;
            CanDoubleJump = true;
            HasDoubleJumped = false;
            PendingHoldBoost = false;
            HoldBoostApplied = false;
            FirstJumpUsedHold = false;
            FirstJumpTime = 0f;
            ValidationSuspendUntil = 0f;
            
            IsDashing = false;
            DashStartTime = 0f;
            DashDirection = Vector3.zero;
            
            AirborneStartTime = 0f;
            AirborneStartVelocity = Vector3.zero;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Check if player is currently grounded using physics raycast
        /// </summary>
        /// <returns>True if grounded, false if airborne</returns>
        public bool CheckGrounded()
        {
            if (Transform == null)
                return false;
                
            bool wasGrounded = IsGrounded;
            Vector3 origin = Transform.position + Vector3.up * 0.1f;
            IsGrounded = Physics.Raycast(origin, Vector3.down, GroundCheckDistance, GroundLayer);
            
            if (IsGrounded && !wasGrounded)
            {
                LastGroundedTime = Time.time;
                CanDoubleJump = true;
                HasDoubleJumped = false;
                OnGroundedStateChanged?.Invoke(true);
            }
            else if (!IsGrounded && wasGrounded)
            {
                OnGroundedStateChanged?.Invoke(false);
            }
            
            return IsGrounded;
        }
        
        /// <summary>
        /// Get current velocity from rigidbody
        /// </summary>
        /// <returns>Current velocity vector</returns>
        public Vector3 GetVelocity()
        {
            return Rigidbody != null ? Rigidbody.linearVelocity : Vector3.zero;
        }
        
        /// <summary>
        /// Set rigidbody velocity
        /// </summary>
        /// <param name="velocity">New velocity vector</param>
        public void SetVelocity(Vector3 velocity)
        {
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = velocity;
            }
        }
        
        /// <summary>
        /// Add force to rigidbody
        /// </summary>
        /// <param name="force">Force vector to apply</param>
        /// <param name="mode">Force mode for application</param>
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            if (Rigidbody != null)
            {
                Rigidbody.AddForce(force, mode);
            }
        }
        
        /// <summary>
        /// Check if enough time has passed since last dash
        /// </summary>
        /// <returns>True if dash is available, false if on cooldown</returns>
        public bool IsDashAvailable()
        {
            return Time.time - LastDashTime >= DashCooldown;
        }
        
        /// <summary>
        /// Validate movement input against network security rules
        /// </summary>
        /// <param name="input">Input vector to validate</param>
        /// <returns>Validated and clamped input vector</returns>
        public Vector3 ValidateInput(Vector3 input)
        {
            // Validate input magnitude
            if (input.magnitude > MaxInputMagnitude)
            {
                input = input.normalized * MaxInputMagnitude;
                if (EnableNetworkValidation)
                {
                    Debug.LogWarning($"[MovementContext] Movement input clamped from {input.magnitude} to {MaxInputMagnitude}");
                }
            }

            // Check for NaN values
            if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsNaN(input.z))
            {
                Debug.LogError("[MovementContext] Invalid movement input (NaN detected)");
                return Vector3.zero;
            }

            return Vector3.ClampMagnitude(input, 1f);
        }
        
        #endregion
    }
}