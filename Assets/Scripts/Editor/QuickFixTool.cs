using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MOBA.Editor
{
    /// <summary>
    /// Quick fix tool for common issues identified in the log audit
    /// </summary>
    public class QuickFixTool : EditorWindow
    {
        [MenuItem("MOBA/Tools/Quick Fix Tool")]
        public static void ShowWindow()
        {
            GetWindow<QuickFixTool>("Quick Fix Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("Quick Fix Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Fix common issues identified in log audit:", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. Fix TestTarget Missing Script"))
            {
                FixTestTargetMissingScript();
            }

            if (GUILayout.Button("2. Clean All Missing Scripts in Scene"))
            {
                CleanAllMissingScriptsInScene();
            }

            if (GUILayout.Button("3. Fix NetworkObjectPoolManager Assignment"))
            {
                FixNetworkObjectPoolManagerAssignment();
            }

            if (GUILayout.Button("4. Validate Network Prefab References"))
            {
                ValidateNetworkPrefabReferences();
            }

            if (GUILayout.Button("5. Fix All Issues"))
            {
                FixAllIssues();
            }
        }

        private void FixTestTargetMissingScript()
        {
            Debug.Log("[QuickFixTool] Fixing TestTarget missing script issue...");
            
            // Find TestTarget GameObject in scene
            GameObject testTarget = GameObject.Find("TestTarget");
            if (testTarget != null)
            {
                // Check for missing components
                Component[] components = testTarget.GetComponents<Component>();
                bool foundMissingScript = false;

                for (int i = components.Length - 1; i >= 0; i--)
                {
                    if (components[i] == null)
                    {
                        Debug.Log("[QuickFixTool] Found missing script component on TestTarget");
                        foundMissingScript = true;
                        
                        // Remove missing component using SerializedObject
                        SerializedObject serializedObject = new SerializedObject(testTarget);
                        SerializedProperty componentsProperty = serializedObject.FindProperty("m_Component");

                        for (int j = componentsProperty.arraySize - 1; j >= 0; j--)
                        {
                            SerializedProperty componentProperty = componentsProperty.GetArrayElementAtIndex(j);
                            if (componentProperty.FindPropertyRelative("component").objectReferenceValue == null)
                            {
                                componentsProperty.DeleteArrayElementAtIndex(j);
                                Debug.Log("[QuickFixTool] Removed missing script component");
                            }
                        }

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(testTarget);
                    }
                }

                if (!foundMissingScript)
                {
                    Debug.Log("[QuickFixTool] No missing scripts found on TestTarget");
                }
                else
                {
                    Debug.Log("[QuickFixTool] ✅ Fixed TestTarget missing script issue");
                }
            }
            else
            {
                Debug.LogWarning("[QuickFixTool] TestTarget GameObject not found in scene");
            }
        }

        private void CleanAllMissingScriptsInScene()
        {
            Debug.Log("[QuickFixTool] Cleaning all missing scripts in scene...");
            
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int totalFixed = 0;

            foreach (GameObject obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                bool hasMissingScript = false;

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        hasMissingScript = true;
                        break;
                    }
                }

                if (hasMissingScript)
                {
                    SerializedObject serializedObject = new SerializedObject(obj);
                    SerializedProperty componentsProperty = serializedObject.FindProperty("m_Component");

                    for (int i = componentsProperty.arraySize - 1; i >= 0; i--)
                    {
                        SerializedProperty componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                        if (componentProperty.FindPropertyRelative("component").objectReferenceValue == null)
                        {
                            componentsProperty.DeleteArrayElementAtIndex(i);
                            totalFixed++;
                        }
                    }

                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(obj);
                    Debug.Log($"[QuickFixTool] Fixed missing scripts on {obj.name}");
                }
            }

            Debug.Log($"[QuickFixTool] ✅ Cleaned {totalFixed} missing script references from scene");
        }

        private void ValidateNetworkPrefabReferences()
        {
            Debug.Log("[QuickFixTool] Validating network prefab references...");
            
            // Find NetworkSystemIntegration
            var networkIntegration = FindFirstObjectByType<MOBA.Networking.NetworkSystemIntegration>();
            if (networkIntegration != null)
            {
                // Use reflection to check prefab fields
                var type = networkIntegration.GetType();
                var playerPrefabField = type.GetField("playerPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var projectilePrefabField = type.GetField("projectilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var playerPrefab = playerPrefabField?.GetValue(networkIntegration) as GameObject;
                var projectilePrefab = projectilePrefabField?.GetValue(networkIntegration) as GameObject;

                Debug.Log($"[QuickFixTool] Player Prefab: {(playerPrefab != null ? playerPrefab.name : "NULL")}");
                Debug.Log($"[QuickFixTool] Projectile Prefab: {(projectilePrefab != null ? projectilePrefab.name : "NULL")}");

                if (playerPrefab == null)
                {
                    Debug.LogWarning("[QuickFixTool] ⚠️ Player Prefab is not assigned in NetworkSystemIntegration");
                }

                if (projectilePrefab == null)
                {
                    Debug.LogWarning("[QuickFixTool] ⚠️ Projectile Prefab is not assigned in NetworkSystemIntegration");
                }

                if (playerPrefab != null && projectilePrefab != null)
                {
                    Debug.Log("[QuickFixTool] ✅ All network prefabs are properly assigned");
                }
            }
            else
            {
                Debug.LogError("[QuickFixTool] NetworkSystemIntegration not found in scene");
            }
        }

        private void FixAllIssues()
        {
            Debug.Log("[QuickFixTool] Running all fixes...");
            FixTestTargetMissingScript();
            CleanAllMissingScriptsInScene();
            FixNetworkObjectPoolManagerAssignment();
            ValidateNetworkPrefabReferences();
            Debug.Log("[QuickFixTool] ✅ All fixes completed");
        }

        private void FixNetworkObjectPoolManagerAssignment()
        {
            Debug.Log("[QuickFixTool] Fixing NetworkObjectPoolManager assignment...");
            
            // Find NetworkSystemIntegration
            var networkIntegration = FindFirstObjectByType<MOBA.Networking.NetworkSystemIntegration>();
            if (networkIntegration != null)
            {
                // Force create the NetworkObjectPoolManager singleton
                var poolManager = MOBA.Networking.NetworkObjectPoolManager.Instance;
                
                if (poolManager != null)
                {
                    Debug.Log($"[QuickFixTool] ✅ NetworkObjectPoolManager singleton created: {poolManager.gameObject.name}");
                    
                    // Try to assign it to NetworkSystemIntegration using reflection
                    var type = networkIntegration.GetType();
                    var poolManagerField = type.GetField("poolManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (poolManagerField != null)
                    {
                        poolManagerField.SetValue(networkIntegration, poolManager);
                        Debug.Log("[QuickFixTool] ✅ NetworkObjectPoolManager assigned to NetworkSystemIntegration");
                        EditorUtility.SetDirty(networkIntegration);
                    }
                    else
                    {
                        Debug.LogWarning("[QuickFixTool] Could not find poolManager field in NetworkSystemIntegration");
                    }
                }
                else
                {
                    Debug.LogError("[QuickFixTool] ❌ Failed to create NetworkObjectPoolManager singleton");
                }
            }
            else
            {
                Debug.LogError("[QuickFixTool] NetworkSystemIntegration not found in scene");
            }
        }
    }
}
