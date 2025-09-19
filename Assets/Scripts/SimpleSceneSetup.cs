using UnityEngine;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;
using MOBA.Networking;
using MOBA.Debugging;
using MOBA.Abilities;

namespace MOBA
{
    /// <summary>
    /// Simple scene setup for MOBA testing
    /// </summary>
    public class SimpleSceneSetup : MonoBehaviour
    {
        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Scene,
                GameDebugSystemTag.Scene,
                mechanic,
                subsystem: nameof(SimpleSceneSetup));
        }
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

        private bool sceneSetupPerformed;

        IEnumerator Start()
        {
            if (!autoStart)
            {
                yield break;
            }

            yield return null;
            SetupScene();
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            if (sceneSetupPerformed)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Initialization),
                    "SetupScene called multiple times; ignoring subsequent request.");
                return;
            }

            sceneSetupPerformed = true;

            CreateGround();
            CreatePlayer();
            CreateGameManager();
            CreateCamera();

            if (includeNetworking)
            {
                CreateNetworkManager();
            }
            
            if (includeUI)
            {
                CreateUI();
            }
            
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                "Simple scene setup completed.",
                ("IncludeNetworking", includeNetworking),
                ("IncludeUI", includeUI));
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
                var rb = player.AddComponent<Rigidbody>();
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                if (player.GetComponent<CapsuleCollider>() == null)
                {
                    player.AddComponent<CapsuleCollider>();
                }

                player.AddComponent<SimplePlayerController>();

                player.AddComponent<PlayerInput>();

                player.AddComponent<EnhancedAbilitySystem>();
                var simple = player.AddComponent<SimpleAbilitySystem>();
                simple.SynchroniseAbilities();

            }

            if (player.GetComponent<SimpleInputHandler>() == null)
            {
                player.AddComponent<SimpleInputHandler>();
            }
        }

        void CreateGameManager()
        {
            var existing = FindFirstObjectByType<SimpleGameManager>();
            if (existing != null)
            {
                return;
            }

            var gameManagerObj = new GameObject("Game Manager");
            gameManagerObj.AddComponent<SimpleGameManager>();
        }

        void CreateNetworkManager()
        {
            var existing = FindFirstObjectByType<NetworkManager>();
            if (existing != null)
            {
                return;
            }

            if (!NetworkBootstrapGuard.TryReserve(out var guardReason))
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Initialization),
                    "Skipped automatic network manager creation.",
                    ("Reason", guardReason));
                return;
            }

            try
            {
                var networkManagerObj = new GameObject("Network Manager");
                networkManagerObj.AddComponent<NetworkManager>();
                networkManagerObj.AddComponent<UnityTransport>();
                networkManagerObj.AddComponent<SimpleNetworkManager>();
            }
            finally
            {
                NetworkBootstrapGuard.Release();
            }
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
