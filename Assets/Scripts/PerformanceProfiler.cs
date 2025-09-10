using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;
using System.Diagnostics;

namespace MOBA
{
    /// <summary>
    /// Performance profiling system for MOBA game optimization
    /// Integrates with Unity Profiler and provides custom performance metrics
    /// Based on Game Programming Patterns performance considerations
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        [Header("Profiling Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private bool logToConsole = false;

        [Header("Performance Thresholds")]
        [SerializeField] private float targetFrameTime = 16.67f; // 60fps
        [SerializeField] private int targetMemoryMB = 512;
        [SerializeField] private float maxAllocationRate = 10f; // KB per frame

        // Profiler markers
        private static readonly ProfilerMarker abilityCastMarker = new("MOBA.AbilityCast");
        private static readonly ProfilerMarker projectileUpdateMarker = new("MOBA.ProjectileUpdate");
        private static readonly ProfilerMarker stateMachineMarker = new("MOBA.StateMachine");
        private static readonly ProfilerMarker damageCalculationMarker = new("MOBA.DamageCalculation");

        // Performance tracking
        private float lastUpdateTime;
        private float lastFrameWarningTime;
        private float lastMemoryWarningTime;
        private float lastAllocationWarningTime;
        private FrameTiming[] frameTimings = new FrameTiming[1];
        private Dictionary<string, PerformanceMetric> metrics = new();

        // Memory tracking
        private long lastMemoryUsage;
        private float totalAllocatedThisFrame;

        private void Awake()
        {
            InitializeMetrics();
        }

        private void InitializeMetrics()
        {
            metrics["FrameTime"] = new PerformanceMetric("Frame Time", "ms", targetFrameTime);
            metrics["MemoryUsage"] = new PerformanceMetric("Memory Usage", "MB", targetMemoryMB);
            metrics["AllocationRate"] = new PerformanceMetric("Allocation Rate", "KB/frame", maxAllocationRate);
            metrics["AbilityCasts"] = new PerformanceMetric("Ability Casts", "per sec", 10f);
            metrics["ProjectileCount"] = new PerformanceMetric("Active Projectiles", "count", 50f);
            metrics["StateTransitions"] = new PerformanceMetric("State Transitions", "per sec", 20f);
        }

        private void Update()
        {
            if (!enableProfiling) return;

            // Update performance metrics
            UpdateFrameTime();
            UpdateMemoryUsage();
            UpdateAllocationRate();

            // Periodic detailed profiling
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                PerformDetailedProfiling();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateFrameTime()
        {
            FrameTimingManager.CaptureFrameTimings();
            uint numFrames = FrameTimingManager.GetLatestTimings(1, frameTimings);
            
            if (numFrames > 0)
            {
                float frameTime = (float)frameTimings[0].cpuFrameTime;
                metrics["FrameTime"].Update(frameTime);

                // Only log warnings every 2 seconds to reduce spam
                if (frameTime > targetFrameTime * 1.5f && Time.time - lastFrameWarningTime > 2.0f)
                {
                    LogPerformanceWarning($"High frame time: {frameTime:F2}ms (target: {targetFrameTime}ms)");
                    lastFrameWarningTime = Time.time;
                }
            }
        }        private void UpdateMemoryUsage()
        {
            long currentMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            float memoryMB = currentMemory / (1024f * 1024f);
            metrics["MemoryUsage"].Update(memoryMB);

            // Only log memory warnings every 5 seconds to reduce spam
            if (memoryMB > targetMemoryMB && Time.time - lastMemoryWarningTime > 5.0f)
            {
                LogPerformanceWarning($"High memory usage: {memoryMB:F1}MB (target: {targetMemoryMB}MB)");
                lastMemoryWarningTime = Time.time;
            }

            lastMemoryUsage = currentMemory;
        }

        private void UpdateAllocationRate()
        {
            long currentMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            long allocatedThisFrame = currentMemory - lastMemoryUsage;

            float allocationKB = allocatedThisFrame / 1024f;
            metrics["AllocationRate"].Update(allocationKB);

            // Only log allocation warnings every 3 seconds to reduce spam
            if (allocationKB > maxAllocationRate && Time.time - lastAllocationWarningTime > 3.0f)
            {
                LogPerformanceWarning($"High allocation rate: {allocationKB:F1}KB/frame (target: {maxAllocationRate}KB/frame)");
                lastAllocationWarningTime = Time.time;
            }
        }

        private void PerformDetailedProfiling()
        {
            // Profile object pools
            ProfileObjectPools();

            // Profile ability system
            ProfileAbilitySystem();

            // Profile projectile system
            ProfileProjectileSystem();

            // Profile state machine
            ProfileStateMachine();

            // Log summary
            if (logToConsole)
            {
                LogPerformanceSummary();
            }
        }

        private void ProfileObjectPools()
        {
            // Profile ProjectilePool - access the pool directly instead of using GetComponent
            var projectilePool = FindAnyObjectByType<ProjectilePool>();
            if (projectilePool != null)
            {
                // ProjectilePool should have a method to get active count
                try
                {
                    // Use reflection to safely access the pool if needed
                    var poolField = typeof(ProjectilePool).GetField("projectilePool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (poolField != null)
                    {
                        var pool = poolField.GetValue(projectilePool);
                        if (pool != null)
                        {
                            var activeCountProperty = pool.GetType().GetProperty("ActiveCount");
                            if (activeCountProperty != null)
                            {
                                int activeCount = (int)activeCountProperty.GetValue(pool);
                                metrics["ProjectileCount"].Update(activeCount);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    // Fallback - just update with 0 if we can't access the pool
                    metrics["ProjectileCount"].Update(0);
                    UnityEngine.Debug.LogWarning($"[PerformanceProfiler] Could not access ProjectilePool count: {e.Message}");
                }
            }
            else
            {
                metrics["ProjectileCount"].Update(0);
            }
        }

        private void ProfileAbilitySystem()
        {
            var abilitySystem = FindAnyObjectByType<AbilitySystem>();
            if (abilitySystem != null)
            {
                // Track ability cast frequency (would need to be implemented in AbilitySystem)
                // metrics["AbilityCasts"].Update(abilitySystem.GetCastsPerSecond());
            }
        }

        private void ProfileProjectileSystem()
        {
            using (projectileUpdateMarker.Auto())
            {
                var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var projectile in projectiles)
                {
                    // Profile individual projectile updates
                    // This would be more detailed in a full implementation
                }
            }
        }

        private void ProfileStateMachine()
        {
            using (stateMachineMarker.Auto())
            {
                var controllers = Object.FindObjectsByType<MOBACharacterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var controller in controllers)
                {
                    // Profile state machine updates through controller
                    // Track transition frequency
                    // State machine profiling would be implemented here
                }
            }
        }

        /// <summary>
        /// Profiles ability casting performance
        /// </summary>
        public static void ProfileAbilityCast(System.Action abilityAction)
        {
            using (abilityCastMarker.Auto())
            {
                abilityAction?.Invoke();
            }
        }

        /// <summary>
        /// Profiles damage calculation performance
        /// </summary>
        public static void ProfileDamageCalculation(System.Action damageAction)
        {
            using (damageCalculationMarker.Auto())
            {
                damageAction?.Invoke();
            }
        }

        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        public Dictionary<string, (float current, float target, bool isWarning)> GetMetrics()
        {
            var result = new Dictionary<string, (float, float, bool)>();

            foreach (var kvp in metrics)
            {
                var metric = kvp.Value;
                bool isWarning = metric.Current > metric.Target * 1.2f;
                result[kvp.Key] = (metric.Current, metric.Target, isWarning);
            }

            return result;
        }

        /// <summary>
        /// Logs a performance warning
        /// </summary>
        private void LogPerformanceWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"[PerformanceProfiler] {message}");
        }

        /// <summary>
        /// Logs performance summary
        /// </summary>
        private void LogPerformanceSummary()
        {
            UnityEngine.Debug.Log("[PerformanceProfiler] === Performance Summary ===");

            foreach (var kvp in GetMetrics())
            {
                var (current, target, isWarning) = kvp.Value;
                string status = isWarning ? "⚠️" : "✅";
                UnityEngine.Debug.Log($"{status} {kvp.Key}: {current:F2} / {target:F2}");
            }
        }

        private void OnGUI()
        {
            if (!enableProfiling || !Application.isEditor) return;

            GUI.Label(new Rect(10, 100, 300, 20), "=== Performance Profiler ===");

            int y = 120;
            foreach (var kvp in GetMetrics())
            {
                var (current, target, isWarning) = kvp.Value;
                string color = isWarning ? "red" : "green";
                GUI.Label(new Rect(10, y, 400, 20), $"{kvp.Key}: {current:F2} / {target:F2}");
                y += 20;
            }
        }

        /// <summary>
        /// Performance metric data structure
        /// </summary>
        private class PerformanceMetric
        {
            public string Name { get; }
            public string Unit { get; }
            public float Target { get; }
            public float Current { get; private set; }
            public float Min { get; private set; }
            public float Max { get; private set; }
            public float Average { get; private set; }

            private float sum;
            private int count;

            public PerformanceMetric(string name, string unit, float target)
            {
                Name = name;
                Unit = unit;
                Target = target;
                Min = float.MaxValue;
                Max = float.MinValue;
                Current = 0f;
                Average = 0f;
                sum = 0f;
                count = 0;
            }

            public void Update(float value)
            {
                Current = value;
                Min = Mathf.Min(Min, value);
                Max = Mathf.Max(Max, value);

                sum += value;
                count++;
                Average = sum / count;
            }

            public void Reset()
            {
                Min = float.MaxValue;
                Max = float.MinValue;
                Current = 0f;
                Average = 0f;
                sum = 0f;
                count = 0;
            }
        }
    }

    /// <summary>
    /// Static helper class for easy profiling integration
    /// </summary>
    public static class PerformanceProfiling
    {
        public static void ProfileMethod(string methodName, System.Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action?.Invoke();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 16) // More than one frame at 60fps
            {
                UnityEngine.Debug.LogWarning($"[Performance] {methodName} took {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        public static T ProfileMethod<T>(string methodName, System.Func<T> action) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            var result = action?.Invoke();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 16)
            {
                UnityEngine.Debug.LogWarning($"[Performance] {methodName} took {stopwatch.ElapsedMilliseconds}ms");
            }

            return result;
        }
    }
}