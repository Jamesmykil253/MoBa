using UnityEngine;

namespace MOBA.Testing
{
    /// <summary>
    /// Quick scene setup tool for MOBA testing
    /// Creates all necessary GameObjects and components in one click
    /// </summary>
    public class QuickMOBASetup : MonoBehaviour
    {
        [Header("Quick Setup Configuration")]
        [SerializeField] private bool setupOnAwake = false;
        [SerializeField] private bool createPlayer = true;
        [SerializeField] private bool createGround = true;
        [SerializeField] private bool setupCamera = true;
        [SerializeField] private bool addTestingFramework = true;
        
        [Header("Player Setup")]
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0, 1, 0);
        [SerializeField] private Vector3 cameraPosition = new Vector3(0, 5, -10);
        [SerializeField] private Vector3 cameraRotation = new Vector3(20, 0, 0);
        
        private void Awake()
        {
            if (setupOnAwake)
            {
                SetupMOBAScene();
            }
        }
        
        /// <summary>
        /// One-click MOBA scene setup
        /// </summary>
        [ContextMenu("Setup MOBA Scene")]
        public void SetupMOBAScene()
        {
            Debug.Log("🎯 [QuickMOBASetup] Setting up MOBA scene...");
            
            CreateGameManagers();
            
            if (createGround)
                CreateGround();
                
            if (createPlayer)
                CreatePlayer();
                
            if (setupCamera)
                SetupMainCamera();
                
            if (addTestingFramework)
                CreateTestingFramework();
            
            Debug.Log("✅ [QuickMOBASetup] MOBA scene setup complete!");
            Debug.Log("🎮 Press PLAY to start testing your MOBA game!");
        }
        
        private void CreateGameManagers()
        {
            // Create network systems container
            GameObject networkSystems = new GameObject("MOBA_NetworkSystems");
            networkSystems.transform.position = Vector3.zero;
            
            // Create game manager container  
            GameObject gameManager = new GameObject("MOBA_GameManager");
            gameManager.transform.position = Vector3.zero;
            
            Debug.Log("📦 Created game manager containers");
        }
        
        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            
            Debug.Log("🌍 Created ground plane");
        }
        
        private void CreatePlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "MOBA_Player";
            player.transform.position = playerSpawnPosition;
            
            // Add Rigidbody for physics
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
                rb = player.AddComponent<Rigidbody>();
            
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            Debug.Log("🏃‍♂️ Created MOBA player");
        }
        
        private void SetupMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraGO = new GameObject("Main Camera");
                mainCamera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";
            }
            
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.eulerAngles = cameraRotation;
            
            Debug.Log("📷 Setup main camera");
        }
        
        private void CreateTestingFramework()
        {
            GameObject testing = new GameObject("MOBA_Testing");
            testing.transform.position = Vector3.zero;
            
            Debug.Log("🧪 Created testing framework container");
            Debug.Log("📝 Manually add MOBASystemTester and Priority1FixesTester components");
        }
        
        /// <summary>
        /// Quick test to verify setup worked
        /// </summary>
        [ContextMenu("Test Scene Setup")]
        public void TestSceneSetup()
        {
            Debug.Log("🔍 [QuickMOBASetup] Testing scene setup...");
            
            // Check for required objects
            GameObject player = GameObject.Find("MOBA_Player");
            GameObject ground = GameObject.Find("Ground");
            GameObject testing = GameObject.Find("MOBA_Testing");
            Camera camera = Camera.main;
            
            int score = 0;
            
            if (player != null)
            {
                Debug.Log("✅ Player found");
                score++;
            }
            else
            {
                Debug.LogWarning("❌ Player not found");
            }
            
            if (ground != null)
            {
                Debug.Log("✅ Ground found");
                score++;
            }
            else
            {
                Debug.LogWarning("❌ Ground not found");
            }
            
            if (camera != null)
            {
                Debug.Log("✅ Camera found");
                score++;
            }
            else
            {
                Debug.LogWarning("❌ Camera not found");
            }
            
            if (testing != null)
            {
                Debug.Log("✅ Testing framework found");
                score++;
            }
            else
            {
                Debug.LogWarning("❌ Testing framework not found");
            }
            
            Debug.Log($"🎯 Scene setup score: {score}/4");
            
            if (score == 4)
            {
                Debug.Log("🎉 Perfect! Your MOBA scene is ready for testing!");
                Debug.Log("🎮 Press PLAY and use WASD to move, Mouse to look around");
            }
            else
            {
                Debug.LogWarning("⚠️ Some components missing. Run 'Setup MOBA Scene' first");
            }
        }
    }
}
