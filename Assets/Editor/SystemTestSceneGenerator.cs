using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using MOBA;
using MOBA.ErrorHandling;

public static class SystemTestSceneGenerator
{
    private const string TestScenesFolder = "Assets/Scenes/SystemTests";

    private const string GroundPrefabPath = "Assets/Prefabs/Environment/Ground.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Gameplay/Player.prefab";
    private const string EnemyPrefabPath = "Assets/Prefabs/Gameplay/Enemy.prefab";
    private const string AbilityRigPrefabPath = "Assets/Scenes/Test/AbilitySystem.prefab";
    private const string NetworkRigPrefabPath = "Assets/Scenes/Test/MOBA_NetworkSystems.prefab";
    private const string ProjectilePoolPrefabPath = "Assets/Scenes/Test/ProjectilePool.prefab";
    private const string GameManagerPrefabPath = "Assets/Scenes/Test/MOBA_GameManager.prefab";
    private const string CommandManagerPrefabPath = "Assets/Scenes/Test/CommandManager.prefab";

    [MenuItem("Tools/System Tests/Generate Test Scenes")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(TestScenesFolder);

        GenerateAbilityScene();
        GenerateNetworkingScene();
        GeneratePoolingScene();
        GenerateMovementScene();
        GenerateErrorHandlingScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("System Tests", "System test scenes regenerated successfully.", "OK");
    }

    private static void GenerateAbilityScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "AbilitySystemTest";

        var bootstrap = CreateBootstrap(scene);
        ConfigureBootstrap(bootstrap,
            cameraPosition: new Vector3(0f, 6f, -10f),
            lookAt: Vector3.zero,
            spawnInstructions: new[]
            {
                SpawnInstruction("Ability Ground", GroundPrefabPath, Vector3.zero),
                ManagerInstruction("Game Manager", GameManagerPrefabPath),
                ManagerInstruction("Command Manager", CommandManagerPrefabPath),
                AbilityRigOrFallbackInstruction()
            });

