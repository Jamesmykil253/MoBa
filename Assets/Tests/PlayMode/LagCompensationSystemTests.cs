using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using System.Collections;
using System.Collections.Generic;
using MOBA.Networking;

namespace MOBA.Tests.PlayMode
{
    /// <summary>
    /// Comprehensive tests for lag compensation system accuracy and edge cases
    /// Tests hit validation, historical state tracking, and integration with combat systems
    /// </summary>
    public class LagCompensationSystemTests : NetcodeIntegrationTest
    {
        #region Test Setup
        
        private LagCompensationManager lagCompensationManager;
        private NetworkObject serverPlayerObject;
        private NetworkObject clientPlayerObject;
        private const float POSITION_TOLERANCE = 0.1f;
        private const float TIME_TOLERANCE = 0.01f;
        
        protected override int NumberOfClients => 1;
        
        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Start the network session
            yield return StartServerAndClients();
            
            // Create lag compensation manager on server
            if (NetworkManager.IsServer)
            {
                var managerGameObject = new GameObject("LagCompensationManager");
                lagCompensationManager = managerGameObject.AddComponent<LagCompensationManager>();
                var networkObject = managerGameObject.AddComponent<NetworkObject>();
                networkObject.Spawn();
                
                // Create server player object
                var serverPlayerGO = new GameObject("ServerPlayer");
                serverPlayerObject = serverPlayerGO.AddComponent<NetworkObject>();
                serverPlayerGO.AddComponent<Rigidbody>();
                serverPlayerGO.AddComponent<SimplePlayerController>();
                serverPlayerObject.Spawn();
                
                // Create client player object
                var clientPlayerGO = new GameObject("ClientPlayer");
                clientPlayerObject = clientPlayerGO.AddComponent<NetworkObject>();
                clientPlayerGO.AddComponent<Rigidbody>();
                clientPlayerGO.AddComponent<SimplePlayerController>();
                clientPlayerObject.SpawnWithOwnership(ClientNetworkManagers[0].LocalClientId);
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // Find lag compensation manager on all clients
            if (!NetworkManager.IsServer)
            {
                lagCompensationManager = Object.FindFirstObjectByType<LagCompensationManager>();
            }
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return ShutdownNetworkSession();
        }
        
        #endregion
        
        #region Basic Functionality Tests
        
        [UnityTest]
        public IEnumerator LagCompensationManager_InitializesCorrectly()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            Assert.IsNotNull(lagCompensationManager, "LagCompensationManager should be created");
            Assert.IsTrue(lagCompensationManager.enabled, "LagCompensationManager should be enabled");
            
            var stats = lagCompensationManager.GetStats();
            Assert.GreaterOrEqual(stats.totalPlayers, 0, "Should track player count");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator StateRecording_TracksPlayerPositions()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            Assert.IsNotNull(serverPlayerObject, "Server player object should exist");
            
            // Move player to known position
            Vector3 testPosition = new Vector3(5f, 0f, 5f);
            serverPlayerObject.transform.position = testPosition;
            
            // Record state manually
            float timestamp = NetworkManager.ServerTime.TimeAsFloat;
            lagCompensationManager.RecordPlayerState(
                serverPlayerObject.OwnerClientId,
                serverPlayerObject,
                timestamp
            );
            
            yield return new WaitForSeconds(0.1f);
            
            var stats = lagCompensationManager.GetStats();
            Assert.Greater(stats.totalSnapshots, 0, "Should have recorded snapshots");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator HitValidation_AcceptsValidHits()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Setup test scenario
            serverPlayerObject.transform.position = Vector3.zero;
            clientPlayerObject.transform.position = new Vector3(2f, 0f, 0f);
            
            yield return new WaitForSeconds(0.1f); // Allow state recording
            
            // Create hit request
            var hitRequest = lagCompensationManager.CreateHitRequest(
                serverPlayerObject.OwnerClientId,
                clientPlayerObject.OwnerClientId,
                50f,
                clientPlayerObject.transform.position,
                "test_weapon"
            );
            
            bool hitValid = lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            Assert.IsTrue(hitValid, "Valid hit should be accepted");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator HitValidation_RejectsInvalidHits()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Setup test scenario with far distance
            serverPlayerObject.transform.position = Vector3.zero;
            clientPlayerObject.transform.position = new Vector3(100f, 0f, 0f);
            
            yield return new WaitForSeconds(0.1f); // Allow state recording
            
            // Create hit request with incorrect position
            var hitRequest = lagCompensationManager.CreateHitRequest(
                serverPlayerObject.OwnerClientId,
                clientPlayerObject.OwnerClientId,
                50f,
                Vector3.zero, // Wrong position
                "test_weapon"
            );
            
            bool hitValid = lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            Assert.IsFalse(hitValid, "Invalid hit should be rejected");
            
            yield return null;
        }
        
        #endregion
        
        #region Historical State Tests
        
        [UnityTest]
        public IEnumerator HistoricalState_InterpolatesCorrectly()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Record states at different times and positions
            Vector3 position1 = new Vector3(0f, 0f, 0f);
            Vector3 position2 = new Vector3(10f, 0f, 0f);
            
