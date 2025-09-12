using UnityEngine;
using UnityEditor;
using MOBA;

namespace MOBA.Editor
{
    /// <summary>
    /// Editor utility to help set up missing components on Player prefabs
    /// Run this to automatically add UnifiedPlayerController to prefabs that need it
    /// </summary>
    public class PrefabComponentSetup : MonoBehaviour
    {
        [MenuItem("MOBA/Setup Missing Components")]
        public static void SetupMissingComponents()
        {
            SetupPlayerPrefab();
            SetupNetworkPlayerPrefab();
            
            Debug.Log("[PrefabComponentSetup] Component setup completed!");
        }

        private static void SetupPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Gameplay/Player.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] Could not find prefab at {prefabPath}");
                return;
            }

            // Create a temporary instance to modify
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            
            try
            {
                SetupPlayerComponents(instance, "Player");
                
                // Save changes back to prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
                Debug.Log($"[PrefabComponentSetup] Updated {prefabPath} with required components");
            }
            finally
            {
                DestroyImmediate(instance);
            }
        }

        private static void SetupNetworkPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Network/Players/NetworkPlayer.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] Could not find prefab at {prefabPath}");
                return;
            }

            // Create a temporary instance to modify
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            
            try
            {
                SetupPlayerComponents(instance, "NetworkPlayer");
                
                // Save changes back to prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
                Debug.Log($"[PrefabComponentSetup] Updated {prefabPath} with required components");
            }
            finally
            {
                DestroyImmediate(instance);
            }
        }

        private static void SetupPlayerComponents(GameObject playerObject, string prefabType)
        {
            Debug.Log($"[PrefabComponentSetup] Setting up components for {prefabType}");

            // Ensure UnifiedPlayerController exists
            var unifiedController = playerObject.GetComponent<UnifiedPlayerController>();
            if (unifiedController == null)
            {
                unifiedController = playerObject.AddComponent<UnifiedPlayerController>();
                Debug.Log($"[PrefabComponentSetup] Added UnifiedPlayerController to {prefabType}");
            }

            // Ensure StateMachineIntegration exists
            var stateMachineIntegration = playerObject.GetComponent<StateMachineIntegration>();
            if (stateMachineIntegration == null)
            {
                stateMachineIntegration = playerObject.AddComponent<StateMachineIntegration>();
                Debug.Log($"[PrefabComponentSetup] Added StateMachineIntegration to {prefabType}");
            }

            // Ensure InputRelay exists
            var inputRelay = playerObject.GetComponent<InputRelay>();
            if (inputRelay == null)
            {
                inputRelay = playerObject.AddComponent<InputRelay>();
                Debug.Log($"[PrefabComponentSetup] Added InputRelay to {prefabType}");
            }

            // Ensure Rigidbody exists (required for movement)
            var rigidbody = playerObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = playerObject.AddComponent<Rigidbody>();
                rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                Debug.Log($"[PrefabComponentSetup] Added Rigidbody to {prefabType}");
            }

            // Ensure Collider exists
            var collider = playerObject.GetComponent<Collider>();
            if (collider == null)
            {
                var capsuleCollider = playerObject.AddComponent<CapsuleCollider>();
                capsuleCollider.height = 2f;
                capsuleCollider.radius = 0.5f;
                Debug.Log($"[PrefabComponentSetup] Added CapsuleCollider to {prefabType}");
            }

            // Ensure Animator exists (for state machine integration)
            var animator = playerObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = playerObject.AddComponent<Animator>();
                Debug.Log($"[PrefabComponentSetup] Added Animator to {prefabType}");
            }

            Debug.Log($"[PrefabComponentSetup] Component setup completed for {prefabType}");
        }

        [MenuItem("MOBA/Validate Prefab Components")]
        public static void ValidatePrefabComponents()
        {
            ValidatePlayerPrefab("Assets/Prefabs/Gameplay/Player.prefab", "Player");
            ValidatePlayerPrefab("Assets/Prefabs/Network/Players/NetworkPlayer.prefab", "NetworkPlayer");
        }

        private static void ValidatePlayerPrefab(string prefabPath, string prefabType)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"[PrefabComponentSetup] Could not find {prefabType} prefab at {prefabPath}");
                return;
            }

            bool hasAllComponents = true;
            
            if (prefab.GetComponent<UnifiedPlayerController>() == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] {prefabType} is missing UnifiedPlayerController component");
                hasAllComponents = false;
            }

            if (prefab.GetComponent<StateMachineIntegration>() == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] {prefabType} is missing StateMachineIntegration component");
                hasAllComponents = false;
            }

            if (prefab.GetComponent<InputRelay>() == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] {prefabType} is missing InputRelay component");
                hasAllComponents = false;
            }

            if (prefab.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] {prefabType} is missing Rigidbody component");
                hasAllComponents = false;
            }

            if (prefab.GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[PrefabComponentSetup] {prefabType} is missing Collider component");
                hasAllComponents = false;
            }

            if (hasAllComponents)
            {
                Debug.Log($"[PrefabComponentSetup] ✓ {prefabType} has all required components");
            }
            else
            {
                Debug.LogWarning($"[PrefabComponentSetup] {prefabType} is missing some components. Run 'MOBA/Setup Missing Components' to fix.");
            }
        }
    }
}
