using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using TMPro;
using System.IO;
using System.Collections.Generic;
using MOBA.Networking;
using MOBA;

namespace MOBA.Editor
{
    /// <summary>
    /// Comprehensive prefab generator for MOBA project
    /// Creates all necessary prefabs with exact components and proper hierarchy
    /// </summary>
    public class MOBAPrefabGenerator : EditorWindow
    {
        private bool createNetworkPrefabs = true;
        private bool createGameplayPrefabs = true;
        private bool createUIPrefabs = true;
        private bool createEnvironmentPrefabs = true;
        private bool overwriteExisting = false;

        private string prefabsPath = "Assets/Prefabs/";
        private string resourcesPath = "Assets/Resources/";
        [MenuItem("MOBA/Generate All Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<MOBAPrefabGenerator>("MOBA Prefab Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("MOBA Prefab Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Configuration options
            createNetworkPrefabs = EditorGUILayout.Toggle("Create Network Prefabs", createNetworkPrefabs);
            createGameplayPrefabs = EditorGUILayout.Toggle("Create Gameplay Prefabs", createGameplayPrefabs);
            createUIPrefabs = EditorGUILayout.Toggle("Create UI Prefabs", createUIPrefabs);
            createEnvironmentPrefabs = EditorGUILayout.Toggle("Create Environment Prefabs", createEnvironmentPrefabs);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

            GUILayout.Space(10);
            prefabsPath = EditorGUILayout.TextField("Prefabs Path:", prefabsPath);
            resourcesPath = EditorGUILayout.TextField("Resources Path:", resourcesPath);

            GUILayout.Space(20);

            if (GUILayout.Button("Generate All Prefabs", GUILayout.Height(40)))
            {
                GenerateAllPrefabs();
            }

            GUILayout.Space(10);

            // Individual generation buttons
            GUILayout.Label("Individual Generation:", EditorStyles.boldLabel);

            if (GUILayout.Button("Network Player Prefab"))
                CreateNetworkPlayerPrefab();

            if (GUILayout.Button("Network Projectile Prefab"))
                CreateNetworkProjectilePrefab();

            if (GUILayout.Button("MOBA Scene Setup Prefab"))
                CreateMOBASceneSetupPrefab();

            if (GUILayout.Button("UI Canvas Prefab"))
                CreateUICanvasPrefab();

            if (GUILayout.Button("Environment Prefabs"))
                CreateEnvironmentPrefabs();
        }

        private void GenerateAllPrefabs()
        {
            Debug.Log("[MOBAPrefabGenerator] Starting comprehensive prefab generation...");

            // Ensure directories exist
            EnsureDirectoryExists(prefabsPath);
            EnsureDirectoryExists(resourcesPath);
            EnsureDirectoryExists(prefabsPath + "Network/");
            EnsureDirectoryExists(prefabsPath + "Gameplay/");
            EnsureDirectoryExists(prefabsPath + "UI/");
            EnsureDirectoryExists(prefabsPath + "Environment/");

            try
            {
                if (createNetworkPrefabs)
                {
                    CreateNetworkPrefabs();
                }

                if (createGameplayPrefabs)
                {
                    CreateGameplayPrefabs();
                }

                if (createUIPrefabs)
                {
                    CreateUIPrefabs();
                }

                if (createEnvironmentPrefabs)
                {
                    CreateEnvironmentPrefabs();
                }

                // Refresh asset database
                AssetDatabase.Refresh();

                Debug.Log("[MOBAPrefabGenerator] All prefabs generated successfully!");
                EditorUtility.DisplayDialog("Success", "All MOBA prefabs have been generated successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBAPrefabGenerator] Error generating prefabs: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate prefabs: {e.Message}", "OK");
            }
        }

        #region Network Prefabs

        private void CreateNetworkPrefabs()
        {
            Debug.Log("[MOBAPrefabGenerator] Creating network prefabs...");
            CreateNetworkPlayerPrefab();
            CreateNetworkProjectilePrefab();
            CreateNetworkManagerPrefab();
            CreateNetworkSystemIntegrationPrefab();
        }

        private void CreateNetworkPlayerPrefab()
        {
            string prefabPath = prefabsPath + "Network/NetworkPlayer.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] NetworkPlayer prefab already exists, skipping...");
                return;
            }

            GameObject player = new GameObject("NetworkPlayer");

            // Core components
            var capsuleCollider = player.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;
            capsuleCollider.center = Vector3.up;

            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0.05f;
            rigidbody.useGravity = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Network components
            var networkObject = player.AddComponent<NetworkObject>();
            var networkPlayerController = player.AddComponent<NetworkPlayerController>();

            // MOBA gameplay components
            player.AddComponent<PlayerController>();
            player.AddComponent<MOBACharacterController>();
            player.AddComponent<InputRelay>();
            player.AddComponent<StateMachineIntegration>();

            // Visual representation
            CreatePlayerVisual(player);

            // Save as prefab
            SavePrefab(player, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created NetworkPlayer prefab at {prefabPath}");
        }

        private void CreateNetworkProjectilePrefab()
        {
            string prefabPath = prefabsPath + "Network/NetworkProjectile.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] NetworkProjectile prefab already exists, skipping...");
                return;
            }

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "NetworkProjectile";
            projectile.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Remove default collider, add trigger collider
            DestroyImmediate(projectile.GetComponent<Collider>());
            var sphereCollider = projectile.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.5f;

