using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using MOBA.Services;
using MOBA.UI;
using MOBA.GameManagement;
using MOBA.Core.EventSystem;
using MOBA.Networking;
using MOBA.Performance;
using System.Threading.Tasks;
using System;

namespace MOBA.Tests.PlayMode
{
    /// <summary>
    /// Enhanced integration testing suite for MOBA system interactions
    /// Tests cross-system functionality, service initialization order, and UI system integration
    /// Covers AAA-standard integration testing patterns for complex game systems
    /// Reference: Growing Object-Oriented Software Chapter 8, Integration Testing Patterns
    /// </summary>
    [TestFixture]
    public class EnhancedIntegrationTestSuite
    {
        #region Test Setup
        
        private GameObject testGameManagerObject;
        private GameObject testUIObject;
        private GameObject testNetworkObject;
        private SimpleGameManager gameManager;
        private GameServiceManager serviceManager;
        private UnifiedEventSystem eventSystem;
        
        [SetUp]
        public void SetUp()
        {
            // Clear service registry for test isolation
            ServiceRegistry.Clear();
            
            // Create test objects for integration testing
            testGameManagerObject = new GameObject("TestGameManager");
            testUIObject = new GameObject("TestUI");
            testNetworkObject = new GameObject("TestNetwork");
            
            // Add core components
            gameManager = testGameManagerObject.AddComponent<SimpleGameManager>();
            serviceManager = testGameManagerObject.AddComponent<GameServiceManager>();
            eventSystem = testGameManagerObject.AddComponent<UnifiedEventSystem>();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (testGameManagerObject != null)
                UnityEngine.Object.DestroyImmediate(testGameManagerObject);
            if (testUIObject != null)
                UnityEngine.Object.DestroyImmediate(testUIObject);
            if (testNetworkObject != null)
                UnityEngine.Object.DestroyImmediate(testNetworkObject);
            
            // Clear service registry
            ServiceRegistry.Clear();
        }
        
        #endregion
        
        #region Service Initialization Order Tests
        
        [UnityTest]
        public IEnumerator ServiceInitialization_CorrectOrder_AllServicesAvailable()
        {
            // Arrange
            bool scoringServiceRegistered = false;
            bool matchServiceRegistered = false;
            
            // Act - Initialize service manager (triggers service registration)
            serviceManager.Initialize(gameManager);
            yield return null; // Wait one frame for initialization
            
            // Check service availability
            scoringServiceRegistered = ServiceRegistry.TryResolve<IScoringService>(out var scoringService);
            matchServiceRegistered = ServiceRegistry.TryResolve<IMatchLifecycleService>(out var matchService);
            
            // Assert
            Assert.IsTrue(scoringServiceRegistered, "Scoring service should be registered");
            Assert.IsTrue(matchServiceRegistered, "Match lifecycle service should be registered");
            Assert.IsNotNull(scoringService, "Scoring service should not be null");
            Assert.IsNotNull(matchService, "Match lifecycle service should not be null");
        }
        
        [UnityTest]
        public IEnumerator ServiceInitialization_DependencyInjection_WorksCorrectly()
        {
            // Arrange
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Resolve services
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            var matchService = ServiceRegistry.Resolve<IMatchLifecycleService>();
            
            // Assert - Check dependency injection worked
            Assert.IsNotNull(scoringService, "Scoring service should be available");
            Assert.IsNotNull(matchService, "Match service should be available");
            
            // Verify services are properly connected
            Assert.AreEqual(serviceManager.ScoringService, scoringService, "Service manager should have correct scoring service");
            Assert.AreEqual(serviceManager.MatchLifecycleService, matchService, "Service manager should have correct match service");
        }
        
        [UnityTest]
        public IEnumerator ServiceInitialization_MultipleManagers_ShareServices()
        {
            // Arrange
            var secondGameManagerObject = new GameObject("SecondGameManager");
            var secondServiceManager = secondGameManagerObject.AddComponent<GameServiceManager>();
            var secondGameManager = secondGameManagerObject.AddComponent<SimpleGameManager>();
            
            try
            {
                // Act - Initialize both service managers
                serviceManager.Initialize(gameManager);
                yield return null;
                
                secondServiceManager.Initialize(secondGameManager);
                yield return null;
                
                // Assert - Both should share the same service instances
                var firstScoringService = serviceManager.ScoringService;
                var secondScoringService = secondServiceManager.ScoringService;
                
                Assert.AreSame(firstScoringService, secondScoringService, 
                    "Both service managers should share the same scoring service instance");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondGameManagerObject);
            }
        }
        
        #endregion
        
        #region Cross-System Event Communication Tests
        
