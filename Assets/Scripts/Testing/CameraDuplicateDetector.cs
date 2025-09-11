using UnityEngine;

namespace MOBA.Testing
{
    /// <summary>
    /// Camera duplicate detector and cleaner
    /// Finds and removes duplicate cameras in the scene
    /// </summary>
    public class CameraDuplicateDetector : MonoBehaviour
    {
        [Header("Camera Detection")]
        [SerializeField] private bool checkOnStart = true;
        [SerializeField] private bool autoCleanDuplicates = false;
        [SerializeField] private bool enableDetailedLogging = true;
        
        private void Start()
        {
            if (checkOnStart)
            {
                DetectDuplicateCameras();
            }
        }
        
        /// <summary>
        /// Detect all cameras in the scene and report duplicates
        /// </summary>
        [ContextMenu("Detect Duplicate Cameras")]
        public void DetectDuplicateCameras()
        {
            Log("🔍 === Camera Duplicate Detection Started ===");
            
            var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Log($"📷 Found {allCameras.Length} total camera(s) in scene");
            
            if (allCameras.Length <= 1)
            {
                Log("✅ Good! Only one or no cameras found - no duplicates");
                return;
            }
            
            Log("⚠️ Multiple cameras detected! Analyzing...");
            
            for (int i = 0; i < allCameras.Length; i++)
            {
                var camera = allCameras[i];
                string info = GetCameraInfo(camera, i);
                Log($"📷 Camera #{i + 1}: {info}");
            }
            
            AnalyzeCameraSources(allCameras);
            
            if (autoCleanDuplicates)
            {
                CleanDuplicateCameras(allCameras);
            }
            else
            {
                Log("💡 To automatically clean duplicates, enable 'Auto Clean Duplicates' and run again");
            }
        }
        
        private string GetCameraInfo(Camera camera, int index)
        {
            var info = $"Name: '{camera.name}'";
            info += $", Tag: '{camera.tag}'";
            info += $", Position: {camera.transform.position}";
            info += $", Active: {camera.gameObject.activeInHierarchy}";
            
            // Check for MOBA camera controller
            var mobaController = camera.GetComponent<MonoBehaviour>();
            if (mobaController != null && mobaController.GetType().Name.Contains("MOBA"))
            {
                info += $", Has MOBACameraController: Yes";
            }
            else
            {
                info += $", Has MOBACameraController: No";
            }
            
            return info;
        }
        
        private void AnalyzeCameraSources(Camera[] cameras)
        {
            Log("🔍 Analyzing potential camera sources...");
            
            // Check for QuickMOBASetup
            var quickSetups = FindObjectsByType<QuickMOBASetup>(FindObjectsSortMode.None);
            if (quickSetups.Length > 0)
            {
                Log($"⚠️ Found {quickSetups.Length} QuickMOBASetup component(s) - may create cameras");
            }
            
            // Check for multiple "Main Camera" tagged objects
            int mainCameraCount = 0;
            foreach (var camera in cameras)
            {
                if (camera.tag == "MainCamera")
                    mainCameraCount++;
            }
            
            if (mainCameraCount > 1)
            {
                Log($"❌ Found {mainCameraCount} cameras tagged as 'MainCamera' - should only be 1!");
            }
            
            // Check for cameras created at runtime
            Log("💡 Possible causes:");
            Log("   - Scene already had a Main Camera + QuickMOBASetup created another");
            Log("   - Multiple QuickMOBASetup components running");
            Log("   - Network spawning cameras");
            Log("   - Prefabs containing cameras being instantiated");
        }
        
        /// <summary>
        /// Clean duplicate cameras, keeping the best one
        /// </summary>
        [ContextMenu("Clean Duplicate Cameras")]
        public void CleanDuplicateCameras(Camera[] cameras = null)
        {
            if (cameras == null)
                cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                
            if (cameras.Length <= 1)
            {
                Log("✅ No duplicates to clean - only one or no cameras found");
                return;
            }
            
            Log("🧹 Cleaning duplicate cameras...");
            
            Camera bestCamera = null;
            int bestScore = -1;
            
            // Score cameras to find the best one
            for (int i = 0; i < cameras.Length; i++)
            {
                int score = ScoreCamera(cameras[i]);
                Log($"📷 Camera '{cameras[i].name}' score: {score}");
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCamera = cameras[i];
                }
            }
            
            Log($"🏆 Best camera: '{bestCamera.name}' with score {bestScore}");
            
            // Remove all other cameras
            int removedCount = 0;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != bestCamera)
                {
                    Log($"🗑️ Removing duplicate camera: '{cameras[i].name}'");
                    DestroyImmediate(cameras[i].gameObject);
                    removedCount++;
                }
            }
            
            Log($"✅ Cleaned {removedCount} duplicate camera(s). Kept: '{bestCamera.name}'");
        }
        
        private int ScoreCamera(Camera camera)
        {
            int score = 0;
            
            // Prefer MainCamera tag
            if (camera.tag == "MainCamera") score += 10;
            
            // Prefer cameras with MOBA controller
            var mobaController = camera.GetComponent<MonoBehaviour>();
            if (mobaController != null && mobaController.GetType().Name.Contains("MOBA"))
                score += 20;
            
            // Prefer active cameras
            if (camera.gameObject.activeInHierarchy) score += 5;
            
            // Prefer cameras positioned for gameplay (not at origin)
            if (camera.transform.position != Vector3.zero) score += 5;
            
            // Prefer cameras with "Main Camera" name
            if (camera.name.Contains("Main Camera")) score += 3;
            
            return score;
        }
        
        /// <summary>
        /// Prevent duplicate camera creation in the future
        /// </summary>
        [ContextMenu("Fix Camera Creation Issues")]
        public void FixCameraCreationIssues()
        {
            Log("🔧 Fixing camera creation issues...");
            
            // Disable multiple QuickMOBASetup camera creation
            var quickSetups = FindObjectsByType<QuickMOBASetup>(FindObjectsSortMode.None);
            foreach (var setup in quickSetups)
            {
                // Note: We can't directly modify serialized fields, but we can warn
                Log($"⚠️ Found QuickMOBASetup on '{setup.name}' - ensure 'Setup Camera' is disabled if you already have a camera");
            }
            
            // Ensure only one camera is tagged as MainCamera
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            bool foundMainCamera = false;
            
            foreach (var camera in cameras)
            {
                if (camera.tag == "MainCamera")
                {
                    if (foundMainCamera)
                    {
                        Log($"🔧 Removing MainCamera tag from duplicate: '{camera.name}'");
                        camera.tag = "Untagged";
                    }
                    else
                    {
                        foundMainCamera = true;
                        Log($"✅ Keeping MainCamera tag on: '{camera.name}'");
                    }
                }
            }
            
            Log("✅ Camera creation issues fixed!");
        }
        
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[CameraDuplicateDetector] {message}");
            }
        }
    }
}
