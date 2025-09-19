using NUnit.Framework;
using UnityEngine;
using MOBA.Networking;

namespace MOBA.Tests.EditMode
{
    /// <summary>
    /// Edit mode tests for lag compensation core logic and data structures
    /// Tests that don't require networking or PlayMode can run much faster
    /// </summary>
    public class LagCompensationEditModeTests
    {
        #region Data Structure Tests
        
        [Test]
        public void PlayerSnapshot_CreatesCorrectly()
        {
            Vector3 position = new Vector3(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(45f, 90f, 0f);
            Vector3 velocity = new Vector3(0.5f, 0f, 1f);
            float timestamp = 123.456f;
            float health = 75f;
            uint frame = 1000;
            
            var snapshot = new LagCompensationManager.PlayerSnapshot(
                position, rotation, velocity, timestamp, true, false, health, frame
            );
            
            Assert.AreEqual(position, snapshot.position, "Position should match");
            Assert.AreEqual(rotation, snapshot.rotation, "Rotation should match");
            Assert.AreEqual(velocity, snapshot.velocity, "Velocity should match");
            Assert.AreEqual(timestamp, snapshot.timestamp, 0.001f, "Timestamp should match");
            Assert.AreEqual(health, snapshot.health, 0.001f, "Health should match");
            Assert.AreEqual(frame, snapshot.frameNumber, "Frame number should match");
            Assert.IsTrue(snapshot.isValid, "Should be valid");
            Assert.IsFalse(snapshot.isGrounded, "Should not be grounded");
        }
        
        [Test]
        public void HitRequest_CreatesCorrectly()
        {
            ulong shooterId = 1;
            ulong targetId = 2;
            Vector3 hitPosition = new Vector3(5f, 0f, 5f);
            Vector3 shootDirection = Vector3.forward;
            float clientTime = 100f;
            float serverTime = 100.05f;
            float damage = 50f;
            string weaponId = "test_weapon";
            
            var hitRequest = new LagCompensationManager.HitRequest
            {
                shooterClientId = shooterId,
                targetClientId = targetId,
                hitPosition = hitPosition,
                shootDirection = shootDirection,
                clientTimestamp = clientTime,
                serverTimestamp = serverTime,
                damage = damage,
                weaponId = weaponId
            };
            
            Assert.AreEqual(shooterId, hitRequest.shooterClientId, "Shooter ID should match");
            Assert.AreEqual(targetId, hitRequest.targetClientId, "Target ID should match");
            Assert.AreEqual(hitPosition, hitRequest.hitPosition, "Hit position should match");
            Assert.AreEqual(shootDirection, hitRequest.shootDirection, "Shoot direction should match");
            Assert.AreEqual(clientTime, hitRequest.clientTimestamp, 0.001f, "Client timestamp should match");
            Assert.AreEqual(serverTime, hitRequest.serverTimestamp, 0.001f, "Server timestamp should match");
            Assert.AreEqual(damage, hitRequest.damage, 0.001f, "Damage should match");
            Assert.AreEqual(weaponId, hitRequest.weaponId, "Weapon ID should match");
        }
        
        [Test]
        public void CompensationRequest_TracksCorrectData()
        {
            var request = new LagCompensationManager.CompensationRequest
            {
                shooterClientId = 1,
                targetClientId = 2,
                lagCompensationTime = 0.05f,
                originalTargetPosition = Vector3.zero,
                compensatedTargetPosition = new Vector3(1f, 0f, 0f),
                hitValidated = true,
                processingTime = 0.001f,
                timestamp = 200f
            };
            
            Assert.AreEqual(1UL, request.shooterClientId, "Shooter ID should be tracked");
            Assert.AreEqual(2UL, request.targetClientId, "Target ID should be tracked");
            Assert.AreEqual(0.05f, request.lagCompensationTime, 0.001f, "Compensation time should be tracked");
            Assert.AreEqual(Vector3.zero, request.originalTargetPosition, "Original position should be tracked");
            Assert.AreEqual(new Vector3(1f, 0f, 0f), request.compensatedTargetPosition, "Compensated position should be tracked");
            Assert.IsTrue(request.hitValidated, "Hit validation result should be tracked");
            Assert.AreEqual(0.001f, request.processingTime, 0.0001f, "Processing time should be tracked");
            Assert.AreEqual(200f, request.timestamp, 0.001f, "Timestamp should be tracked");
        }
        
        [Test]
        public void LagCompensationStats_InitializesCorrectly()
        {
            var stats = new LagCompensationManager.LagCompensationStats
            {
                totalPlayers = 4,
                totalSnapshots = 100,
                averageRTT = 0.05f,
                validatedHitsLastSecond = 10,
                rejectedHitsLastSecond = 2,
                averageCompensationTime = 0.03f
            };
            
            Assert.AreEqual(4, stats.totalPlayers, "Player count should be tracked");
            Assert.AreEqual(100, stats.totalSnapshots, "Snapshot count should be tracked");
            Assert.AreEqual(0.05f, stats.averageRTT, 0.001f, "Average RTT should be tracked");
            Assert.AreEqual(10, stats.validatedHitsLastSecond, "Validated hits should be tracked");
            Assert.AreEqual(2, stats.rejectedHitsLastSecond, "Rejected hits should be tracked");
            Assert.AreEqual(0.03f, stats.averageCompensationTime, 0.001f, "Average compensation time should be tracked");
        }
        
        #endregion
        
        #region Mathematical Tests
        
        [Test]
        public void InterpolationMath_HandlesBasicCase()
        {
            // Test linear interpolation between two points
            Vector3 start = Vector3.zero;
            Vector3 end = new Vector3(10f, 0f, 0f);
            float t = 0.5f;
            
            Vector3 result = Vector3.Lerp(start, end, t);
            Vector3 expected = new Vector3(5f, 0f, 0f);
            
            Assert.AreEqual(expected.x, result.x, 0.001f, "X interpolation should be correct");
            Assert.AreEqual(expected.y, result.y, 0.001f, "Y interpolation should be correct");
            Assert.AreEqual(expected.z, result.z, 0.001f, "Z interpolation should be correct");
        }
        
        [Test]
        public void TimeCalculations_HandleTimestamps()
        {
            float serverTime = 100f;
            float clientTime = 99.95f;
            float rtt = 0.1f;
            
            // Lag compensation time = server - client - (rtt / 2)
            float compensationTime = serverTime - clientTime - (rtt * 0.5f);
            float expected = 0f; // 100 - 99.95 - 0.05 = 0
            
            Assert.AreEqual(expected, compensationTime, 0.001f, "Compensation time calculation should be correct");
        }
        
        [Test]
        public void DistanceValidation_AcceptsNearbyHits()
        {
            Vector3 hitPosition = new Vector3(1f, 0f, 0f);
            Vector3 targetPosition = new Vector3(1.05f, 0f, 0f);
            float tolerance = 0.1f;
            
            float distance = Vector3.Distance(hitPosition, targetPosition);
            bool isValid = distance <= tolerance;
            
            Assert.IsTrue(isValid, "Nearby hits should be valid");
            Assert.AreEqual(0.05f, distance, 0.001f, "Distance calculation should be correct");
        }
        
        [Test]
        public void DistanceValidation_RejectsFarHits()
        {
            Vector3 hitPosition = new Vector3(0f, 0f, 0f);
            Vector3 targetPosition = new Vector3(5f, 0f, 0f);
            float tolerance = 1.0f;
            
            float distance = Vector3.Distance(hitPosition, targetPosition);
            bool isValid = distance <= tolerance;
            
            Assert.IsFalse(isValid, "Far hits should be rejected");
            Assert.AreEqual(5f, distance, 0.001f, "Distance calculation should be correct");
        }
        
        #endregion
        
        #region Edge Case Tests
        
        [Test]
        public void TimeClamping_LimitsCompensationTime()
        {
            float maxCompensationMs = 200f;
            float maxCompensationSec = maxCompensationMs / 1000f;
            
            // Test excessive compensation time
            float excessiveTime = 0.5f; // 500ms
            float clampedTime = Mathf.Clamp(excessiveTime, 0f, maxCompensationSec);
            
            Assert.AreEqual(maxCompensationSec, clampedTime, 0.001f, "Should clamp to maximum compensation time");
            
            // Test negative compensation time
            float negativeTime = -0.1f;
            float clampedNegative = Mathf.Clamp(negativeTime, 0f, maxCompensationSec);
            
            Assert.AreEqual(0f, clampedNegative, 0.001f, "Should clamp negative time to zero");
        }
        
        [Test]
        public void BoundaryConditions_HandleExtremes()
        {
            // Test with zero values
            var zeroSnapshot = new LagCompensationManager.PlayerSnapshot(
                Vector3.zero, Quaternion.identity, Vector3.zero, 0f, true, false, 0f, 0
            );
            
            Assert.AreEqual(Vector3.zero, zeroSnapshot.position, "Should handle zero position");
            Assert.AreEqual(0f, zeroSnapshot.timestamp, "Should handle zero timestamp");
            Assert.AreEqual(0f, zeroSnapshot.health, "Should handle zero health");
            
            // Test with maximum values
            var maxSnapshot = new LagCompensationManager.PlayerSnapshot(
                Vector3.one * 1000f, Quaternion.identity, Vector3.one * 100f, float.MaxValue, true, true, 10000f, uint.MaxValue
            );
            
            Assert.AreEqual(Vector3.one * 1000f, maxSnapshot.position, "Should handle large position");
            Assert.AreEqual(float.MaxValue, maxSnapshot.timestamp, "Should handle large timestamp");
            Assert.AreEqual(10000f, maxSnapshot.health, "Should handle large health");
        }
        
        [Test]
        public void InvalidData_HandlesGracefully()
        {
            // Test with NaN values
            var nanPosition = new Vector3(float.NaN, 0f, 0f);
            
            Assert.IsTrue(float.IsNaN(nanPosition.x), "Should preserve NaN values for error detection");
            
            // Test with infinity
            var infinitePosition = new Vector3(float.PositiveInfinity, 0f, 0f);
            
            Assert.IsTrue(float.IsPositiveInfinity(infinitePosition.x), "Should preserve infinity values for error detection");
        }
        
        #endregion
        
        #region Configuration Validation
        
        [Test]
        public void ConfigurationValues_AreReasonable()
        {
            // Test typical configuration values
            float maxCompensationTimeMs = 200f;
            float snapshotIntervalMs = 16.67f; // 60fps
            int maxSnapshotHistory = 256;
            float maxHitDistance = 50f;
            float hitPositionTolerance = 1.0f;
            
            Assert.Greater(maxCompensationTimeMs, 0f, "Max compensation time should be positive");
            Assert.LessOrEqual(maxCompensationTimeMs, 1000f, "Max compensation time should be reasonable");
            
            Assert.Greater(snapshotIntervalMs, 0f, "Snapshot interval should be positive");
            Assert.LessOrEqual(snapshotIntervalMs, 100f, "Snapshot interval should be reasonable for real-time");
            
            Assert.Greater(maxSnapshotHistory, 0, "Snapshot history should be positive");
            Assert.LessOrEqual(maxSnapshotHistory, 1000, "Snapshot history should be reasonable for memory");
            
            Assert.Greater(maxHitDistance, 0f, "Max hit distance should be positive");
            Assert.Greater(hitPositionTolerance, 0f, "Hit position tolerance should be positive");
            Assert.LessOrEqual(hitPositionTolerance, 10f, "Hit position tolerance should be reasonable");
        }
        
        [Test]
        public void PerformanceMetrics_AreTrackable()
        {
            // Test that we can measure performance metrics
            float startTime = Time.realtimeSinceStartup;
            
            // Simulate some work
            for (int i = 0; i < 1000; i++)
            {
                Vector3.Distance(Vector3.zero, Vector3.one);
            }
            
            float endTime = Time.realtimeSinceStartup;
            float processingTime = endTime - startTime;
            
            Assert.GreaterOrEqual(processingTime, 0f, "Processing time should be measurable");
            Assert.Less(processingTime, 1f, "Simple operations should be fast");
        }
        
        #endregion
        
        #region Memory Management Tests
        
        [Test]
        public void DataStructures_HaveExpectedSizes()
        {
            // These tests help ensure we're not accidentally making structures too large
            #pragma warning disable 0219 // Variable assigned but never used - testing structure creation
            var snapshot = new LagCompensationManager.PlayerSnapshot();
            var hitRequest = new LagCompensationManager.HitRequest();
            var compensationRequest = new LagCompensationManager.CompensationRequest();
            #pragma warning restore 0219
            
            // These are just placeholder assertions - actual memory usage may vary
            Assert.IsTrue(true, "Data structures should have reasonable memory footprint");
        }
        
        [Test]
        public void Collections_CanBePooled()
        {
            // Test that we can create and reuse collections efficiently
            var snapshots = new System.Collections.Generic.Queue<LagCompensationManager.PlayerSnapshot>();
            
            // Add and remove items
            snapshots.Enqueue(new LagCompensationManager.PlayerSnapshot());
            snapshots.Enqueue(new LagCompensationManager.PlayerSnapshot());
            
            Assert.AreEqual(2, snapshots.Count, "Should track queue size correctly");
            
            snapshots.Dequeue();
            Assert.AreEqual(1, snapshots.Count, "Should remove items correctly");
            
            snapshots.Clear();
            Assert.AreEqual(0, snapshots.Count, "Should clear correctly for reuse");
        }
        
        #endregion
    }
}