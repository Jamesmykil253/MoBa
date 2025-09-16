using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple and effective camera controller for MOBA gameplay
    /// No complex patterns - just clean, functional camera control
    /// </summary>
    public class SimpleCameraController : MonoBehaviour
    {
        [Header("Follow Target")]
        public Transform target;
        
        [Header("Camera Settings")]
        public Vector3 offset = new Vector3(0, 15, -10);
        public float followSpeed = 5f;
        public float rotationSpeed = 2f;
        
        [Header("Zoom")]
        public float zoomSpeed = 2f;
        public float minZoom = 5f;
        public float maxZoom = 20f;
        
        [Header("Pan Limits")]
        public float panLimit = 10f;
        
        private Camera cam;
        private Vector3 targetPosition;
        private bool isPanning = false;
        private Vector3 lastMousePosition;
        private float nextTargetSearchTime = 0f;
        [SerializeField] private float targetSearchInterval = 1f;
        
        void Start()
        {
            cam = GetComponent<Camera>();
            TryResolveTarget(true);
        }
        
        void LateUpdate()
        {
            if (!EnsureTarget()) return;
            
            HandleInput();
            UpdateCameraPosition();
        }
        
        // New Input System support
        // Requires InputActionAsset with actions: "Camera/Zoom" (Value: float), "Camera/Pan" (Button), "Camera/PointerPosition" (Value: Vector2)
        [Header("Input System")]
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset inputActions;
        private UnityEngine.InputSystem.InputAction zoomAction;
        private UnityEngine.InputSystem.InputAction panAction;
        private UnityEngine.InputSystem.InputAction pointerPositionAction;
        private Vector2 lastPointerPosition;

        void OnEnable()
        {
            if (inputActions != null)
            {
                zoomAction = inputActions.FindAction("Camera/Zoom");
                panAction = inputActions.FindAction("Camera/Pan");
                pointerPositionAction = inputActions.FindAction("Camera/PointerPosition");
                zoomAction?.Enable();
                panAction?.Enable();
                pointerPositionAction?.Enable();
            }
        }

        void OnDisable()
        {
            zoomAction?.Disable();
            panAction?.Disable();
            pointerPositionAction?.Disable();
        }

        void HandleInput()
        {
            if (zoomAction != null)
            {
                float scroll = zoomAction.ReadValue<float>();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    offset = Vector3.Lerp(offset, offset + Vector3.forward * scroll * zoomSpeed, Time.deltaTime * 10f);
                    offset = Vector3.ClampMagnitude(offset, maxZoom);
                    if (offset.magnitude < minZoom)
                        offset = offset.normalized * minZoom;
                }
            }

            if (panAction != null && pointerPositionAction != null)
            {
                bool panPressed = panAction.ReadValue<float>() > 0.5f;
                Vector2 pointerPos = pointerPositionAction.ReadValue<Vector2>();
                if (panPressed && !isPanning)
                {
                    isPanning = true;
                    lastPointerPosition = pointerPos;
                }
                else if (!panPressed && isPanning)
                {
                    isPanning = false;
                }

                if (isPanning)
                {
                    Vector2 delta = pointerPos - lastPointerPosition;
                    Vector3 panAmount = new Vector3(-delta.x, 0, -delta.y) * 0.01f;
                    offset += panAmount;
                    lastPointerPosition = pointerPos;
                }
            }
        }
        
        void UpdateCameraPosition()
        {
            targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            transform.LookAt(target.position);
        }
        
        /// <summary>
        /// Set a new target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Reset camera to default position
        /// </summary>
        public void ResetPosition()
        {
            offset = new Vector3(0, 15, -10);
        }

        private bool EnsureTarget()
        {
            if (target != null) return true;

            TryResolveTarget(false);
            return target != null;
        }

        private void TryResolveTarget(bool force)
        {
            if (!force && Time.time < nextTargetSearchTime)
            {
                return;
            }

            nextTargetSearchTime = Time.time + Mathf.Max(0.1f, targetSearchInterval);

            var player = FindFirstObjectByType<SimplePlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
}
