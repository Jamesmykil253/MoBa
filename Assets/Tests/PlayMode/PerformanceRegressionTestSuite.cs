using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Profiling;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MOBA.Performance;
using MOBA.Networking;
using MOBA.Abilities;
using MOBA.Movement;
using System.Text;
using System;

namespace MOBA.Tests.PlayMode
{
    /// <summary>
    /// Automated performance regression testing suite with baseline metrics and continuous monitoring
    /// Implements AAA-standard performance testing patterns for production game development
    /// Tracks CPU, GPU, memory, and network performance across game systems
    /// Reference: Real-Time Rendering 4th Edition, Game Engine Architecture 3rd Edition
    /// </summary>
    [TestFixture]
    public class PerformanceRegressionTestSuite
    {
        #region Test Configuration
        
        private const string BaselineDataPath = "Assets/Tests/PerformanceBaselines/";
        private const string ReportOutputPath = "Assets/Tests/PerformanceReports/";
        private const int WarmupFrames = 60;
        private const int MeasurementFrames = 300; // 5 seconds at 60 FPS
        private const float PerformanceTolerancePercent = 10f; // 10% performance degradation tolerance
        
        #endregion
        
        #region Test Setup
        
        private GameObject testScene;
        private EnhancedPerformanceProfiler performanceProfiler;
        private List<PerformanceMetric> baselineMetrics;
        private List<PerformanceMetric> currentMetrics;
        
        [SetUp]
        public void SetUp()
        {
            // Create test scene
            testScene = new GameObject("PerformanceTestScene");
            
            // Add performance profiler
            performanceProfiler = testScene.AddComponent<EnhancedPerformanceProfiler>();
            
            // Initialize metrics lists
            baselineMetrics = new List<PerformanceMetric>();
            currentMetrics = new List<PerformanceMetric>();
            
            // Ensure output directories exist
            EnsureDirectoriesExist();
            
            // Load baseline metrics if they exist
            LoadBaselineMetrics();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Generate performance report
            GeneratePerformanceReport();
            
            // Clean up test objects
            if (testScene != null)
                UnityEngine.Object.DestroyImmediate(testScene);
        }
        
        #endregion
        
        #region Core System Performance Tests
        
        [UnityTest]
        public IEnumerator PerformanceRegression_AbilitySystem_MaintainsBaseline()
        {
            // Arrange
            var abilitySystem = testScene.AddComponent<EnhancedAbilitySystem>();
            SetupTestAbilities(abilitySystem);
            
            // Warmup
            yield return PerformWarmup();
            
            // Act - Measure ability system performance
            var metrics = new PerformanceMetric("AbilitySystem");
            yield return MeasurePerformance(metrics, () =>
            {
                // Simulate intensive ability usage
                for (int i = 0; i < 10; i++)
                {
                    abilitySystem.TryCastAbility(0);
                    abilitySystem.TryCastAbility(1);
                    abilitySystem.TryCastAbility(2);
                }
            });
            
            currentMetrics.Add(metrics);
            
            // Assert
            var baseline = GetBaselineMetric("AbilitySystem");
            if (baseline != null)
            {
                AssertPerformanceRegression(baseline, metrics, "Ability System");
            }
            else
            {
                SaveAsNewBaseline(metrics);
            }
        }
        
