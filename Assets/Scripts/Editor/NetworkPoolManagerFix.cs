using UnityEngine;
using UnityEditor;
using MOBA.Networking;
using Unity.Netcode;

namespace MOBA.Editor
{
    /// <summary>
    /// Comprehensive solution for network pool manager setup and issue resolution
    /// </summary>
    public static class NetworkPoolManagerFix
    {
        [MenuItem("MOBA/Network/Fix All Pool Manager Issues")]
        public static void FixAllPoolManagerIssues()
        {
            Debug.Log("[Pool Manager Fix] === STARTING COMPREHENSIVE FIX ===");
            
            // Step 1: Clean up duplicate pool managers
            CleanUpDuplicatePoolManagers();
            
            // Step 2: Ensure proper pool manager assignment
            EnsureProperPoolManagerAssignment();
            
            // Step 3: Validate and fix network prefabs
            ValidateNetworkPrefabs();
            
            Debug.Log("[Pool Manager Fix] === COMPREHENSIVE FIX COMPLETE ===");
        }
        
        private static void CleanUpDuplicatePoolManagers()
        {
            Debug.Log("[Pool Manager Fix] Cleaning up duplicate pool managers...");
            
            // Find all NetworkPoolObjectManager instances
            NetworkPoolObjectManager[] componentManagers = Object.FindObjectsByType<NetworkPoolObjectManager>(FindObjectsSortMode.None);
            
            if (componentManagers.Length > 1)
            {
                Debug.LogWarning($"[Pool Manager Fix] Found {componentManagers.Length} NetworkPoolObjectManager instances, keeping only one");
                
                // Keep the first one, destroy the rest
                for (int i = 1; i < componentManagers.Length; i++)
                {
                    Debug.Log($"[Pool Manager Fix] Destroying duplicate NetworkPoolObjectManager on: {componentManagers[i].gameObject.name}");
                    Object.DestroyImmediate(componentManagers[i].gameObject);
                }
            }
            
            // Find all NetworkObjectPoolManager instances (singletons)
            NetworkObjectPoolManager[] singletonManagers = Object.FindObjectsByType<NetworkObjectPoolManager>(FindObjectsSortMode.None);
            
            if (singletonManagers.Length > 1)
            {
                Debug.LogWarning($"[Pool Manager Fix] Found {singletonManagers.Length} NetworkObjectPoolManager singletons, keeping only one");
                
                // Keep the first one, destroy the rest
                for (int i = 1; i < singletonManagers.Length; i++)
                {
                    Debug.Log($"[Pool Manager Fix] Destroying duplicate NetworkObjectPoolManager on: {singletonManagers[i].gameObject.name}");
                    Object.DestroyImmediate(singletonManagers[i].gameObject);
                }
            }
        }
        
        private static void EnsureProperPoolManagerAssignment()
        {
            Debug.Log("[Pool Manager Fix] Ensuring proper pool manager assignment...");
            
            NetworkSystemIntegration networkSystem = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (networkSystem == null)
            {
                Debug.LogError("[Pool Manager Fix] No NetworkSystemIntegration found in scene!");
                return;
            }
            
            SerializedObject serializedObject = new SerializedObject(networkSystem);
            SerializedProperty componentPoolManagerProperty = serializedObject.FindProperty("componentPoolManager");
            SerializedProperty poolManagerProperty = serializedObject.FindProperty("poolManager");
            
            // Check if component pool manager is assigned
            if (componentPoolManagerProperty != null && componentPoolManagerProperty.objectReferenceValue == null)
            {
                // Try to find an existing NetworkPoolObjectManager
                NetworkPoolObjectManager existingManager = Object.FindAnyObjectByType<NetworkPoolObjectManager>();
                
                if (existingManager == null)
                {
                    // Create a new one
                    GameObject poolManagerGO = new GameObject("NetworkPoolObjectManager");
                    existingManager = poolManagerGO.AddComponent<NetworkPoolObjectManager>();
                    Debug.Log("[Pool Manager Fix] Created new NetworkPoolObjectManager");
                }
                
                componentPoolManagerProperty.objectReferenceValue = existingManager;
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"[Pool Manager Fix] Assigned {existingManager.gameObject.name} to componentPoolManager field");
            }
            
