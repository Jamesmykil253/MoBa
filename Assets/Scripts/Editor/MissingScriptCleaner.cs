using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MOBA.Editor
{
    /// <summary>
    /// Editor utility to find and clean up missing script references in the scene
    /// </summary>
    public class MissingScriptCleaner : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<GameObject> objectsWithMissingScripts = new List<GameObject>();

        [MenuItem("MOBA/Tools/Missing Script Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<MissingScriptCleaner>("Missing Script Cleaner");
        }

        private void OnGUI()
        {
            GUILayout.Label("Missing Script Cleaner", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("This tool helps identify and remove missing script references from GameObjects.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Scan Scene for Missing Scripts"))
            {
                ScanForMissingScripts();
            }

            if (objectsWithMissingScripts.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Found {objectsWithMissingScripts.Count} objects with missing scripts:", EditorStyles.boldLabel);

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (var obj in objectsWithMissingScripts)
                {
                    if (obj != null)
                    {
                        GUILayout.BeginHorizontal();
                        
                        if (GUILayout.Button(obj.name, GUILayout.Width(200)))
                        {
                            Selection.activeGameObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }

                        if (GUILayout.Button("Clean", GUILayout.Width(60)))
                        {
                            CleanMissingScripts(obj);
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                
                GUILayout.EndScrollView();

                GUILayout.Space(10);
                if (GUILayout.Button("Clean All Missing Scripts"))
                {
                    CleanAllMissingScripts();
                }
            }
            else if (objectsWithMissingScripts.Count == 0 && GUI.changed == false)
            {
                GUILayout.Label("No missing scripts found. Click 'Scan Scene' to check.", EditorStyles.helpBox);
            }
        }

        private void ScanForMissingScripts()
        {
            objectsWithMissingScripts.Clear();

            // Find all GameObjects in the scene using the new non-deprecated method
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                // Get all components on this GameObject
                Component[] components = obj.GetComponents<Component>();

                for (int i = 0; i < components.Length; i++)
                {
                    // Check if component is null (missing script)
                    if (components[i] == null)
                    {
                        if (!objectsWithMissingScripts.Contains(obj))
                        {
                            objectsWithMissingScripts.Add(obj);
                        }
                        break;
                    }
                }
            }

            Debug.Log($"[MissingScriptCleaner] Scan complete. Found {objectsWithMissingScripts.Count} objects with missing scripts.");
        }

        private void CleanMissingScripts(GameObject obj)
        {
            if (obj == null) return;

            // Get all components
            Component[] components = obj.GetComponents<Component>();
            List<Component> missingComponents = new List<Component>();

            // Find missing components
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingComponents.Add(components[i]);
                }
            }

            // Remove missing components using SerializedObject
            if (missingComponents.Count > 0)
            {
                SerializedObject serializedObject = new SerializedObject(obj);
                SerializedProperty componentsProperty = serializedObject.FindProperty("m_Component");

                // Remove null components from the array
                for (int i = componentsProperty.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                    if (componentProperty.FindPropertyRelative("component").objectReferenceValue == null)
                    {
                        componentsProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);

                Debug.Log($"[MissingScriptCleaner] Cleaned {missingComponents.Count} missing scripts from {obj.name}");
            }

            // Refresh the scan
            ScanForMissingScripts();
        }

        private void CleanAllMissingScripts()
        {
            int totalCleaned = 0;

            foreach (GameObject obj in objectsWithMissingScripts)
            {
                if (obj != null)
                {
                    Component[] components = obj.GetComponents<Component>();
                    int missingCount = 0;

                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingCount++;
                        }
                    }

                    if (missingCount > 0)
                    {
                        CleanMissingScripts(obj);
                        totalCleaned += missingCount;
                    }
                }
            }

            Debug.Log($"[MissingScriptCleaner] Cleaned {totalCleaned} missing script references from {objectsWithMissingScripts.Count} objects.");
            
            // Refresh the scan after cleaning
            ScanForMissingScripts();
        }
    }
}