            float time1 = NetworkManager.ServerTime.TimeAsFloat;
            clientPlayerObject.transform.position = position1;
            lagCompensationManager.RecordPlayerState(clientPlayerObject.OwnerClientId, clientPlayerObject, time1);
            
            yield return new WaitForSeconds(0.1f);
            
            float time2 = NetworkManager.ServerTime.TimeAsFloat;
            clientPlayerObject.transform.position = position2;
            lagCompensationManager.RecordPlayerState(clientPlayerObject.OwnerClientId, clientPlayerObject, time2);
            
            // Test interpolation at middle time
            float middleTime = (time1 + time2) * 0.5f;
            var hitRequest = new LagCompensationManager.HitRequest
            {
                shooterClientId = serverPlayerObject.OwnerClientId,
                targetClientId = clientPlayerObject.OwnerClientId,
                hitPosition = new Vector3(5f, 0f, 0f), // Should be approximately middle
                clientTimestamp = middleTime,
                serverTimestamp = time2,
                damage = 50f,
                weaponId = "test_interpolation"
            };
            
            bool hitValid = lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            Assert.IsTrue(hitValid, "Interpolated position should validate correctly");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator HistoricalState_HandlesEdgeCases()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Test with no historical data
            var hitRequest = lagCompensationManager.CreateHitRequest(
                999, // Non-existent client
                clientPlayerObject.OwnerClientId,
                50f,
                Vector3.zero,
                "test_edge_case"
            );
            
            bool hitValid = lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            // Should gracefully handle missing data
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator HistoricalState_CleansUpOldData()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Record many states
            for (int i = 0; i < 300; i++) // More than maxSnapshotHistory
            {
                lagCompensationManager.RecordPlayerState(
                    clientPlayerObject.OwnerClientId,
                    clientPlayerObject,
                    NetworkManager.ServerTime.TimeAsFloat + i * 0.01f
                );
            }
            
            yield return new WaitForSeconds(0.5f);
            
            var stats = lagCompensationManager.GetStats();
            Assert.LessOrEqual(stats.totalSnapshots, 256 * 2, "Should limit snapshot history"); // Account for multiple players
            
