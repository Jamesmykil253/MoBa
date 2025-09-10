using UnityEngine;
using TMPro;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Scene setup script for creating a complete MOBA test environment
    /// Attach this to an empty GameObject in a new scene
    /// </summary>
    public class MOBASceneSetup : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private bool includeUI = true;

        // Removed automatic Start() method to prevent automatic loading
        // Use ManualSetup() method instead for manual scene setup

        /// <summary>
        /// Sets up a complete MOBA test scene with all systems
        /// </summary>
        public void SetupCompleteScene()
        {
            Debug.Log("Setting up complete MOBA test scene...");

            // Create player
            CreatePlayer();

            // Create camera
            CreateCamera();

            // Create UI
            if (includeUI)
            {
                CreateUI();
            }

            // Create test environment
            CreateTestEnvironment();

            // Create global systems
            CreateGlobalSystems();

            Debug.Log("MOBA test scene setup complete!");
        }

        private void CreatePlayer()
        {
            // Create player GameObject
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.position = Vector3.zero;

            // Add all required components
            var playerController = playerObj.AddComponent<PlayerController>();
            var characterController = playerObj.AddComponent<MOBACharacterController>();
            var inputRelay = playerObj.AddComponent<InputRelay>();
            var testScene = playerObj.AddComponent<MOBATestScene>();

            // Add physics components
            var capsuleCollider = playerObj.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;
            capsuleCollider.center = Vector3.up;

            var rigidbody = playerObj.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0.05f;
            rigidbody.useGravity = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // Add visual representation
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            sphere.transform.SetParent(playerObj.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = new Vector3(1f, 1f, 1f);

            var renderer = sphere.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.blue;

            // Destroy the collider from the primitive since we have our own
            Destroy(sphere.GetComponent<Collider>());

            Debug.Log("Player created with all components");
        }

        private void CreateCamera()
        {
            // Create camera GameObject
            GameObject cameraObj = new GameObject("MainCamera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.tag = "MainCamera";

            // Position camera for third-person view
            cameraObj.transform.position = new Vector3(0, 8, -12);
            cameraObj.transform.rotation = Quaternion.Euler(30, 0, 0);

            // Add camera controller
            var cameraController = cameraObj.AddComponent<MOBACameraController>();
            var playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform != null)
            {
                cameraController.SetTarget(playerTransform);
            }

            Debug.Log("Camera created with MOBA controller");
        }

        private void CreateUI()
        {
            // Create UI Canvas
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create status text
            CreateTextElement(canvasObj, "StatusText", new Vector2(10, -10), new Vector2(400, 100),
                "MOBA Test Scene\nStatus: Initializing...");

            // Create controls text
            CreateTextElement(canvasObj, "ControlsText", new Vector2(10, -120), new Vector2(400, 150),
                "CONTROLS:\nWASD: Move\nSpace: Jump\nQ/E/R: Abilities\nMouse: Camera\nF1: Test All");

            // Create debug text
            CreateTextElement(canvasObj, "DebugText", new Vector2(-10, -10), new Vector2(300, 100),
                "Debug Info:\nPosition: (0,0,0)\nHealth: 1000", TextAnchor.UpperRight);

            Debug.Log("UI elements created");
        }

        private void CreateTextElement(GameObject parent, string name, Vector2 anchoredPosition,
            Vector2 size, string text, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = (TextAlignmentOptions)alignment;

            // Find and assign to test scene
            var testScene = FindFirstObjectByType<MOBATestScene>();
            if (testScene != null)
            {
                switch (name)
                {
                    case "StatusText":
                        testScene.statusText = textComponent;
                        break;
                    case "ControlsText":
                        testScene.controlsText = textComponent;
                        break;
                    case "DebugText":
                        testScene.debugText = textComponent;
                        break;
                }
            }
        }

        private void CreateTestEnvironment()
        {
            // Create ground
            GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundObj.name = "Ground";
            groundObj.transform.position = new Vector3(0, -1, 0);
            groundObj.transform.localScale = new Vector3(50, 2, 50);

            var groundRenderer = groundObj.GetComponent<Renderer>();
            groundRenderer.material = new Material(Shader.Find("Standard"));
            groundRenderer.material.color = new Color(0.2f, 0.2f, 0.2f);

            // Create test target
            GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObj.name = "TestTarget";
            targetObj.transform.position = new Vector3(5, 1, 0);
            targetObj.transform.localScale = new Vector3(1, 2, 1);

            var targetRenderer = targetObj.GetComponent<Renderer>();
            targetRenderer.material = new Material(Shader.Find("Standard"));
            targetRenderer.material.color = Color.red;

            // Add damageable component
            var damageable = targetObj.AddComponent<TestDamageable>();
            damageable.maxHealth = 1000f;
            damageable.currentHealth = 1000f;

            // Create scoring zone
            GameObject zoneObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zoneObj.name = "ScoringZone";
            zoneObj.transform.position = new Vector3(-5, 0, 0);
            zoneObj.transform.localScale = new Vector3(3, 1, 3);

            var zoneRenderer = zoneObj.GetComponent<Renderer>();
            zoneRenderer.material = new Material(Shader.Find("Standard"));
            zoneRenderer.material.color = Color.green;

            Debug.Log("Test environment created");
        }

        private void CreateGlobalSystems()
        {
            // Create CommandManager
            GameObject cmdObj = new GameObject("CommandManager");
            cmdObj.AddComponent<CommandManager>();

            // Create AbilitySystem
            GameObject abilityObj = new GameObject("AbilitySystem");
            abilityObj.AddComponent<AbilitySystem>();

            // Create FlyweightFactory
            GameObject factoryObj = new GameObject("FlyweightFactory");
            factoryObj.AddComponent<FlyweightFactory>();

            // Create ProjectilePool
            GameObject poolObj = new GameObject("ProjectilePool");
            var projectilePool = poolObj.AddComponent<ProjectilePool>();
            projectilePool.flyweightFactory = factoryObj.GetComponent<FlyweightFactory>();

            // Create projectile prefab
            GameObject projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectilePrefab";
            projectilePrefab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var projectileRenderer = projectilePrefab.GetComponent<Renderer>();
            projectileRenderer.material = new Material(Shader.Find("Standard"));
            projectileRenderer.material.color = Color.yellow;

            // Add projectile component
            projectilePrefab.AddComponent<Projectile>();

            // Assign to pool
            projectilePool.projectilePrefab = projectilePrefab;

            Debug.Log("Global systems created");
        }

        /// <summary>
        /// Manual setup method for editor use
        /// </summary>
        public void ManualSetup()
        {
            SetupCompleteScene();
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 200, 30), "Setup MOBA Scene"))
            {
                SetupCompleteScene();
            }

            GUI.Label(new Rect(10, 50, 400, 20), "MOBA Scene Setup - Press button to create test environment");
        }
    }
}