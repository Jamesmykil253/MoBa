using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOBA.Analytics
{
    /// <summary>
    /// Advanced analytics and telemetry system for gameplay and performance data
    /// Implements comprehensive data collection, analysis, and export capabilities
    /// Based on Clean Code principles and privacy-by-design
    /// </summary>
    public class AnalyticsSystem : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableDataExport = true;
        [SerializeField] private float dataFlushInterval = 60f; // Flush data every minute
        
        [Header("Data Collection")]
        [SerializeField] private bool collectGameplayData = true;
        [SerializeField] private bool collectPerformanceData = true;
        [SerializeField] private bool collectNetworkData = true;
        [SerializeField] private bool collectUIInteractionData = true;
        
        [Header("Export Settings")]
        [SerializeField] private string exportPath = "Analytics";
        
        // Data collections
        private List<GameplayEvent> gameplayEvents = new List<GameplayEvent>();
        private List<PerformanceSnapshot> performanceSnapshots = new List<PerformanceSnapshot>();
        private List<NetworkEvent> networkEvents = new List<NetworkEvent>();
        private List<UIInteractionEvent> uiInteractionEvents = new List<UIInteractionEvent>();
        
        // Session tracking
        private AnalyticsSession currentSession;
        private List<AnalyticsSession> completedSessions = new List<AnalyticsSession>();
        
        // System references
        private Performance.PerformanceOptimizationSystem performanceSystem;
        private MOBA.Networking.NetworkProfiler networkProfiler;
        private RSBCombatSystem rsbCombatSystem;
        private CryptoCoinSystem cryptoCoinSystem;
        
        // Analytics state
        private bool isRecording = false;
        private float lastFlushTime = 0f;
        private string sessionId;
        
        // Data aggregation
        private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        private Dictionary<string, float> eventDurations = new Dictionary<string, float>();
        private Dictionary<string, List<float>> metricHistory = new Dictionary<string, List<float>>();

        private void Awake()
        {
            InitializeAnalyticsSystem();
            CacheSystemReferences();
        }

        private void Start()
        {
            if (enableAnalytics)
            {
                StartNewSession();
                SetupEventSubscriptions();
                InvokeRepeating(nameof(CollectPerformanceSnapshot), 5f, 5f);
            }
        }

        private void Update()
        {
            if (!enableAnalytics || !isRecording) return;
            
            // Flush data periodically
            if (Time.time - lastFlushTime >= dataFlushInterval)
            {
                FlushDataToStorage();
            }
            
            // Update session duration
            if (currentSession != null)
            {
                currentSession.sessionDuration = Time.time - currentSession.startTime;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushDataToStorage();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushDataToStorage();
            }
        }

        private void OnDestroy()
        {
            if (enableAnalytics)
            {
                EndCurrentSession();
                FlushDataToStorage();
                UnsubscribeFromEvents();
            }
        }

        /// <summary>
        /// Initialize the analytics system
        /// </summary>
        private void InitializeAnalyticsSystem()
        {
            // Generate unique session ID
            sessionId = System.Guid.NewGuid().ToString();
            
            // Create export directory if needed
            if (enableDataExport)
            {
                string fullPath = Path.Combine(Application.persistentDataPath, exportPath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
            
            Debug.Log($"[AnalyticsSystem] Initialized with session ID: {sessionId}");
        }

        /// <summary>
        /// Cache references to other systems for data collection
        /// </summary>
        private void CacheSystemReferences()
        {
            performanceSystem = FindAnyObjectByType<Performance.PerformanceOptimizationSystem>();
            networkProfiler = FindAnyObjectByType<MOBA.Networking.NetworkProfiler>();
            rsbCombatSystem = FindAnyObjectByType<RSBCombatSystem>();
            cryptoCoinSystem = FindAnyObjectByType<CryptoCoinSystem>();
        }

        /// <summary>
        /// Set up event subscriptions for data collection
        /// </summary>
        private void SetupEventSubscriptions()
        {
            // Performance events
            if (performanceSystem != null)
            {
                Performance.PerformanceOptimizationSystem.OnPerformanceMetricsUpdated += OnPerformanceMetricsUpdated;
                Performance.PerformanceOptimizationSystem.OnQualityLevelChanged += OnQualityLevelChanged;
            }
            
            // Crypto coin events
            if (cryptoCoinSystem != null)
            {
                CryptoCoinSystem.OnCoinPickup += OnCoinPickup;
                CryptoCoinSystem.OnCoinsScored += OnCoinsScored;
                CryptoCoinSystem.OnPlayerKill += OnPlayerKill;
            }
            
            // UI events would be subscribed here if available
            // Network events would be subscribed here if available
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (performanceSystem != null)
            {
                Performance.PerformanceOptimizationSystem.OnPerformanceMetricsUpdated -= OnPerformanceMetricsUpdated;
                Performance.PerformanceOptimizationSystem.OnQualityLevelChanged -= OnQualityLevelChanged;
            }
            
            if (cryptoCoinSystem != null)
            {
                CryptoCoinSystem.OnCoinPickup -= OnCoinPickup;
                CryptoCoinSystem.OnCoinsScored -= OnCoinsScored;
                CryptoCoinSystem.OnPlayerKill -= OnPlayerKill;
            }
        }

        /// <summary>
        /// Start a new analytics session
        /// </summary>
        private void StartNewSession()
        {
            currentSession = new AnalyticsSession
            {
                sessionId = sessionId,
                startTime = Time.time,
                timestamp = System.DateTime.Now,
                platform = Application.platform.ToString(),
                unityVersion = Application.unityVersion,
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsMemorySize = SystemInfo.graphicsMemorySize
            };
            
            isRecording = true;
            
            TrackGameplayEvent("session_started", new Dictionary<string, object>
            {
                ["session_id"] = sessionId,
                ["platform"] = currentSession.platform,
                ["device_model"] = currentSession.deviceModel
            });
            
            Debug.Log($"[AnalyticsSystem] Started new session: {sessionId}");
        }

        /// <summary>
        /// End the current analytics session
        /// </summary>
        private void EndCurrentSession()
        {
            if (currentSession == null) return;
            
            currentSession.endTime = Time.time;
            currentSession.sessionDuration = currentSession.endTime - currentSession.startTime;
            
            // Calculate session statistics
            CalculateSessionStatistics();
            
            // Add to completed sessions
            completedSessions.Add(currentSession);
            
            TrackGameplayEvent("session_ended", new Dictionary<string, object>
            {
                ["session_id"] = sessionId,
                ["duration"] = currentSession.sessionDuration,
                ["events_count"] = gameplayEvents.Count
            });
            
            isRecording = false;
            
            Debug.Log($"[AnalyticsSystem] Ended session {sessionId} (Duration: {currentSession.sessionDuration:F2}s)");
        }

        /// <summary>
        /// Track a gameplay event with optional parameters
        /// </summary>
        public void TrackGameplayEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics || !collectGameplayData) return;
            
            var gameplayEvent = new GameplayEvent
            {
                eventName = eventName,
                timestamp = Time.time,
                sessionId = sessionId,
                parametersJson = ConvertParametersToJson(parameters)
            };
            
            gameplayEvents.Add(gameplayEvent);
            
            // Update event counts
            if (eventCounts.ContainsKey(eventName))
            {
                eventCounts[eventName]++;
            }
            else
            {
                eventCounts[eventName] = 1;
            }
            
            Debug.Log($"[AnalyticsSystem] Tracked event: {eventName}");
        }

        /// <summary>
        /// Track a timed event (start timing)
        /// </summary>
        public void StartTimedEvent(string eventName)
        {
            TrackGameplayEvent($"{eventName}_started", new Dictionary<string, object>
            {
                ["start_time"] = Time.time
            });
        }

        /// <summary>
        /// Track a timed event (end timing)
        /// </summary>
        public void EndTimedEvent(string eventName, Dictionary<string, object> additionalParameters = null)
        {
            var parameters = additionalParameters ?? new Dictionary<string, object>();
            parameters["end_time"] = Time.time;
            
            TrackGameplayEvent($"{eventName}_ended", parameters);
            
            // Calculate duration if start event exists
            var startEvent = gameplayEvents.FindLast(e => e.eventName == $"{eventName}_started");
            if (startEvent != null)
            {
                float duration = Time.time - startEvent.timestamp;
                parameters["duration"] = duration;
                
                // Track duration statistics
                if (!eventDurations.ContainsKey(eventName))
                {
                    eventDurations[eventName] = 0f;
                }
                eventDurations[eventName] += duration;
            }
        }

        /// <summary>
        /// Collect a performance snapshot
        /// </summary>
        private void CollectPerformanceSnapshot()
        {
            if (!enableAnalytics || !collectPerformanceData || performanceSystem == null) return;
            
            var metrics = performanceSystem.GetCurrentMetrics();
            var snapshot = new PerformanceSnapshot
            {
                timestamp = Time.time,
                sessionId = sessionId,
                frameRate = metrics.frameRate,
                memoryUsage = metrics.memoryUsage,
                drawCalls = metrics.drawCalls,
                triangles = metrics.triangles,
                vertices = metrics.vertices,
                cpuUsage = metrics.cpuUsage,
                qualityLevel = metrics.qualityLevel.ToString()
            };
            
            performanceSnapshots.Add(snapshot);
            
            // Track performance metrics history
            TrackMetricHistory("frameRate", metrics.frameRate);
            TrackMetricHistory("memoryUsage", metrics.memoryUsage);
            TrackMetricHistory("drawCalls", metrics.drawCalls);
        }

        /// <summary>
        /// Track network event
        /// </summary>
        public void TrackNetworkEvent(string eventType, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics || !collectNetworkData) return;
            
            var networkEvent = new NetworkEvent
            {
                eventType = eventType,
                timestamp = Time.time,
                sessionId = sessionId,
                parametersJson = ConvertParametersToJson(parameters)
            };
            
            networkEvents.Add(networkEvent);
        }

        /// <summary>
        /// Track UI interaction event
        /// </summary>
        public void TrackUIInteraction(string elementName, string interactionType, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics || !collectUIInteractionData) return;
            
            var uiEvent = new UIInteractionEvent
            {
                elementName = elementName,
                interactionType = interactionType,
                timestamp = Time.time,
                sessionId = sessionId,
                parametersJson = ConvertParametersToJson(parameters)
            };
            
            uiInteractionEvents.Add(uiEvent);
        }

        /// <summary>
        /// Convert parameters dictionary to JSON string for Unity JsonUtility compatibility
        /// </summary>
        private string ConvertParametersToJson(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "{}";
            }
            
            // Simple JSON conversion for basic types
            var jsonPairs = new List<string>();
            foreach (var kvp in parameters)
            {
                string value;
                if (kvp.Value == null)
                {
                    value = "null";
                }
                else if (kvp.Value is string)
                {
                    value = $"\"{kvp.Value}\"";
                }
                else if (kvp.Value is bool)
                {
                    value = kvp.Value.ToString().ToLower();
                }
                else
                {
                    value = kvp.Value.ToString();
                }
                
                jsonPairs.Add($"\"{kvp.Key}\": {value}");
            }
            
            return "{" + string.Join(", ", jsonPairs) + "}";
        }

        /// <summary>
        /// Track metric history for analysis
        /// </summary>
        private void TrackMetricHistory(string metricName, float value)
        {
            if (!metricHistory.ContainsKey(metricName))
            {
                metricHistory[metricName] = new List<float>();
            }
            
            metricHistory[metricName].Add(value);
            
            // Keep only recent history (last 100 values)
            if (metricHistory[metricName].Count > 100)
            {
                metricHistory[metricName].RemoveAt(0);
            }
        }

        /// <summary>
        /// Calculate session statistics
        /// </summary>
        private void CalculateSessionStatistics()
        {
            if (currentSession == null) return;
            
            // Calculate performance averages
            if (performanceSnapshots.Count > 0)
            {
                float avgFrameRate = 0f;
                float avgMemoryUsage = 0f;
                
                foreach (var snapshot in performanceSnapshots)
                {
                    avgFrameRate += snapshot.frameRate;
                    avgMemoryUsage += snapshot.memoryUsage;
                }
                
                currentSession.averageFrameRate = avgFrameRate / performanceSnapshots.Count;
                currentSession.averageMemoryUsage = avgMemoryUsage / performanceSnapshots.Count;
            }
            
            // Calculate event statistics
            currentSession.totalEvents = gameplayEvents.Count;
            currentSession.totalUIInteractions = uiInteractionEvents.Count;
            currentSession.totalNetworkEvents = networkEvents.Count;
        }

        /// <summary>
        /// Flush collected data to storage
        /// </summary>
        private void FlushDataToStorage()
        {
            if (!enableDataExport) return;
            
            lastFlushTime = Time.time;
            
            // Export current session data
            ExportSessionData();
            
            Debug.Log("[AnalyticsSystem] Data flushed to storage");
        }

        /// <summary>
        /// Export session data to files
        /// </summary>
        private void ExportSessionData()
        {
            try
            {
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string basePath = Path.Combine(Application.persistentDataPath, exportPath);
                
                // Export gameplay events
                if (gameplayEvents.Count > 0)
                {
                    string gameplayPath = Path.Combine(basePath, $"gameplay_{sessionId}_{timestamp}.json");
                    ExportToJson(gameplayEvents, gameplayPath);
                }
                
                // Export performance data
                if (performanceSnapshots.Count > 0)
                {
                    string performancePath = Path.Combine(basePath, $"performance_{sessionId}_{timestamp}.json");
                    ExportToJson(performanceSnapshots, performancePath);
                }
                
                // Export network events
                if (networkEvents.Count > 0)
                {
                    string networkPath = Path.Combine(basePath, $"network_{sessionId}_{timestamp}.json");
                    ExportToJson(networkEvents, networkPath);
                }
                
                // Export UI interactions
                if (uiInteractionEvents.Count > 0)
                {
                    string uiPath = Path.Combine(basePath, $"ui_{sessionId}_{timestamp}.json");
                    ExportToJson(uiInteractionEvents, uiPath);
                }
                
                // Export session summary
                if (currentSession != null)
                {
                    string sessionPath = Path.Combine(basePath, $"session_{sessionId}_{timestamp}.json");
                    ExportToJson(currentSession, sessionPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnalyticsSystem] Failed to export data: {e.Message}");
            }
        }

        /// <summary>
        /// Export data to JSON file using Unity's JsonUtility
        /// Handles complex types by creating wrapper structures
        /// </summary>
        private void ExportToJson<T>(T data, string filePath)
        {
            try
            {
                string json;
                
                // Handle special cases for Unity JsonUtility limitations
                if (data is List<GameplayEvent> gameplayEvents)
                {
                    var wrapper = new GameplayEventWrapper { events = gameplayEvents };
                    json = JsonUtility.ToJson(wrapper, true);
                }
                else if (data is List<PerformanceSnapshot> performanceSnapshots)
                {
                    var wrapper = new PerformanceSnapshotWrapper { snapshots = performanceSnapshots };
                    json = JsonUtility.ToJson(wrapper, true);
                }
                else if (data is List<NetworkEvent> networkEvents)
                {
                    var wrapper = new NetworkEventWrapper { events = networkEvents };
                    json = JsonUtility.ToJson(wrapper, true);
                }
                else if (data is List<UIInteractionEvent> uiEvents)
                {
                    var wrapper = new UIInteractionEventWrapper { events = uiEvents };
                    json = JsonUtility.ToJson(wrapper, true);
                }
                else
                {
                    // For simple objects like AnalyticsSession
                    json = JsonUtility.ToJson(data, true);
                }
                
                File.WriteAllText(filePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AnalyticsSystem] Failed to export to {filePath}: {e.Message}");
            }
        }

        /// <summary>
        /// Event handlers
        /// </summary>
        private void OnPerformanceMetricsUpdated(Performance.PerformanceMetrics metrics)
        {
            // Performance data is collected in CollectPerformanceSnapshot
        }

        private void OnQualityLevelChanged(Performance.QualityLevel newLevel)
        {
            TrackGameplayEvent("quality_level_changed", new Dictionary<string, object>
            {
                ["new_level"] = newLevel.ToString(),
                ["timestamp"] = Time.time
            });
        }

        private void OnCoinPickup(int coins)
        {
            TrackGameplayEvent("coin_pickup", new Dictionary<string, object>
            {
                ["coins"] = coins,
                ["total_coins"] = cryptoCoinSystem?.GetCarriedCoins() ?? 0
            });
        }

        private void OnCoinsScored(int playerId, int coins)
        {
            TrackGameplayEvent("coins_scored", new Dictionary<string, object>
            {
                ["player_id"] = playerId,
                ["coins"] = coins,
                ["team_score"] = CryptoCoinSystem.GetTeamScore()
            });
        }

        private void OnPlayerKill(int coins)
        {
            TrackGameplayEvent("player_kill", new Dictionary<string, object>
            {
                ["coins_earned"] = coins
            });
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public AnalyticsSession GetCurrentSession() => currentSession;
        
        public List<AnalyticsSession> GetCompletedSessions() => new List<AnalyticsSession>(completedSessions);
        
        public Dictionary<string, int> GetEventCounts() => new Dictionary<string, int>(eventCounts);
        
        public Dictionary<string, float> GetEventDurations() => new Dictionary<string, float>(eventDurations);
        
        public List<float> GetMetricHistory(string metricName)
        {
            return metricHistory.TryGetValue(metricName, out List<float> history) ? 
                   new List<float>(history) : new List<float>();
        }

        public void ExportAllData()
        {
            FlushDataToStorage();
        }

        public void ClearAllData()
        {
            gameplayEvents.Clear();
            performanceSnapshots.Clear();
            networkEvents.Clear();
            uiInteractionEvents.Clear();
            eventCounts.Clear();
            eventDurations.Clear();
            metricHistory.Clear();
            
            Debug.Log("[AnalyticsSystem] All data cleared");
        }
    }

    /// <summary>
    /// Data structures for analytics
    /// </summary>
    [System.Serializable]
    public class AnalyticsSession
    {
        public string sessionId;
        public float startTime;
        public float endTime;
        public float sessionDuration;
        public System.DateTime timestamp;
        public string platform;
        public string unityVersion;
        public string deviceModel;
        public string operatingSystem;
        public string processorType;
        public int systemMemorySize;
        public string graphicsDeviceName;
        public int graphicsMemorySize;
        public float averageFrameRate;
        public float averageMemoryUsage;
        public int totalEvents;
        public int totalUIInteractions;
        public int totalNetworkEvents;
    }

    [System.Serializable]
    public class GameplayEvent
    {
        public string eventName;
        public float timestamp;
        public string sessionId;
        public string parametersJson; // Store parameters as JSON string for Unity compatibility
    }

    [System.Serializable]
    public class PerformanceSnapshot
    {
        public float timestamp;
        public string sessionId;
        public float frameRate;
        public float memoryUsage;
        public int drawCalls;
        public int triangles;
        public int vertices;
        public float cpuUsage;
        public string qualityLevel;
    }

    [System.Serializable]
    public class NetworkEvent
    {
        public string eventType;
        public float timestamp;
        public string sessionId;
        public string parametersJson; // Store parameters as JSON string for Unity compatibility
    }

    [System.Serializable]
    public class UIInteractionEvent
    {
        public string elementName;
        public string interactionType;
        public float timestamp;
        public string sessionId;
        public string parametersJson; // Store parameters as JSON string for Unity compatibility
    }

    /// <summary>
    /// Wrapper structures for Unity JsonUtility compatibility
    /// Unity's JsonUtility doesn't handle Dictionary and complex generics well
    /// </summary>
    [System.Serializable]
    public class GameplayEventWrapper
    {
        public List<GameplayEvent> events;
    }

    [System.Serializable]
    public class PerformanceSnapshotWrapper
    {
        public List<PerformanceSnapshot> snapshots;
    }

    [System.Serializable]
    public class NetworkEventWrapper
    {
        public List<NetworkEvent> events;
    }

    [System.Serializable]
    public class UIInteractionEventWrapper
    {
        public List<UIInteractionEvent> events;
    }
}
