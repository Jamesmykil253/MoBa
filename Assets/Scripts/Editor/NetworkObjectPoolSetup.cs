
using UnityEngine;
using UnityEditor;
using MOBA.Networking;

namespace MOBA.Editor
{
    public static class NetworkObjectPoolSetup
    {
        [MenuItem("MOBA/Network/Create Pool Object Manager Component")]
        public static void CreatePoolManagerComponent()
        {
            // Find existing NetworkSystemIntegration
            NetworkSystemIntegration networkSystem = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (networkSystem == null)
            {
                Debug.LogError("No NetworkSystemIntegration found in scene!");
                return;
            }
            
            // Check if there's already a pool manager component
            NetworkPoolObjectManager existingComponent = Object.FindAnyObjectByType<NetworkPoolObjectManager>();
            if (existingComponent != null)
            {
                Debug.Log($"NetworkPoolObjectManager already exists on {existingComponent.gameObject.name}");
                return;
            }
            
            // Create new GameObject for the pool manager
            GameObject poolManagerGO = new GameObject("NetworkPoolObjectManager");
            NetworkPoolObjectManager poolManager = poolManagerGO.AddComponent<NetworkPoolObjectManager>();
            
            // Try to assign it to the NetworkSystemIntegration's componentPoolManager field
            SerializedObject serializedObject = new SerializedObject(networkSystem);
            SerializedProperty componentPoolManagerProperty = serializedObject.FindProperty("componentPoolManager");
            if (componentPoolManagerProperty != null)
            {
                componentPoolManagerProperty.objectReferenceValue = poolManager;
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"Created and assigned NetworkPoolObjectManager to {networkSystem.gameObject.name}");
            }
            else
            {
                Debug.Log($"Created NetworkPoolObjectManager on {poolManagerGO.name} - please assign manually to Component Pool Manager field");
            }
            
            // Select the created object
            Selection.activeGameObject = poolManagerGO;
        }
    }
}