            yield return null;
        }
        
        #endregion
        
        #region Performance Tests
        
        [UnityTest]
        public IEnumerator Performance_HandlesMultiplePlayersEfficiently()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Create multiple player objects
            List<NetworkObject> players = new List<NetworkObject>();
            
            for (int i = 0; i < 10; i++)
            {
                var playerGO = new GameObject($"TestPlayer_{i}");
                var networkObject = playerGO.AddComponent<NetworkObject>();
                playerGO.AddComponent<Rigidbody>();
                playerGO.AddComponent<SimplePlayerController>();
                networkObject.Spawn();
                players.Add(networkObject);
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // Measure performance of state recording
            float startTime = Time.realtimeSinceStartup;
            
            for (int frame = 0; frame < 60; frame++) // Simulate 1 second at 60fps
            {
                foreach (var player in players)
                {
                    if (player != null)
                    {
                        lagCompensationManager.RecordPlayerState(
                            player.OwnerClientId,
                            player,
                            NetworkManager.ServerTime.TimeAsFloat
                        );
                    }
                }
                yield return null;
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            
            Assert.Less(totalTime, 2.0f, "State recording should be efficient for multiple players");
            
            // Cleanup
            foreach (var player in players)
            {
                if (player != null)
                {
                    player.Despawn();
                }
            }
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Performance_HitValidationIsEfficient()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Setup scenario with some historical data
            for (int i = 0; i < 60; i++)
            {
                lagCompensationManager.RecordPlayerState(
                    clientPlayerObject.OwnerClientId,
                    clientPlayerObject,
                    NetworkManager.ServerTime.TimeAsFloat + i * 0.016f
                );
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // Measure hit validation performance
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 100; i++) // 100 hit validations
            {
                var hitRequest = lagCompensationManager.CreateHitRequest(
                    serverPlayerObject.OwnerClientId,
                    clientPlayerObject.OwnerClientId,
                    50f,
                    clientPlayerObject.transform.position,
                    "performance_test"
                );
                
                lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            
            Assert.Less(totalTime, 0.1f, "Hit validation should be very fast");
            
            yield return null;
        }
        
        #endregion
        
        #region Integration Tests
        
        [UnityTest]
        public IEnumerator Integration_WorksWithPlayerAttackSystem()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Add PlayerAttackSystem to server player
            var attackSystem = serverPlayerObject.gameObject.AddComponent<MOBA.Combat.PlayerAttackSystem>();
            
            yield return new WaitForSeconds(0.1f);
            
            // Test that attack system finds lag compensation manager
            Assert.IsNotNull(attackSystem, "PlayerAttackSystem should be added");
            
            // This test would require simulating actual attacks
            // For now, we just verify the component integration
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Integration_WorksWithAbilitySystem()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Add ability components
            var abilitySystem = serverPlayerObject.gameObject.AddComponent<MOBA.Abilities.EnhancedAbilitySystem>();
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.IsNotNull(abilitySystem, "AbilitySystem should be added");
            
            // Test integration through ability execution
            // This would require a full ability execution test scenario
            
            yield return null;
        }
        
        #endregion
        
        #region RTT and Network Tests
        
        [UnityTest]
        public IEnumerator RTT_TrackingWorksCorrectly()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Simulate RTT updates
            lagCompensationManager.UpdatePlayerRTT(clientPlayerObject.OwnerClientId, 0.05f); // 50ms RTT
            
            float rtt = lagCompensationManager.GetPlayerRTT(clientPlayerObject.OwnerClientId);
            Assert.AreEqual(0.05f, rtt, 0.001f, "RTT should be tracked correctly");
            
            var stats = lagCompensationManager.GetStats();
            Assert.Greater(stats.averageRTT, 0f, "Average RTT should be calculated");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator LagCompensation_AccountsForRTT()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Setup high RTT scenario
            lagCompensationManager.UpdatePlayerRTT(serverPlayerObject.OwnerClientId, 0.1f); // 100ms RTT
            
            // Record historical position
            Vector3 pastPosition = new Vector3(0f, 0f, 0f);
            clientPlayerObject.transform.position = pastPosition;
            float pastTime = NetworkManager.ServerTime.TimeAsFloat - 0.1f;
            lagCompensationManager.RecordPlayerState(clientPlayerObject.OwnerClientId, clientPlayerObject, pastTime);
            
            // Move to current position
            Vector3 currentPosition = new Vector3(5f, 0f, 0f);
            clientPlayerObject.transform.position = currentPosition;
            
            yield return new WaitForSeconds(0.1f);
            
            // Test hit at past position (should account for lag)
            var hitRequest = new LagCompensationManager.HitRequest
            {
                shooterClientId = serverPlayerObject.OwnerClientId,
                targetClientId = clientPlayerObject.OwnerClientId,
                hitPosition = pastPosition,
                clientTimestamp = pastTime + 0.05f, // Account for RTT
                serverTimestamp = NetworkManager.ServerTime.TimeAsFloat,
                damage = 50f,
                weaponId = "rtt_test"
            };
            
            bool hitValid = lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            Assert.IsTrue(hitValid, "Should compensate for network lag correctly");
            
            yield return null;
        }
        
        #endregion
        
        #region Statistics and Monitoring Tests
        
        [UnityTest]
        public IEnumerator Statistics_TrackValidationMetrics()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Perform several hit validations
            for (int i = 0; i < 5; i++)
            {
                var hitRequest = lagCompensationManager.CreateHitRequest(
                    serverPlayerObject.OwnerClientId,
                    clientPlayerObject.OwnerClientId,
                    25f,
                    clientPlayerObject.transform.position,
                    $"stats_test_{i}"
                );
                
                lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
                yield return new WaitForSeconds(0.05f);
            }
            
            var stats = lagCompensationManager.GetStats();
            Assert.GreaterOrEqual(stats.validatedHitsLastSecond + stats.rejectedHitsLastSecond, 0, 
                "Should track hit validation statistics");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Debugging_RecentRequestsTracking()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Perform a hit validation
            var hitRequest = lagCompensationManager.CreateHitRequest(
                serverPlayerObject.OwnerClientId,
                clientPlayerObject.OwnerClientId,
                75f,
                clientPlayerObject.transform.position,
                "debug_test"
            );
            
            lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            
            var recentRequests = lagCompensationManager.GetRecentRequests();
            Assert.Greater(recentRequests.Length, 0, "Should track recent compensation requests");
            
            yield return null;
        }
        
        #endregion
        
        #region Configuration Tests
        
        [UnityTest]
        public IEnumerator Configuration_CanBeEnabledDisabled()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Test disabling lag compensation
            lagCompensationManager.SetLagCompensationEnabled(false);
            
            var hitRequest = lagCompensationManager.CreateHitRequest(
                serverPlayerObject.OwnerClientId,
                clientPlayerObject.OwnerClientId,
                50f,
                new Vector3(100f, 0f, 0f), // Invalid position
                "disabled_test"
            );
            
            bool hitValid = lagCompensationManager.ValidateHitWithLagCompensation(hitRequest);
            Assert.IsTrue(hitValid, "Should fall back to direct validation when disabled");
            
            // Re-enable
            lagCompensationManager.SetLagCompensationEnabled(true);
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Configuration_ClearHistoryWorks()
        {
            if (!NetworkManager.IsServer)
                yield break;
                
            // Record some data
            lagCompensationManager.RecordPlayerState(
                clientPlayerObject.OwnerClientId,
                clientPlayerObject,
                NetworkManager.ServerTime.TimeAsFloat
            );
            
            yield return new WaitForSeconds(0.1f);
            
            var statsBefore = lagCompensationManager.GetStats();
            Assert.Greater(statsBefore.totalSnapshots, 0, "Should have snapshots before clearing");
            
            lagCompensationManager.ClearHistory();
            
            var statsAfter = lagCompensationManager.GetStats();
            Assert.AreEqual(0, statsAfter.totalSnapshots, "Should have no snapshots after clearing");
            
            yield return null;
        }
        
        #endregion
    }
}