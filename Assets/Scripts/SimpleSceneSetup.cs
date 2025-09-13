using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Simple scene setup for MOBA testing
    /// </summary>
    public class SimpleSceneSetup : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool includeNetworking = true;
        [SerializeField] private bool includeUI = true;
        [SerializeField] private bool autoStart = false;

        [Header("Player Settings")]
        [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
        [SerializeField] private GameObject playerPrefab;

        [Header("Environment")]
        [SerializeField] private GameObject groundPrefab;
        [SerializeField] private Vector3 groundScale = new Vector3(20, 1, 20);

        void Start()
        {
            if (autoStart)
            {
                SetupScene();
            }
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            CreateGround();
            CreatePlayer();
            CreateGameManager();
            
            if (includeNetworking)
            {
                CreateNetworkManager();
            }
            
            if (includeUI)
            {
                CreateUI();
            }
            
            Debug.Log("Simple MOBA scene setup complete!");
        }

        void CreateGround()
        {
            if (groundPrefab != null)
            {
                var ground = Instantiate(groundPrefab);
                ground.transform.localScale = groundScale;
                ground.name = "Ground";
            }
            else
            {
                // Create basic ground plane
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.localScale = groundScale;
                ground.name = "Ground";
            }
        }

        void CreatePlayer()
        {
            GameObject player;
            
            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            }
            else
            {
                // Create basic player
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.transform.position = playerSpawnPosition;
                player.name = "Player";
                
                // Add components
                player.AddComponent<SimplePlayerController>();
                player.AddComponent<SimpleAbilitySystem>();
                player.AddComponent<Rigidbody>();
            }
        }

        void CreateGameManager()
        {
            var gameManagerObj = new GameObject("Game Manager");
            gameManagerObj.AddComponent<SimpleGameManager>();
        }

        void CreateNetworkManager()
        {
            var networkManagerObj = new GameObject("Network Manager");
            networkManagerObj.AddComponent<SimpleNetworkManager>();
        }

        void CreateUI()
        {
            // Create basic UI canvas
            var canvasObj = new GameObject("UI Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        void CreateCamera()
        {
            // Use modern Unity 6000+ API instead of deprecated Camera.main
            Camera mainCamera = FindFirstObjectByType<Camera>();
            if (mainCamera == null)
            {
                var cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<SimpleCameraController>();
                cameraObj.tag = "MainCamera";
            }
        }
    }
}