            // Add Rigidbody for physics
            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            // Network components
            projectile.AddComponent<NetworkObject>();
            projectile.AddComponent<NetworkProjectile>();

            // Gameplay components
            projectile.AddComponent<Projectile>();

            // Visual components
            var renderer = projectile.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;
            renderer.material.SetFloat("_Metallic", 0.5f);
            renderer.material.SetFloat("_Smoothness", 0.8f);

            // Trail renderer for visual effect
            var trailRenderer = projectile.AddComponent<TrailRenderer>();
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startColor = Color.yellow;
            trailRenderer.endColor = Color.yellow;
            trailRenderer.time = 0.5f;
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0.02f;

            SavePrefab(projectile, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created NetworkProjectile prefab at {prefabPath}");
        }

        private void CreateNetworkManagerPrefab()
        {
            string prefabPath = prefabsPath + "Network/NetworkManager.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] NetworkManager prefab already exists, skipping...");
                return;
            }

            GameObject networkManager = new GameObject("NetworkManager");

            // Add NetworkManager component
            var netManager = networkManager.AddComponent<NetworkManager>();
            
            // Add transport (Unity Transport by default)
            var transport = networkManager.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            netManager.NetworkConfig.NetworkTransport = transport;

            // Add NetworkGameManager
            networkManager.AddComponent<NetworkGameManager>();

            // Configure NetworkManager
            netManager.NetworkConfig.TickRate = 60;
            netManager.NetworkConfig.EnableSceneManagement = false;

            SavePrefab(networkManager, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created NetworkManager prefab at {prefabPath}");
        }

