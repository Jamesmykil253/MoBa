using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Diagnostics;
using UnityEngine.Profiling;
using MOBA.Debugging;

namespace MOBA.Performance
{
    /// <summary>
    /// Interface for object pools to report metrics
    /// </summary>
    public interface IObjectPool
    {
        int ActiveCount { get; }
        int TotalCount { get; }
    }
    
    /// <summary>
    /// Enhanced performance profiling system with comprehensive monitoring capabilities.
    /// Tracks memory allocation, network bandwidth, frame times, and object pool efficiency.
    /// Provides real-time performance metrics and automated optimization suggestions.
    /// </summary>
    public class EnhancedPerformanceProfiler : MonoBehaviour
    {
        public static EnhancedPerformanceProfiler Instance { get; private set; }

        #region Configuration
        
        [Header("Profiling Configuration")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool enableMemoryTracking = true;
        [SerializeField] private bool enableNetworkBandwidthMonitoring = true;
        [SerializeField] private bool enableFrameTimeAnalysis = true;
        [SerializeField] private bool enableObjectPoolMonitoring = true;
        [SerializeField] private bool enableAutomatedOptimizations = false;
        
        [Header("Monitoring Intervals")]
        [SerializeField, Range(0.1f, 5f)] private float memoryCheckInterval = 1f;
        [SerializeField, Range(0.1f, 2f)] private float networkCheckInterval = 0.5f;
        [SerializeField, Range(0.016f, 1f)] private float frameTimeCheckInterval = 0.1f;
        [SerializeField, Range(1f, 10f)] private float poolCheckInterval = 2f;
        
        [Header("Performance Thresholds")]
        [SerializeField] private long memoryWarningThreshold = 500_000_000; // 500MB
        [SerializeField] private float frameTimeWarningThreshold = 33.33f; // 30 FPS
        [SerializeField] private float networkBandwidthWarningThreshold = 1000; // 1KB/s
        [SerializeField] private float poolEfficiencyWarningThreshold = 0.5f; // 50%
        
        #endregion
        
        #region Performance Metrics
        
        private readonly Dictionary<string, MemoryMetrics> memoryMetrics = new Dictionary<string, MemoryMetrics>();
        private readonly Dictionary<string, NetworkBandwidthMetrics> networkMetrics = new Dictionary<string, NetworkBandwidthMetrics>();
        private readonly Dictionary<string, FrameTimeMetrics> frameTimeMetrics = new Dictionary<string, FrameTimeMetrics>();
        private readonly Dictionary<string, ObjectPoolMetrics> poolMetrics = new Dictionary<string, ObjectPoolMetrics>();
        
        private readonly CircularBuffer<float> frameTimeHistory = new CircularBuffer<float>(120); // 2 seconds at 60 FPS
        private readonly CircularBuffer<long> memoryHistory = new CircularBuffer<long>(60); // 1 minute at 1 sample/second
        
        #endregion
        
        #region Timing State
        
        private float lastMemoryCheck = 0f;
        private float lastNetworkCheck = 0f;
        private float lastFrameTimeCheck = 0f;
        private float lastPoolCheck = 0f;
        
        private long lastFrameMemory = 0L;
        private long lastNetworkBytes = 0L;
        private float lastFrameTime = 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when performance warning is detected. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<PerformanceWarning> OnPerformanceWarning;
        
        /// <summary>
        /// Raised when performance metrics are updated. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<PerformanceSnapshot> OnMetricsUpdated;
        
        #endregion
        
        #region Initialization
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (enableProfiling)
            {
                GameDebug.Log(BuildContext(),
                    "Enhanced Performance Profiler initialized.",
                    ("MemoryTracking", enableMemoryTracking.ToString()),
                    ("NetworkMonitoring", enableNetworkBandwidthMonitoring.ToString()),
                    ("FrameTimeAnalysis", enableFrameTimeAnalysis.ToString()),
                    ("ObjectPoolMonitoring", enableObjectPoolMonitoring.ToString()));
            }
        }
        
