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
        
        void Start()
        {
            cam = GetComponent<Camera>();
            
            // Auto-find player if no target set
            if (target == null)
            {
                var player = FindFirstObjectByType<SimplePlayerController>();
                if (player != null)
                    target = player.transform;
            }
        }
        
        void LateUpdate()
        {
            if (target == null) return;
            
            HandleInput();
            UpdateCameraPosition();
        }
        
        void HandleInput()
        {
            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                offset = Vector3.Lerp(offset, offset + Vector3.forward * scroll * zoomSpeed, Time.deltaTime * 10f);
                offset = Vector3.ClampMagnitude(offset, maxZoom);
                if (offset.magnitude < minZoom)
                    offset = offset.normalized * minZoom;
            }
            
            // Pan with middle mouse button
            if (Input.GetMouseButtonDown(2))
            {
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                isPanning = false;
            }
            
            if (isPanning)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                Vector3 panAmount = new Vector3(-delta.x, 0, -delta.y) * 0.01f;
                offset += panAmount;
                lastMousePosition = Input.mousePosition;
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
    }
}
