using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Legacy movement façade that now forwards to <see cref="UnifiedMovementSystem"/>.
    /// </summary>
    [System.Serializable]
    public class PlayerMovement
    {
        [SerializeField] private UnifiedMovementSystem movementSystem = new UnifiedMovementSystem();

        public bool IsGrounded => movementSystem.IsGrounded;
        public Vector3 MovementInput => movementSystem.MovementInput;
        public float MovementSpeed => movementSystem.MovementSpeed;

        public void Initialize(Transform playerTransform, Rigidbody rigidbody)
        {
            movementSystem.Initialize(playerTransform, rigidbody);
        }

        public void SetMovementInput(Vector3 input)
        {
            movementSystem.SetMovementInput(input);
        }

        public void UpdateGroundDetection()
        {
            // UnifiedMovementSystem performs ground checks during UpdateMovement.
        }

        public bool TryJump()
        {
            return movementSystem.TryJump();
        }

        public void ApplyMovement()
        {
            movementSystem.UpdateMovement();
        }
    }
}
