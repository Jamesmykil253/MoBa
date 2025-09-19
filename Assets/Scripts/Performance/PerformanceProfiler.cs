using UnityEngine;
using UnityEngine.Profiling;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.Performance
{
    /// <summary>
    /// Real-time performance monitoring and automatic quality adjustment
    /// Monitors frame time, memory usage, network performance for mobile optimization
    /// Reference: Game Programming Patterns Chapter 17, Unity Optimization Guide
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        #region Singleton
        
        private static PerformanceProfiler instance;
        public static PerformanceProfiler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PerformanceProfiler>();
                    if (instance == null)
                    {
                        var go = new GameObject("PerformanceProfiler");
                        instance = go.AddComponent<PerformanceProfiler>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        #endregion
        
        #region Configuration
        
        [Header("Performance Monitoring")]
        [SerializeField, Tooltip("Enable performance monitoring")]
        private bool enableProfiling = true;
        
        [SerializeField, Tooltip("Profiling update interval")]
        private float profilingInterval = 1f;
        
        [SerializeField, Tooltip("Performance history size")]
        private int historySize = 60;
        
        [SerializeField, Tooltip("Enable automatic quality adjustment")]
        private bool enableAutoQuality = true;
        
        [Header("Frame Rate Targets")]
        [SerializeField, Tooltip("Target frame rate")]
        private int targetFrameRate = 60;
        
        [SerializeField, Tooltip("Minimum acceptable frame rate")]
        private int minFrameRate = 30;
        
        [SerializeField, Tooltip("Maximum frame rate for quality increase")]
        private int maxFrameRateForIncrease = 55;
        
        [Header("Memory Monitoring")]
        [SerializeField, Tooltip("Memory warning threshold (MB)")]
        private float memoryWarningThreshold = 512f;
        
        [SerializeField, Tooltip("Memory critical threshold (MB)")]
        private float memoryCriticalThreshold = 768f;
        
        [SerializeField, Tooltip("Enable garbage collection monitoring")]
        private bool enableGCMonitoring = true;
        
        [Header("Network Monitoring")]
        [SerializeField, Tooltip("Enable network performance monitoring")]
        private bool enableNetworkMonitoring = true;
        
        [SerializeField, Tooltip("Network latency warning threshold (ms)")]
        private float latencyWarningThreshold = 100f;
        
        [SerializeField, Tooltip("Packet loss warning threshold")]
        private float packetLossThreshold = 0.05f;
        
        [Header("Quality Adjustment")]
        #pragma warning disable 0414 // Field assigned but never used - reserved for quality adjustment
        [SerializeField, Tooltip("Quality adjustment sensitivity")]
        [Range(0.1f, 2f)] private float qualityAdjustmentSensitivity = 1f;
        #pragma warning restore 0414
        
        [SerializeField, Tooltip("Minimum time between quality changes")]
        private float qualityChangeInterval = 5f;
        
        [SerializeField, Tooltip("Performance levels for quality adjustment")]
        private PerformanceLevel[] performanceLevels;
        
        #endregion
        
        #region Performance Data Structures
        
        /// <summary>
        /// Performance quality levels
        /// </summary>
        [System.Serializable]
        public class PerformanceLevel
        {
            public string levelName;
            public int qualityLevel;
            public float renderScale = 1f;
            public int shadowQuality = 3;
            public int textureQuality = 0;
            public int particleQuality = 3;
            public int vfxQuality = 3;
            public bool enablePostProcessing = true;
            public bool enableAntiAliasing = true;
            public float lodBias = 1f;
            public int maxLODLevel = 0;
        }
        
        /// <summary>
        /// Frame performance data
        /// </summary>
        [System.Serializable]
        public struct FrameData
        {
            public float frameTime;
            public float fps;
            public float cpuTime;
            public float gpuTime;
            public float renderTime;
            public int drawCalls;
            public int vertices;
            public int triangles;
            public long memoryUsed;
            public long gcMemory;
            public float timestamp;
            
            public FrameData(float time)
            {
                timestamp = time;
                frameTime = Time.deltaTime;
                fps = 1f / frameTime;
                cpuTime = Time.deltaTime * 1000f;
                gpuTime = 0f; // Would need GPU timing
                renderTime = 0f;
                drawCalls = 0;
                vertices = 0;
                triangles = 0;
                memoryUsed = Profiler.GetTotalAllocatedMemoryLong();
                gcMemory = System.GC.GetTotalMemory(false);
            }
        }
        
        /// <summary>
        /// Network performance data
        /// </summary>
        [System.Serializable]
        public struct NetworkData
        {
            public float latency;
            public float packetLoss;
            public float bandwidth;
            public int connectedClients;
            public float networkTime;
            public float timestamp;
            
            public NetworkData(float time)
            {
                timestamp = time;
                latency = 0f;
                packetLoss = 0f;
                bandwidth = 0f;
                connectedClients = 0;
                networkTime = 0f;
            }
        }
        
        /// <summary>
        /// Memory performance data
        /// </summary>
        [System.Serializable]
        public struct MemoryData
        {
            public long totalMemory;
            public long usedMemory;
            public long gcMemory;
            public long textureMemory;
            public long meshMemory;
            public long audioMemory;
            public long animationMemory;
            public int gcCollections;
            public float timestamp;
            
            public MemoryData(float time)
            {
                timestamp = time;
                totalMemory = Profiler.GetTotalAllocatedMemoryLong();
                usedMemory = Profiler.GetTotalReservedMemoryLong();
                gcMemory = System.GC.GetTotalMemory(false);
                textureMemory = Profiler.GetAllocatedMemoryForGraphicsDriver();
                meshMemory = 0; // Would need mesh-specific tracking
                audioMemory = 0; // Would need audio-specific tracking
                animationMemory = 0; // Would need animation-specific tracking
                gcCollections = System.GC.CollectionCount(0) + System.GC.CollectionCount(1) + System.GC.CollectionCount(2);
            }
        }
        
        /// <summary>
        /// Comprehensive performance statistics
        /// </summary>
        [System.Serializable]
        public struct PerformanceStats
        {
            public FrameData currentFrame;
            public FrameData averageFrame;
            public NetworkData currentNetwork;
            public MemoryData currentMemory;
            public int currentQualityLevel;
            public float thermalState;
            public bool isPerformanceGood;
            public string performanceIssues;
        }
        
        #endregion
        
        #region State Data
        
        // Performance history
        private Queue<FrameData> frameHistory = new Queue<FrameData>();
        private Queue<NetworkData> networkHistory = new Queue<NetworkData>();
        private Queue<MemoryData> memoryHistory = new Queue<MemoryData>();
        
        // Current performance state
        private FrameData currentFrameData;
        private NetworkData currentNetworkData;
        private MemoryData currentMemoryData;
        
        // Quality management
        private int currentQualityLevel = 2;
        private float lastQualityChange = 0f;
        private bool qualityAdjustmentEnabled = true;
        
        // Performance tracking
        private float lastProfilingUpdate = 0f;
        private float performanceScore = 1f;
        private List<string> performanceIssues = new List<string>();
        
        // Thermal monitoring (mobile)
        private float thermalState = 0f;
        #pragma warning disable 0414 // Field assigned but never used - reserved for thermal throttling
        private bool isThermalThrottling = false;
        #pragma warning restore 0414
        
        // Memory tracking
        private long lastGCMemory = 0;
        private int lastGCCount = 0;
        private float gcFrequency = 0f;
        
        // Network monitoring
        private NetworkManager networkManager;
        private float averageLatency = 0f;
        #pragma warning disable 0414 // Field assigned but never used - reserved for network monitoring
        private float averagePacketLoss = 0f;
        #pragma warning restore 0414
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event fired when performance quality level changes
        /// </summary>
        public System.Action<int> OnQualityLevelChanged;
        
        /// <summary>
        /// Event fired when performance issues are detected
        /// </summary>
        public System.Action<List<string>> OnPerformanceIssuesDetected;
        
        /// <summary>
        /// Event fired when memory warning is triggered
        /// </summary>
        public System.Action<float> OnMemoryWarning;
        
        /// <summary>
        /// Event fired when thermal throttling is detected
        /// </summary>
        public System.Action<float> OnThermalThrottling;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeProfiler();
        }
        
        private void Start()
        {
            // Find network manager if available
            networkManager = FindFirstObjectByType<NetworkManager>();
            
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Initialize performance levels if not set
            if (performanceLevels == null || performanceLevels.Length == 0)
            {
                InitializeDefaultPerformanceLevels();
            }
            
            // Start profiling
            if (enableProfiling)
            {
                StartProfiling();
            }
        }
        
        private void Update()
        {
            if (!enableProfiling) return;
            
            // Update current frame data
            UpdateCurrentFrameData();
            
            // Update profiling at intervals
            if (Time.time - lastProfilingUpdate >= profilingInterval)
            {
                UpdateProfiling();
                lastProfilingUpdate = Time.time;
            }
            
            // Monitor thermal state (mobile)
            UpdateThermalMonitoring();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Save performance data when app is paused
                SavePerformanceData();
            }
            else
            {
                // Resume profiling when app is unpaused
                if (enableProfiling)
                {
                    StartProfiling();
                }
            }
        }
        
        private void OnDestroy()
        {
            SavePerformanceData();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the performance profiler
        /// </summary>
        private void InitializeProfiler()
        {
            // Enable Unity profiler
            Profiler.enabled = true;
            
            // Set initial quality level
            currentQualityLevel = Mathf.Clamp(currentQualityLevel, 0, performanceLevels?.Length - 1 ?? 2);
            
            // Initialize memory tracking
            lastGCMemory = System.GC.GetTotalMemory(false);
            lastGCCount = System.GC.CollectionCount(0) + System.GC.CollectionCount(1) + System.GC.CollectionCount(2);
            
            Debug.Log("[PerformanceProfiler] Profiler initialized");
        }
        
        /// <summary>
        /// Initialize default performance levels
        /// </summary>
        private void InitializeDefaultPerformanceLevels()
        {
            performanceLevels = new PerformanceLevel[]
            {
                new PerformanceLevel
                {
                    levelName = "Low",
                    qualityLevel = 0,
                    renderScale = 0.7f,
                    shadowQuality = 0,
                    textureQuality = 2,
                    particleQuality = 1,
                    vfxQuality = 1,
                    enablePostProcessing = false,
                    enableAntiAliasing = false,
                    lodBias = 0.5f,
                    maxLODLevel = 2
                },
                new PerformanceLevel
                {
                    levelName = "Medium",
                    qualityLevel = 1,
                    renderScale = 0.85f,
                    shadowQuality = 1,
                    textureQuality = 1,
                    particleQuality = 2,
                    vfxQuality = 2,
                    enablePostProcessing = true,
                    enableAntiAliasing = false,
                    lodBias = 0.75f,
                    maxLODLevel = 1
                },
                new PerformanceLevel
                {
                    levelName = "High",
                    qualityLevel = 2,
                    renderScale = 1f,
                    shadowQuality = 2,
                    textureQuality = 0,
                    particleQuality = 3,
                    vfxQuality = 3,
                    enablePostProcessing = true,
                    enableAntiAliasing = true,
                    lodBias = 1f,
                    maxLODLevel = 0
                },
                new PerformanceLevel
                {
                    levelName = "Ultra",
                    qualityLevel = 3,
                    renderScale = 1.2f,
                    shadowQuality = 3,
                    textureQuality = 0,
                    particleQuality = 3,
                    vfxQuality = 3,
                    enablePostProcessing = true,
                    enableAntiAliasing = true,
                    lodBias = 1.25f,
                    maxLODLevel = 0
                }
            };
        }
        
        /// <summary>
        /// Start profiling
        /// </summary>
        private void StartProfiling()
        {
            lastProfilingUpdate = Time.time;
            Debug.Log("[PerformanceProfiler] Profiling started");
        }
        
        #endregion
        
        #region Performance Monitoring
        
        /// <summary>
        /// Update current frame performance data
        /// </summary>
        private void UpdateCurrentFrameData()
        {
            currentFrameData = new FrameData(Time.time);
            
            // Add additional frame data
            // Draw calls tracking simplified for runtime
            currentFrameData.drawCalls = 0; // FrameDebugger is editor-only
        }
        
        /// <summary>
        /// Update comprehensive profiling data
        /// </summary>
        private void UpdateProfiling()
        {
            // Update frame history
            UpdateFrameHistory();
            
            // Update memory data
            UpdateMemoryData();
            
            // Update network data
            if (enableNetworkMonitoring)
            {
                UpdateNetworkData();
            }
            
            // Analyze performance
            AnalyzePerformance();
            
            // Auto-adjust quality if enabled
            if (enableAutoQuality && qualityAdjustmentEnabled)
            {
                AutoAdjustQuality();
            }
        }
        
        /// <summary>
        /// Update frame history with current data
        /// </summary>
        private void UpdateFrameHistory()
        {
            frameHistory.Enqueue(currentFrameData);
            
            if (frameHistory.Count > historySize)
            {
                frameHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// Update memory performance data
        /// </summary>
        private void UpdateMemoryData()
        {
            currentMemoryData = new MemoryData(Time.time);
            
            // Track garbage collection
            if (enableGCMonitoring)
            {
                int currentGCCount = currentMemoryData.gcCollections;
                if (currentGCCount > lastGCCount)
                {
                    gcFrequency = Time.time - (lastGCMemory > 0 ? 1f : 0f);
                    lastGCCount = currentGCCount;
                }
                lastGCMemory = currentMemoryData.gcMemory;
            }
            
            // Add to history
            memoryHistory.Enqueue(currentMemoryData);
            if (memoryHistory.Count > historySize)
            {
                memoryHistory.Dequeue();
            }
            
            // Check memory warnings
            float memoryMB = currentMemoryData.usedMemory / (1024f * 1024f);
            if (memoryMB > memoryCriticalThreshold)
            {
                OnMemoryWarning?.Invoke(memoryMB);
                TriggerMemoryCleanup();
            }
            else if (memoryMB > memoryWarningThreshold)
            {
                OnMemoryWarning?.Invoke(memoryMB);
            }
        }
        
        /// <summary>
        /// Update network performance data
        /// </summary>
        private void UpdateNetworkData()
        {
            currentNetworkData = new NetworkData(Time.time);
            
            if (networkManager != null && networkManager.IsClient)
            {
                // Get network stats (simplified - real implementation would use actual network stats)
                currentNetworkData.connectedClients = networkManager.ConnectedClients.Count;
                
                // Calculate average latency (would need actual RTT measurement)
                if (networkHistory.Count > 0)
                {
                    float totalLatency = 0f;
                    foreach (var data in networkHistory)
                    {
                        totalLatency += data.latency;
                    }
                    averageLatency = totalLatency / networkHistory.Count;
                }
            }
            
            // Add to history
            networkHistory.Enqueue(currentNetworkData);
            if (networkHistory.Count > historySize)
            {
                networkHistory.Dequeue();
            }
            
            // Check network warnings
            if (currentNetworkData.latency > latencyWarningThreshold)
            {
                AddPerformanceIssue($"High network latency: {currentNetworkData.latency:F1}ms");
            }
            
            if (currentNetworkData.packetLoss > packetLossThreshold)
            {
                AddPerformanceIssue($"High packet loss: {currentNetworkData.packetLoss:P1}");
            }
        }
        
        /// <summary>
        /// Update thermal monitoring for mobile devices
        /// </summary>
        private void UpdateThermalMonitoring()
        {
            #if UNITY_ANDROID || UNITY_IOS
            // Get thermal state (simplified - would use platform-specific APIs)
            thermalState = Mathf.Clamp01(SystemInfo.batteryLevel); // Placeholder
            
            // Check for thermal throttling
            if (thermalState > 0.8f && !isThermalThrottling)
            {
                isThermalThrottling = true;
                OnThermalThrottling?.Invoke(thermalState);
                
                // Reduce quality to prevent overheating
                if (currentQualityLevel > 0)
                {
                    SetQualityLevel(currentQualityLevel - 1);
                }
            }
            else if (thermalState < 0.6f && isThermalThrottling)
            {
                isThermalThrottling = false;
            }
            #endif
        }
        
        #endregion
        
        #region Performance Analysis
        
        /// <summary>
        /// Analyze current performance and identify issues
        /// </summary>
        private void AnalyzePerformance()
        {
            performanceIssues.Clear();
            
            // Analyze frame rate
            float avgFPS = CalculateAverageFPS();
            if (avgFPS < minFrameRate)
            {
                AddPerformanceIssue($"Low frame rate: {avgFPS:F1} FPS (target: {targetFrameRate})");
            }
            
            // Analyze frame time consistency
            float frameTimeVariance = CalculateFrameTimeVariance();
            if (frameTimeVariance > 5f) // 5ms variance threshold
            {
                AddPerformanceIssue($"Inconsistent frame timing: {frameTimeVariance:F1}ms variance");
            }
            
            // Analyze memory usage
            float memoryMB = currentMemoryData.usedMemory / (1024f * 1024f);
            if (memoryMB > memoryWarningThreshold)
            {
                AddPerformanceIssue($"High memory usage: {memoryMB:F1}MB");
            }
            
            // Analyze garbage collection frequency
            if (gcFrequency > 0 && gcFrequency < 1f)
            {
                AddPerformanceIssue($"Frequent garbage collection: {1f/gcFrequency:F1} times per second");
            }
            
            // Calculate overall performance score
            performanceScore = CalculatePerformanceScore();
            
            // Fire events if issues detected
            if (performanceIssues.Count > 0)
            {
                OnPerformanceIssuesDetected?.Invoke(new List<string>(performanceIssues));
            }
        }
        
        /// <summary>
        /// Calculate average FPS from frame history
        /// </summary>
        private float CalculateAverageFPS()
        {
            if (frameHistory.Count == 0) return currentFrameData.fps;
            
            float totalFPS = 0f;
            foreach (var frame in frameHistory)
            {
                totalFPS += frame.fps;
            }
            
            return totalFPS / frameHistory.Count;
        }
        
        /// <summary>
        /// Calculate frame time variance for consistency analysis
        /// </summary>
        private float CalculateFrameTimeVariance()
        {
            if (frameHistory.Count < 2) return 0f;
            
            float avgFrameTime = 0f;
            foreach (var frame in frameHistory)
            {
                avgFrameTime += frame.frameTime;
            }
            avgFrameTime /= frameHistory.Count;
            
            float variance = 0f;
            foreach (var frame in frameHistory)
            {
                float diff = frame.frameTime - avgFrameTime;
                variance += diff * diff;
            }
            
            return Mathf.Sqrt(variance / frameHistory.Count) * 1000f; // Convert to milliseconds
        }
        
        /// <summary>
        /// Calculate overall performance score (0-1)
        /// </summary>
        private float CalculatePerformanceScore()
        {
            float fpsScore = Mathf.Clamp01(CalculateAverageFPS() / targetFrameRate);
            float memoryScore = 1f - Mathf.Clamp01((currentMemoryData.usedMemory / (1024f * 1024f)) / memoryWarningThreshold);
            float networkScore = enableNetworkMonitoring ? 
                1f - Mathf.Clamp01(averageLatency / latencyWarningThreshold) : 1f;
            
            return (fpsScore + memoryScore + networkScore) / 3f;
        }
        
        /// <summary>
        /// Add performance issue to current list
        /// </summary>
        private void AddPerformanceIssue(string issue)
        {
            if (!performanceIssues.Contains(issue))
            {
                performanceIssues.Add(issue);
            }
        }
        
        #endregion
        
        #region Quality Management
        
        /// <summary>
        /// Automatically adjust quality based on performance
        /// </summary>
        private void AutoAdjustQuality()
        {
            if (Time.time - lastQualityChange < qualityChangeInterval)
                return;
                
            float avgFPS = CalculateAverageFPS();
            
            // Reduce quality if performance is poor
            if (avgFPS < minFrameRate && currentQualityLevel > 0)
            {
                SetQualityLevel(currentQualityLevel - 1);
                Debug.Log($"[PerformanceProfiler] Reduced quality to {currentQualityLevel} due to low FPS: {avgFPS:F1}");
            }
            // Increase quality if performance is good
            else if (avgFPS > maxFrameRateForIncrease && currentQualityLevel < performanceLevels.Length - 1)
            {
                SetQualityLevel(currentQualityLevel + 1);
                Debug.Log($"[PerformanceProfiler] Increased quality to {currentQualityLevel} due to good FPS: {avgFPS:F1}");
            }
        }
        
        /// <summary>
        /// Set quality level and apply settings
        /// </summary>
        /// <param name="qualityLevel">Quality level index</param>
        public void SetQualityLevel(int qualityLevel)
        {
            qualityLevel = Mathf.Clamp(qualityLevel, 0, performanceLevels.Length - 1);
            
            if (qualityLevel == currentQualityLevel) return;
            
            currentQualityLevel = qualityLevel;
            lastQualityChange = Time.time;
            
            // Apply quality settings
            ApplyQualitySettings(performanceLevels[qualityLevel]);
            
            OnQualityLevelChanged?.Invoke(qualityLevel);
            
            Debug.Log($"[PerformanceProfiler] Quality level changed to {qualityLevel}: {performanceLevels[qualityLevel].levelName}");
        }
        
        /// <summary>
        /// Apply quality settings to the engine
        /// </summary>
        private void ApplyQualitySettings(PerformanceLevel level)
        {
            // Apply Unity quality settings
            QualitySettings.SetQualityLevel(level.qualityLevel, true);
            
            // Apply custom settings
            QualitySettings.shadowResolution = (ShadowResolution)level.shadowQuality;
            QualitySettings.globalTextureMipmapLimit = level.textureQuality;
            QualitySettings.lodBias = level.lodBias;
            QualitySettings.maximumLODLevel = level.maxLODLevel;
            
            // Apply render scale (would need render pipeline integration)
            // This would require URP/HDRP specific implementation
            
            // Apply VFX quality
            if (MOBA.VFX.VFXManager.Instance != null)
            {
                MOBA.VFX.VFXManager.Instance.SetVFXQualityLevel(level.vfxQuality);
            }
            
            // Apply audio quality
            if (MOBA.Audio.AudioManager.Instance != null)
            {
                // Could adjust audio quality settings here
            }
        }
        
        #endregion
        
        #region Memory Management
        
        /// <summary>
        /// Trigger memory cleanup when needed
        /// </summary>
        private void TriggerMemoryCleanup()
        {
            // Force garbage collection
            System.GC.Collect();
            
            // Unload unused assets
            Resources.UnloadUnusedAssets();
            
            // Notify other systems to clean up
            if (MOBA.VFX.VFXManager.Instance != null)
            {
                // Could trigger VFX cleanup
            }
            
            if (MOBA.Audio.AudioManager.Instance != null)
            {
                // Could trigger audio cleanup
            }
            
            Debug.Log("[PerformanceProfiler] Memory cleanup triggered");
        }
        
        #endregion
        
        #region Data Persistence
        
        /// <summary>
        /// Save performance data for analysis
        /// </summary>
        private void SavePerformanceData()
        {
            // Save performance data to PlayerPrefs or file
            PlayerPrefs.SetInt("PerformanceProfiler_QualityLevel", currentQualityLevel);
            PlayerPrefs.SetFloat("PerformanceProfiler_PerformanceScore", performanceScore);
            PlayerPrefs.Save();
            
            Debug.Log("[PerformanceProfiler] Performance data saved");
        }
        
        /// <summary>
        /// Load saved performance data
        /// </summary>
        private void LoadPerformanceData()
        {
            currentQualityLevel = PlayerPrefs.GetInt("PerformanceProfiler_QualityLevel", 2);
            performanceScore = PlayerPrefs.GetFloat("PerformanceProfiler_PerformanceScore", 1f);
            
            Debug.Log($"[PerformanceProfiler] Performance data loaded: Quality={currentQualityLevel}, Score={performanceScore:F2}");
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get current performance statistics
        /// </summary>
        /// <returns>Comprehensive performance stats</returns>
        public PerformanceStats GetPerformanceStats()
        {
            FrameData averageFrame = CalculateAverageFrameData();
            
            return new PerformanceStats
            {
                currentFrame = currentFrameData,
                averageFrame = averageFrame,
                currentNetwork = currentNetworkData,
                currentMemory = currentMemoryData,
                currentQualityLevel = currentQualityLevel,
                thermalState = thermalState,
                isPerformanceGood = performanceScore > 0.7f,
                performanceIssues = string.Join(", ", performanceIssues)
            };
        }
        
        /// <summary>
        /// Calculate average frame data from history
        /// </summary>
        private FrameData CalculateAverageFrameData()
        {
            if (frameHistory.Count == 0) return currentFrameData;
            
            FrameData avg = new FrameData(Time.time);
            
            foreach (var frame in frameHistory)
            {
                avg.frameTime += frame.frameTime;
                avg.fps += frame.fps;
                avg.cpuTime += frame.cpuTime;
                avg.drawCalls += frame.drawCalls;
                avg.memoryUsed += frame.memoryUsed;
            }
            
            int count = frameHistory.Count;
            avg.frameTime /= count;
            avg.fps /= count;
            avg.cpuTime /= count;
            avg.drawCalls /= count;
            avg.memoryUsed /= count;
            
            return avg;
        }
        
        /// <summary>
        /// Enable/disable automatic quality adjustment
        /// </summary>
        /// <param name="enabled">Enable auto quality</param>
        public void SetAutoQuality(bool enabled)
        {
            qualityAdjustmentEnabled = enabled;
        }
        
        /// <summary>
        /// Enable/disable performance profiling
        /// </summary>
        /// <param name="enabled">Enable profiling</param>
        public void SetProfilingEnabled(bool enabled)
        {
            enableProfiling = enabled;
            
            if (enabled)
            {
                StartProfiling();
            }
        }
        
        /// <summary>
        /// Get current performance score (0-1)
        /// </summary>
        /// <returns>Performance score</returns>
        public float GetPerformanceScore()
        {
            return performanceScore;
        }
        
        /// <summary>
        /// Get current quality level
        /// </summary>
        /// <returns>Quality level index</returns>
        public int GetQualityLevel()
        {
            return currentQualityLevel;
        }
        
        /// <summary>
        /// Get performance issues list
        /// </summary>
        /// <returns>List of current performance issues</returns>
        public List<string> GetPerformanceIssues()
        {
            return new List<string>(performanceIssues);
        }
        
        /// <summary>
        /// Force memory cleanup
        /// </summary>
        public void ForceMemoryCleanup()
        {
            TriggerMemoryCleanup();
        }
        
        /// <summary>
        /// Reset performance history
        /// </summary>
        public void ResetHistory()
        {
            frameHistory.Clear();
            networkHistory.Clear();
            memoryHistory.Clear();
            performanceIssues.Clear();
            
            Debug.Log("[PerformanceProfiler] Performance history reset");
        }
        
        #endregion
    }
}