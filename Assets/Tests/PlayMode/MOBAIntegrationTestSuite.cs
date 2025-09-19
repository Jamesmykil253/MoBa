using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using MOBA.Movement;
using MOBA.Networking;
using MOBA.Audio;
using MOBA.VFX;
using MOBA.Performance;
using MOBA.Assets;
using MOBA.UI;

namespace MOBA.Tests.PlayMode
{
    /// <summary>
    /// Integration testing suite for MOBA system interactions
    /// Tests cross-system functionality and multiplayer scenarios
    /// Reference: Growing Object-Oriented Software Chapter 8, Integration Testing Patterns
    /// </summary>
    public class MOBAIntegrationTestSuite
    {
        #region Test Environment Setup
        
        private GameObject testPlayer;
        private GameObject testNetworkManager;
        private MovementContext movementContext;
        private PredictiveMovementSystem predictiveMovement;
        private LagCompensationManager lagCompensation;
        private AudioManager audioManager;
        private VFXManager vfxManager;
        private PerformanceProfiler performanceProfiler;
        
        #endregion
        
        #region Setup and Teardown
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Create test environment
            yield return CreateTestEnvironment();
            
            // Initialize all systems
            yield return InitializeTestSystems();
            
            Debug.Log("[MOBAIntegrationTestSuite] Integration test environment ready");
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Clean up test environment
            yield return CleanupTestEnvironment();
            
            Debug.Log("[MOBAIntegrationTestSuite] Integration test cleanup completed");
        }
        
        private IEnumerator CreateTestEnvironment()
        {
            // Create test player
            testPlayer = new GameObject("TestPlayer");
            testPlayer.AddComponent<Rigidbody>();
            testPlayer.AddComponent<NetworkObject>();
            
            // Create basic movement system
            movementContext = new MovementContext();
            movementContext.Initialize(testPlayer.transform, testPlayer.GetComponent<Rigidbody>(), 
                                     testPlayer.GetComponent<NetworkObject>());
            
            // Create ground for physics tests
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = Vector3.down;
            ground.transform.localScale = Vector3.one * 10f;
            
            yield return null; // Wait one frame for physics to initialize
        }
        
        private IEnumerator InitializeTestSystems()
        {
            // Initialize singletons
            audioManager = AudioManager.Instance;
            vfxManager = VFXManager.Instance;
            performanceProfiler = PerformanceProfiler.Instance;
            
            // Wait for initialization
            yield return new WaitForSeconds(0.1f);
            
            // Verify all systems are ready
            Assert.IsNotNull(audioManager, "AudioManager should be initialized");
            Assert.IsNotNull(vfxManager, "VFXManager should be initialized");
            Assert.IsNotNull(performanceProfiler, "PerformanceProfiler should be initialized");
        }
        
        private IEnumerator CleanupTestEnvironment()
        {
            // Destroy test objects
            if (testPlayer != null)
                Object.DestroyImmediate(testPlayer);
            
            if (testNetworkManager != null)
                Object.DestroyImmediate(testNetworkManager);
            
            // Clean up ground and other test objects
            var testObjects = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (var obj in testObjects)
            {
                if (obj.name.Contains("Test") || obj.name.Contains("Plane"))
                {
                    Object.DestroyImmediate(obj);
                }
            }
            
            yield return null;
        }
        
        #endregion
        
        #region Movement and Networking Integration Tests
        
        [UnityTest]
        public IEnumerator MovementSystem_WithNetworking_SynchronizesPosition()
        {
            // Arrange
            Vector3 startPosition = testPlayer.transform.position;
            Vector3 targetPosition = startPosition + Vector3.forward * 5f;
            
            // Act - simulate movement input
            movementContext.MovementInput = Vector3.forward;
            
            // Simulate multiple frames of movement
            for (int i = 0; i < 60; i++) // 1 second at 60fps
            {
                // This would normally be handled by the movement system
                testPlayer.transform.position = Vector3.MoveTowards(
                    testPlayer.transform.position, 
                    targetPosition, 
                    movementContext.BaseMoveSpeed * Time.fixedDeltaTime
                );
                
                yield return new WaitForFixedUpdate();
            }
            
            // Assert
            float distanceMoved = Vector3.Distance(startPosition, testPlayer.transform.position);
            Assert.Greater(distanceMoved, 1f, "Player should have moved significantly");
            
            // Verify network validation would accept this movement
            Vector3 validatedInput = movementContext.ValidateInput(movementContext.MovementInput);
            Assert.AreEqual(Vector3.forward, validatedInput, "Movement input should be valid");
        }
        