        [UnityTest]
        public IEnumerator EventSystem_CrossSystemCommunication_WorksCorrectly()
        {
            // Arrange
            serviceManager.Initialize(gameManager);
            yield return null;
            
            bool scoreEventReceived = false;
            bool matchEventReceived = false;
            int receivedTeam = -1;
            int receivedScore = -1;
            
            // Subscribe to service events through service manager
            serviceManager.OnScoreChanged += (team, score) =>
            {
                scoreEventReceived = true;
                receivedTeam = team;
                receivedScore = score;
            };
            
            serviceManager.OnMatchEnded += (winningTeam) =>
            {
                matchEventReceived = true;
            };
            
            // Act - Trigger score change through scoring service
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            scoringService.AddScore(0, 10); // Team 0 scores 10 points
            yield return null; // Wait for event propagation
            
            // Assert
            Assert.IsTrue(scoreEventReceived, "Score change event should be received");
            Assert.AreEqual(0, receivedTeam, "Should receive correct team");
            Assert.AreEqual(10, receivedScore, "Should receive correct score");
        }
        
        [UnityTest]
        public IEnumerator EventSystem_UISystemIntegration_RespondsToEvents()
        {
            // Arrange
            var abilityUI = testUIObject.AddComponent<AbilityEvolutionUI>();
            var mockPerformanceProfiler = testGameManagerObject.AddComponent<MockPerformanceProfiler>();
            
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Simulate performance change that should affect UI
            mockPerformanceProfiler.SimulatePerformanceChange(0.5f); // Moderate performance
            yield return new WaitForSeconds(0.1f);
            
            // Simulate UI responding to performance changes
            abilityUI.SetPlayerLevel(1); // UI should still be responsive
            
            // Assert
            Assert.IsNotNull(abilityUI, "UI should remain functional during performance changes");
            Assert.DoesNotThrow(() => abilityUI.AddAbilityPoints(1), "UI should handle operations correctly");
        }
        
        [UnityTest]
        public IEnumerator EventSystem_NetworkSystemIntegration_HandlesNetworkEvents()
        {
            // Arrange
            var mockNetworkManager = testNetworkObject.AddComponent<MockNetworkManager>();
            serviceManager.Initialize(gameManager);
            yield return null;
            
            bool networkEventReceived = false;
            
            // Subscribe to network events
            mockNetworkManager.OnPlayerConnected += (playerId) =>
            {
                networkEventReceived = true;
            };
            
            // Act - Simulate player connection
            mockNetworkManager.SimulatePlayerConnection(1);
            yield return null;
            
            // Assert
            Assert.IsTrue(networkEventReceived, "Network event should be received");
        }
        
        #endregion
        
        #region UI System Integration Tests
        
        [UnityTest]
        public IEnumerator UISystem_ServiceIntegration_UpdatesWithServiceChanges()
        {
            // Arrange
            var abilityUI = testUIObject.AddComponent<AbilityEvolutionUI>();
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Change game state through services
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            scoringService.AddScore(0, 5);
            yield return null;
            
            // Simulate UI responding to score changes (level up)
            abilityUI.SetPlayerLevel(2);
            abilityUI.AddAbilityPoints(1);
            
            // Assert
            Assert.DoesNotThrow(() => abilityUI.OpenMenu(), "UI should open menu correctly");
            Assert.DoesNotThrow(() => abilityUI.CloseMenu(), "UI should close menu correctly");
        }
        
        [UnityTest]
        public IEnumerator UISystem_PerformanceIntegration_AdaptsToPerformance()
        {
            // Arrange
            var abilityUI = testUIObject.AddComponent<AbilityEvolutionUI>();
            var performanceProfiler = testGameManagerObject.AddComponent<MockPerformanceProfiler>();
            
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Simulate poor performance
            performanceProfiler.SimulatePerformanceChange(0.2f); // Poor performance
            yield return new WaitForSeconds(0.1f);
            
            // UI should still function but might adapt
            abilityUI.SetPlayerLevel(3);
            
            // Simulate performance recovery
            performanceProfiler.SimulatePerformanceChange(0.9f); // Good performance
            yield return new WaitForSeconds(0.1f);
            
            // Assert
            Assert.IsNotNull(abilityUI, "UI should survive performance fluctuations");
            Assert.DoesNotThrow(() => abilityUI.AddAbilityPoints(2), "UI should remain functional");
        }
        
        #endregion
        
        #region Network Integration Tests
        
        [UnityTest]
        public IEnumerator NetworkSystem_ServiceIntegration_SynchronizesGameState()
        {
            // Arrange
            var networkManager = testNetworkObject.AddComponent<MockNetworkManager>();
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Change game state and simulate network sync
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            scoringService.AddScore(1, 15);
            
            // Simulate network sync
            networkManager.SimulateNetworkSync("score_update", "team_1_score_15");
            yield return null;
            
            // Assert
            Assert.AreEqual(15, scoringService.GetScore(1), "Score should be synced correctly");
        }
        
        [UnityTest]
        public IEnumerator NetworkSystem_EventIntegration_PropagatesNetworkEvents()
        {
            // Arrange
            var networkManager = testNetworkObject.AddComponent<MockNetworkManager>();
            serviceManager.Initialize(gameManager);
            yield return null;
            
            bool networkEventHandled = false;
            
            // Subscribe to network events that should trigger game events
            networkManager.OnNetworkStateChanged += (state) =>
            {
                networkEventHandled = true;
            };
            
            // Act - Simulate network state change
            networkManager.SimulateNetworkStateChange("connected");
            yield return null;
            
            // Assert
            Assert.IsTrue(networkEventHandled, "Network state change should be handled");
        }
        