        SaveScene(scene, Path.Combine(TestScenesFolder, "AbilitySystemTest.unity"));
    }

    private static void GenerateNetworkingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "NetworkingSystemTest";

        var bootstrap = CreateBootstrap(scene);
        ConfigureBootstrap(bootstrap,
            cameraPosition: new Vector3(0f, 7f, -12f),
            lookAt: Vector3.zero,
            spawnInstructions: new[]
            {
                SpawnInstruction("Networking Ground", GroundPrefabPath, Vector3.zero),
                ManagerInstruction("Game Manager", GameManagerPrefabPath),
                ManagerInstruction("Command Manager", CommandManagerPrefabPath),
                ManagerInstruction("Network Systems Rig", NetworkRigPrefabPath)
            });

        SaveScene(scene, Path.Combine(TestScenesFolder, "NetworkingSystemTest.unity"));
    }

    private static void GeneratePoolingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "PoolingSystemTest";

        var bootstrap = CreateBootstrap(scene);
        ConfigureBootstrap(bootstrap,
            cameraPosition: new Vector3(0f, 7f, -13f),
            lookAt: Vector3.zero,
            spawnInstructions: new[]
            {
                SpawnInstruction("Pooling Ground", GroundPrefabPath, Vector3.zero),
                ManagerInstruction("Game Manager", GameManagerPrefabPath),
                ManagerInstruction("Command Manager", CommandManagerPrefabPath),
                ProjectilePoolInstruction(),
                SpawnInstruction("Pool Test Player", PlayerPrefabPath, new Vector3(-3f, 1f, -3f), new Vector3(0f, 45f, 0f)),
                SpawnInstruction("Pool Test Enemy", EnemyPrefabPath, new Vector3(3f, 1f, 3f), new Vector3(0f, -45f, 0f))
            });

        SaveScene(scene, Path.Combine(TestScenesFolder, "PoolingSystemTest.unity"));
    }

    private static void GenerateMovementScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MovementSystemTest";

        var bootstrap = CreateBootstrap(scene);
        ConfigureBootstrap(bootstrap,
            cameraPosition: new Vector3(0f, 6f, -9f),
            lookAt: new Vector3(0f, 1f, 0f),
            spawnInstructions: new[]
            {
                SpawnInstruction("Movement Ground", GroundPrefabPath, Vector3.zero),
                ManagerInstruction("Game Manager", GameManagerPrefabPath),
                ManagerInstruction("Command Manager", CommandManagerPrefabPath),
                SpawnInstruction("Movement Test Player", PlayerPrefabPath, new Vector3(0f, 1f, 0f))
            });

        SaveScene(scene, Path.Combine(TestScenesFolder, "MovementSystemTest.unity"));
    }

    private static void GenerateErrorHandlingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ErrorHandlingSystemTest";

        var bootstrap = CreateBootstrap(scene, enableErrorHandler: true);
        ConfigureBootstrap(bootstrap,
            cameraPosition: new Vector3(0f, 7f, -9f),
            lookAt: Vector3.zero,
            spawnInstructions: new[]
            {
                ManagerInstruction("Command Manager", CommandManagerPrefabPath)
            });

        SaveScene(scene, Path.Combine(TestScenesFolder, "ErrorHandlingSystemTest.unity"));
    }

    private static SystemTestSceneBootstrap CreateBootstrap(Scene scene, bool enableErrorHandler = false)
    {
        var bootstrapGO = new GameObject("SceneBootstrap");
        SceneManager.MoveGameObjectToScene(bootstrapGO, scene);

        var bootstrap = bootstrapGO.AddComponent<SystemTestSceneBootstrap>();
        var serialized = new SerializedObject(bootstrap);
        serialized.Update();
        serialized.FindProperty("createCamera").boolValue = true;
        serialized.FindProperty("attachPerformanceOverlay").boolValue = true;
        serialized.FindProperty("createDirectionalLight").boolValue = true;
        serialized.FindProperty("createEventSystem").boolValue = true;
        serialized.FindProperty("createErrorHandler").boolValue = enableErrorHandler;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return bootstrap;
    }

    private static void ConfigureBootstrap(SystemTestSceneBootstrap bootstrap, Vector3 cameraPosition, Vector3 lookAt, SystemTestSceneBootstrap.SpawnInstruction[] spawnInstructions)
    {
        var serialized = new SerializedObject(bootstrap);
        serialized.Update();
        serialized.FindProperty("cameraPosition").vector3Value = cameraPosition;
        serialized.FindProperty("cameraLookAt").vector3Value = lookAt;

        var validInstructions = new System.Collections.Generic.List<SystemTestSceneBootstrap.SpawnInstruction>();
        if (spawnInstructions != null)
        {
            foreach (var instruction in spawnInstructions)
            {
                if (instruction.prefab != null)
                {
                    validInstructions.Add(instruction);
                }
            }
        }

        var instructionsProp = serialized.FindProperty("spawnInstructions");
        instructionsProp.arraySize = validInstructions.Count;
        for (int i = 0; i < validInstructions.Count; i++)
        {
            var element = instructionsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("name").stringValue = validInstructions[i].name;
            element.FindPropertyRelative("prefab").objectReferenceValue = validInstructions[i].prefab;
            element.FindPropertyRelative("position").vector3Value = validInstructions[i].position;
            element.FindPropertyRelative("eulerAngles").vector3Value = validInstructions[i].eulerAngles;
            element.FindPropertyRelative("scale").vector3Value = validInstructions[i].scale;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SystemTestSceneBootstrap.SpawnInstruction SpawnInstruction(string name, string prefabPath, Vector3 position, Vector3? eulerAngles = null, Vector3? scale = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"SystemTestSceneGenerator: Prefab not found at {prefabPath}.");
            return default;
        }

        return new SystemTestSceneBootstrap.SpawnInstruction
        {
            name = name,
            prefab = prefab,
            position = position,
            eulerAngles = eulerAngles ?? Vector3.zero,
            scale = scale ?? Vector3.one
        };
    }

    private static SystemTestSceneBootstrap.SpawnInstruction ProjectilePoolInstruction()
    {
        var poolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePoolPrefabPath);
        if (poolPrefab == null)
        {
            Debug.LogWarning("SystemTestSceneGenerator: Projectile pool prefab missing; Pooling scene will spawn without a rig.");
            return default;
        }

        return new SystemTestSceneBootstrap.SpawnInstruction
        {
            name = "Projectile Pool Rig",
            prefab = poolPrefab,
            position = Vector3.zero,
            eulerAngles = Vector3.zero,
            scale = Vector3.one
        };
    }

    private static SystemTestSceneBootstrap.SpawnInstruction AbilityRigOrFallbackInstruction()
    {
        var abilityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AbilityRigPrefabPath);
        if (abilityPrefab != null)
        {
            return SpawnInstruction("Ability Rig", AbilityRigPrefabPath, Vector3.zero);
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogWarning("SystemTestSceneGenerator: Ability rig and player prefab missing; AbilitySystemTest scene will spawn without actors.");
            return new SystemTestSceneBootstrap.SpawnInstruction();
        }

        return new SystemTestSceneBootstrap.SpawnInstruction
        {
            name = "Ability Test Player",
            prefab = playerPrefab,
            position = new Vector3(0f, 1f, 0f),
            eulerAngles = Vector3.zero,
            scale = Vector3.one
        };
    }

    private static SystemTestSceneBootstrap.SpawnInstruction ManagerInstruction(string name, string prefabPath)
    {
        return SpawnInstruction(name, prefabPath, Vector3.zero);
    }

    private static void SaveScene(Scene scene, string path)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }
}
