using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Quick fix script to add required components to player objects in the scene
    /// Attach this to any GameObject and run the setup methods from the context menu
    /// </summary>
    public class PlayerSetupHelper : MonoBehaviour
    {
        [ContextMenu("Fix All Players In Scene")]
        public void FixAllPlayersInScene()
        {
            // Find all GameObjects that might be player objects
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int fixedCount = 0;
            
            foreach (var obj in allObjects)
            {
                // Check if this looks like a player object
                if (IsPlayerObject(obj))
                {
                    SetupPlayerObject(obj);
                    fixedCount++;
                }
            }
            
            Debug.Log($"[PlayerSetupHelper] Fixed {fixedCount} player objects in the scene");
        }

        [ContextMenu("Fix Players With StateMachineIntegration")]
        public void FixPlayersWithStateMachineIntegration()
        {
            var stateMachineComponents = FindObjectsByType<StateMachineIntegration>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int fixedCount = 0;
            
            foreach (var stateMachine in stateMachineComponents)
            {
                SetupPlayerObject(stateMachine.gameObject);
                fixedCount++;
            }
            
            Debug.Log($"[PlayerSetupHelper] Fixed {fixedCount} objects with StateMachineIntegration");
        }

        private bool IsPlayerObject(GameObject obj)
        {
            // Check if the object has any player-related components or names
            return obj.name.ToLower().Contains("player") || 
                   obj.GetComponent<StateMachineIntegration>() != null ||
                   obj.GetComponent<InputRelay>() != null ||
                   obj.GetComponent<UnifiedPlayerController>() != null;
        }

        private void SetupPlayerObject(GameObject playerObject)
        {
            Debug.Log($"[PlayerSetupHelper] Setting up {playerObject.name}");

            // Add UnifiedPlayerController if missing
            var unifiedController = playerObject.GetComponent<UnifiedPlayerController>();
            if (unifiedController == null)
            {
                unifiedController = playerObject.AddComponent<UnifiedPlayerController>();
                Debug.Log($"[PlayerSetupHelper] Added UnifiedPlayerController to {playerObject.name}");
            }

            // Add StateMachineIntegration if missing
            var stateMachineIntegration = playerObject.GetComponent<StateMachineIntegration>();
            if (stateMachineIntegration == null)
            {
                stateMachineIntegration = playerObject.AddComponent<StateMachineIntegration>();
                Debug.Log($"[PlayerSetupHelper] Added StateMachineIntegration to {playerObject.name}");
            }

            // Add InputRelay if missing
            var inputRelay = playerObject.GetComponent<InputRelay>();
            if (inputRelay == null)
            {
                inputRelay = playerObject.AddComponent<InputRelay>();
                Debug.Log($"[PlayerSetupHelper] Added InputRelay to {playerObject.name}");
            }

            // Add Rigidbody if missing
            var rigidbody = playerObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = playerObject.AddComponent<Rigidbody>();
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                Debug.Log($"[PlayerSetupHelper] Added Rigidbody to {playerObject.name}");
            }

            // Add Collider if missing
            var collider = playerObject.GetComponent<Collider>();
            if (collider == null)
            {
                var capsuleCollider = playerObject.AddComponent<CapsuleCollider>();
                capsuleCollider.height = 2f;
                capsuleCollider.radius = 0.5f;
                Debug.Log($"[PlayerSetupHelper] Added CapsuleCollider to {playerObject.name}");
            }

            // Add Animator if missing
            var animator = playerObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = playerObject.AddComponent<Animator>();
                Debug.Log($"[PlayerSetupHelper] Added Animator to {playerObject.name}");
            }

            // Initialize the UnifiedPlayerController
            unifiedController.Initialize();

            Debug.Log($"[PlayerSetupHelper] Setup completed for {playerObject.name}");
        }
    }
}