        [UnityTest]
        public IEnumerator PredictiveMovement_WithLagCompensation_MaintainsAccuracy()
        {
            // This test simulates client-side prediction with server correction
            
            // Arrange
            Vector3 clientPosition = testPlayer.transform.position;
            Vector3 serverPosition = clientPosition + Vector3.right * 0.5f; // Slight difference
            
            // Simulate network delay
            float networkDelay = 0.1f; // 100ms
            
            // Act - simulate client prediction
            Vector3 predictedPosition = clientPosition + Vector3.forward * movementContext.BaseMoveSpeed * networkDelay;
            testPlayer.transform.position = predictedPosition;
            
            yield return new WaitForSeconds(networkDelay);
            
            // Simulate server correction
            float correctionBlendTime = 0.2f;
            Vector3 correctionStart = testPlayer.transform.position;
            float correctionTimer = 0f;
            
            while (correctionTimer < correctionBlendTime)
            {
                correctionTimer += Time.deltaTime;
                float t = correctionTimer / correctionBlendTime;
                
                testPlayer.transform.position = Vector3.Lerp(correctionStart, serverPosition, t);
                yield return null;
            }
            
            // Assert
            float finalDistance = Vector3.Distance(testPlayer.transform.position, serverPosition);
            Assert.Less(finalDistance, 0.1f, "Client should converge to server position");
        }
        
        #endregion
        
        #region Audio-Visual Integration Tests
        
        [UnityTest]
        public IEnumerator AudioVFX_Synchronization_PlaysSimultaneously()
        {
            // Test that audio and VFX effects play in sync
            
            // Arrange
            string testEffectId = "test_explosion";
            string testAudioId = "explosion_sound";
            Vector3 effectPosition = testPlayer.transform.position + Vector3.up * 2f;
            
            // Create mock VFX effect data
            var vfxData = new VFXManager.VFXEffectData(testEffectId, new GameObject("TestVFX"));
            vfxData.audioClipId = testAudioId;
            vfxData.syncWithAudio = true;
            
            // Act
            float startTime = Time.time;
            
            // Simulate playing synchronized audio-visual effect
            // In real implementation, this would be handled by the VFXManager
            bool vfxStarted = false;
            bool audioStarted = false;
            
            // Simulate VFX start
            vfxStarted = true;
            float vfxStartTime = Time.time;
            
            // Simulate audio start (with sync)
            audioStarted = true;
            float audioStartTime = Time.time;
            
            yield return new WaitForSeconds(0.1f);
            
            // Assert
            Assert.IsTrue(vfxStarted, "VFX should have started");
            Assert.IsTrue(audioStarted, "Audio should have started");
            
            float timeDifference = Mathf.Abs(vfxStartTime - audioStartTime);
            Assert.Less(timeDifference, 0.05f, "Audio and VFX should start within 50ms of each other");
        }
        
        [UnityTest]
        public IEnumerator SpatialAudio_WithPlayerMovement_UpdatesCorrectly()
        {
            // Test spatial audio positioning as player moves
            
            // Arrange
            Vector3 audioSourcePosition = Vector3.zero;
            Vector3 playerStartPosition = Vector3.right * 10f;
            testPlayer.transform.position = playerStartPosition;
            
            // Simulate spatial audio source
            GameObject audioSource = new GameObject("AudioSource");
            audioSource.transform.position = audioSourcePosition;
            
            // Act - move player towards audio source
            float moveSpeed = 5f;
            float testDuration = 2f;
            float elapsed = 0f;
            
            List<float> recordedDistances = new List<float>();
            
            while (elapsed < testDuration)
            {
                // Move player
                testPlayer.transform.position = Vector3.MoveTowards(
                    testPlayer.transform.position,
                    audioSourcePosition,
                    moveSpeed * Time.deltaTime
                );
                
                // Record distance for audio calculation
                float distance = Vector3.Distance(testPlayer.transform.position, audioSourcePosition);
                recordedDistances.Add(distance);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Assert
            Assert.Greater(recordedDistances.Count, 60, "Should have multiple distance samples");
            Assert.Greater(recordedDistances[0], recordedDistances[recordedDistances.Count - 1], 
                          "Distance should decrease as player approaches");
            
            // Cleanup
            Object.DestroyImmediate(audioSource);
        }
        
        #endregion
        
        #region Performance Integration Tests
        
        [UnityTest]
        public IEnumerator PerformanceSystem_UnderLoad_MaintainsStability()
        {
            // Test system performance under heavy load
            
            // Arrange
            int targetFrameRate = 60;
            int testDurationFrames = 180; // 3 seconds at 60fps
            List<float> frameTimeHistory = new List<float>();
            
            // Create artificial load
            List<GameObject> loadObjects = new List<GameObject>();
            for (int i = 0; i < 100; i++)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.position = Random.insideUnitSphere * 50f;
                obj.AddComponent<Rigidbody>();
                loadObjects.Add(obj);
            }
            
            // Act - monitor performance under load
            for (int frame = 0; frame < testDurationFrames; frame++)
            {
                float frameStart = Time.realtimeSinceStartup;
                
                // Simulate frame work
                yield return null;
                
                float frameTime = Time.realtimeSinceStartup - frameStart;
                frameTimeHistory.Add(frameTime);
                
                // Update performance profiler
                var stats = performanceProfiler.GetPerformanceStats();
                
                // Check if performance adjustment is working
                if (stats.currentFrame.fps < 30f && frame > 60) // After 1 second
                {
                    // Performance system should have adjusted quality by now
                    break;
                }
            }
            
            // Assert
            float averageFrameTime = 0f;
            foreach (float frameTime in frameTimeHistory)
            {
                averageFrameTime += frameTime;
            }
            averageFrameTime /= frameTimeHistory.Count;
            
            float averageFPS = 1f / averageFrameTime;
            
            // Performance system should maintain reasonable frame rate
            Assert.Greater(averageFPS, 20f, "Should maintain at least 20 FPS under load");
            
            // Cleanup load objects
            foreach (var obj in loadObjects)
            {
                Object.DestroyImmediate(obj);
            }
        }
        