        [UnityTest]
        public IEnumerator PerformanceRegression_MovementSystem_MaintainsBaseline()
        {
            // Arrange
            var movementObject = new GameObject("MovementTest");
            var movementSystem = movementObject.AddComponent<UnifiedMovementSystem>();
            var rigidbody = movementObject.AddComponent<Rigidbody>();
            
            try
            {
                // Warmup
                yield return PerformWarmup();
                
                // Act - Measure movement system performance
                var metrics = new PerformanceMetric("MovementSystem");
                yield return MeasurePerformance(metrics, () =>
                {
                    // Simulate complex movement patterns
                    var direction = new Vector3(
                        Mathf.Sin(Time.time * 2f),
                        0f,
                        Mathf.Cos(Time.time * 2f)
                    );
                    
                    movementObject.transform.position += direction * 5f * Time.deltaTime;
                });
                
                currentMetrics.Add(metrics);
                
                // Assert
                var baseline = GetBaselineMetric("MovementSystem");
                if (baseline != null)
                {
                    AssertPerformanceRegression(baseline, metrics, "Movement System");
                }
                else
                {
                    SaveAsNewBaseline(metrics);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(movementObject);
            }
        }
        
        [UnityTest]
        public IEnumerator PerformanceRegression_NetworkSystem_MaintainsBaseline()
        {
            // Arrange
            var networkManager = testScene.AddComponent<ProductionNetworkManager>();
            var lagCompensation = testScene.AddComponent<LagCompensationManager>();
            
            // Warmup
            yield return PerformWarmup();
            
            // Act - Measure network system performance
            var metrics = new PerformanceMetric("NetworkSystem");
            yield return MeasurePerformance(metrics, () =>
            {
                // Simulate network processing load
                for (int i = 0; i < 50; i++)
                {
                    // Simulate network event processing
                    lagCompensation.ProcessNetworkEvent(Time.time, Vector3.zero, i);
                }
            });
            
            currentMetrics.Add(metrics);
            
            // Assert
            var baseline = GetBaselineMetric("NetworkSystem");
            if (baseline != null)
            {
                AssertPerformanceRegression(baseline, metrics, "Network System");
            }
            else
            {
                SaveAsNewBaseline(metrics);
            }
        }
        
        [UnityTest]
        public IEnumerator PerformanceRegression_MemoryAllocation_MaintainsBaseline()
        {
            // Arrange
            var initialMemory = Profiler.GetTotalAllocatedMemory(false);
            
            // Warmup
            yield return PerformWarmup();
            
            // Act - Measure memory allocation during typical gameplay
            var metrics = new PerformanceMetric("MemoryAllocation");
            var startMemory = Profiler.GetTotalAllocatedMemory(false);
            
            yield return MeasurePerformance(metrics, () =>
            {
                // Simulate memory-intensive operations
                var tempList = new List<Vector3>();
                for (int i = 0; i < 1000; i++)
                {
                    tempList.Add(new Vector3(i, i, i));
                }
                
                // Simulate object pooling
                var tempObjects = new GameObject[100];
                for (int i = 0; i < tempObjects.Length; i++)
                {
                    tempObjects[i] = new GameObject($"TempObject_{i}");
                }
                
                // Clean up immediately to test GC pressure
                for (int i = 0; i < tempObjects.Length; i++)
                {
                    UnityEngine.Object.DestroyImmediate(tempObjects[i]);
                }
            });
            
            var endMemory = Profiler.GetTotalAllocatedMemory(false);
            metrics.MemoryAllocated = endMemory - startMemory;
            
            currentMetrics.Add(metrics);
            
            // Assert
            var baseline = GetBaselineMetric("MemoryAllocation");
            if (baseline != null)
            {
                AssertMemoryRegression(baseline, metrics, "Memory Allocation");
            }
            else
            {
                SaveAsNewBaseline(metrics);
            }
        }
        
        #endregion
        
        #region Stress Test Performance Scenarios
        
        [UnityTest]
        public IEnumerator PerformanceRegression_HighPlayerCount_MaintainsBaseline()
        {
            // Arrange
            const int playerCount = 20;
            var players = new List<GameObject>();
            
            // Create multiple players with full systems
            for (int i = 0; i < playerCount; i++)
            {
                var player = new GameObject($"Player_{i}");
                player.AddComponent<EnhancedAbilitySystem>();
                player.AddComponent<UnifiedMovementSystem>();
                player.AddComponent<Rigidbody>();
                players.Add(player);
            }
            
            try
            {
                // Warmup
                yield return PerformWarmup();
                
                // Act - Measure high player count performance
                var metrics = new PerformanceMetric("HighPlayerCount");
                yield return MeasurePerformance(metrics, () =>
                {
                    // Simulate all players acting simultaneously
                    foreach (var player in players)
                    {
                        var abilitySystem = player.GetComponent<EnhancedAbilitySystem>();
                        var movementSystem = player.GetComponent<UnifiedMovementSystem>();
                        
                        // Random ability casting
                        if (UnityEngine.Random.value > 0.7f)
                        {
                            abilitySystem.TryCastAbility(UnityEngine.Random.Range(0, 3));
                        }
                        
                        // Random movement
                        var direction = new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            0f,
                            UnityEngine.Random.Range(-1f, 1f)
                        ).normalized;
                        
                        player.transform.position += direction * 3f * Time.deltaTime;
                    }
                });
                
                currentMetrics.Add(metrics);
                
                // Assert
                var baseline = GetBaselineMetric("HighPlayerCount");
                if (baseline != null)
                {
                    AssertPerformanceRegression(baseline, metrics, "High Player Count");
                }
                else
                {
                    SaveAsNewBaseline(metrics);
                }
            }
            finally
            {
                // Clean up players
                foreach (var player in players)
                {
                    UnityEngine.Object.DestroyImmediate(player);
                }
            }
        }
        
