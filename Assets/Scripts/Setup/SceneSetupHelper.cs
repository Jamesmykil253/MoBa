using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MOBA.Setup
{
    /// <summary>
    /// Utility script for scene maintenance and cleanup tasks
    /// </summary>
    public class SceneSetupHelper : MonoBehaviour
    {
        [Header("Debug Tools")]
        [SerializeField] private bool verboseLogging = false;
        
        [ContextMenu("Find All Missing Script References")]
        public void FindAllMissingScriptReferences()
        {
#if UNITY_EDITOR
            int foundObjects = 0;
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            Debug.Log($"[SceneSetupHelper] Scanning {allObjects.Length} GameObjects for missing script references...");

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
                    foundObjects++;
                    Debug.LogWarning($"[SceneSetupHelper] Found {nullCount} missing script(s) on GameObject: {GetFullPath(obj)}", obj);
                    
                    if (verboseLogging)
                    {
                        // Show which component slots are null
                        for (int i = 0; i < components.Length; i++)
                        {
                            if (components[i] == null)
                            {
                                Debug.LogWarning($"  - Component slot {i} is missing", obj);
                            }
                        }
                    }
                }
            }

            Debug.Log($"[SceneSetupHelper] ✅ Scan complete. Found {foundObjects} objects with missing scripts out of {allObjects.Length} total objects.");
            
            if (foundObjects > 0)
            {
                Debug.LogWarning($"[SceneSetupHelper] Use 'Clean Missing Script References' to remove them.");
            }
#else
            Debug.LogWarning("[SceneSetupHelper] Missing script scanning is only available in the Unity Editor");
#endif
        }

        [ContextMenu("Clean Missing Script References")]
        public void CleanMissingScriptReferences()
        {
#if UNITY_EDITOR
            int totalCleaned = 0;
            int objectsCleaned = 0;
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            Debug.Log($"[SceneSetupHelper] Cleaning missing script references from {allObjects.Length} GameObjects...");

            foreach (GameObject obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                bool hasNull = false;

                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        hasNull = true;
                        break;
                    }
                }

                if (hasNull)
                {
                    // Use GameObjectUtility to remove missing scripts
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    if (removed > 0)
                    {
                        totalCleaned += removed;
                        objectsCleaned++;
                        Debug.Log($"[SceneSetupHelper] Removed {removed} missing scripts from {GetFullPath(obj)}", obj);
                        
                        // Mark the object as dirty to ensure changes are saved
                        EditorUtility.SetDirty(obj);
                    }
                }
            }

            Debug.Log($"[SceneSetupHelper] ✅ Cleaned {totalCleaned} missing script references from {objectsCleaned} objects");
            
            if (totalCleaned > 0)
            {
                // Force a scene save to persist the changes
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[SceneSetupHelper] Scene marked as dirty - please save the scene to persist changes");
            }
#else
            Debug.LogWarning("[SceneSetupHelper] Missing script cleaning is only available in the Unity Editor");
#endif
        }
        
        private string GetFullPath(GameObject obj)
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
        
        [ContextMenu("Force Memory Cleanup")]
        public void ForceMemoryCleanup()
        {
            // REMOVED: MemoryManager was removed during cleanup
            // Memory management is now handled by Unity's garbage collector
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
            Debug.Log("[SceneSetupHelper] Performed manual memory cleanup");
        }
    }
}