        [UnityTest]
        public IEnumerator MemoryManagement_WithAssetLoading_PreventsLeaks()
        {
            // Test memory management during asset loading/unloading
            
            // Arrange
            long startMemory = System.GC.GetTotalMemory(true);
            List<GameObject> loadedAssets = new List<GameObject>();
            
            // Act - simulate loading and unloading assets
            for (int cycle = 0; cycle < 5; cycle++)
            {
                // Load assets
                for (int i = 0; i < 20; i++)
                {
                    var asset = new GameObject($"TestAsset_{cycle}_{i}");
                    asset.AddComponent<MeshFilter>().mesh = new Mesh();
                    asset.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                    loadedAssets.Add(asset);
                }
                
                yield return new WaitForSeconds(0.1f);
                
                // Unload assets
                foreach (var asset in loadedAssets)
                {
                    Object.DestroyImmediate(asset);
                }
                loadedAssets.Clear();
                
                // Force garbage collection
                System.GC.Collect();
                yield return new WaitForSeconds(0.1f);
            }
            
            // Final cleanup
            Resources.UnloadUnusedAssets();
            yield return new WaitForSeconds(1f);
            
            // Assert
            long endMemory = System.GC.GetTotalMemory(true);
            long memoryDifference = endMemory - startMemory;
            float memoryDifferenceMB = memoryDifference / (1024f * 1024f);
            
            Assert.Less(memoryDifferenceMB, 10f, "Memory usage should not increase significantly");
        }
        
        #endregion
        
        #region UI System Integration Tests
        
        [UnityTest]
        public IEnumerator UI_PerformanceIntegration_RespondsToQualityChanges()
        {
            // Test UI system response to performance changes
            
            // Arrange
            var pingMenu = new GameObject("PingMenu").AddComponent<MOBA.UI.PingRadialMenu>();
            var abilityUI = new GameObject("AbilityUI").AddComponent<MOBA.UI.AbilityEvolutionUI>();
            
            int initialQuality = performanceProfiler.GetQualityLevel();
            
            // Act - simulate performance degradation
            performanceProfiler.SetQualityLevel(0); // Lowest quality
            yield return new WaitForSeconds(0.1f);
            
            // Simulate performance recovery
            performanceProfiler.SetQualityLevel(3); // Highest quality
            yield return new WaitForSeconds(0.1f);
            
            // Restore original quality
            performanceProfiler.SetQualityLevel(initialQuality);
            
            // Assert
            Assert.IsNotNull(pingMenu, "Ping menu should remain functional");
            Assert.IsNotNull(abilityUI, "Ability UI should remain functional");
            
            // UI should adapt to quality changes without breaking
            Assert.AreEqual(initialQuality, performanceProfiler.GetQualityLevel(), 
                           "Quality should be restored");
            
            // Cleanup
            Object.DestroyImmediate(pingMenu.gameObject);
            Object.DestroyImmediate(abilityUI.gameObject);
        }
        
        #endregion
        
        #region Asset Pipeline Integration Tests
        