        private void Update()
        {
            if (!enableProfiling) return;
            
            float currentTime = Time.unscaledTime;
            
            // Memory allocation tracking
            if (enableMemoryTracking && currentTime - lastMemoryCheck >= memoryCheckInterval)
            {
                TrackMemoryAllocation();
                lastMemoryCheck = currentTime;
            }
            
            // Network bandwidth monitoring
            if (enableNetworkBandwidthMonitoring && currentTime - lastNetworkCheck >= networkCheckInterval)
            {
                TrackNetworkBandwidth();
                lastNetworkCheck = currentTime;
            }
            
            // Frame time analysis
            if (enableFrameTimeAnalysis && currentTime - lastFrameTimeCheck >= frameTimeCheckInterval)
            {
                TrackFrameTime();
                lastFrameTimeCheck = currentTime;
            }
            
            // Object pool efficiency
            if (enableObjectPoolMonitoring && currentTime - lastPoolCheck >= poolCheckInterval)
            {
                TrackObjectPoolEfficiency();
                lastPoolCheck = currentTime;
            }
        }
        
        #endregion
        
        #region Memory Tracking
        
        private void TrackMemoryAllocation()
        {
            long currentMemory = System.GC.GetTotalMemory(false);
            long allocatedMemory = currentMemory - lastFrameMemory;
            
            memoryHistory.Add(currentMemory);
            
            var metrics = GetOrCreateMemoryMetrics("System");
            metrics.UpdateMemoryUsage(currentMemory, allocatedMemory);
            
            // Unity-specific memory tracking
            long unityMemory = Profiler.GetTotalAllocatedMemoryLong();
            var unityMetrics = GetOrCreateMemoryMetrics("Unity");
            unityMetrics.UpdateMemoryUsage(unityMemory, 0);
            
            lastFrameMemory = currentMemory;
            
            // Check for memory warnings
            if (currentMemory > memoryWarningThreshold)
            {
                RaisePerformanceWarning(PerformanceWarningType.HighMemoryUsage, 
                    $"Memory usage exceeds threshold: {currentMemory / 1024 / 1024}MB");
            }
        }
        
        private MemoryMetrics GetOrCreateMemoryMetrics(string name)
        {
            if (!memoryMetrics.TryGetValue(name, out var metrics))
            {
                metrics = new MemoryMetrics();
                memoryMetrics[name] = metrics;
            }
            return metrics;
        }
        
        #endregion
        
        #region Network Bandwidth Monitoring
        
        private void TrackNetworkBandwidth()
        {
            if (NetworkManager.Singleton == null) return;
            
            // Track bytes sent/received (approximation using NetworkManager stats)
            var networkManager = NetworkManager.Singleton;
            
            // Get current network statistics
            long currentNetworkBytes = 0; // TODO: Implement actual network byte tracking
            
            var metrics = GetOrCreateNetworkMetrics("Global");
            long bandwidthDelta = currentNetworkBytes - lastNetworkBytes;
            float bandwidth = bandwidthDelta / networkCheckInterval;
            
            metrics.UpdateBandwidth(bandwidth, currentNetworkBytes);
            
            lastNetworkBytes = currentNetworkBytes;
            
            // Check for bandwidth warnings
            if (bandwidth > networkBandwidthWarningThreshold)
            {
                RaisePerformanceWarning(PerformanceWarningType.HighNetworkBandwidth,
                    $"Network bandwidth usage high: {bandwidth:F2} bytes/sec");
            }
        }
        
        private NetworkBandwidthMetrics GetOrCreateNetworkMetrics(string name)
        {
            if (!networkMetrics.TryGetValue(name, out var metrics))
            {
                metrics = new NetworkBandwidthMetrics();
                networkMetrics[name] = metrics;
            }
            return metrics;
        }
        
        #endregion
        
        #region Frame Time Analysis
        
        private void TrackFrameTime()
        {
            float currentFrameTime = Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
            frameTimeHistory.Add(currentFrameTime);
            
            var metrics = GetOrCreateFrameTimeMetrics("Global");
            metrics.UpdateFrameTime(currentFrameTime);
            
            // Check for frame time warnings
            if (currentFrameTime > frameTimeWarningThreshold)
            {
                RaisePerformanceWarning(PerformanceWarningType.LowFrameRate,
                    $"Frame time exceeds threshold: {currentFrameTime:F2}ms");
            }
            
            // Check for significant frame time changes
            if (lastFrameTime > 0f && Mathf.Abs(currentFrameTime - lastFrameTime) > frameTimeWarningThreshold * 0.5f)
            {
                RaisePerformanceWarning(PerformanceWarningType.LowFrameRate,
                    $"Frame time spike detected: {currentFrameTime:F2}ms (was {lastFrameTime:F2}ms)");
            }
            
            lastFrameTime = currentFrameTime;
        }
        
