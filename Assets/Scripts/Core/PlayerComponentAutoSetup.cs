using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Runtime component auto-setup for player objects
    /// Automatically adds missing components when a player object is spawned
    /// </summary>
    [System.Serializable]
    public class PlayerComponentAutoSetup : MonoBehaviour
    {
        [Header("Auto-Setup Configuration")]
        [SerializeField] private bool autoSetupOnAwake = true;
        [SerializeField] private bool validateComponents = true;
        [SerializeField] private bool addMissingComponents = true;

        private void Awake()
        {
            if (autoSetupOnAwake)
            {
                SetupRequiredComponents();
            }
        }

        [ContextMenu("Setup Required Components")]
        public void SetupRequiredComponents()
        {
            Debug.Log($"[PlayerComponentAutoSetup] Setting up components on {gameObject.name}");

            bool componentsAdded = false;

            // Ensure UnifiedPlayerController exists
            var unifiedController = GetComponent<UnifiedPlayerController>();
            if (unifiedController == null && addMissingComponents)
            {
                unifiedController = gameObject.AddComponent<UnifiedPlayerController>();
                Debug.Log($"[PlayerComponentAutoSetup] Added UnifiedPlayerController to {gameObject.name}");
                componentsAdded = true;
            }

            // Ensure StateMachineIntegration exists
            var stateMachineIntegration = GetComponent<StateMachineIntegration>();
            if (stateMachineIntegration == null && addMissingComponents)
            {
                stateMachineIntegration = gameObject.AddComponent<StateMachineIntegration>();
                Debug.Log($"[PlayerComponentAutoSetup] Added StateMachineIntegration to {gameObject.name}");
                componentsAdded = true;
            }

            // Ensure InputRelay exists
            var inputRelay = GetComponent<InputRelay>();
            if (inputRelay == null && addMissingComponents)
            {
                inputRelay = gameObject.AddComponent<InputRelay>();
                Debug.Log($"[PlayerComponentAutoSetup] Added InputRelay to {gameObject.name}");
                componentsAdded = true;
            }

            // Ensure Rigidbody exists
            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null && addMissingComponents)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                Debug.Log($"[PlayerComponentAutoSetup] Added Rigidbody to {gameObject.name}");
                componentsAdded = true;
            }

            // Ensure Collider exists
            var collider = GetComponent<Collider>();
            if (collider == null && addMissingComponents)
            {
                var capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                capsuleCollider.height = 2f;
                capsuleCollider.radius = 0.5f;
                Debug.Log($"[PlayerComponentAutoSetup] Added CapsuleCollider to {gameObject.name}");
                componentsAdded = true;
            }

            // Ensure Animator exists
            var animator = GetComponent<Animator>();
            if (animator == null && addMissingComponents)
            {
                animator = gameObject.AddComponent<Animator>();
                Debug.Log($"[PlayerComponentAutoSetup] Added Animator to {gameObject.name}");
                componentsAdded = true;
            }

            if (componentsAdded)
            {
                Debug.Log($"[PlayerComponentAutoSetup] Component setup completed for {gameObject.name}");
                
                // Initialize the UnifiedPlayerController if it was just added
                if (unifiedController != null)
                {
                    unifiedController.Initialize();
                }
            }
            else if (validateComponents)
            {
                Debug.Log($"[PlayerComponentAutoSetup] All required components already present on {gameObject.name}");
            }

            ValidateComponentReferences();
        }

        private void ValidateComponentReferences()
        {
            if (!validateComponents) return;

            var stateMachineIntegration = GetComponent<StateMachineIntegration>();
            var unifiedController = GetComponent<UnifiedPlayerController>();
            var inputRelay = GetComponent<InputRelay>();

            if (stateMachineIntegration != null && unifiedController != null)
            {
                // Use reflection to set the characterController field if it's null
                var controllerField = typeof(StateMachineIntegration).GetField("characterController", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (controllerField != null && controllerField.GetValue(stateMachineIntegration) == null)
                {
                    controllerField.SetValue(stateMachineIntegration, unifiedController);
                    Debug.Log($"[PlayerComponentAutoSetup] Linked UnifiedPlayerController to StateMachineIntegration");
                }
            }

            if (inputRelay != null && unifiedController != null)
            {
                // Use reflection to set the characterController field if it's null
                var controllerField = typeof(InputRelay).GetField("characterController", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (controllerField != null && controllerField.GetValue(inputRelay) == null)
                {
                    controllerField.SetValue(inputRelay, unifiedController);
                    Debug.Log($"[PlayerComponentAutoSetup] Linked UnifiedPlayerController to InputRelay");
                }
            }
        }

        [ContextMenu("Validate Components")]
        public void ValidateComponents()
        {
            Debug.Log($"[PlayerComponentAutoSetup] Validating components on {gameObject.name}");

            bool hasAllComponents = true;
            
            if (GetComponent<UnifiedPlayerController>() == null)
            {
                Debug.LogWarning($"[PlayerComponentAutoSetup] {gameObject.name} is missing UnifiedPlayerController component");
                hasAllComponents = false;
            }

            if (GetComponent<StateMachineIntegration>() == null)
            {
                Debug.LogWarning($"[PlayerComponentAutoSetup] {gameObject.name} is missing StateMachineIntegration component");
                hasAllComponents = false;
            }

            if (GetComponent<InputRelay>() == null)
            {
                Debug.LogWarning($"[PlayerComponentAutoSetup] {gameObject.name} is missing InputRelay component");
                hasAllComponents = false;
            }

            if (GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning($"[PlayerComponentAutoSetup] {gameObject.name} is missing Rigidbody component");
                hasAllComponents = false;
            }

            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[PlayerComponentAutoSetup] {gameObject.name} is missing Collider component");
                hasAllComponents = false;
            }

            if (hasAllComponents)
            {
                Debug.Log($"[PlayerComponentAutoSetup] ✓ {gameObject.name} has all required components");
            }
            else
            {
                Debug.LogWarning($"[PlayerComponentAutoSetup] {gameObject.name} is missing some required components");
            }
        }
    }
}
