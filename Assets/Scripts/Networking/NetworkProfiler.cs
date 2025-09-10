using Unity.Netcode;
using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;
using System.Diagnostics;

namespace MOBA.Networking
{
    /// <summary>
    /// Network profiling and performance monitoring
    /// </summary>
    public class NetworkProfiler : MonoBehaviour
    {
        [Header("Profiling Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private float profilingInterval = 1f;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool saveToFile = false;

        // Performance counters
        private ProfilerCounterValue<int> connectedClientsCounter;
        private ProfilerCounterValue<int> networkMessagesSentCounter;
        private ProfilerCounterValue<int> networkMessagesReceivedCounter;
        private ProfilerCounterValue<float> averageLatencyCounter;
        private ProfilerCounterValue<int> bytesSentPerSecondCounter;
        private ProfilerCounterValue<int> bytesReceivedPerSecondCounter;

        // Network stats tracking
        private int lastFrameMessagesSent;
        private int lastFrameMessagesReceived;
        private int lastFrameBytesSent;
        private int lastFrameBytesReceived;
        private float lastProfilingTime;

        // Latency tracking
        private Dictionary<ulong, float> clientLatencies = new Dictionary<ulong, float>();
        private Dictionary<ulong, Stopwatch> pingStopwatches = new Dictionary<ulong, Stopwatch>();

        // Performance history
        private Queue<NetworkStats> statsHistory = new Queue<NetworkStats>();
        private const int MAX_HISTORY_SIZE = 60; // 1 minute at 1Hz

        private NetworkManager networkManager;
        private NetworkGameManager gameManager;

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
            gameManager = Object.FindFirstObjectByType<NetworkGameManager>();

            if (enableProfiling)
            {
                InitializeProfilers();
            }
        }

        private void InitializeProfilers()
        {
            // Create profiler counters
            connectedClientsCounter = new ProfilerCounterValue<int>(
                ProfilerCategory.Network,
                "Connected Clients",
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            networkMessagesSentCounter = new ProfilerCounterValue<int>(
                ProfilerCategory.Network,
                "Messages Sent/s",
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            networkMessagesReceivedCounter = new ProfilerCounterValue<int>(
                ProfilerCategory.Network,
                "Messages Received/s",
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            averageLatencyCounter = new ProfilerCounterValue<float>(
                ProfilerCategory.Network,
                "Average Latency (ms)",
                ProfilerMarkerDataUnit.TimeNanoseconds,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            bytesSentPerSecondCounter = new ProfilerCounterValue<int>(
                ProfilerCategory.Network,
                "Bytes Sent/s",
                ProfilerMarkerDataUnit.Bytes,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            bytesReceivedPerSecondCounter = new ProfilerCounterValue<int>(
                ProfilerCategory.Network,
                "Bytes Received/s",
                ProfilerMarkerDataUnit.Bytes,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            UnityEngine.Debug.Log("[NetworkProfiler] Profilers initialized");
        }

        private void Start()
        {
            if (enableProfiling)
            {
                InvokeRepeating(nameof(UpdateProfiling), profilingInterval, profilingInterval);
            }
        }

        private void Update()
        {
            if (!enableProfiling || !networkManager.IsListening) return;

            // Update counters
            UpdateCounters();

            // Track latency
            UpdateLatencyTracking();
        }

        private void UpdateCounters()
        {
            // Connected clients
            int connectedClients = gameManager != null ? gameManager.ConnectedPlayers : 0;
            connectedClientsCounter.Value = connectedClients;

            // Network traffic (simplified - would need transport-specific implementation)
            // For UnityTransport, we'd need to access internal counters
            // This is a placeholder for actual network statistics
            networkMessagesSentCounter.Value = GetMessagesSentPerSecond();
            networkMessagesReceivedCounter.Value = GetMessagesReceivedPerSecond();
            bytesSentPerSecondCounter.Value = GetBytesSentPerSecond();
            bytesReceivedPerSecondCounter.Value = GetBytesReceivedPerSecond();

            // Average latency
            float avgLatency = CalculateAverageLatency();
            averageLatencyCounter.Value = (int)(avgLatency * 1000000); // Convert to nanoseconds
        }

        private void UpdateLatencyTracking()
        {
            if (!networkManager.IsServer) return;

            // Send ping to all clients periodically
            if (Time.time - lastProfilingTime >= profilingInterval)
            {
                SendPingToClients();
            }
        }

        private void SendPingToClients()
        {
            if (!networkManager.IsServer) return;

            foreach (var clientId in networkManager.ConnectedClients.Keys)
            {
                if (!pingStopwatches.ContainsKey(clientId))
                {
                    pingStopwatches[clientId] = new Stopwatch();
                }

                pingStopwatches[clientId].Restart();
                PingClientRpc(clientId);
            }
        }

        [ClientRpc]
        private void PingClientRpc(ulong targetClientId)
        {
            if (networkManager.LocalClientId == targetClientId)
            {
                // Respond to ping
                PongServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void PongServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (pingStopwatches.TryGetValue(clientId, out var stopwatch))
            {
                stopwatch.Stop();
                float latency = (float)stopwatch.Elapsed.TotalMilliseconds;
                clientLatencies[clientId] = latency;
            }
        }

        private void UpdateProfiling()
        {
            if (!enableProfiling) return;

            // Collect current stats
            var currentStats = new NetworkStats
            {
                timestamp = Time.time,
                connectedClients = gameManager?.ConnectedPlayers ?? 0,
                messagesSentPerSecond = GetMessagesSentPerSecond(),
                messagesReceivedPerSecond = GetMessagesReceivedPerSecond(),
                bytesSentPerSecond = GetBytesSentPerSecond(),
                bytesReceivedPerSecond = GetBytesReceivedPerSecond(),
                averageLatency = CalculateAverageLatency(),
                memoryUsage = GetMemoryUsage(),
                cpuUsage = GetCpuUsage()
            };

            // Add to history
            statsHistory.Enqueue(currentStats);
            if (statsHistory.Count > MAX_HISTORY_SIZE)
            {
                statsHistory.Dequeue();
            }

            // Log stats
            if (logToConsole)
            {
                LogNetworkStats(currentStats);
            }

            // Save to file if enabled
            if (saveToFile)
            {
                SaveStatsToFile(currentStats);
            }
        }

        private void LogNetworkStats(NetworkStats stats)
        {
            UnityEngine.Debug.Log($"[NetworkProfiler] Clients: {stats.connectedClients}, " +
                     $"Msgs: {stats.messagesSentPerSecond}/{stats.messagesReceivedPerSecond}, " +
                     $"Bytes: {stats.bytesSentPerSecond}/{stats.bytesReceivedPerSecond}, " +
                     $"Latency: {stats.averageLatency:F1}ms, " +
                     $"Memory: {stats.memoryUsage}MB, " +
                     $"CPU: {stats.cpuUsage:F1}%");
        }

        private void SaveStatsToFile(NetworkStats stats)
        {
            // Implementation for saving stats to file
            // Could use System.IO.File.AppendAllText
            string logLine = $"{stats.timestamp},{stats.connectedClients},{stats.messagesSentPerSecond}," +
                           $"{stats.messagesReceivedPerSecond},{stats.bytesSentPerSecond}," +
                           $"{stats.bytesReceivedPerSecond},{stats.averageLatency},{stats.memoryUsage},{stats.cpuUsage}\n";

            // Save to persistent data path
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "network_stats.csv");
            System.IO.File.AppendAllText(filePath, logLine);
        }

        // Placeholder methods for network statistics
        // In a real implementation, these would access transport-specific counters
        private int GetMessagesSentPerSecond()
        {
            // Placeholder - would need transport-specific implementation
            return Random.Range(50, 200); // Simulated data
        }

        private int GetMessagesReceivedPerSecond()
        {
            // Placeholder - would need transport-specific implementation
            return Random.Range(50, 200); // Simulated data
        }

        private int GetBytesSentPerSecond()
        {
            // Placeholder - would need transport-specific implementation
            return Random.Range(5000, 20000); // Simulated data
        }

        private int GetBytesReceivedPerSecond()
        {
            // Placeholder - would need transport-specific implementation
            return Random.Range(5000, 20000); // Simulated data
        }

        private float CalculateAverageLatency()
        {
            if (clientLatencies.Count == 0) return 0f;

            float totalLatency = 0f;
            foreach (var latency in clientLatencies.Values)
            {
                totalLatency += latency;
            }

            return totalLatency / clientLatencies.Count;
        }

        private float GetMemoryUsage()
        {
            // Get current memory usage
            return (float)System.GC.GetTotalMemory(false) / (1024 * 1024); // MB
        }

        private float GetCpuUsage()
        {
            // Placeholder - CPU usage tracking would require platform-specific implementation
            return Random.Range(10f, 50f); // Simulated data
        }

        // Public API for accessing stats
        public NetworkStats GetCurrentStats()
        {
            return statsHistory.Count > 0 ? statsHistory.Peek() : new NetworkStats();
        }

        public NetworkStats[] GetStatsHistory()
        {
            return statsHistory.ToArray();
        }

        public float GetClientLatency(ulong clientId)
        {
            return clientLatencies.TryGetValue(clientId, out float latency) ? latency : 0f;
        }

        // Debug GUI
        private void OnGUI()
        {
            if (!enableProfiling || !Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, Screen.height - 210, 400, 200));
            GUILayout.Label("Network Profiler", GUILayout.Width(380));

            var currentStats = GetCurrentStats();
            GUILayout.Label($"Connected Clients: {currentStats.connectedClients}");
            GUILayout.Label($"Messages/s: {currentStats.messagesSentPerSecond} sent, {currentStats.messagesReceivedPerSecond} received");
            GUILayout.Label($"Bytes/s: {currentStats.bytesSentPerSecond} sent, {currentStats.bytesReceivedPerSecond} received");
            GUILayout.Label($"Average Latency: {currentStats.averageLatency:F1}ms");
            GUILayout.Label($"Memory Usage: {currentStats.memoryUsage:F1}MB");
            GUILayout.Label($"CPU Usage: {currentStats.cpuUsage:F1}%");

            if (networkManager.IsServer)
            {
                GUILayout.Label("Client Latencies:");
                foreach (var kvp in clientLatencies)
                {
                    GUILayout.Label($"  Client {kvp.Key}: {kvp.Value:F1}ms");
                }
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            // Profiler counters are automatically cleaned up by Unity
            // No manual disposal needed
        }
    }

    [System.Serializable]
    public struct NetworkStats
    {
        public float timestamp;
        public int connectedClients;
        public int messagesSentPerSecond;
        public int messagesReceivedPerSecond;
        public int bytesSentPerSecond;
        public int bytesReceivedPerSecond;
        public float averageLatency;
        public float memoryUsage;
        public float cpuUsage;
    }
}