        private FrameTimeMetrics GetOrCreateFrameTimeMetrics(string name)
        {
            if (!frameTimeMetrics.TryGetValue(name, out var metrics))
            {
                metrics = new FrameTimeMetrics();
                frameTimeMetrics[name] = metrics;
            }
            return metrics;
        }
        
        #endregion
        
        #region Object Pool Monitoring
        
        private void TrackObjectPoolEfficiency()
        {
            // Monitor object pools in the scene (find MonoBehaviour components that implement pooling)
            var monoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            
            foreach (var behaviour in monoBehaviours)
            {
                // Check if this MonoBehaviour has pooling capabilities
                if (behaviour.GetType().Name.Contains("Pool"))
                {
                    var poolName = behaviour.name;
                    var metrics = GetOrCreatePoolMetrics(poolName);
                    
                    // Try to get pool statistics via reflection or interface
                    // For now, use placeholder values - this would need to be customized per pool implementation
                    int activeCount = 0;
                    int totalCount = 1;
                    
                    // Try to get actual values if the pool implements a known interface
                    if (behaviour is IObjectPool poolInterface)
                    {
                        activeCount = poolInterface.ActiveCount;
                        totalCount = poolInterface.TotalCount;
                    }
                    
                    float efficiency = totalCount > 0 ? (float)activeCount / totalCount : 0f;
                    metrics.UpdateEfficiency(efficiency, activeCount, totalCount);
                    
                    // Check for pool efficiency warnings
                    if (efficiency < poolEfficiencyWarningThreshold && totalCount > 0)
                    {
                        RaisePerformanceWarning(PerformanceWarningType.LowPoolEfficiency,
                            $"Pool '{poolName}' efficiency low: {efficiency:P1}");
                    }
                }
            }
        }
        
        private ObjectPoolMetrics GetOrCreatePoolMetrics(string name)
        {
            if (!poolMetrics.TryGetValue(name, out var metrics))
            {
                metrics = new ObjectPoolMetrics();
                poolMetrics[name] = metrics;
            }
            return metrics;
        }
        
        #endregion
        
        #region Performance Analysis
        
        /// <summary>
        /// Get a comprehensive performance snapshot
        /// </summary>
        public PerformanceSnapshot GetPerformanceSnapshot()
        {
            return new PerformanceSnapshot
            {
                Timestamp = Time.unscaledTime,
                MemoryMetrics = new Dictionary<string, MemoryMetrics>(memoryMetrics),
                NetworkMetrics = new Dictionary<string, NetworkBandwidthMetrics>(networkMetrics),
                FrameTimeMetrics = new Dictionary<string, FrameTimeMetrics>(frameTimeMetrics),
                PoolMetrics = new Dictionary<string, ObjectPoolMetrics>(poolMetrics),
                CurrentFPS = 1f / Time.unscaledDeltaTime,
                AverageFrameTime = GetAverageFrameTime(),
                TotalMemoryUsage = System.GC.GetTotalMemory(false)
            };
        }
        
        /// <summary>
        /// Get performance recommendations based on current metrics
        /// </summary>
        public List<string> GetPerformanceRecommendations()
        {
            var recommendations = new List<string>();
            
            // Memory recommendations
            long currentMemory = System.GC.GetTotalMemory(false);
            if (currentMemory > memoryWarningThreshold)
            {
                recommendations.Add("Consider calling System.GC.Collect() or reducing memory allocations");
            }
            
            // Frame rate recommendations
            float avgFrameTime = GetAverageFrameTime();
            if (avgFrameTime > frameTimeWarningThreshold)
            {
                recommendations.Add("Frame rate is low - consider reducing visual quality or optimizing scripts");
            }
            
            // Object pool recommendations
            foreach (var kvp in poolMetrics)
            {
                var metrics = kvp.Value;
                if (metrics.LatestEfficiency < poolEfficiencyWarningThreshold)
                {
                    recommendations.Add($"Pool '{kvp.Key}' has low efficiency - consider adjusting pool size");
                }
            }
            
            return recommendations;
        }
        
        private float GetAverageFrameTime()
        {
            if (frameTimeHistory.Count == 0) return 0f;
            
            float sum = 0f;
            for (int i = 0; i < frameTimeHistory.Count; i++)
            {
                sum += frameTimeHistory[i];
            }
            return sum / frameTimeHistory.Count;
        }
        
        #endregion
        
        #region Warning System
        
