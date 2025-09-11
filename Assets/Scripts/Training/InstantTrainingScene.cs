using UnityEngine;
using MOBA.Training;

namespace MOBA
{
    /// <summary>
    /// Simple component to instantly convert any scene to a training scene
    /// Just add this to an empty GameObject in your scene and it will auto-setup everything
    /// </summary>
    public class InstantTrainingScene : MonoBehaviour
    {
        [Header("One-Click Training Setup")]
        [SerializeField] private bool setupImmediately = true;
        [Tooltip("The player prefab to use for training (if null, will try to find one)")]
        [SerializeField] private GameObject playerPrefab;
        
        private void Start()
        {
            if (setupImmediately)
            {
                CreateTrainingScene();
            }
        }
        
        /// <summary>
        /// Create a complete training scene with one click
        /// </summary>
        [ContextMenu("Create Training Scene Now")]
        public void CreateTrainingScene()
        {
            Debug.Log("[InstantTrainingScene] 🎯 Creating instant training scene...");
            
            // Add TrainingSceneSetup component
            TrainingSceneSetup setup = gameObject.GetComponent<TrainingSceneSetup>();
            if (setup == null)
            {
                setup = gameObject.AddComponent<TrainingSceneSetup>();
            }
            
            // Configure setup
            if (playerPrefab != null)
            {
                // Use reflection to set the player prefab
                var field = typeof(TrainingSceneSetup).GetField("playerPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(setup, playerPrefab);
                }
            }
            
            // Trigger setup
            setup.SetupTrainingScene();
            
            // Self-destruct after setup
            Destroy(this);
            
            Debug.Log("[InstantTrainingScene] ✅ Training scene created! Press Play to start training.");
        }
        
        private void OnGUI()
        {
            // Show setup button in play mode
            if (Application.isPlaying)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 220, Screen.height - 60, 200, 50));
                
                if (GUILayout.Button("🎯 Setup Training Scene", GUILayout.Height(40)))
                {
                    CreateTrainingScene();
                }
                
                GUILayout.EndArea();
            }
        }
    }
}
