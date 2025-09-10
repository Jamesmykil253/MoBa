using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MOBA.Editor
{
    /// <summary>
    /// Immediate cleanup script for missing script references
    /// </summary>
    public static class MissingScriptFixer
    {
#if UNITY_EDITOR
        [MenuItem("MOBA/Fix/Remove All Missing Script References")]
        public static void RemoveAllMissingScriptReferences()
        {
            int totalCleaned = 0;
            int objectsCleaned = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            Debug.Log($"[MissingScriptFixer] Scanning {allObjects.Length} GameObjects for missing script references...");

            foreach (GameObject obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                bool hasNull = false;
                int nullCount = 0;

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        hasNull = true;
                        nullCount++;
                    }
                }

                if (hasNull)
                {
                    Debug.LogWarning($"[MissingScriptFixer] Found {nullCount} missing script(s) on: {GetFullPath(obj)}", obj);
                    
                    // Use GameObjectUtility to remove missing scripts
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    if (removed > 0)
                    {
                        totalCleaned += removed;
                        objectsCleaned++;
                        Debug.Log($"[MissingScriptFixer] ✅ Removed {removed} missing scripts from: {GetFullPath(obj)}", obj);
                        
                        // Mark the object as dirty to ensure changes are saved
                        EditorUtility.SetDirty(obj);
                    }
                }
            }

            Debug.Log($"[MissingScriptFixer] ✅ CLEANUP COMPLETE: Removed {totalCleaned} missing script references from {objectsCleaned} objects");
            
            if (totalCleaned > 0)
            {
                // Force a scene save to persist the changes
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[MissingScriptFixer] Scene marked as dirty - please save the scene to persist changes");
                
                // Show dialog to user
                EditorUtility.DisplayDialog("Missing Scripts Cleaned", 
                    $"Successfully removed {totalCleaned} missing script references from {objectsCleaned} GameObjects.\n\nPlease save the scene to persist these changes.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Missing Scripts", 
                    "No missing script references were found in the current scene.", 
                    "OK");
            }
        }

        [MenuItem("MOBA/Fix/Scan for Missing Script References")]
        public static void ScanForMissingScriptReferences()
        {
            int foundObjects = 0;
            int totalMissingScripts = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            Debug.Log($"[MissingScriptFixer] Scanning {allObjects.Length} GameObjects for missing script references...");

            foreach (GameObject obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                int nullCount = 0;

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        nullCount++;
                    }
                }

                if (nullCount > 0)
                {
                    foundObjects++;
                    totalMissingScripts += nullCount;
                    Debug.LogWarning($"[MissingScriptFixer] Found {nullCount} missing script(s) on: {GetFullPath(obj)}", obj);
                }
            }

            string message;
            if (foundObjects > 0)
            {
                message = $"Found {totalMissingScripts} missing script references on {foundObjects} GameObjects.\n\nUse 'MOBA > Fix > Remove All Missing Script References' to clean them up.";
                Debug.LogWarning($"[MissingScriptFixer] ⚠️ SCAN COMPLETE: {message}");
            }
            else
            {
                message = "No missing script references found in the current scene.";
                Debug.Log($"[MissingScriptFixer] ✅ SCAN COMPLETE: {message}");
            }
            
            EditorUtility.DisplayDialog("Missing Script Scan Results", message, "OK");
        }

        private static string GetFullPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
#endif
    }
}
