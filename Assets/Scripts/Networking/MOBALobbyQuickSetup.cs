using UnityEngine;

namespace MOBA.Networking
{
    /// <summary>
    /// Quick setup script for MOBA lobby development
    /// Add this to your main scene for instant lobby functionality
    /// </summary>
    public class MOBALobbyQuickSetup : MonoBehaviour
    {
        [Header("🚀 One-Click Lobby Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool showDebugUI = true;
        
        [Header("⚙️ Quick Configuration")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private bool autoCreateLobby = true;
        [SerializeField] private bool enableQuickStart = true;
        
        private LobbySceneSetup sceneSetup;
        private LobbySystem lobbySystem;
        private LobbyIntegration integration;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupMOBALobby();
            }
        }
        
        [ContextMenu("🚀 Setup MOBA Lobby")]
        public void SetupMOBALobby()
        {
            Debug.Log("[MOBALobbyQuickSetup] 🚀 Setting up MOBA lobby system...");
            
            // Create scene setup component
            if (sceneSetup == null)
            {
                var setupGO = new GameObject("LobbySceneSetup");
                sceneSetup = setupGO.AddComponent<LobbySceneSetup>();
            }
            
            // Configure and run setup
            sceneSetup.SetMaxPlayers(maxPlayers);
            sceneSetup.SetLobbyName("MOBA Development Lobby");
            sceneSetup.EnableAutoLobbyCreation(autoCreateLobby);
            sceneSetup.SetupLobbyScene();
            
            // Get references to created systems
            lobbySystem = FindFirstObjectByType<LobbySystem>();
            integration = FindFirstObjectByType<LobbyIntegration>();
            
            Debug.Log("[MOBALobbyQuickSetup] ✅ MOBA Lobby setup complete!");
            
            if (enableQuickStart && Application.isEditor)
            {
                Debug.Log("[MOBALobbyQuickSetup] ⚡ Starting development lobby...");
                Invoke(nameof(AutoStartLobby), 1f);
            }
        }
        
        private void AutoStartLobby()
        {
            if (integration != null)
            {
                integration.QuickStart();
            }
            else if (lobbySystem != null)
            {
                lobbySystem.QuickStartDevelopment();
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugUI || !Application.isEditor) return;
            
            GUILayout.BeginArea(new Rect(10, Screen.height - 200, 350, 190));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("🎮 MOBA Lobby Quick Setup", HeaderStyle());
            
            if (!sceneSetup?.IsFullyConfigured ?? true)
            {
                GUILayout.Label("⚠️ Lobby not configured", WarningStyle());
                if (GUILayout.Button("🚀 Setup Lobby Now"))
                {
                    SetupMOBALobby();
                }
            }
            else
            {
                GUILayout.Label("✅ Lobby Ready", SuccessStyle());
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("⚡ Quick Start"))
                {
                    AutoStartLobby();
                }
                
                if (GUILayout.Button("🏗️ Create Lobby"))
                {
                    integration?.CreateLobby();
                }
                
                if (GUILayout.Button("🔌 Join Lobby"))
                {
                    integration?.JoinLobby();
                }
                
                if (GUILayout.Button("🚪 Leave Lobby"))
                {
                    integration?.LeaveLobby();
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private GUIStyle HeaderStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            return style;
        }
        
        private GUIStyle WarningStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            return style;
        }
        
        private GUIStyle SuccessStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.green;
            return style;
        }
    }
}