        private void CreateNetworkSystemIntegrationPrefab()
        {
            string prefabPath = prefabsPath + "Network/NetworkSystemIntegration.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] NetworkSystemIntegration prefab already exists, skipping...");
                return;
            }

            GameObject integration = new GameObject("NetworkSystemIntegration");
            integration.AddComponent<NetworkSystemIntegration>();

            SavePrefab(integration, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created NetworkSystemIntegration prefab at {prefabPath}");
        }

        #endregion

        #region Gameplay Prefabs

        private void CreateGameplayPrefabs()
        {
            Debug.Log("[MOBAPrefabGenerator] Creating gameplay prefabs...");
            CreateLocalPlayerPrefab();
            CreateProjectilePrefab();
            CreateEnemyPrefab();
            CreateCoinPrefab();
        }

        private void CreateLocalPlayerPrefab()
        {
            string prefabPath = prefabsPath + "Gameplay/Player.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] Player prefab already exists, skipping...");
                return;
            }

            GameObject player = new GameObject("Player");

            // Core components
            var capsuleCollider = player.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;
            capsuleCollider.center = Vector3.up;

            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // MOBA gameplay components
            player.AddComponent<PlayerController>();
            player.AddComponent<MOBACharacterController>();
            player.AddComponent<InputRelay>();
            player.AddComponent<StateMachineIntegration>();

            // Visual representation
            CreatePlayerVisual(player);

            SavePrefab(player, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created Player prefab at {prefabPath}");
        }

        private void CreateProjectilePrefab()
        {
            string prefabPath = prefabsPath + "Gameplay/Projectile.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] Projectile prefab already exists, skipping...");
                return;
            }

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile";
            projectile.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Configure collider as trigger
            var collider = projectile.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            // Add Rigidbody
            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;

            // Add Projectile component
            projectile.AddComponent<Projectile>();

            // Visual setup
            var renderer = projectile.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.red;

            SavePrefab(projectile, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created Projectile prefab at {prefabPath}");
        }

        private void CreateEnemyPrefab()
        {
            string prefabPath = prefabsPath + "Gameplay/Enemy.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] Enemy prefab already exists, skipping...");
                return;
            }

            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Enemy";
            enemy.transform.localScale = new Vector3(1f, 2f, 1f);

            // Add components
            var rigidbody = enemy.AddComponent<Rigidbody>();
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // Add health component
            var healthComponent = enemy.AddComponent<HealthComponent>();
            healthComponent.maxHealth = 100f;
            healthComponent.currentHealth = 100f;

            // Visual setup
            var renderer = enemy.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.red;

            SavePrefab(enemy, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created Enemy prefab at {prefabPath}");
        }

        private void CreateCoinPrefab()
        {
            string prefabPath = prefabsPath + "Gameplay/Coin.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] Coin prefab already exists, skipping...");
                return;
            }

            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin";
            coin.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

            // Configure as trigger
            var collider = coin.GetComponent<CapsuleCollider>();
            collider.isTrigger = true;

            // Add rotation animation
            var rotator = coin.AddComponent<CoinRotator>();

            // Visual setup
            var renderer = coin.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;
            renderer.material.SetFloat("_Metallic", 1f);
            renderer.material.SetFloat("_Smoothness", 0.9f);

            SavePrefab(coin, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created Coin prefab at {prefabPath}");
        }

        #endregion

        #region UI Prefabs

        private void CreateUIPrefabs()
        {
            Debug.Log("[MOBAPrefabGenerator] Creating UI prefabs...");
            CreateUICanvasPrefab();
            CreateHUDPrefab();
            CreateNetworkTestUIPrefab();
        }

        private void CreateUICanvasPrefab()
        {
            string prefabPath = prefabsPath + "UI/UICanvas.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] UICanvas prefab already exists, skipping...");
                return;
            }

            GameObject canvas = new GameObject("UICanvas");
            
            // Add Canvas component
            var canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 1;

            // Add CanvasScaler
            var scaler = canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            SavePrefab(canvas, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created UICanvas prefab at {prefabPath}");
        }

        private void CreateHUDPrefab()
        {
            string prefabPath = prefabsPath + "UI/GameHUD.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] GameHUD prefab already exists, skipping...");
                return;
            }

            GameObject hud = new GameObject("GameHUD");
            
            // Add RectTransform and set as full screen
            var rectTransform = hud.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Create health bar
            CreateHealthBarUI(hud);

            // Create ability UI
            CreateAbilityUI(hud);

            // Create coin display
            CreateCoinDisplayUI(hud);

            SavePrefab(hud, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created GameHUD prefab at {prefabPath}");
        }

        private void CreateNetworkTestUIPrefab()
        {
            string prefabPath = prefabsPath + "UI/NetworkTestUI.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] NetworkTestUI prefab already exists, skipping...");
                return;
            }

            GameObject testUI = new GameObject("NetworkTestUI");
            var rectTransform = testUI.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add NetworkTestSetup component
            testUI.AddComponent<NetworkTestSetup>();

            // Create network buttons
            CreateNetworkButtonsUI(testUI);

            SavePrefab(testUI, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created NetworkTestUI prefab at {prefabPath}");
        }

        #endregion

        #region Environment Prefabs

        private void CreateEnvironmentPrefabs()
        {
            Debug.Log("[MOBAPrefabGenerator] Creating environment prefabs...");
            CreateGroundPrefab();
            CreateScoringZonePrefab();
            CreateTestTargetPrefab();
        }

        private void CreateGroundPrefab()
        {
            string prefabPath = prefabsPath + "Environment/Ground.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] Ground prefab already exists, skipping...");
                return;
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(50f, 2f, 50f);
            ground.transform.position = new Vector3(0f, -1f, 0f);

            // Configure collider
            var collider = ground.GetComponent<BoxCollider>();
            collider.isTrigger = false;

            // Add Rigidbody as kinematic
            var rigidbody = ground.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            // Visual setup
            var renderer = ground.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0.2f, 0.6f, 0.2f); // Green ground

            SavePrefab(ground, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created Ground prefab at {prefabPath}");
        }

        private void CreateScoringZonePrefab()
        {
            string prefabPath = prefabsPath + "Environment/ScoringZone.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] ScoringZone prefab already exists, skipping...");
                return;
            }

            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zone.name = "ScoringZone";
            zone.transform.localScale = new Vector3(3f, 1f, 3f);

            // Configure as trigger
            var collider = zone.GetComponent<CapsuleCollider>();
            collider.isTrigger = true;

            // Visual setup
            var renderer = zone.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.green;
            renderer.material.SetFloat("_Metallic", 0f);
            renderer.material.SetFloat("_Smoothness", 0.5f);

            // Make it semi-transparent
            renderer.material.SetFloat("_Mode", 3); // Transparent mode
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3000;

            var color = renderer.material.color;
            color.a = 0.5f;
            renderer.material.color = color;

            SavePrefab(zone, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created ScoringZone prefab at {prefabPath}");
        }

        private void CreateTestTargetPrefab()
        {
            string prefabPath = prefabsPath + "Environment/TestTarget.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] TestTarget prefab already exists, skipping...");
                return;
            }

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "TestTarget";
            target.transform.localScale = new Vector3(1f, 2f, 1f);

            // Add health component
            target.AddComponent<HealthComponent>();

            // Visual setup
            var renderer = target.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.red;

            SavePrefab(target, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created TestTarget prefab at {prefabPath}");
        }

        #endregion

        #region Scene Setup Prefab

        private void CreateMOBASceneSetupPrefab()
        {
            string prefabPath = prefabsPath + "MOBASceneSetup.prefab";
            if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[MOBAPrefabGenerator] MOBASceneSetup prefab already exists, skipping...");
                return;
            }

            GameObject sceneSetup = new GameObject("MOBASceneSetup");
            sceneSetup.AddComponent<MOBASceneSetup>();

            SavePrefab(sceneSetup, prefabPath);
            Debug.Log($"[MOBAPrefabGenerator] Created MOBASceneSetup prefab at {prefabPath}");
        }

        #endregion

        #region Helper Methods

        private void CreatePlayerVisual(GameObject player)
        {
            // Create visual representation
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = Vector3.up;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Remove collider from visual (parent has the functional collider)
            DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // Set material
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.blue;
        }

        private void CreateHealthBarUI(GameObject parent)
        {
            GameObject healthBar = new GameObject("HealthBar");
            healthBar.transform.SetParent(parent.transform);

            var rectTransform = healthBar.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.1f, 0.9f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.95f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Background
            var background = new GameObject("Background");
            background.transform.SetParent(healthBar.transform);
            var bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = background.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = Color.black;

            // Fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(healthBar.transform);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = Color.red;
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
        }

        private void CreateAbilityUI(GameObject parent)
        {
            GameObject abilityPanel = new GameObject("AbilityPanel");
            abilityPanel.transform.SetParent(parent.transform);

            var rectTransform = abilityPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.3f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.7f, 0.15f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Create 4 ability buttons
            for (int i = 0; i < 4; i++)
            {
                GameObject button = new GameObject($"Ability{i + 1}");
                button.transform.SetParent(abilityPanel.transform);

                var buttonRect = button.AddComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(i * 0.25f, 0f);
                buttonRect.anchorMax = new Vector2((i + 1) * 0.25f - 0.02f, 1f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;

                var buttonComponent = button.AddComponent<UnityEngine.UI.Button>();
                var buttonImage = button.AddComponent<UnityEngine.UI.Image>();
                buttonImage.color = Color.cyan;
            }
        }

        private void CreateCoinDisplayUI(GameObject parent)
        {
            GameObject coinDisplay = new GameObject("CoinDisplay");
            coinDisplay.transform.SetParent(parent.transform);

            var rectTransform = coinDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.8f, 0.9f);
            rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var text = coinDisplay.AddComponent<TextMeshProUGUI>();
            text.text = "Coins: 0";
            text.color = Color.yellow;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateNetworkButtonsUI(GameObject parent)
        {
            // Host button
            CreateButton(parent, "HostButton", new Vector2(0.1f, 0.8f), new Vector2(0.25f, 0.85f), "Host");

            // Client button
            CreateButton(parent, "ClientButton", new Vector2(0.3f, 0.8f), new Vector2(0.45f, 0.85f), "Client");

            // Server button
            CreateButton(parent, "ServerButton", new Vector2(0.5f, 0.8f), new Vector2(0.65f, 0.85f), "Server");

            // Disconnect button
            CreateButton(parent, "DisconnectButton", new Vector2(0.7f, 0.8f), new Vector2(0.85f, 0.85f), "Disconnect");

            // Status text
            GameObject statusText = new GameObject("StatusText");
            statusText.transform.SetParent(parent.transform);

            var statusRect = statusText.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.7f);
            statusRect.anchorMax = new Vector2(0.9f, 0.75f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            var statusTMP = statusText.AddComponent<TextMeshProUGUI>();
            statusTMP.text = "Status: Disconnected";
            statusTMP.color = Color.white;
            statusTMP.fontSize = 18;
            statusTMP.alignment = TextAlignmentOptions.Center;
        }

        private void CreateButton(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, string text)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent.transform);

            var rectTransform = button.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var buttonComponent = button.AddComponent<UnityEngine.UI.Button>();
            var buttonImage = button.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = Color.white;

            // Button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(button.transform);

            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textTMP = buttonText.AddComponent<TextMeshProUGUI>();
            textTMP.text = text;
            textTMP.color = Color.black;
            textTMP.fontSize = 14;
            textTMP.alignment = TextAlignmentOptions.Center;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentPath = Path.GetDirectoryName(path);
                string folderName = Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
                {
                    EnsureDirectoryExists(parentPath);
                }

                AssetDatabase.CreateFolder(parentPath, folderName);
                Debug.Log($"[MOBAPrefabGenerator] Created directory: {path}");
            }
        }

        private void SavePrefab(GameObject obj, string path)
        {
            try
            {
                // Create prefab
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
                
                if (prefab != null)
                {
                    Debug.Log($"[MOBAPrefabGenerator] Successfully saved prefab: {path}");
                }
                else
                {
                    Debug.LogError($"[MOBAPrefabGenerator] Failed to save prefab: {path}");
                }

                // Clean up scene object
                DestroyImmediate(obj);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MOBAPrefabGenerator] Error saving prefab {path}: {e.Message}");
                DestroyImmediate(obj);
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple coin rotator component for coin prefab
    /// </summary>
    public class CoinRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 90f;

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        }
    }

    /// <summary>
    /// Simple health component for test targets
    /// </summary>
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] public float maxHealth = 100f;
        [SerializeField] public float currentHealth = 100f;

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Debug.Log($"{gameObject.name} destroyed!");
                Destroy(gameObject);
            }
        }
    }
}