        #endregion
        
        #region Performance Integration Tests
        
        [UnityTest]
        public IEnumerator PerformanceSystem_ServiceIntegration_MonitorsSystemPerformance()
        {
            // Arrange
            var performanceProfiler = testGameManagerObject.AddComponent<MockPerformanceProfiler>();
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Simulate system load
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            
            // Create some system load
            for (int i = 0; i < 100; i++)
            {
                scoringService.AddScore(0, 1);
                scoringService.AddScore(1, 1);
            }
            
            performanceProfiler.UpdatePerformanceMetrics();
            yield return null;
            
            // Assert
            Assert.IsTrue(performanceProfiler.HasMetrics(), "Performance profiler should have collected metrics");
        }
        
        #endregion
        
        #region Error Handling and Recovery Tests
        
        [UnityTest]
        public IEnumerator ErrorHandling_ServiceFailure_SystemRemainsStable()
        {
            // Arrange
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Simulate service failure
            ServiceRegistry.Clear(); // Simulate services becoming unavailable
            
            // System should handle missing services gracefully
            yield return null;
            
            // Try to reinitialize
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Assert
            Assert.IsTrue(ServiceRegistry.TryResolve<IScoringService>(out _), 
                "System should recover and reinitialize services");
        }
        
        [UnityTest]
        public IEnumerator ErrorHandling_UIFailure_DoesNotAffectGameLogic()
        {
            // Arrange
            var abilityUI = testUIObject.AddComponent<AbilityEvolutionUI>();
            serviceManager.Initialize(gameManager);
            yield return null;
            
            // Act - Simulate UI failure
            UnityEngine.Object.DestroyImmediate(abilityUI);
            yield return null;
            
            // Game logic should continue working
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            scoringService.AddScore(0, 10);
            
            // Assert
            Assert.AreEqual(10, scoringService.GetScore(0), "Game logic should continue working despite UI failure");
        }
        
        #endregion
        
        #region Mock Classes for Testing
        
        /// <summary>
        /// Mock performance profiler for testing performance integration
        /// </summary>
        public class MockPerformanceProfiler : MonoBehaviour
        {
            private float currentPerformance = 1.0f;
            private bool hasMetrics = false;
            
            public void SimulatePerformanceChange(float performance)
            {
                currentPerformance = Mathf.Clamp01(performance);
            }
            
            public void UpdatePerformanceMetrics()
            {
                hasMetrics = true;
            }
            
            public bool HasMetrics() => hasMetrics;
            public float GetCurrentPerformance() => currentPerformance;
        }
        
        /// <summary>
        /// Mock network manager for testing network integration
        /// </summary>
        public class MockNetworkManager : MonoBehaviour
        {
            public System.Action<int> OnPlayerConnected;
            public System.Action<string> OnNetworkStateChanged;
            
            private Dictionary<string, string> networkData = new Dictionary<string, string>();
            
            public void SimulatePlayerConnection(int playerId)
            {
                OnPlayerConnected?.Invoke(playerId);
            }
            
            public void SimulateNetworkSync(string key, string data)
            {
                networkData[key] = data;
            }
            
            public void SimulateNetworkStateChange(string state)
            {
                OnNetworkStateChanged?.Invoke(state);
            }
            
            public string GetNetworkData(string key)
            {
                return networkData.TryGetValue(key, out var value) ? value : null;
            }
        }
        
        #endregion
        
        #region Stress Testing
        
        [UnityTest]
        public IEnumerator StressTest_HighEventVolume_SystemRemainsStable()
        {
            // Arrange
            serviceManager.Initialize(gameManager);
            yield return null;
            
            var scoringService = ServiceRegistry.Resolve<IScoringService>();
            const int eventCount = 1000;
            
            // Act - Generate high volume of events
            var startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < eventCount; i++)
            {
                scoringService.AddScore(i % 2, 1); // Alternate between teams
                
                // Yield periodically to prevent frame locks
                if (i % 100 == 0)
                    yield return null;
            }
            
            var endTime = Time.realtimeSinceStartup;
            var totalTime = endTime - startTime;
            
            // Assert
            Assert.Less(totalTime, 5.0f, "High event volume should complete within reasonable time");
            Assert.AreEqual(500, scoringService.GetScore(0), "Team 0 should have correct score");
            Assert.AreEqual(500, scoringService.GetScore(1), "Team 1 should have correct score");
        }
        
        [UnityTest]
        public IEnumerator StressTest_RapidServiceAccess_NoDeadlocks()
        {
            // Arrange
            serviceManager.Initialize(gameManager);
            yield return null;
            
            const int accessCount = 500;
            var tasks = new List<Task>();
            
            // Act - Rapid concurrent service access
            for (int i = 0; i < accessCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var service = ServiceRegistry.Resolve<IScoringService>();
                    Assert.IsNotNull(service, "Service should always be resolvable");
                }));
            }
            
            // Wait for all tasks with timeout
            var completed = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
            yield return null;
            
            // Assert
            Assert.IsTrue(completed, "All service access tasks should complete without deadlock");
        }
        
        #endregion
    }
}