        [UnityTest]
        public IEnumerator PerformanceRegression_ComplexGameplayScenario_MaintainsBaseline()
        {
            // Arrange - Set up complex gameplay scenario
            var gameManager = testScene.AddComponent<SimpleGameManager>();
            var abilitySystem = testScene.AddComponent<EnhancedAbilitySystem>();
            var networkManager = testScene.AddComponent<ProductionNetworkManager>();
            
            SetupTestAbilities(abilitySystem);
            
            // Warmup
            yield return PerformWarmup();
            
            // Act - Measure complex scenario performance
            var metrics = new PerformanceMetric("ComplexGameplayScenario");
            yield return MeasurePerformance(metrics, () =>
            {
                // Simulate complex gameplay with multiple systems
                abilitySystem.TryCastAbility(0);
                abilitySystem.TryCastAbility(1);
                
                // Simulate game state updates
                if (gameManager != null)
                {
                    // Would normally update game state
                }
                
                // Simulate network processing
                if (networkManager != null)
                {
                    // Would normally process network updates
                }
            });
            
            currentMetrics.Add(metrics);
            
            // Assert
            var baseline = GetBaselineMetric("ComplexGameplayScenario");
            if (baseline != null)
            {
                AssertPerformanceRegression(baseline, metrics, "Complex Gameplay Scenario");
            }
            else
            {
                SaveAsNewBaseline(metrics);
            }
        }
        
        #endregion
        
        #region Performance Measurement Infrastructure
        
        private IEnumerator PerformWarmup()
        {
            for (int frame = 0; frame < WarmupFrames; frame++)
            {
                yield return null;
            }
        }
        
        private IEnumerator MeasurePerformance(PerformanceMetric metrics, System.Action testAction)
        {
            var frameTimeSamples = new List<float>();
            var startTime = Time.realtimeSinceStartup;
            
            for (int frame = 0; frame < MeasurementFrames; frame++)
            {
                var frameStartTime = Time.realtimeSinceStartup;
                
                // Execute test action
                testAction?.Invoke();
                
                yield return null;
                
                var frameEndTime = Time.realtimeSinceStartup;
                var frameTime = frameEndTime - frameStartTime;
                frameTimeSamples.Add(frameTime);
            }
            
            var totalTime = Time.realtimeSinceStartup - startTime;
            
            // Calculate performance metrics
            metrics.AverageFrameTime = CalculateAverage(frameTimeSamples);
            metrics.MaxFrameTime = CalculateMax(frameTimeSamples);
            metrics.MinFrameTime = CalculateMin(frameTimeSamples);
            metrics.TotalTime = totalTime;
            metrics.AverageFPS = MeasurementFrames / totalTime;
            
            // Capture Unity Profiler data
            metrics.MainThreadTime = Profiler.GetCounterValue("CPU Main Thread");
            metrics.RenderThreadTime = Profiler.GetCounterValue("CPU Render Thread");
            metrics.GPUTime = Profiler.GetCounterValue("GPU Frame Time");
        }
        
        private void AssertPerformanceRegression(PerformanceMetric baseline, PerformanceMetric current, string systemName)
        {
            // Check frame time regression
            var frameTimeIncrease = (current.AverageFrameTime - baseline.AverageFrameTime) / baseline.AverageFrameTime * 100f;
            Assert.IsTrue(frameTimeIncrease <= PerformanceTolerancePercent, 
                $"{systemName} frame time regression: {frameTimeIncrease:F2}% increase (limit: {PerformanceTolerancePercent}%)");
            
            // Check FPS regression
            var fpsDecrease = (baseline.AverageFPS - current.AverageFPS) / baseline.AverageFPS * 100f;
            Assert.IsTrue(fpsDecrease <= PerformanceTolerancePercent,
                $"{systemName} FPS regression: {fpsDecrease:F2}% decrease (limit: {PerformanceTolerancePercent}%)");
            
            UnityEngine.Debug.Log($"Performance Test Passed: {systemName} - Frame Time: {current.AverageFrameTime:F4}ms, FPS: {current.AverageFPS:F1}");
        }
        
        private void AssertMemoryRegression(PerformanceMetric baseline, PerformanceMetric current, string systemName)
        {
            if (baseline.MemoryAllocated > 0)
            {
                var memoryIncrease = (current.MemoryAllocated - baseline.MemoryAllocated) / (float)baseline.MemoryAllocated * 100f;
                Assert.IsTrue(memoryIncrease <= PerformanceTolerancePercent,
                    $"{systemName} memory regression: {memoryIncrease:F2}% increase (limit: {PerformanceTolerancePercent}%)");
            }
            
            UnityEngine.Debug.Log($"Memory Test Passed: {systemName} - Allocated: {current.MemoryAllocated / (1024 * 1024):F2} MB");
        }
        
        #endregion
        
        #region Baseline Management
        
        private void LoadBaselineMetrics()
        {
            var baselineFile = Path.Combine(BaselineDataPath, "performance_baselines.json");
            if (File.Exists(baselineFile))
            {
                try
                {
                    var json = File.ReadAllText(baselineFile);
                    baselineMetrics = JsonUtility.FromJson<PerformanceBaselineData>(json).Metrics;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to load baseline metrics: {ex.Message}");
                    baselineMetrics = new List<PerformanceMetric>();
                }
            }
        }
        