        private void RaisePerformanceWarning(PerformanceWarningType type, string message)
        {
            var warning = new PerformanceWarning
            {
                Type = type,
                Message = message,
                Timestamp = Time.unscaledTime,
                Severity = GetWarningSeverity(type)
            };
            
            OnPerformanceWarning?.Invoke(warning);
            
            GameDebug.LogWarning(BuildContext(),
                $"Performance Warning: {message}",
                ("Type", type.ToString()),
                ("Severity", warning.Severity.ToString()));
                
            // Apply automated optimizations if enabled
            if (enableAutomatedOptimizations)
            {
                ApplyAutomatedOptimization(type);
            }
        }
        
        private PerformanceWarningSeverity GetWarningSeverity(PerformanceWarningType type)
        {
            return type switch
            {
                PerformanceWarningType.HighMemoryUsage => PerformanceWarningSeverity.High,
                PerformanceWarningType.LowFrameRate => PerformanceWarningSeverity.High,
                PerformanceWarningType.HighNetworkBandwidth => PerformanceWarningSeverity.Medium,
                PerformanceWarningType.LowPoolEfficiency => PerformanceWarningSeverity.Low,
                _ => PerformanceWarningSeverity.Medium
            };
        }
        
        private void ApplyAutomatedOptimization(PerformanceWarningType type)
        {
            switch (type)
            {
                case PerformanceWarningType.HighMemoryUsage:
                    System.GC.Collect();
                    GameDebug.Log(BuildContext(), "Applied automated optimization: Garbage Collection");
                    break;
                    
                case PerformanceWarningType.LowFrameRate:
                    // Could reduce quality settings, disable non-essential effects, etc.
                    GameDebug.Log(BuildContext(), "Frame rate optimization opportunities identified");
                    break;
                    
                case PerformanceWarningType.LowPoolEfficiency:
                    // Could adjust pool sizes automatically
                    GameDebug.Log(BuildContext(), "Pool efficiency optimization opportunities identified");
                    break;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Start profiling a custom operation
        /// </summary>
        public IDisposable ProfileOperation(string operationName)
        {
            return MOBA.Debugging.PerformanceProfiler.Measure(operationName);
        }
        
        /// <summary>
        /// Record a custom memory allocation
        /// </summary>
        public void RecordMemoryAllocation(string source, long bytes)
        {
            var metrics = GetOrCreateMemoryMetrics(source);
            metrics.UpdateMemoryUsage(bytes, bytes);
        }
        
        /// <summary>
        /// Record custom network bandwidth usage
        /// </summary>
        public void RecordNetworkBandwidth(string source, float bytesPerSecond)
        {
            var metrics = GetOrCreateNetworkMetrics(source);
            metrics.UpdateBandwidth(bytesPerSecond, 0);
        }
        
        /// <summary>
        /// Clear all performance metrics
        /// </summary>
        public void ClearMetrics()
        {
            memoryMetrics.Clear();
            networkMetrics.Clear();
            frameTimeMetrics.Clear();
            poolMetrics.Clear();
            frameTimeHistory.Clear();
            memoryHistory.Clear();
            
            MOBA.Debugging.PerformanceProfiler.Clear();
        }
        
        #endregion
        
        #region Helper Methods
        
        private GameDebugContext BuildContext()
        {
            return new GameDebugContext(
                GameDebugCategory.Performance,
                GameDebugSystemTag.Performance,
                GameDebugMechanicTag.General,
                nameof(EnhancedPerformanceProfiler),
                gameObject?.name);
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            // Clear events
            OnPerformanceWarning = null;
            OnMetricsUpdated = null;
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    public enum PerformanceWarningType
    {
        HighMemoryUsage,
        LowFrameRate,
        HighNetworkBandwidth,
        LowPoolEfficiency
    }
    
    public enum PerformanceWarningSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public struct PerformanceWarning
    {
        public PerformanceWarningType Type;
        public string Message;
        public float Timestamp;
        public PerformanceWarningSeverity Severity;
    }
    
    public struct PerformanceSnapshot
    {
        public float Timestamp;
        public Dictionary<string, MemoryMetrics> MemoryMetrics;
        public Dictionary<string, NetworkBandwidthMetrics> NetworkMetrics;
        public Dictionary<string, FrameTimeMetrics> FrameTimeMetrics;
        public Dictionary<string, ObjectPoolMetrics> PoolMetrics;
        public float CurrentFPS;
        public float AverageFrameTime;
        public long TotalMemoryUsage;
    }
    
    public class MemoryMetrics
    {
        public long LatestMemoryUsage { get; private set; }
        public long PeakMemoryUsage { get; private set; }
        public long TotalAllocated { get; private set; }
        public float AverageAllocationRate { get; private set; }
        
        private readonly CircularBuffer<long> allocationHistory = new CircularBuffer<long>(60);
        
        public void UpdateMemoryUsage(long currentUsage, long allocated)
        {
            LatestMemoryUsage = currentUsage;
            if (currentUsage > PeakMemoryUsage)
                PeakMemoryUsage = currentUsage;
                
            TotalAllocated += allocated;
            allocationHistory.Add(allocated);
            
            // Calculate average allocation rate
            if (allocationHistory.Count > 0)
            {
                long sum = 0;
                for (int i = 0; i < allocationHistory.Count; i++)
                {
                    sum += allocationHistory[i];
                }
                AverageAllocationRate = (float)sum / allocationHistory.Count;
            }
        }
    }
    
    public class NetworkBandwidthMetrics
    {
        public float LatestBandwidth { get; private set; }
        public float PeakBandwidth { get; private set; }
        public long TotalBytesTransferred { get; private set; }
        public float AverageBandwidth { get; private set; }
        
        private readonly CircularBuffer<float> bandwidthHistory = new CircularBuffer<float>(60);
        
        public void UpdateBandwidth(float bandwidth, long totalBytes)
        {
            LatestBandwidth = bandwidth;
            if (bandwidth > PeakBandwidth)
                PeakBandwidth = bandwidth;
                
            TotalBytesTransferred = totalBytes;
            bandwidthHistory.Add(bandwidth);
            
            // Calculate average bandwidth
            if (bandwidthHistory.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < bandwidthHistory.Count; i++)
                {
                    sum += bandwidthHistory[i];
                }
                AverageBandwidth = sum / bandwidthHistory.Count;
            }
        }
    }
    
    public class FrameTimeMetrics
    {
        public float LatestFrameTime { get; private set; }
        public float PeakFrameTime { get; private set; }
        public float AverageFrameTime { get; private set; }
        public int FrameCount { get; private set; }
        
        private readonly CircularBuffer<float> frameTimeHistory = new CircularBuffer<float>(120);
        
        public void UpdateFrameTime(float frameTime)
        {
            LatestFrameTime = frameTime;
            if (frameTime > PeakFrameTime)
                PeakFrameTime = frameTime;
                
            FrameCount++;
            frameTimeHistory.Add(frameTime);
            
            // Calculate average frame time
            if (frameTimeHistory.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < frameTimeHistory.Count; i++)
                {
                    sum += frameTimeHistory[i];
                }
                AverageFrameTime = sum / frameTimeHistory.Count;
            }
        }
    }
    
    public class ObjectPoolMetrics
    {
        public float LatestEfficiency { get; private set; }
        public float AverageEfficiency { get; private set; }
        public int LatestActiveCount { get; private set; }
        public int LatestTotalCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        
        private readonly CircularBuffer<float> efficiencyHistory = new CircularBuffer<float>(30);
        
        public void UpdateEfficiency(float efficiency, int activeCount, int totalCount)
        {
            LatestEfficiency = efficiency;
            LatestActiveCount = activeCount;
            LatestTotalCount = totalCount;
            
            if (activeCount > PeakActiveCount)
                PeakActiveCount = activeCount;
                
            efficiencyHistory.Add(efficiency);
            
            // Calculate average efficiency
            if (efficiencyHistory.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < efficiencyHistory.Count; i++)
                {
                    sum += efficiencyHistory[i];
                }
                AverageEfficiency = sum / efficiencyHistory.Count;
            }
        }
    }
    
    /// <summary>
    /// Simple circular buffer implementation for performance metrics
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] buffer;
        private int head;
        private int count;
        
        public CircularBuffer(int capacity)
        {
            buffer = new T[capacity];
            head = 0;
            count = 0;
        }
        
        public void Add(T item)
        {
            buffer[head] = item;
            head = (head + 1) % buffer.Length;
            
            if (count < buffer.Length)
                count++;
        }
        
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();
                    
                int actualIndex = (head - count + index + buffer.Length) % buffer.Length;
                return buffer[actualIndex];
            }
        }
        
        public int Count => count;
        
        public void Clear()
        {
            head = 0;
            count = 0;
        }
    }
    
    #endregion
}