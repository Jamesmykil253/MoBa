using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.IO;
using MOBA.Networking;

namespace MOBA.Editor
{
    /// <summary>
    /// Comprehensive Player Prefab Creator based on codebase audit
    /// Creates a fully functional player prefab with all necessary components and proper references
    /// </summary>
    public class ComprehensivePlayerPrefabCreator : EditorWindow
    {
        [Header("Prefab Settings")]
        private bool overwriteExisting = true;
        private string prefabName = "Player";
        private Vector3 prefabScale = Vector3.one;
        private Vector3 colliderCenter = new Vector3(0f, 1f, 0f);
        private float colliderHeight = 2f;
        private float colliderRadius = 0.5f;
        
        [Header("Physics Settings")]
        private float mass = 1f;
        private float linearDamping = 0f;
        private float angularDamping = 0.05f;
        private bool useGravity = true;
        private RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;
        private CollisionDetectionMode collisionDetection = CollisionDetectionMode.Continuous;
        
        [Header("Player Stats")]
        private float maxHealth = 1000f;
        private float moveSpeed = 350f;
        private float jumpForce = 8f;
        private float doubleJumpForce = 6f;
        private float damageMultiplier = 1f;
        
        [Header("Visual Settings")]
        private bool createVisualMesh = true;
        private Color playerColor = Color.blue;
        private Vector3 visualScale = Vector3.one;
        private Vector3 visualPosition = new Vector3(0f, 1f, 0f);
        
        [Header("Network Settings")]
        private bool isNetworkPrefab = true;
        private bool enableClientPrediction = true;
        
        [MenuItem("MOBA/Prefab Creator/Create Comprehensive Player Prefab")]
        public static void ShowWindow()
        {
            GetWindow<ComprehensivePlayerPrefabCreator>("Comprehensive Player Prefab Creator");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Comprehensive Player Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Prefab Settings
            EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
            prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);
            prefabScale = EditorGUILayout.Vector3Field("Scale", prefabScale);
            
            EditorGUILayout.Space();
            
            // Collider Settings
            EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
            colliderCenter = EditorGUILayout.Vector3Field("Center", colliderCenter);
            colliderHeight = EditorGUILayout.FloatField("Height", colliderHeight);
            colliderRadius = EditorGUILayout.FloatField("Radius", colliderRadius);
            
            EditorGUILayout.Space();
            
            // Physics Settings
            EditorGUILayout.LabelField("Physics Settings", EditorStyles.boldLabel);
            mass = EditorGUILayout.FloatField("Mass", mass);
            linearDamping = EditorGUILayout.FloatField("Linear Damping", linearDamping);
            angularDamping = EditorGUILayout.FloatField("Angular Damping", angularDamping);
            useGravity = EditorGUILayout.Toggle("Use Gravity", useGravity);
            interpolation = (RigidbodyInterpolation)EditorGUILayout.EnumPopup("Interpolation", interpolation);
            collisionDetection = (CollisionDetectionMode)EditorGUILayout.EnumPopup("Collision Detection", collisionDetection);
            
            EditorGUILayout.Space();
            
            // Player Stats
            EditorGUILayout.LabelField("Player Stats", EditorStyles.boldLabel);
            maxHealth = EditorGUILayout.FloatField("Max Health", maxHealth);
            moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
            jumpForce = EditorGUILayout.FloatField("Jump Force", jumpForce);
            doubleJumpForce = EditorGUILayout.FloatField("Double Jump Force", doubleJumpForce);
            damageMultiplier = EditorGUILayout.FloatField("Damage Multiplier", damageMultiplier);
            
            EditorGUILayout.Space();
            
            // Visual Settings
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            createVisualMesh = EditorGUILayout.Toggle("Create Visual Mesh", createVisualMesh);
            if (createVisualMesh)
            {
                playerColor = EditorGUILayout.ColorField("Player Color", playerColor);
                visualScale = EditorGUILayout.Vector3Field("Visual Scale", visualScale);
                visualPosition = EditorGUILayout.Vector3Field("Visual Position", visualPosition);
            }
            
            EditorGUILayout.Space();
            
            // Network Settings
            EditorGUILayout.LabelField("Network Settings", EditorStyles.boldLabel);
            isNetworkPrefab = EditorGUILayout.Toggle("Network Prefab", isNetworkPrefab);
            if (isNetworkPrefab)
            {
                enableClientPrediction = EditorGUILayout.Toggle("Client Prediction", enableClientPrediction);
            }
            
            EditorGUILayout.Space();
            