            // Clear the singleton pool manager reference to prefer component-based approach
            if (poolManagerProperty != null && poolManagerProperty.objectReferenceValue != null)
            {
                poolManagerProperty.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[Pool Manager Fix] Cleared singleton poolManager reference to use component-based approach");
            }
        }
        
        private static void ValidateNetworkPrefabs()
        {
            Debug.Log("[Pool Manager Fix] Validating network prefabs...");
            
            // Find player and projectile prefabs referenced by NetworkSystemIntegration
            NetworkSystemIntegration networkSystem = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (networkSystem == null) return;
            
            SerializedObject serializedObject = new SerializedObject(networkSystem);
            SerializedProperty playerPrefabProperty = serializedObject.FindProperty("playerPrefab");
            SerializedProperty projectilePrefabProperty = serializedObject.FindProperty("projectilePrefab");
            
            // Validate player prefab
            if (playerPrefabProperty?.objectReferenceValue != null)
            {
                GameObject playerPrefab = playerPrefabProperty.objectReferenceValue as GameObject;
                ValidatePrefabNetworkObject(playerPrefab, "Player");
            }
            
            // Validate projectile prefab
            if (projectilePrefabProperty?.objectReferenceValue != null)
            {
                GameObject projectilePrefab = projectilePrefabProperty.objectReferenceValue as GameObject;
                ValidatePrefabNetworkObject(projectilePrefab, "Projectile");
            }
        }
        
        private static void ValidatePrefabNetworkObject(GameObject prefab, string prefabType)
        {
            if (prefab == null) return;
            
            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogWarning($"[Pool Manager Fix] {prefabType} prefab '{prefab.name}' is missing NetworkObject component");
                
                // Add NetworkObject component to the prefab
                networkObject = prefab.AddComponent<NetworkObject>();
                EditorUtility.SetDirty(prefab);
                Debug.Log($"[Pool Manager Fix] Added NetworkObject component to {prefab.name}");
            }
            else
            {
                Debug.Log($"[Pool Manager Fix] {prefabType} prefab '{prefab.name}' has NetworkObject component (ID: {networkObject.NetworkObjectId})");
            }
        }
        
        [MenuItem("MOBA/Network/Reset Pool Manager Setup")]
        public static void ResetPoolManagerSetup()
        {
            Debug.Log("[Pool Manager Reset] === RESETTING POOL MANAGER SETUP ===");
            
            // Remove all existing pool managers
            NetworkPoolObjectManager[] componentManagers = Object.FindObjectsByType<NetworkPoolObjectManager>(FindObjectsSortMode.None);
            foreach (var manager in componentManagers)
            {
                Debug.Log($"[Pool Manager Reset] Destroying NetworkPoolObjectManager on: {manager.gameObject.name}");
                Object.DestroyImmediate(manager.gameObject);
            }
            
            NetworkObjectPoolManager[] singletonManagers = Object.FindObjectsByType<NetworkObjectPoolManager>(FindObjectsSortMode.None);
            foreach (var manager in singletonManagers)
            {
                Debug.Log($"[Pool Manager Reset] Destroying NetworkObjectPoolManager on: {manager.gameObject.name}");
                Object.DestroyImmediate(manager.gameObject);
            }
            
            // Clear references in NetworkSystemIntegration
            NetworkSystemIntegration networkSystem = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (networkSystem != null)
            {
                SerializedObject serializedObject = new SerializedObject(networkSystem);
                
                var componentPoolManagerProperty = serializedObject.FindProperty("componentPoolManager");
                var poolManagerProperty = serializedObject.FindProperty("poolManager");
                
                if (componentPoolManagerProperty != null)
                {
                    componentPoolManagerProperty.objectReferenceValue = null;
                }
                
                if (poolManagerProperty != null)
                {
                    poolManagerProperty.objectReferenceValue = null;
                }
                
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[Pool Manager Reset] Cleared all pool manager references from NetworkSystemIntegration");
            }
            
            Debug.Log("[Pool Manager Reset] === RESET COMPLETE - Use 'Create Pool Object Manager Component' to set up fresh ===");
        }
    }
}
