using UnityEngine;
using UnityEditor;
using MOBA.Networking;

namespace MOBA.Editor
{
    public static class NetworkPoolManagerDiagnostics
    {
        [MenuItem("MOBA/Network/Diagnose Pool Manager Issues")]
        public static void DiagnosePoolManagerIssues()
        {
            Debug.Log("[Pool Manager Diagnostics] === STARTING DIAGNOSTICS ===");
            
            // Find all NetworkSystemIntegration instances
            NetworkSystemIntegration[] networkSystems = Object.FindObjectsByType<NetworkSystemIntegration>(FindObjectsSortMode.None);
            Debug.Log($"[Pool Manager Diagnostics] Found {networkSystems.Length} NetworkSystemIntegration instances");
            
            foreach (var system in networkSystems)
            {
                Debug.Log($"[Pool Manager Diagnostics] NetworkSystemIntegration on: {system.gameObject.name}");
                
                // Check assigned managers using SerializedObject to access private fields
                SerializedObject serializedObject = new SerializedObject(system);
                
                var componentPoolManagerProp = serializedObject.FindProperty("componentPoolManager");
                var poolManagerProp = serializedObject.FindProperty("poolManager");
                
                Debug.Log($"[Pool Manager Diagnostics]   - componentPoolManager: {(componentPoolManagerProp?.objectReferenceValue != null ? componentPoolManagerProp.objectReferenceValue.name : "NULL")}");
                Debug.Log($"[Pool Manager Diagnostics]   - poolManager: {(poolManagerProp?.objectReferenceValue != null ? poolManagerProp.objectReferenceValue.name : "NULL")}");
            }
            
            // Find all NetworkPoolObjectManager instances
            NetworkPoolObjectManager[] componentManagers = Object.FindObjectsByType<NetworkPoolObjectManager>(FindObjectsSortMode.None);
            Debug.Log($"[Pool Manager Diagnostics] Found {componentManagers.Length} NetworkPoolObjectManager instances");
            
            foreach (var manager in componentManagers)
            {
                Debug.Log($"[Pool Manager Diagnostics] NetworkPoolObjectManager on: {manager.gameObject.name}");
            }
            
            // Find all NetworkObjectPoolManager instances (singletons)
            NetworkObjectPoolManager[] singletonManagers = Object.FindObjectsByType<NetworkObjectPoolManager>(FindObjectsSortMode.None);
            Debug.Log($"[Pool Manager Diagnostics] Found {singletonManagers.Length} NetworkObjectPoolManager instances");
            
            foreach (var manager in singletonManagers)
            {
                Debug.Log($"[Pool Manager Diagnostics] NetworkObjectPoolManager on: {manager.gameObject.name}");
            }
            
            Debug.Log("[Pool Manager Diagnostics] === DIAGNOSTICS COMPLETE ===");
        }
        
        [MenuItem("MOBA/Network/Clean Up Duplicate Pool Managers")]
        public static void CleanUpDuplicatePoolManagers()
        {
            Debug.Log("[Pool Manager Cleanup] === STARTING CLEANUP ===");
            
            // Find all NetworkPoolObjectManager instances
            NetworkPoolObjectManager[] componentManagers = Object.FindObjectsByType<NetworkPoolObjectManager>(FindObjectsSortMode.None);
            Debug.Log($"[Pool Manager Cleanup] Found {componentManagers.Length} NetworkPoolObjectManager instances");
            
            if (componentManagers.Length > 1)
            {
                // Keep the first one, destroy the rest
                for (int i = 1; i < componentManagers.Length; i++)
                {
                    Debug.Log($"[Pool Manager Cleanup] Destroying duplicate NetworkPoolObjectManager on: {componentManagers[i].gameObject.name}");
                    Object.DestroyImmediate(componentManagers[i].gameObject);
                }
            }
            
            // Check if NetworkSystemIntegration has the component assigned
            NetworkSystemIntegration networkSystem = Object.FindAnyObjectByType<NetworkSystemIntegration>();
            if (networkSystem != null && componentManagers.Length > 0)
            {
                SerializedObject serializedObject = new SerializedObject(networkSystem);
                SerializedProperty componentPoolManagerProperty = serializedObject.FindProperty("componentPoolManager");
                
                if (componentPoolManagerProperty != null && componentPoolManagerProperty.objectReferenceValue == null)
                {
                    componentPoolManagerProperty.objectReferenceValue = componentManagers[0];
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log($"[Pool Manager Cleanup] Assigned {componentManagers[0].gameObject.name} to NetworkSystemIntegration");
                }
            }
            
            Debug.Log("[Pool Manager Cleanup] === CLEANUP COMPLETE ===");
        }
        
        [MenuItem("MOBA/Network/Fix Network Prefab Duplicates")]
        public static void FixNetworkPrefabDuplicates()
        {
            Debug.Log("[Network Prefab Fix] === FIXING NETWORK PREFAB DUPLICATES ===");
            
            // Find all NetworkObject components in the project
            string[] guids = AssetDatabase.FindAssets("t:GameObject");
            
            var hashCounts = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    var networkObject = prefab.GetComponent<Unity.Netcode.NetworkObject>();
                    if (networkObject != null)
                    {
                        string prefabId = prefab.name; // Use prefab name as identifier
                        if (!hashCounts.ContainsKey(prefabId))
                        {
                            hashCounts[prefabId] = new System.Collections.Generic.List<string>();
                        }
                        hashCounts[prefabId].Add(path);
                    }
                }
            }
            
            // Report duplicates
            foreach (var kvp in hashCounts)
            {
                if (kvp.Value.Count > 1)
                {
                    Debug.LogError($"[Network Prefab Fix] Duplicate NetworkObject prefab name {kvp.Key} found in:");
                    foreach (string path in kvp.Value)
                    {
                        Debug.LogError($"[Network Prefab Fix]   - {path}");
                    }
                }
            }
            
            Debug.Log("[Network Prefab Fix] === NETWORK PREFAB CHECK COMPLETE ===");
        }
    }
}
