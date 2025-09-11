using UnityEngine;
using UnityEditor;
using System.IO;

namespace MOBA.Editor
{
    /// <summary>
    /// Editor utility to automatically fix projectile prefabs with missing scripts
    /// </summary>
    public static class ProjectilePrefabFixer
    {
        [MenuItem("MOBA/Fix/Fix All Projectile Prefabs")]
        public static void FixAllProjectilePrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int fixedCount = 0;
            int totalProjectilePrefabs = 0;
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Check if this is a projectile prefab
                if (IsProjectilePrefab(path))
                {
                    totalProjectilePrefabs++;
                    
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        bool wasFixed = FixProjectilePrefab(prefab, path);
                        if (wasFixed)
                        {
                            fixedCount++;
                        }
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[ProjectilePrefabFixer] ✅ Fixed {fixedCount} out of {totalProjectilePrefabs} projectile prefabs");
            
            EditorUtility.DisplayDialog("Projectile Prefab Fixer", 
                $"Fixed {fixedCount} out of {totalProjectilePrefabs} projectile prefabs.\n\nAll projectile prefabs should now work correctly.", 
                "OK");
        }
        
        [MenuItem("MOBA/Fix/Scan Projectile Prefabs")]
        public static void ScanProjectilePrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int problemCount = 0;
            int totalProjectilePrefabs = 0;
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (IsProjectilePrefab(path))
                {
                    totalProjectilePrefabs++;
                    
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        bool hasProblems = ScanPrefabForProblems(prefab, path);
                        if (hasProblems)
                        {
                            problemCount++;
                        }
                    }
                }
            }
            
            string message = problemCount > 0 
                ? $"Found problems in {problemCount} out of {totalProjectilePrefabs} projectile prefabs.\n\nUse 'MOBA > Fix > Fix All Projectile Prefabs' to fix them."
                : $"All {totalProjectilePrefabs} projectile prefabs are OK!";
                
            Debug.Log($"[ProjectilePrefabFixer] Scan complete: {message}");
            EditorUtility.DisplayDialog("Projectile Prefab Scan Results", message, "OK");
        }
        
        private static bool IsProjectilePrefab(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
            return fileName.Contains("projectile") || path.ToLower().Contains("projectile");
        }
        
        private static bool FixProjectilePrefab(GameObject prefab, string path)
        {
            bool wasFixed = false;
            
            // Check for missing scripts
            Component[] components = prefab.GetComponents<Component>();
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
                Debug.LogWarning($"[ProjectilePrefabFixer] Found missing scripts in {path}", prefab);
                
                // Clean missing scripts using GameObjectUtility
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                if (removed > 0)
                {
                    Debug.Log($"[ProjectilePrefabFixer] Removed {removed} missing scripts from {path}", prefab);
                    wasFixed = true;
                }
            }
            
            // Ensure required components exist
            bool addedComponents = EnsureRequiredComponents(prefab);
            if (addedComponents)
            {
                wasFixed = true;
            }
            
            if (wasFixed)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[ProjectilePrefabFixer] ✅ Fixed projectile prefab: {path}", prefab);
            }
            
            return wasFixed;
        }
        
        private static bool EnsureRequiredComponents(GameObject prefab)
        {
            bool addedComponents = false;
            
            // Ensure Projectile component
            if (prefab.GetComponent<MOBA.Projectile>() == null)
            {
                prefab.AddComponent<MOBA.Projectile>();
                Debug.Log($"[ProjectilePrefabFixer] Added Projectile component to {prefab.name}");
                addedComponents = true;
            }
            
            // Ensure Rigidbody component
            if (prefab.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = prefab.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log($"[ProjectilePrefabFixer] Added Rigidbody to {prefab.name}");
                addedComponents = true;
            }
            
            // Ensure Collider component
            if (prefab.GetComponent<Collider>() == null)
            {
                SphereCollider collider = prefab.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.1f;
                Debug.Log($"[ProjectilePrefabFixer] Added SphereCollider to {prefab.name}");
                addedComponents = true;
            }
            
            // Add ProjectilePrefabFixer component if it doesn't exist
            if (prefab.GetComponent<MOBA.ProjectilePrefabFixer>() == null)
            {
                prefab.AddComponent<MOBA.ProjectilePrefabFixer>();
                Debug.Log($"[ProjectilePrefabFixer] Added ProjectilePrefabFixer to {prefab.name}");
                addedComponents = true;
            }
            
            return addedComponents;
        }
        
        private static bool ScanPrefabForProblems(GameObject prefab, string path)
        {
            bool hasProblems = false;
            
            // Check for missing scripts
            Component[] components = prefab.GetComponents<Component>();
            int missingScripts = 0;
            
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingScripts++;
                }
            }
            
            if (missingScripts > 0)
            {
                Debug.LogWarning($"[ProjectilePrefabFixer] {path} has {missingScripts} missing script(s)", prefab);
                hasProblems = true;
            }
            
            // Check for missing required components
            if (prefab.GetComponent<MOBA.Projectile>() == null)
            {
                Debug.LogWarning($"[ProjectilePrefabFixer] {path} is missing Projectile component", prefab);
                hasProblems = true;
            }
            
            if (prefab.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning($"[ProjectilePrefabFixer] {path} is missing Rigidbody component", prefab);
                hasProblems = true;
            }
            
            if (prefab.GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[ProjectilePrefabFixer] {path} is missing Collider component", prefab);
                hasProblems = true;
            }
            
            return hasProblems;
        }
    }
}