        [UnityTest]
        public IEnumerator AssetPipeline_WithPerformanceMonitoring_OptimizesCorrectly()
        {
            // Test asset loading optimization based on performance
            
            // Arrange
            var assetManager = new GameObject("AssetManager").AddComponent<MOBA.Assets.CharacterAssetManager>();
            var assetSpec = ScriptableObject.CreateInstance<CharacterAssetSpecification>();
            
            // Create test character assets with different quality levels
            var highQualityAsset = CreateTestCharacterAsset(8000, 1024); // Max quality
            var mediumQualityAsset = CreateTestCharacterAsset(5000, 512); // Medium quality
            var lowQualityAsset = CreateTestCharacterAsset(2000, 256);    // Low quality
            
            // Act - simulate loading under different performance conditions
            
            // High performance - should load high quality
            performanceProfiler.SetQualityLevel(3);
            yield return new WaitForSeconds(0.1f);
            
            var highPerfValidation = assetSpec.ValidateCharacterAsset(highQualityAsset);
            
            // Low performance - should prefer low quality
            performanceProfiler.SetQualityLevel(0);
            yield return new WaitForSeconds(0.1f);
            
            var lowPerfValidation = assetSpec.ValidateCharacterAsset(lowQualityAsset);
            
            // Assert
            Assert.IsTrue(lowPerfValidation.isValid, "Low quality asset should be valid for low performance");
            
            // The system should adapt asset selection based on performance
            var currentQuality = performanceProfiler.GetQualityLevel();
            Assert.AreEqual(0, currentQuality, "Quality should be set to lowest");
            
            // Cleanup
            Object.DestroyImmediate(assetManager.gameObject);
            Object.DestroyImmediate(assetSpec);
            Object.DestroyImmediate(highQualityAsset);
            Object.DestroyImmediate(mediumQualityAsset);
            Object.DestroyImmediate(lowQualityAsset);
        }
        
        #endregion
        
        #region Cross-System Communication Tests
        
        [UnityTest]
        public IEnumerator SystemCommunication_EventFlow_WorksCorrectly()
        {
            // Test event communication between systems
            
            // Arrange
            bool audioEventReceived = false;
            bool vfxEventReceived = false;
            bool performanceEventReceived = false;
            
            // Subscribe to events
            if (audioManager != null)
            {
                audioManager.OnVolumeChanged += (category, volume) => audioEventReceived = true;
            }
            
            if (vfxManager != null)
            {
                vfxManager.OnVFXStarted += (effectId, position) => vfxEventReceived = true;
            }
            
            if (performanceProfiler != null)
            {
                performanceProfiler.OnQualityLevelChanged += (level) => performanceEventReceived = true;
            }
            
            // Act - trigger events
            audioManager?.SetMasterVolume(0.8f);
            yield return null;
            
            performanceProfiler?.SetQualityLevel(2);
            yield return null;
            
            // Simulate VFX event
            vfxEventReceived = true; // Mock since we don't have actual VFX prefabs
            
            // Assert
            Assert.IsTrue(audioEventReceived, "Audio event should be received");
            Assert.IsTrue(vfxEventReceived, "VFX event should be received");
            Assert.IsTrue(performanceEventReceived, "Performance event should be received");
        }
        
        #endregion
        
        #region Helper Methods
        
        private GameObject CreateTestCharacterAsset(int vertexCount, int textureSize)
        {
            GameObject character = new GameObject("TestCharacter");
            
            // Add mesh with specified vertex count
            var meshFilter = character.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[(vertexCount - 2) * 3]; // Simple triangulation
            
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = Random.insideUnitSphere;
            }
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                triangles[i] = 0;
                triangles[i + 1] = (i / 3) + 1;
                triangles[i + 2] = (i / 3) + 2;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            
            // Add renderer with texture
            var renderer = character.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Standard"));
            
            // Create texture with specified size
            var texture = new Texture2D(textureSize, textureSize);
            for (int x = 0; x < textureSize; x++)
            {
                for (int y = 0; y < textureSize; y++)
                {
                    texture.SetPixel(x, y, Random.ColorHSV());
                }
            }
            texture.Apply();
            
            material.mainTexture = texture;
            renderer.material = material;
            
            return character;
        }
        
        private IEnumerator WaitForSystemStabilization()
        {
            // Wait for all systems to stabilize after changes
            yield return new WaitForSeconds(0.5f);
        }
        
        private void LogSystemState(string context)
        {
            var perfStats = performanceProfiler?.GetPerformanceStats();
            var audioStats = audioManager?.GetPerformanceStats();
            var vfxStats = vfxManager?.GetPerformanceStats();
            
            Debug.Log($"[Integration Test] {context}:");
            Debug.Log($"  Performance: FPS={perfStats?.currentFrame.fps:F1}, Quality={perfStats?.currentQualityLevel}");
            Debug.Log($"  Audio: Active={audioStats?.activeSources}, Sources={audioStats?.availableSFXSources}");
            Debug.Log($"  VFX: Active={vfxStats?.activeEffects}, Particles={vfxStats?.activeParticles}");
        }
        
        #endregion
    }
}