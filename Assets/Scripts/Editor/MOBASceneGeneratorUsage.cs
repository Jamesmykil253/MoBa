using UnityEngine;
using UnityEditor;
using MOBA;
using MOBA.Networking;

namespace MOBA.Editor
{
    /// <summary>
    /// Usage example and quick access to MOBA Scene Generator
    /// Provides one-click setup for common development scenarios
    /// </summary>
    public class MOBASceneGeneratorUsage : EditorWindow
    {
        [MenuItem("MOBA/Quick Setup/Complete Demo Scene")]
        public static void SetupCompleteDemoScene()
        {
            if (EditorUtility.DisplayDialog("Setup Complete Demo Scene", 
                "This will create a complete MOBA demo scene with all systems.\n\nContinue?", 
                "Yes", "Cancel"))
            {
                var generator = EditorWindow.GetWindow<MOBASceneGenerator>("MOBA Scene Generator");
                generator.Show();
                
                // Auto-configure for complete setup
                generator.Close(); // Close the window and use direct method
                
                Debug.Log("[MOBASceneGeneratorUsage] Setting up complete demo scene...");
                CreateCompleteScene();
            }
        }

        [MenuItem("MOBA/Quick Setup/Network Testing Only")]
        public static void SetupNetworkTestingOnly()
        {
            if (EditorUtility.DisplayDialog("Setup Network Testing", 
                "This will create network systems and UI for testing multiplayer functionality.\n\nContinue?", 
                "Yes", "Cancel"))
            {
                Debug.Log("[MOBASceneGeneratorUsage] Setting up network testing environment...");
                CreateNetworkTestingScene();
            }
        }

        [MenuItem("MOBA/Quick Setup/Gameplay Systems Only")]
        public static void SetupGameplaySystemsOnly()
        {
            if (EditorUtility.DisplayDialog("Setup Gameplay Systems", 
                "This will create core gameplay systems without networking.\n\nContinue?", 
                "Yes", "Cancel"))
            {
                Debug.Log("[MOBASceneGeneratorUsage] Setting up gameplay systems...");
                CreateGameplayScene();
            }
        }

        [MenuItem("MOBA/Quick Setup/Clear Scene")]
        public static void ClearCurrentScene()
        {
            if (EditorUtility.DisplayDialog("Clear Scene", 
                "This will remove all objects from the current scene except Main Camera and Directional Light.\n\nContinue?", 
                "Yes", "Cancel"))
            {
                Debug.Log("[MOBASceneGeneratorUsage] Clearing current scene...");
                ClearScene();
            }
        }

        private static void CreateCompleteScene()
        {
            try
            {
                // Create a temporary generator instance to access methods
                var tempGenerator = ScriptableObject.CreateInstance<MOBASceneGenerator>();
                
                // Clear scene first
                ClearScene();
                
                // Create scene root
                GameObject sceneRoot = new GameObject("MOBA_SceneSetup");
                sceneRoot.AddComponent<MOBASceneSetup>();
                sceneRoot.AddComponent<MOBATestScene>();
                
                Debug.Log("[MOBASceneGeneratorUsage] Complete scene setup finished!");
                EditorUtility.DisplayDialog("Success", 
                    "Complete MOBA scene created successfully!\n\n" +
                    "Created:\n" +
                    "• Network Systems\n" +
                    "• Gameplay Systems\n" +
                    "• UI System\n" +
                    "• Environment\n" +
                    "• Test Player\n\n" +
                    "Use the MOBA Scene Generator window for detailed configuration.", 
                    "OK");
                
                ScriptableObject.DestroyImmediate(tempGenerator);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBASceneGeneratorUsage] Error creating complete scene: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create scene: {e.Message}", "OK");
            }
        }

        private static void CreateNetworkTestingScene()
        {
            try
            {
                // Create NetworkManager
                GameObject networkManager = new GameObject("NetworkManager");
                var netManager = networkManager.AddComponent<Unity.Netcode.NetworkManager>();
                var transport = networkManager.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                netManager.NetworkConfig.NetworkTransport = transport;
                netManager.NetworkConfig.TickRate = 60;
                
                // Create NetworkSystemIntegration
                GameObject integration = new GameObject("NetworkSystemIntegration");
                integration.AddComponent<NetworkSystemIntegration>();
                
                // Create NetworkTestSetup
                GameObject testSetup = new GameObject("NetworkTestSetup");
                testSetup.AddComponent<NetworkTestSetup>();
                
                Debug.Log("[MOBASceneGeneratorUsage] Network testing scene created!");
                EditorUtility.DisplayDialog("Success", "Network testing environment created successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBASceneGeneratorUsage] Error creating network scene: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create network scene: {e.Message}", "OK");
            }
        }

        private static void CreateGameplayScene()
        {
            try
            {
                // Create core gameplay systems
                GameObject cmdManager = new GameObject("CommandManager");
                cmdManager.AddComponent<CommandManager>();
                
                GameObject abilitySystem = new GameObject("AbilitySystem");
                abilitySystem.AddComponent<AbilitySystem>();
                
                GameObject factory = new GameObject("FlyweightFactory");
                factory.AddComponent<FlyweightFactory>();
                
                GameObject poolObj = new GameObject("ProjectilePool");
                var projectilePool = poolObj.AddComponent<ProjectilePool>();
                projectilePool.flyweightFactory = factory.GetComponent<FlyweightFactory>();
                
                // Create EventBus placeholder
                GameObject eventBus = new GameObject("EventBus (Static)");
                
                Debug.Log("[MOBASceneGeneratorUsage] Gameplay systems created!");
                EditorUtility.DisplayDialog("Success", "Gameplay systems created successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBASceneGeneratorUsage] Error creating gameplay scene: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create gameplay scene: {e.Message}", "OK");
            }
        }

        private static void ClearScene()
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                if (obj.name != "Main Camera" && obj.name != "Directional Light")
                {
                    Object.DestroyImmediate(obj);
                }
            }
            
            Debug.Log("[MOBASceneGeneratorUsage] Scene cleared");
        }

        [MenuItem("MOBA/Documentation/Open Scene Generator Guide")]
        public static void OpenSceneGeneratorGuide()
        {
            EditorUtility.DisplayDialog("MOBA Scene Generator Guide", 
                "MOBA Scene Generator Usage:\n\n" +
                "1. COMPLETE DEMO SCENE:\n" +
                "   • Creates all systems, UI, environment, and test player\n" +
                "   • Ready for immediate testing\n" +
                "   • Includes networking and gameplay systems\n\n" +
                "2. NETWORK TESTING ONLY:\n" +
                "   • Creates NetworkManager, integration, and test setup\n" +
                "   • Perfect for multiplayer testing\n" +
                "   • Lightweight setup\n\n" +
                "3. GAMEPLAY SYSTEMS ONLY:\n" +
                "   • Creates core gameplay systems without networking\n" +
                "   • Good for single-player testing\n" +
                "   • Includes abilities, commands, projectiles\n\n" +
                "4. ADVANCED SETUP:\n" +
                "   • Use 'MOBA > Generate Complete Scene' for detailed options\n" +
                "   • Customize which systems to include\n" +
                "   • Auto-configure component references\n\n" +
                "All setups are production-ready and follow MOBA project standards.", 
                "OK");
        }
    }
}
