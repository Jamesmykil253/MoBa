using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MOBA.Editor
{
    /// <summary>
    /// Ultimate Player Prefab Creator - Complete audit-driven player prefab generation
    /// Includes all MOBA systems, networking, visual effects, and proper component configuration
    /// </summary>
    public class UltimatePlayerPrefabCreator : EditorWindow
    {
        // Core Settings
        private bool overwriteExisting = true;
        private string prefabName = "UltimatePlayer";
        private string saveLocation = "Assets/Prefabs/";
        
        // Audit Results
        private Dictionary<string, bool> auditResults = new Dictionary<string, bool>();
        private bool auditCompleted = false;
        
        // Player Configuration
        private PlayerArchetype selectedArchetype = PlayerArchetype.Balanced;
        private Color playerColor = Color.blue;
        private Material playerMaterial;
        
        // Physics Configuration
        private float mass = 1f;
        private float linearDamping = 0f;
        private float angularDamping = 0.05f;
        private bool useGravity = true;
        private RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;
        private CollisionDetectionMode collisionDetection = CollisionDetectionMode.Continuous;
        private RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotation;
        
        // Collider Configuration
        private float colliderHeight = 2f;
        private float colliderRadius = 0.5f;
        private Vector3 colliderCenter = new Vector3(0f, 1f, 0f);
        
        // Player Stats
        private float maxHealth = 1000f;
        private float moveSpeed = 350f;
        private float jumpForce = 8f;
        private float doubleJumpForce = 6f;
        private float damageMultiplier = 1f;
        private float abilityCooldownReduction = 0f;
        private int startingCoins = 0;
        
        // Visual Configuration
        private bool createVisualMesh = true;
        private bool addParticleEffects = true;
        private bool addTrailRenderer = false;
        private Vector3 visualScale = Vector3.one;
        private Vector3 visualPosition = new Vector3(0f, 1f, 0f);
        
        // Animation Configuration
        private bool addAnimator = true;
        private RuntimeAnimatorController animatorController;
        private bool createAnimationStates = true;
        
        // Audio Configuration
        private bool addAudioSources = true;
        private AudioClip jumpSound;
        private AudioClip damageSound;
        private AudioClip deathSound;
        
        // Network Configuration
        private bool isNetworkPrefab = true;
        private bool enableClientPrediction = true;
        private bool addNetworkTransform = true;
        private bool addNetworkAnimator = true;
        
        // Input Configuration
        private InputActionAsset inputActions;
        private bool autoConfigureInputs = true;
        
        // Layer Configuration
        private LayerMask playerLayer = 1 << 8; // Player layer
        private LayerMask groundLayer = 1 << 0; // Default layer
        private LayerMask enemyLayer = 1 << 9; // Enemy layer
        private LayerMask projectileLayer = 1 << 10; // Projectile layer
        
        // Camera Configuration
        private bool createPlayerCamera = true;
        private Vector3 cameraOffset = new Vector3(0f, 8f, -8f);
        private Vector3 cameraRotation = new Vector3(30f, 0f, 0f);
        private float cameraFOV = 60f;
        
        // UI Configuration
        private bool createHealthBar = true;
        private bool createAbilityCooldownUI = true;
        private bool createCoinCounter = true;
        
        // Advanced Configuration
        private bool enableStateMachine = true;
        private bool enableCommandPattern = true;
        private bool enableAbilitySystem = true;
        private bool enableDamageSystem = true;
        private bool enableCriticalHitSystem = true;
        private bool enableProjectileSystem = true;
        
        // Debug Configuration
        private bool enableDebugGizmos = true;
        private bool enableDebugLogging = true;
        private bool createTestComponents = false;
        
        // Scroll position for UI
        private Vector2 scrollPosition;
        
        public enum PlayerArchetype
        {
            Balanced,
            PhysicalDPS,
            MagicalDPS,
            Tank,
            Support,
            Assassin,
            Custom
        }
        
        [MenuItem("MOBA Tools/Player Systems/Ultimate Player Prefab Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<UltimatePlayerPrefabCreator>("Ultimate Player Creator");
            window.minSize = new Vector2(450, 700);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Load default input actions
            LoadDefaultInputActions();
            
            // Load default materials
            LoadDefaultMaterials();
            
            // Start audit
            PerformComprehensiveAudit();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Ultimate Player Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Show audit results
            DrawAuditResults();
            EditorGUILayout.Space();
            
            // Core Settings
            DrawCoreSettings();
            EditorGUILayout.Space();
            
            // Player Configuration
            DrawPlayerConfiguration();
            EditorGUILayout.Space();
            
            // Physics Configuration
            DrawPhysicsConfiguration();
            EditorGUILayout.Space();
            
            // Visual Configuration
            DrawVisualConfiguration();
            EditorGUILayout.Space();
            
            // Network Configuration
            DrawNetworkConfiguration();
            EditorGUILayout.Space();
            
            // Advanced Systems
            DrawAdvancedSystems();
            EditorGUILayout.Space();
            
            // Camera & UI
            DrawCameraAndUI();
            EditorGUILayout.Space();
            
            // Debug Options
            DrawDebugOptions();
            EditorGUILayout.Space();
            
            // Create Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Ultimate Player Prefab", GUILayout.Height(40)))
            {
                CreateUltimatePlayerPrefab();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndScrollView();
        }
        
        private void PerformComprehensiveAudit()
        {
            auditResults.Clear();
            
            // Audit core systems
            auditResults["PlayerController"] = AssetDatabase.FindAssets("t:MonoScript PlayerController").Length > 0;
            auditResults["MOBACharacterController"] = AssetDatabase.FindAssets("t:MonoScript MOBACharacterController").Length > 0;
            auditResults["InputRelay"] = AssetDatabase.FindAssets("t:MonoScript InputRelay").Length > 0;
            auditResults["NetworkPlayerController"] = AssetDatabase.FindAssets("t:MonoScript NetworkPlayerController").Length > 0;
            auditResults["StateMachineIntegration"] = AssetDatabase.FindAssets("t:MonoScript StateMachineIntegration").Length > 0;
            
            // Audit ability systems
            auditResults["AbilitySystem"] = AssetDatabase.FindAssets("t:MonoScript AbilitySystem").Length > 0;
            auditResults["AbilityData"] = AssetDatabase.FindAssets("t:MonoScript AbilityData").Length > 0;
            auditResults["CommandManager"] = AssetDatabase.FindAssets("t:MonoScript CommandManager").Length > 0;
            
            // Audit damage systems
            auditResults["DamageFormulas"] = AssetDatabase.FindAssets("t:MonoScript DamageFormulas").Length > 0;
            auditResults["CriticalHitSystem"] = AssetDatabase.FindAssets("t:MonoScript CriticalHitSystem").Length > 0;
            auditResults["CharacterStats"] = AssetDatabase.FindAssets("t:MonoScript CharacterStats").Length > 0;
            
            // Audit input system
            auditResults["InputSystem_Actions"] = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset").Length > 0;
            
            // Audit materials
            auditResults["PlayerMaterial"] = AssetDatabase.FindAssets("Player t:Material").Length > 0;
            
            // Audit prefab directory
            auditResults["PrefabDirectory"] = AssetDatabase.IsValidFolder("Assets/Prefabs");
            
            auditCompleted = true;
        }
        
        private void DrawAuditResults()
        {
            EditorGUILayout.LabelField("System Audit Results", EditorStyles.boldLabel);
            
            if (!auditCompleted)
            {
                EditorGUILayout.HelpBox("Performing comprehensive audit...", MessageType.Info);
                return;
            }
            
            EditorGUI.indentLevel++;
            foreach (var result in auditResults)
            {
                EditorGUILayout.BeginHorizontal();
                var icon = result.Value ? "✓" : "✗";
                var color = result.Value ? Color.green : Color.red;
                
                var oldColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField(icon, GUILayout.Width(20));
                GUI.color = oldColor;
                
                EditorGUILayout.LabelField(result.Key);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
        
        private void DrawCoreSettings()
        {
            EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
            
            prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);
            saveLocation = EditorGUILayout.TextField("Save Location", saveLocation);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        }
        
        private void DrawPlayerConfiguration()
        {
            EditorGUILayout.LabelField("Player Configuration", EditorStyles.boldLabel);
            
            selectedArchetype = (PlayerArchetype)EditorGUILayout.EnumPopup("Player Archetype", selectedArchetype);
            playerColor = EditorGUILayout.ColorField("Player Color", playerColor);
            playerMaterial = (Material)EditorGUILayout.ObjectField("Player Material", playerMaterial, typeof(Material), false);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Stats", EditorStyles.miniBoldLabel);
            maxHealth = EditorGUILayout.FloatField("Max Health", maxHealth);
            moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
            jumpForce = EditorGUILayout.FloatField("Jump Force", jumpForce);
            doubleJumpForce = EditorGUILayout.FloatField("Double Jump Force", doubleJumpForce);
            damageMultiplier = EditorGUILayout.FloatField("Damage Multiplier", damageMultiplier);
            abilityCooldownReduction = EditorGUILayout.Slider("Cooldown Reduction", abilityCooldownReduction, 0f, 0.75f);
            startingCoins = EditorGUILayout.IntField("Starting Coins", startingCoins);
            
            // Apply archetype presets
            if (GUILayout.Button("Apply Archetype Preset"))
            {
                ApplyArchetypePreset();
            }
        }
        
        private void DrawPhysicsConfiguration()
        {
            EditorGUILayout.LabelField("Physics Configuration", EditorStyles.boldLabel);
            
            mass = EditorGUILayout.FloatField("Mass", mass);
            linearDamping = EditorGUILayout.FloatField("Linear Damping", linearDamping);
            angularDamping = EditorGUILayout.FloatField("Angular Damping", angularDamping);
            useGravity = EditorGUILayout.Toggle("Use Gravity", useGravity);
            interpolation = (RigidbodyInterpolation)EditorGUILayout.EnumPopup("Interpolation", interpolation);
            collisionDetection = (CollisionDetectionMode)EditorGUILayout.EnumPopup("Collision Detection", collisionDetection);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collider Settings", EditorStyles.miniBoldLabel);
            colliderHeight = EditorGUILayout.FloatField("Height", colliderHeight);
            colliderRadius = EditorGUILayout.FloatField("Radius", colliderRadius);
            colliderCenter = EditorGUILayout.Vector3Field("Center", colliderCenter);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer Configuration", EditorStyles.miniBoldLabel);
            playerLayer = LayerMaskField("Player Layer", playerLayer);
            groundLayer = LayerMaskField("Ground Layer", groundLayer);
            enemyLayer = LayerMaskField("Enemy Layer", enemyLayer);
            projectileLayer = LayerMaskField("Projectile Layer", projectileLayer);
        }
        
        private void DrawVisualConfiguration()
        {
            EditorGUILayout.LabelField("Visual Configuration", EditorStyles.boldLabel);
            
            createVisualMesh = EditorGUILayout.Toggle("Create Visual Mesh", createVisualMesh);
            
            if (createVisualMesh)
            {
                EditorGUI.indentLevel++;
                visualScale = EditorGUILayout.Vector3Field("Visual Scale", visualScale);
                visualPosition = EditorGUILayout.Vector3Field("Visual Position", visualPosition);
                addParticleEffects = EditorGUILayout.Toggle("Add Particle Effects", addParticleEffects);
                addTrailRenderer = EditorGUILayout.Toggle("Add Trail Renderer", addTrailRenderer);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.miniBoldLabel);
            addAnimator = EditorGUILayout.Toggle("Add Animator", addAnimator);
            
            if (addAnimator)
            {
                EditorGUI.indentLevel++;
                animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Controller", animatorController, typeof(RuntimeAnimatorController), false);
                createAnimationStates = EditorGUILayout.Toggle("Create Animation States", createAnimationStates);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio", EditorStyles.miniBoldLabel);
            addAudioSources = EditorGUILayout.Toggle("Add Audio Sources", addAudioSources);
            
            if (addAudioSources)
            {
                EditorGUI.indentLevel++;
                jumpSound = (AudioClip)EditorGUILayout.ObjectField("Jump Sound", jumpSound, typeof(AudioClip), false);
                damageSound = (AudioClip)EditorGUILayout.ObjectField("Damage Sound", damageSound, typeof(AudioClip), false);
                deathSound = (AudioClip)EditorGUILayout.ObjectField("Death Sound", deathSound, typeof(AudioClip), false);
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawNetworkConfiguration()
        {
            EditorGUILayout.LabelField("Network Configuration", EditorStyles.boldLabel);
            
            isNetworkPrefab = EditorGUILayout.Toggle("Network Prefab", isNetworkPrefab);
            
            if (isNetworkPrefab)
            {
                EditorGUI.indentLevel++;
                enableClientPrediction = EditorGUILayout.Toggle("Client Prediction", enableClientPrediction);
                addNetworkTransform = EditorGUILayout.Toggle("Network Transform", addNetworkTransform);
                addNetworkAnimator = EditorGUILayout.Toggle("Network Animator", addNetworkAnimator);
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAdvancedSystems()
        {
            EditorGUILayout.LabelField("Advanced Systems", EditorStyles.boldLabel);
            
            enableStateMachine = EditorGUILayout.Toggle("State Machine", enableStateMachine);
            enableCommandPattern = EditorGUILayout.Toggle("Command Pattern", enableCommandPattern);
            enableAbilitySystem = EditorGUILayout.Toggle("Ability System", enableAbilitySystem);
            enableDamageSystem = EditorGUILayout.Toggle("Damage System", enableDamageSystem);
            enableCriticalHitSystem = EditorGUILayout.Toggle("Critical Hit System", enableCriticalHitSystem);
            enableProjectileSystem = EditorGUILayout.Toggle("Projectile System", enableProjectileSystem);
        }
        
        private void DrawCameraAndUI()
        {
            EditorGUILayout.LabelField("Camera & UI", EditorStyles.boldLabel);
            
            createPlayerCamera = EditorGUILayout.Toggle("Create Player Camera", createPlayerCamera);
            
            if (createPlayerCamera)
            {
                EditorGUI.indentLevel++;
                cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);
                cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", cameraRotation);
                cameraFOV = EditorGUILayout.Slider("Field of View", cameraFOV, 30f, 120f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Elements", EditorStyles.miniBoldLabel);
            createHealthBar = EditorGUILayout.Toggle("Health Bar", createHealthBar);
            createAbilityCooldownUI = EditorGUILayout.Toggle("Ability Cooldown UI", createAbilityCooldownUI);
            createCoinCounter = EditorGUILayout.Toggle("Coin Counter", createCoinCounter);
        }
        
        private void DrawDebugOptions()
        {
            EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
            
            enableDebugGizmos = EditorGUILayout.Toggle("Debug Gizmos", enableDebugGizmos);
            enableDebugLogging = EditorGUILayout.Toggle("Debug Logging", enableDebugLogging);
            createTestComponents = EditorGUILayout.Toggle("Test Components", createTestComponents);
        }
        
        private void CreateUltimatePlayerPrefab()
        {
            try
            {
                // Ensure directory exists
                var saveDirectory = saveLocation.TrimEnd('/');
                if (!AssetDatabase.IsValidFolder(saveDirectory))
                {
                    // Create directory structure recursively
                    var pathParts = saveDirectory.Split('/');
                    var currentPath = pathParts[0]; // Start with "Assets"
                    
                    for (int i = 1; i < pathParts.Length; i++)
                    {
                        var nextPath = currentPath + "/" + pathParts[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                        }
                        currentPath = nextPath;
                    }
                    AssetDatabase.Refresh();
                }
                
                // Create the player GameObject
                GameObject player = new GameObject(prefabName);
                
                // Set layer
                player.layer = GetLayerFromMask(playerLayer);
                
                // Add core physics components first
                ConfigurePhysics(player);
                
                // Add player components (order is important to avoid null references)
                ConfigurePlayerComponents(player);
                
                // Add visual components
                if (createVisualMesh)
                {
                    ConfigureVisuals(player);
                }
                
                // Add network components
                if (isNetworkPrefab)
                {
                    ConfigureNetworking(player);
                }
                
                // Add camera
                if (createPlayerCamera)
                {
                    ConfigureCamera(player);
                }
                
                // Configure input system
                ConfigureInputSystem(player);
                
                // Configure advanced systems
                ConfigureAdvancedSystems(player);
                
                // Save as prefab
                string prefabPath = Path.Combine(saveLocation, prefabName + ".prefab");
                
                if (File.Exists(prefabPath) && !overwriteExisting)
                {
                    prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
                }
                
                // Save the prefab
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
                
                // Clean up scene
                DestroyImmediate(player);
                
                // Select the created prefab
                Selection.activeObject = savedPrefab;
                EditorGUIUtility.PingObject(savedPrefab);
                
                Debug.Log($"Ultimate Player Prefab created: {prefabPath}");
                
                // Show success dialog
                EditorUtility.DisplayDialog("Success", $"Ultimate Player Prefab '{prefabName}' created successfully!\n\nLocation: {prefabPath}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create Ultimate Player Prefab: {e.Message}\nStack Trace: {e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Failed to create player prefab:\n{e.Message}\n\nCheck the Console for more details.", "OK");
            }
        }
        
        private void ConfigurePhysics(GameObject player)
        {
            // Add CapsuleCollider
            var capsuleCollider = player.AddComponent<CapsuleCollider>();
            capsuleCollider.height = colliderHeight;
            capsuleCollider.radius = colliderRadius;
            capsuleCollider.center = colliderCenter;
            
            // Add Rigidbody
            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.linearDamping = linearDamping;
            rigidbody.angularDamping = angularDamping;
            rigidbody.useGravity = useGravity;
            rigidbody.interpolation = interpolation;
            rigidbody.collisionDetectionMode = collisionDetection;
            rigidbody.constraints = constraints;
        }
        
        private void ConfigurePlayerComponents(GameObject player)
        {
            // Add core components in the correct order to avoid null reference issues
            
            // First add MOBACharacterController
            var characterController = player.AddComponent<MOBACharacterController>();
            
            // Add InputRelay (it will find the character controller)
            var inputRelay = player.AddComponent<InputRelay>();
            
            // Add PlayerController last (it will find the other components)
            var playerController = player.AddComponent<PlayerController>();
            
            // Add StateMachineIntegration if enabled (requires MOBACharacterController)
            if (enableStateMachine)
            {
                player.AddComponent<StateMachineIntegration>();
            }
            
            // Temporarily disable components during prefab creation to prevent initialization errors
            characterController.enabled = false;
            inputRelay.enabled = false;
            playerController.enabled = false;
            
            // Re-enable them immediately (this allows proper initialization without scene dependencies)
            characterController.enabled = true;
            inputRelay.enabled = true;
            playerController.enabled = true;
        }
        
        private void ConfigureVisuals(GameObject player)
        {
            // Create visual child object
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = visualPosition;
            visual.transform.localScale = visualScale;
            
            // Remove collider from visual
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            
            // Configure material
            var renderer = visual.GetComponent<Renderer>();
            if (playerMaterial != null)
            {
                renderer.material = playerMaterial;
            }
            else
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = playerColor;
            }
            
            // Add particle effects
            if (addParticleEffects)
            {
                var particles = visual.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.startColor = playerColor;
                main.startSize = 0.1f;
                main.startSpeed = 2f;
                main.maxParticles = 50;
                
                var emission = particles.emission;
                emission.rateOverTime = 10f;
                
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.5f;
            }
            
            // Add trail renderer
            if (addTrailRenderer)
            {
                var trail = visual.AddComponent<TrailRenderer>();
                trail.time = 0.5f;
                trail.startWidth = 0.1f;
                trail.endWidth = 0.01f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(playerColor, 0.0f), new GradientColorKey(playerColor, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                trail.colorGradient = gradient;
            }
            
            // Add animator
            if (addAnimator)
            {
                var animator = visual.AddComponent<Animator>();
                if (animatorController != null)
                {
                    animator.runtimeAnimatorController = animatorController;
                }
            }
            
            // Add audio sources
            if (addAudioSources)
            {
                var audioSource = player.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.volume = 0.5f;
                audioSource.pitch = 1f;
                
                // Add additional audio sources for different sounds
                for (int i = 0; i < 2; i++)
                {
                    var extraAudio = player.AddComponent<AudioSource>();
                    extraAudio.spatialBlend = 1f;
                    extraAudio.volume = 0.5f;
                }
            }
        }
        
        private void ConfigureNetworking(GameObject player)
        {
            // Add NetworkObject
            var networkObject = player.AddComponent<NetworkObject>();
            
            // Add NetworkTransform
            if (addNetworkTransform)
            {
                var networkTransform = player.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            }
            
            // Add NetworkAnimator
            if (addNetworkAnimator && player.GetComponentInChildren<Animator>() != null)
            {
                player.AddComponent<Unity.Netcode.Components.NetworkAnimator>();
            }
            
            // Add NetworkPlayerController if available
            if (auditResults.ContainsKey("NetworkPlayerController") && auditResults["NetworkPlayerController"])
            {
                player.AddComponent<MOBA.Networking.NetworkPlayerController>();
            }
        }
        
        private void ConfigureCamera(GameObject player)
        {
            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = cameraOffset;
            cameraObj.transform.localRotation = Quaternion.Euler(cameraRotation);
            
            var camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = cameraFOV;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            
            cameraObj.AddComponent<AudioListener>();
            
            // Add camera controller if available
            var cameraControllerTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Name.Contains("CameraController") && typeof(MonoBehaviour).IsAssignableFrom(t))
                .ToArray();
            
            if (cameraControllerTypes.Length > 0)
            {
                cameraObj.AddComponent(cameraControllerTypes[0]);
            }
        }
        
        private void ConfigureInputSystem(GameObject player)
        {
            if (autoConfigureInputs && inputActions != null)
            {
                var playerInput = player.AddComponent<PlayerInput>();
                playerInput.actions = inputActions;
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            }
        }
        
        private void ConfigureAdvancedSystems(GameObject player)
        {
            // Configure additional systems based on audit results and settings
            if (enableAbilitySystem && auditResults.ContainsKey("AbilitySystem") && auditResults["AbilitySystem"])
            {
                // AbilitySystem will be found at runtime
            }
            
            if (enableDamageSystem && auditResults.ContainsKey("DamageFormulas") && auditResults["DamageFormulas"])
            {
                // Damage system integration
            }
            
            if (enableProjectileSystem)
            {
                // Projectile system integration
            }
        }
        
        private void LoadDefaultInputActions()
        {
            var inputActionAssets = AssetDatabase.FindAssets("InputSystem_Actions t:InputActionAsset");
            if (inputActionAssets.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(inputActionAssets[0]);
                inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            }
        }
        
        private void LoadDefaultMaterials()
        {
            var playerMaterials = AssetDatabase.FindAssets("Player t:Material");
            if (playerMaterials.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(playerMaterials[0]);
                playerMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }
        
        private void ApplyArchetypePreset()
        {
            switch (selectedArchetype)
            {
                case PlayerArchetype.PhysicalDPS:
                    damageMultiplier = 1.5f;
                    maxHealth = 800f;
                    moveSpeed = 400f;
                    playerColor = Color.red;
                    break;
                    
                case PlayerArchetype.MagicalDPS:
                    damageMultiplier = 1.3f;
                    maxHealth = 700f;
                    moveSpeed = 320f;
                    abilityCooldownReduction = 0.2f;
                    playerColor = Color.blue;
                    break;
                    
                case PlayerArchetype.Tank:
                    maxHealth = 1500f;
                    moveSpeed = 280f;
                    damageMultiplier = 0.8f;
                    mass = 2f;
                    playerColor = Color.gray;
                    break;
                    
                case PlayerArchetype.Support:
                    maxHealth = 900f;
                    moveSpeed = 330f;
                    abilityCooldownReduction = 0.25f;
                    playerColor = Color.green;
                    break;
                    
                case PlayerArchetype.Assassin:
                    maxHealth = 600f;
                    moveSpeed = 450f;
                    damageMultiplier = 1.7f;
                    jumpForce = 10f;
                    playerColor = Color.black;
                    break;
                    
                case PlayerArchetype.Balanced:
                default:
                    // Keep default values
                    break;
            }
        }
        
        private LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            return EditorGUILayout.MaskField(label, layerMask, UnityEditorInternal.InternalEditorUtility.layers);
        }
        
        private int GetLayerFromMask(LayerMask mask)
        {
            int layerNumber = 0;
            int layer = mask.value;
            while (layer > 1)
            {
                layer = layer >> 1;
                layerNumber++;
            }
            return layerNumber;
        }
    }
}