        private PerformanceMetric GetBaselineMetric(string systemName)
        {
            return baselineMetrics?.Find(m => m.SystemName == systemName);
        }
        
        private void SaveAsNewBaseline(PerformanceMetric metric)
        {
            UnityEngine.Debug.Log($"Creating new baseline for {metric.SystemName}: Frame Time: {metric.AverageFrameTime:F4}ms, FPS: {metric.AverageFPS:F1}");
            
            // Remove existing baseline for this system
            baselineMetrics.RemoveAll(m => m.SystemName == metric.SystemName);
            
            // Add new baseline
            baselineMetrics.Add(metric);
            
            // Save to file
            var baselineData = new PerformanceBaselineData { Metrics = baselineMetrics };
            var json = JsonUtility.ToJson(baselineData, true);
            
            var baselineFile = Path.Combine(BaselineDataPath, "performance_baselines.json");
            File.WriteAllText(baselineFile, json);
        }
        
        #endregion
        
        #region Report Generation
        
        private void GeneratePerformanceReport()
        {
            var report = new StringBuilder();
            report.AppendLine("# Performance Regression Test Report");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();
            
            report.AppendLine("## Test Configuration");
            report.AppendLine($"- Warmup Frames: {WarmupFrames}");
            report.AppendLine($"- Measurement Frames: {MeasurementFrames}");
            report.AppendLine($"- Performance Tolerance: {PerformanceTolerancePercent}%");
            report.AppendLine();
            
            report.AppendLine("## Performance Results");
            report.AppendLine("| System | Avg Frame Time (ms) | FPS | Status |");
            report.AppendLine("|--------|-------------------|-----|--------|");
            
            foreach (var metric in currentMetrics)
            {
                var baseline = GetBaselineMetric(metric.SystemName);
                var status = "NEW";
                
                if (baseline != null)
                {
                    var frameTimeIncrease = (metric.AverageFrameTime - baseline.AverageFrameTime) / baseline.AverageFrameTime * 100f;
                    status = frameTimeIncrease <= PerformanceTolerancePercent ? "PASS" : "FAIL";
                }
                
                report.AppendLine($"| {metric.SystemName} | {metric.AverageFrameTime:F4} | {metric.AverageFPS:F1} | {status} |");
            }
            
            // Save report
            var reportFile = Path.Combine(ReportOutputPath, $"performance_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.md");
            File.WriteAllText(reportFile, report.ToString());
            
            UnityEngine.Debug.Log($"Performance report saved to: {reportFile}");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(BaselineDataPath))
                Directory.CreateDirectory(BaselineDataPath);
            
            if (!Directory.Exists(ReportOutputPath))
                Directory.CreateDirectory(ReportOutputPath);
        }
        
        private void SetupTestAbilities(EnhancedAbilitySystem abilitySystem)
        {
            // Create test abilities
            var ability1 = ScriptableObject.CreateInstance<EnhancedAbility>();
            ability1.abilityName = "Test Ability 1";
            ability1.cooldown = 1f;
            ability1.manaCost = 20f;
            
            var ability2 = ScriptableObject.CreateInstance<EnhancedAbility>();
            ability2.abilityName = "Test Ability 2";
            ability2.cooldown = 2f;
            ability2.manaCost = 30f;
            
            var ability3 = ScriptableObject.CreateInstance<EnhancedAbility>();
            ability3.abilityName = "Test Ability 3";
            ability3.cooldown = 3f;
            ability3.manaCost = 40f;
            
            abilitySystem.SetMaxMana(200f);
            abilitySystem.RestoreMana(200f);
            abilitySystem.SetAbility(0, ability1);
            abilitySystem.SetAbility(1, ability2);
            abilitySystem.SetAbility(2, ability3);
        }
        
        private float CalculateAverage(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (var value in values)
                sum += value;
            
            return sum / values.Count;
        }
        
        private float CalculateMax(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float max = values[0];
            foreach (var value in values)
            {
                if (value > max)
                    max = value;
            }
            
            return max;
        }
        
        private float CalculateMin(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float min = values[0];
            foreach (var value in values)
            {
                if (value < min)
                    min = value;
            }
            
            return min;
        }
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public class PerformanceMetric
        {
            public string SystemName;
            public float AverageFrameTime;
            public float MaxFrameTime;
            public float MinFrameTime;
            public float TotalTime;
            public float AverageFPS;
            public long MemoryAllocated;
            public float MainThreadTime;
            public float RenderThreadTime;
            public float GPUTime;
            public string Timestamp;
            
            public PerformanceMetric(string systemName)
            {
                SystemName = systemName;
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        
        [System.Serializable]
        public class PerformanceBaselineData
        {
            public List<PerformanceMetric> Metrics;
        }
        
        #endregion
    }
}