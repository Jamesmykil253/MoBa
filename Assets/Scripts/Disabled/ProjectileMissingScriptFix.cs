using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MOBA
{
    /// <summary>
    /// Immediate projectile prefab fixer to resolve the missing script spam
    /// This script should be run once to fix all projectile prefabs in the project
    /// </summary>
    public static class ProjectileMissingScriptFix
    {
        #if UNITY_EDITOR
        [MenuItem("MOBA/Fix/🚨 EMERGENCY: Fix Projectile Missing Scripts")]
        public static void EmergencyFixProjectilePrefabs()
        {
            Debug.Log("==========================================");
            Debug.Log("🚨 EMERGENCY PROJECTILE PREFAB FIX STARTING");
            Debug.Log("==========================================");
            
            int totalFixed = 0;
            
            // Fix all projectile prefabs in the project
            string[] prefabPaths = {
                "Assets/Prefabs/Network/Projectiles/ProjectilePrefab.prefab",
                "Assets/Prefabs/Network/NetworkProjectile.prefab",
                "Assets/Scenes/Test/ProjectilePool.prefab"
            };
            
            foreach (string path in prefabPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    bool wasFixed = FixPrefabAtPath(path);
                    if (wasFixed) totalFixed++;
                }
            }
            
            // Also scan for any other projectile prefabs
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in allPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                
                if (fileName.Contains("projectile") || path.ToLower().Contains("projectile"))
                {
                    bool alreadyProcessed = false;
                    foreach (string processedPath in prefabPaths)
                    {
                        if (path == processedPath)
                        {
                            alreadyProcessed = true;
                            break;
                        }
                    }
                    
                    if (!alreadyProcessed)
                    {
                        bool wasFixed = FixPrefabAtPath(path);
                        if (wasFixed) totalFixed++;
                    }
                }
            }
            
            // Save all assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("==========================================");
            Debug.Log($"✅ EMERGENCY FIX COMPLETE: Fixed {totalFixed} prefabs");
            Debug.Log("The missing script spam should now be resolved!");
            Debug.Log("==========================================");
            
            EditorUtility.DisplayDialog("Emergency Fix Complete", 
                $"Successfully fixed {totalFixed} projectile prefabs!\n\n" +
                "The missing script errors should now be resolved. " +
                "Please test the game to confirm the fix worked.", 
                "OK");
        }
        
        private static bool FixPrefabAtPath(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[ProjectileFix] Could not load prefab at {path}");
                return false;
            }
            
            bool wasFixed = false;
            
            Debug.Log($"[ProjectileFix] Processing: {path}");
            
            // Remove missing scripts
            int removedScripts = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
            if (removedScripts > 0)
            {
                Debug.Log($"[ProjectileFix] ✅ Removed {removedScripts} missing scripts from {prefab.name}");
                wasFixed = true;
            }
            
            // Ensure required components
            bool addedComponents = EnsureRequiredComponents(prefab);
            if (addedComponents)
            {
                wasFixed = true;
            }
            
            if (wasFixed)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[ProjectileFix] ✅ Successfully fixed: {path}");
            }
            else
            {
                Debug.Log($"[ProjectileFix] ℹ️ No fixes needed for: {path}");
            }
            
            return wasFixed;
        }
        
        private static bool EnsureRequiredComponents(GameObject prefab)
        {
            bool addedComponents = false;
            
            // Ensure Projectile component
            if (prefab.GetComponent<Projectile>() == null)
            {
                prefab.AddComponent<Projectile>();
                Debug.Log($"[ProjectileFix] + Added Projectile component to {prefab.name}");
                addedComponents = true;
            }
            
            // Ensure Rigidbody component
            Rigidbody rb = prefab.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = prefab.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log($"[ProjectileFix] + Added Rigidbody to {prefab.name}");
                addedComponents = true;
            }
            
            // Ensure Collider component (trigger)
            Collider collider = prefab.GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider sphereCollider = prefab.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.1f;
                Debug.Log($"[ProjectileFix] + Added SphereCollider to {prefab.name}");
                addedComponents = true;
            }
            else
            {
                // Ensure existing collider is a trigger
                if (!collider.isTrigger)
                {
                    collider.isTrigger = true;
                    Debug.Log($"[ProjectileFix] ✓ Set collider as trigger on {prefab.name}");
                    addedComponents = true;
                }
            }
            
            // Add RuntimeProjectileFixer component for future protection
            if (prefab.GetComponent<RuntimeProjectileFixer>() == null)
            {
                prefab.AddComponent<RuntimeProjectileFixer>();
                Debug.Log($"[ProjectileFix] + Added RuntimeProjectileFixer to {prefab.name}");
                addedComponents = true;
            }
            
            return addedComponents;
        }
        
        [MenuItem("MOBA/Fix/📊 Scan Projectile Prefabs for Issues")]
        public static void ScanProjectilePrefabs()
        {
            Debug.Log("==========================================");
            Debug.Log("📊 SCANNING PROJECTILE PREFABS FOR ISSUES");
            Debug.Log("==========================================");
            
            int totalPrefabs = 0;
            int problemPrefabs = 0;
            
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in allPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                
                if (fileName.Contains("projectile") || path.ToLower().Contains("projectile"))
                {
                    totalPrefabs++;
                    
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        bool hasProblems = ScanPrefabForIssues(prefab, path);
                        if (hasProblems)
                        {
                            problemPrefabs++;
                        }
                    }
                }
            }
            
            Debug.Log("==========================================");
            if (problemPrefabs > 0)
            {
                Debug.LogWarning($"⚠️ SCAN COMPLETE: Found issues in {problemPrefabs} out of {totalPrefabs} projectile prefabs");
                Debug.LogWarning("Use 'MOBA > Fix > 🚨 EMERGENCY: Fix Projectile Missing Scripts' to fix them");
            }
            else
            {
                Debug.Log($"✅ SCAN COMPLETE: All {totalPrefabs} projectile prefabs are OK!");
            }
            Debug.Log("==========================================");
            
            string message = problemPrefabs > 0 
                ? $"Found issues in {problemPrefabs} out of {totalPrefabs} projectile prefabs.\n\nUse the Emergency Fix option to resolve them."
                : $"All {totalPrefabs} projectile prefabs are working correctly!";
                
            EditorUtility.DisplayDialog("Projectile Prefab Scan Results", message, "OK");
        }
        
        private static bool ScanPrefabForIssues(GameObject prefab, string path)
        {
            bool hasIssues = false;
            
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
                Debug.LogWarning($"❌ {path} has {missingScripts} missing script(s)", prefab);
                hasIssues = true;
            }
            
            // Check for missing required components
            if (prefab.GetComponent<Projectile>() == null)
            {
                Debug.LogWarning($"❌ {path} missing Projectile component", prefab);
                hasIssues = true;
            }
            
            if (prefab.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning($"❌ {path} missing Rigidbody component", prefab);
                hasIssues = true;
            }
            
            if (prefab.GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"❌ {path} missing Collider component", prefab);
                hasIssues = true;
            }
            
            if (!hasIssues)
            {
                Debug.Log($"✅ {path} is OK", prefab);
            }
            
            return hasIssues;
        }
        #endif
    }
}