            // Status Display
            EditorGUILayout.LabelField("Audit Results", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GetAuditStatusText(), MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Create Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Comprehensive Player Prefab", GUILayout.Height(40)))
            {
                CreatePlayerPrefab();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            
            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            if (GUILayout.Button("Load from Existing"))
            {
                LoadFromExistingPrefab();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private string GetAuditStatusText()
        {
            var audit = "CODEBASE AUDIT RESULTS:\n\n";
            
            // Core Components Status
            audit += "✅ PlayerController: Requires Rigidbody + Collider\n";
            audit += "✅ MOBACharacterController: Physics & movement\n";
            audit += "✅ InputRelay: Requires PlayerInput component\n";
            audit += "✅ StateMachineIntegration: State management\n";
            audit += isNetworkPrefab ? "✅ NetworkObject: For multiplayer\n" : "⚠️ NetworkObject: Disabled\n";
            
            audit += "\n";
            
            // Material Status
            bool playerMaterialExists = File.Exists("Assets/Materials/Player.mat");
            audit += playerMaterialExists ? "✅ Player.mat: Found\n" : "⚠️ Player.mat: Will be created\n";
            
            // Input Actions Status
            bool inputActionsExist = File.Exists("Assets/InputSystem_Actions.inputactions");
            audit += inputActionsExist ? "✅ InputSystem_Actions: Found\n" : "❌ InputSystem_Actions: Missing\n";
            
            audit += "\n";
            
            // Dependencies Status
            audit += "REQUIRED SCENE OBJECTS:\n";
            audit += "• CommandManager (found via FindAnyObjectByType)\n";
            audit += "• AbilitySystem (found via FindAnyObjectByType)\n";
            audit += "• Camera with 'MainCamera' tag\n";
            
            return audit;
        }
        
        private void CreatePlayerPrefab()
        {
            try
            {
                Debug.Log("[Player Prefab Creator] Starting comprehensive player prefab creation...");
                
                // Check if prefab exists and handle overwrite
                string prefabPath = $"Assets/Prefabs/Gameplay/{prefabName}.prefab";
                if (File.Exists(prefabPath) && !overwriteExisting)
                {
                    EditorUtility.DisplayDialog("Prefab Exists", 
                        $"Prefab {prefabName} already exists. Enable 'Overwrite Existing' to replace it.", "OK");
                    return;
                }
                
                // Create main GameObject
                GameObject playerObject = new GameObject(prefabName);
                playerObject.transform.localScale = prefabScale;
                
                // Add core components based on audit
                CreateCoreComponents(playerObject);
                CreatePhysicsComponents(playerObject);
                CreateGameplayComponents(playerObject);
                
                if (isNetworkPrefab)
                {
                    CreateNetworkComponents(playerObject);
                }
                
                if (createVisualMesh)
                {
                    CreateVisualComponents(playerObject);
                }
                
                // Configure component references
                ConfigureComponentReferences(playerObject);
                
                // Create prefab
                CreatePrefabAsset(playerObject, prefabPath);
                
                // Cleanup
                DestroyImmediate(playerObject);
                
                Debug.Log($"[Player Prefab Creator] ✅ Successfully created {prefabName} prefab at {prefabPath}");
                EditorUtility.DisplayDialog("Success", 
                    $"Player prefab '{prefabName}' created successfully!\n\nLocation: {prefabPath}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Player Prefab Creator] Failed to create prefab: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create prefab: {e.Message}", "OK");
            }
        }
        
        private void CreateCoreComponents(GameObject playerObject)
        {
            // Add CapsuleCollider (required by PlayerController)
            var capsuleCollider = playerObject.AddComponent<CapsuleCollider>();
            capsuleCollider.center = colliderCenter;
            capsuleCollider.height = colliderHeight;
            capsuleCollider.radius = colliderRadius;
            
            Debug.Log("[Player Prefab Creator] ✅ Added CapsuleCollider");
        }
        
        private void CreatePhysicsComponents(GameObject playerObject)
        {
            // Add Rigidbody (required by PlayerController)
            var rigidbody = playerObject.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.linearDamping = linearDamping;
            rigidbody.angularDamping = angularDamping;
            rigidbody.useGravity = useGravity;
            rigidbody.interpolation = interpolation;
            rigidbody.collisionDetectionMode = collisionDetection;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Standard for MOBA characters
            
            Debug.Log("[Player Prefab Creator] ✅ Added Rigidbody with constraints");
        }
        
        private void CreateGameplayComponents(GameObject playerObject)
        {
            // Add PlayerController (main controller)
            var playerController = playerObject.AddComponent<PlayerController>();
            
            // Add MOBACharacterController (physics & state management)
            var characterController = playerObject.AddComponent<MOBACharacterController>();
            
            // Add PlayerInput (required by InputRelay)
            var playerInput = playerObject.AddComponent<PlayerInput>();
            
            // Configure PlayerInput
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                Debug.Log("[Player Prefab Creator] ✅ Assigned InputSystem_Actions to PlayerInput");
            }
            else
            {
                Debug.LogWarning("[Player Prefab Creator] InputSystem_Actions.inputactions not found - PlayerInput will need manual setup");
            }
            
            // Add InputRelay (input management)
            var inputRelay = playerObject.AddComponent<InputRelay>();
            
            // Add StateMachineIntegration (state management)
            var stateMachine = playerObject.AddComponent<StateMachineIntegration>();
            
            Debug.Log("[Player Prefab Creator] ✅ Added all gameplay components");
        }
        
        private void CreateNetworkComponents(GameObject playerObject)
        {
            // Add NetworkObject for multiplayer
            var networkObject = playerObject.AddComponent<NetworkObject>();
            
            // Configure for player object
            networkObject.DontDestroyWithOwner = false;
            networkObject.AutoObjectParentSync = true;
            
            // Optionally add NetworkPlayerController for server-authoritative movement
            if (enableClientPrediction)
            {
                var networkPlayerController = playerObject.AddComponent<NetworkPlayerController>();
                Debug.Log("[Player Prefab Creator] ✅ Added NetworkPlayerController for client prediction");
            }
            
            Debug.Log("[Player Prefab Creator] ✅ Added NetworkObject component");
        }
        
        private void CreateVisualComponents(GameObject playerObject)
        {
            // Create visual child object
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualObject.name = "Visual";
            visualObject.transform.SetParent(playerObject.transform);
            visualObject.transform.localPosition = visualPosition;
            visualObject.transform.localScale = visualScale;
            
            // Remove collider from visual (parent has the functional collider)
            DestroyImmediate(visualObject.GetComponent<CapsuleCollider>());
            
            // Set material
            var renderer = visualObject.GetComponent<Renderer>();
            Material playerMaterial = GetOrCreatePlayerMaterial();
            renderer.material = playerMaterial;
            
            Debug.Log("[Player Prefab Creator] ✅ Created visual mesh with Player material");
        }
        
        private Material GetOrCreatePlayerMaterial()
        {
            string materialPath = "Assets/Materials/Player.mat";
            Material playerMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            
            if (playerMaterial == null)
            {
                // Create new material
                playerMaterial = new Material(Shader.Find("Standard"));
                playerMaterial.color = playerColor;
                playerMaterial.name = "Player";
                
                // Ensure Materials directory exists
                if (!Directory.Exists("Assets/Materials"))
                {
                    Directory.CreateDirectory("Assets/Materials");
                }
                
                AssetDatabase.CreateAsset(playerMaterial, materialPath);
                Debug.Log("[Player Prefab Creator] ✅ Created new Player.mat material");
            }
            else
            {
                // Update existing material color
                playerMaterial.color = playerColor;
                EditorUtility.SetDirty(playerMaterial);
                Debug.Log("[Player Prefab Creator] ✅ Updated existing Player.mat material");
            }
            
            return playerMaterial;
        }
        
        private void ConfigureComponentReferences(GameObject playerObject)
        {
            // Get all components
            var playerController = playerObject.GetComponent<PlayerController>();
            var characterController = playerObject.GetComponent<MOBACharacterController>();
            var inputRelay = playerObject.GetComponent<InputRelay>();
            var stateMachine = playerObject.GetComponent<StateMachineIntegration>();
            var rigidbody = playerObject.GetComponent<Rigidbody>();
            
            // Configure PlayerController via SerializedObject (to access private fields)
            var playerControllerSO = new SerializedObject(playerController);
            
            // Set basic stats
            playerControllerSO.FindProperty("maxHealth").floatValue = maxHealth;
            playerControllerSO.FindProperty("baseMoveSpeed").floatValue = moveSpeed;
            playerControllerSO.FindProperty("jumpForce").floatValue = jumpForce;
            playerControllerSO.FindProperty("doubleJumpForce").floatValue = doubleJumpForce;
            playerControllerSO.FindProperty("damageMultiplier").floatValue = damageMultiplier;
            
            // Set component references
            playerControllerSO.FindProperty("inputRelay").objectReferenceValue = inputRelay;
            playerControllerSO.FindProperty("characterController").objectReferenceValue = characterController;
            
            playerControllerSO.ApplyModifiedProperties();
            
            // Configure MOBACharacterController
            var characterControllerSO = new SerializedObject(characterController);
            characterControllerSO.FindProperty("moveSpeed").floatValue = moveSpeed;
            characterControllerSO.FindProperty("jumpForce").floatValue = jumpForce;
            characterControllerSO.FindProperty("doubleJumpForce").floatValue = doubleJumpForce;
            characterControllerSO.FindProperty("rb").objectReferenceValue = rigidbody;
            characterControllerSO.ApplyModifiedProperties();
            
            // Configure InputRelay
            var inputRelaySO = new SerializedObject(inputRelay);
            inputRelaySO.FindProperty("characterController").objectReferenceValue = characterController;
            inputRelaySO.ApplyModifiedProperties();
            
            // Configure StateMachineIntegration
            var stateMachineSO = new SerializedObject(stateMachine);
            stateMachineSO.FindProperty("characterController").objectReferenceValue = characterController;
            stateMachineSO.FindProperty("playerController").objectReferenceValue = playerController;
            stateMachineSO.FindProperty("inputRelay").objectReferenceValue = inputRelay;
            stateMachineSO.FindProperty("enableStateLogging").boolValue = false; // Disable for prefab
            stateMachineSO.ApplyModifiedProperties();
            
            Debug.Log("[Player Prefab Creator] ✅ Configured all component references");
        }
        
        private void CreatePrefabAsset(GameObject playerObject, string prefabPath)
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(prefabPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create or update prefab
            if (File.Exists(prefabPath))
            {
                // Replace existing prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(playerObject, prefabPath, InteractionMode.AutomatedAction);
                Debug.Log($"[Player Prefab Creator] ✅ Updated existing prefab at {prefabPath}");
            }
            else
            {
                // Create new prefab
                PrefabUtility.SaveAsPrefabAsset(playerObject, prefabPath);
                Debug.Log($"[Player Prefab Creator] ✅ Created new prefab at {prefabPath}");
            }
            
            // Refresh AssetDatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created prefab
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
        }
        
        private void ResetToDefaults()
        {
            overwriteExisting = true;
            prefabName = "Player";
            prefabScale = Vector3.one;
            colliderCenter = new Vector3(0f, 1f, 0f);
            colliderHeight = 2f;
            colliderRadius = 0.5f;
            mass = 1f;
            linearDamping = 0f;
            angularDamping = 0.05f;
            useGravity = true;
            interpolation = RigidbodyInterpolation.Interpolate;
            collisionDetection = CollisionDetectionMode.Continuous;
            maxHealth = 1000f;
            moveSpeed = 350f;
            jumpForce = 8f;
            doubleJumpForce = 6f;
            damageMultiplier = 1f;
            createVisualMesh = true;
            playerColor = Color.blue;
            visualScale = Vector3.one;
            visualPosition = new Vector3(0f, 1f, 0f);
            isNetworkPrefab = true;
            enableClientPrediction = true;
            
            Debug.Log("[Player Prefab Creator] Reset to default values");
        }
        
        private void LoadFromExistingPrefab()
        {
            string prefabPath = $"Assets/Prefabs/Gameplay/{prefabName}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (existingPrefab == null)
            {
                EditorUtility.DisplayDialog("Prefab Not Found", 
                    $"No prefab found at {prefabPath}", "OK");
                return;
            }
            
            // Load settings from existing prefab
            var playerController = existingPrefab.GetComponent<PlayerController>();
            var characterController = existingPrefab.GetComponent<MOBACharacterController>();
            var rigidbody = existingPrefab.GetComponent<Rigidbody>();
            var collider = existingPrefab.GetComponent<CapsuleCollider>();
            
            if (playerController != null)
            {
                var so = new SerializedObject(playerController);
                maxHealth = so.FindProperty("maxHealth").floatValue;
                moveSpeed = so.FindProperty("baseMoveSpeed").floatValue;
                jumpForce = so.FindProperty("jumpForce").floatValue;
                doubleJumpForce = so.FindProperty("doubleJumpForce").floatValue;
                damageMultiplier = so.FindProperty("damageMultiplier").floatValue;
            }
            
            if (rigidbody != null)
            {
                mass = rigidbody.mass;
                linearDamping = rigidbody.linearDamping;
                angularDamping = rigidbody.angularDamping;
                useGravity = rigidbody.useGravity;
                interpolation = rigidbody.interpolation;
                collisionDetection = rigidbody.collisionDetectionMode;
            }
            
            if (collider != null)
            {
                colliderCenter = collider.center;
                colliderHeight = collider.height;
                colliderRadius = collider.radius;
            }
            
            prefabScale = existingPrefab.transform.localScale;
            isNetworkPrefab = existingPrefab.GetComponent<NetworkObject>() != null;
            
            Debug.Log($"[Player Prefab Creator] Loaded settings from {prefabPath}");
        }
    }
}
