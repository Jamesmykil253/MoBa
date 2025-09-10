using UnityEngine;
using UnityEditor;
using MOBA.Networking;
using Unity.Netcode;

namespace MOBA.Editor
{
    /// <summary>
    /// Editor utilities for setting up NetworkSystemIntegration with proper pool managers
    /// </summary>
    public static class NetworkSystemIntegrationSetup
    {
        [MenuItem("MOBA/Network/Setup Network System Integration")]
        public static void SetupNetworkSystemIntegration()
        {
            // Check if NetworkSystemIntegration already exists
            NetworkSystemIntegration existing = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log($"[NetworkSystemIntegrationSetup] Found existing NetworkSystemIntegration on '{existing.gameObject.name}'. Selected it for you.");
                return;
            }
            
            // Create new GameObject with NetworkSystemIntegration
            GameObject integrationGO = new GameObject("NetworkSystemIntegration");
            NetworkSystemIntegration integration = integrationGO.AddComponent<NetworkSystemIntegration>();
            
            // Try to auto-assign NetworkManager
            NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                Debug.Log($"[NetworkSystemIntegrationSetup] Found NetworkManager on '{networkManager.gameObject.name}', will be auto-assigned.");
            }
            else
            {
                Debug.LogWarning("[NetworkSystemIntegrationSetup] No NetworkManager found in scene. You'll need to assign it manually or add a NetworkManager to the scene.");
            }
            
            // Create and assign the component-based pool manager
            GameObject poolManagerGO = new GameObject("NetworkPoolObjectManager");
            poolManagerGO.transform.SetParent(integrationGO.transform);
            NetworkPoolObjectManager poolManager = poolManagerGO.AddComponent<NetworkPoolObjectManager>();
            
            // Use reflection to assign the componentPoolManager field
            var field = typeof(NetworkSystemIntegration).GetField("componentPoolManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(integration, poolManager);
                Debug.Log("[NetworkSystemIntegrationSetup] ✅ Component-based pool manager assigned successfully!");
            }
            else
            {
                Debug.LogError("[NetworkSystemIntegrationSetup] ❌ Could not find componentPoolManager field. You'll need to assign it manually in the inspector.");
            }
            
            // Position appropriately
            if (Selection.activeTransform != null)
            {
                integrationGO.transform.SetParent(Selection.activeTransform);
            }
            
            // Select the new integration object
            Selection.activeGameObject = integrationGO;
            
            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            
            Debug.Log("[NetworkSystemIntegrationSetup] ✅ NetworkSystemIntegration setup complete! Configure prefabs and settings in the inspector.");
        }
        
        [MenuItem("MOBA/Network/Find Network System Integration")]
        public static void FindNetworkSystemIntegration()
        {
            NetworkSystemIntegration integration = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (integration != null)
            {
                Selection.activeGameObject = integration.gameObject;
                EditorGUIUtility.PingObject(integration.gameObject);
                Debug.Log($"[NetworkSystemIntegrationSetup] Found NetworkSystemIntegration on '{integration.gameObject.name}'");
            }
            else
            {
                Debug.LogWarning("[NetworkSystemIntegrationSetup] No NetworkSystemIntegration found in the current scene.");
            }
        }
        
        [MenuItem("MOBA/Network/Validate Network System Setup")]
        public static void ValidateNetworkSystemSetup()
        {
            NetworkSystemIntegration integration = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            
            if (integration == null)
            {
                Debug.LogError("[Validation] ❌ No NetworkSystemIntegration found in scene!");
                return;
            }
            
            Debug.Log("[Validation] === NETWORK SYSTEM VALIDATION ===");
            
            // Check NetworkManager
            NetworkManager networkManager = Object.FindAnyObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                Debug.Log("[Validation] ✅ NetworkManager found in scene");
            }
            else
            {
                Debug.LogWarning("[Validation] ⚠️ No NetworkManager found in scene");
            }
            
            // Check pool managers
            NetworkObjectPoolManager singletonPoolManager = Object.FindAnyObjectByType<NetworkObjectPoolManager>();
            NetworkPoolObjectManager componentPoolManager = Object.FindAnyObjectByType<NetworkPoolObjectManager>();
            
            if (componentPoolManager != null)
            {
                Debug.Log("[Validation] ✅ Component-based NetworkPoolObjectManager found");
            }
            else if (singletonPoolManager != null)
            {
                Debug.Log("[Validation] ✅ Singleton NetworkObjectPoolManager found");
            }
            else
            {
                Debug.LogWarning("[Validation] ⚠️ No pool manager found. Create one using the setup tools.");
            }
            
            // Check event bus
            NetworkEventBus eventBus = Object.FindAnyObjectByType<NetworkEventBus>();
            if (eventBus != null)
            {
                Debug.Log("[Validation] ✅ NetworkEventBus found");
            }
            else
            {
                Debug.LogWarning("[Validation] ⚠️ No NetworkEventBus found (will be auto-created)");
            }
            
            Debug.Log("[Validation] === VALIDATION COMPLETE ===");
            
            // Select the integration for easy access
            Selection.activeGameObject = integration.gameObject;
            EditorGUIUtility.PingObject(integration.gameObject);
        }
    }
}
