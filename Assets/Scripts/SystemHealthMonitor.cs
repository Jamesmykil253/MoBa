using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MOBA
{
    /// <summary>
    /// Comprehensive System Health Monitor
    /// Tracks and reports on all critical issues identified in the code audit
    /// Provides real-time monitoring and automatic issue detection
    /// </summary>
    public class SystemHealthMonitor : MonoBehaviour
    {
        [Header("Monitoring Configuration")]
        [SerializeField] private bool enableRealTimeMonitoring = true;
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private float monitoringInterval = 1f;
        [SerializeField] private bool autoFixCriticalIssues = true;

        [Header("Thresholds")]
        [SerializeField] private float maxAllowedSpeed = 15f;
        [SerializeField] private int maxStateChangesPerSecond = 5;
        // [SerializeField] private float minPoolSuccessRate = 0.9f; // DISABLED: Used by enhanced projectile pool checking (removed)

        // Health Metrics
        private HealthMetrics currentMetrics = new HealthMetrics();
        private List<string> criticalIssues = new List<string>();
        private List<string> warnings = new List<string>();
        private Queue<float> recentSpeedReadings = new Queue<float>();
        private Queue<string> recentStateChanges = new Queue<string>();
        private float lastMonitorTime = 0f;

        // Component References
        private List<ProjectilePool> projectilePools = new List<ProjectilePool>();
        // private List<EnhancedProjectilePool> enhancedProjectilePools = new List<EnhancedProjectilePool>(); // DISABLED: EnhancedProjectilePool removed
        private List<MOBACharacterController> characterControllers = new List<MOBACharacterController>();
        private List<PlayerController> playerControllers = new List<PlayerController>();

        #region Initialization

        private void Start()
        {
            InitializeMonitoring();
            
            if (enableRealTimeMonitoring)
            {
                InvokeRepeating(nameof(PerformHealthCheck), 1f, monitoringInterval);
            }
        }

        private void InitializeMonitoring()
        {
            RefreshComponentReferences();
            Log("System Health Monitor initialized");
            
            // Perform initial health check
            PerformHealthCheck();
        }

        private void RefreshComponentReferences()
        {
            // Find all components we need to monitor
            projectilePools = FindObjectsByType<ProjectilePool>(FindObjectsSortMode.None).ToList();
            // enhancedProjectilePools = FindObjectsByType<EnhancedProjectilePool>(FindObjectsSortMode.None).ToList(); // DISABLED: EnhancedProjectilePool removed
            characterControllers = FindObjectsByType<MOBACharacterController>(FindObjectsSortMode.None).ToList();
            playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).ToList();

            Log($"Monitoring: {projectilePools.Count} pools, " + // + enhancedProjectilePools.Count removed
                $"{characterControllers.Count + playerControllers.Count} controllers");
        }

        #endregion

        #region Health Monitoring

        private void PerformHealthCheck()
        {
            try
            {
                currentMetrics = new HealthMetrics();
                criticalIssues.Clear();
                warnings.Clear();

                // Check all systems
                CheckProjectilePoolHealth();
                CheckMovementSystemHealth();
                CheckComponentIntegrity();
                CheckPerformanceMetrics();
                CheckStateSystemHealth();

                // Apply auto-fixes if enabled
                if (autoFixCriticalIssues)
                {
                    ApplyAutomaticFixes();
                }

                // Update overall health status
                UpdateOverallHealthStatus();
                
                lastMonitorTime = Time.time;
            }
            catch (System.Exception e)
            {
                LogError($"Health check failed: {e.Message}");
            }
        }

        private void CheckProjectilePoolHealth()
        {
            currentMetrics.ProjectilePoolCount = projectilePools.Count; // + enhancedProjectilePools.Count removed
            
            // Check legacy pools
            foreach (var pool in projectilePools)
            {
                if (pool.projectilePrefab == null)
                {
                    criticalIssues.Add($"ProjectilePool {pool.name} has null prefab");
                    currentMetrics.NullPrefabCount++;
                }
                else if (pool.projectilePrefab.GetComponent<Projectile>() == null)
                {
                    criticalIssues.Add($"ProjectilePool {pool.name} prefab missing Projectile component");
                    currentMetrics.MissingComponentCount++;
                }
            }

            // DISABLED: Enhanced pools checking commented out - EnhancedProjectilePool removed
            // Check enhanced pools
            // foreach (var pool in enhancedProjectilePools)
            // {
            //     var stats = pool.GetPoolStats();
            //     currentMetrics.TotalPoolObjects += stats.TotalObjects;
            //     currentMetrics.ActivePoolObjects += stats.ActiveObjects;
            //     
            //     if (stats.SuccessRate < minPoolSuccessRate)
            //     {
            //         criticalIssues.Add($"Enhanced pool {pool.name} has low success rate: {stats.SuccessRate:P1}");
            //         currentMetrics.LowSuccessRatePools++;
            //     }
            //
            //     if (stats.ComponentFixCount > 0)
            //     {
            //         warnings.Add($"Pool {pool.name} applied {stats.ComponentFixCount} component fixes");
            //         currentMetrics.TotalComponentFixes += stats.ComponentFixCount;
            //     }
            // }
        }

        private void CheckMovementSystemHealth()
        {
            currentMetrics.MovementControllerCount = characterControllers.Count + playerControllers.Count;

            // Check character controllers
            foreach (var controller in characterControllers)
            {
                if (controller.TryGetComponent<Rigidbody>(out var rb))
                {
                    float speed = rb.linearVelocity.magnitude;
                    recentSpeedReadings.Enqueue(speed);
                    
                    if (speed > maxAllowedSpeed)
                    {
                        criticalIssues.Add($"Character {controller.name} has extreme speed: {speed:F1} m/s");
                        currentMetrics.ExtremeSpeedCount++;
                    }

                    currentMetrics.MaxRecordedSpeed = Mathf.Max(currentMetrics.MaxRecordedSpeed, speed);
                }
            }

            // Maintain recent speed readings (last 10)
            while (recentSpeedReadings.Count > 10)
            {
                recentSpeedReadings.Dequeue();
            }

            if (recentSpeedReadings.Count > 0)
            {
                currentMetrics.AverageSpeed = recentSpeedReadings.Average();
            }
        }

        private void CheckComponentIntegrity()
        {
            // Check for null references in scene
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var obj in allGameObjects)
            {
                if (obj.name.Contains("Projectile"))
                {
                    var components = obj.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            criticalIssues.Add($"GameObject {obj.name} has null component reference");
                            currentMetrics.NullComponentCount++;
                        }
                    }
                }
            }
        }

        private void CheckPerformanceMetrics()
        {
            currentMetrics.CurrentFPS = 1f / Time.unscaledDeltaTime;
            currentMetrics.MemoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB

            if (currentMetrics.CurrentFPS < 30f)
            {
                warnings.Add($"Low FPS detected: {currentMetrics.CurrentFPS:F1}");
            }

            if (currentMetrics.MemoryUsage > 500f) // 500MB threshold
            {
                warnings.Add($"High memory usage: {currentMetrics.MemoryUsage:F1} MB");
            }
        }

        private void CheckStateSystemHealth()
        {
            // Monitor state changes (this would require integration with state machine)
            // For now, track rapid state change patterns
            while (recentStateChanges.Count > 0 && 
                   Time.time - float.Parse(recentStateChanges.Peek().Split(':')[0]) > 1f)
            {
                recentStateChanges.Dequeue();
            }

            if (recentStateChanges.Count > maxStateChangesPerSecond)
            {
                warnings.Add($"Rapid state changes detected: {recentStateChanges.Count}/sec");
                currentMetrics.RapidStateChangeCount = recentStateChanges.Count;
            }
        }

        #endregion

        #region Auto-Fix System

        private void ApplyAutomaticFixes()
        {
            int fixesApplied = 0;

            // Fix extreme movement speeds
            foreach (var controller in characterControllers)
            {
                if (controller.TryGetComponent<Rigidbody>(out var rb))
                {
                    if (rb.linearVelocity.magnitude > maxAllowedSpeed)
                    {
                        Vector3 clampedVelocity = rb.linearVelocity.normalized * maxAllowedSpeed;
                        clampedVelocity.y = rb.linearVelocity.y; // Preserve gravity
                        rb.linearVelocity = clampedVelocity;
                        fixesApplied++;
                    }
                }
            }

            // Fix missing projectile components
            foreach (var pool in projectilePools)
            {
                if (pool.projectilePrefab != null && pool.projectilePrefab.GetComponent<Projectile>() == null)
                {
                    // Can only fix instances, not prefabs at runtime
                    Log($"Flagged prefab {pool.projectilePrefab.name} for manual fix");
                }
            }

            if (fixesApplied > 0)
            {
                Log($"Applied {fixesApplied} automatic fixes");
                currentMetrics.AutoFixesApplied = fixesApplied;
            }
        }

        #endregion

        #region Health Status

        private void UpdateOverallHealthStatus()
        {
            if (criticalIssues.Count > 0)
            {
                currentMetrics.OverallHealthStatus = HealthStatus.Critical;
            }
            else if (warnings.Count > 0)
            {
                currentMetrics.OverallHealthStatus = HealthStatus.Warning;
            }
            else
            {
                currentMetrics.OverallHealthStatus = HealthStatus.Healthy;
            }

            // Calculate health score (0-100)
            int totalIssues = criticalIssues.Count * 3 + warnings.Count;
            currentMetrics.HealthScore = Mathf.Max(0, 100 - totalIssues * 5);
        }

        public HealthMetrics GetCurrentMetrics()
        {
            return currentMetrics;
        }

        public List<string> GetCriticalIssues()
        {
            return new List<string>(criticalIssues);
        }

        public List<string> GetWarnings()
        {
            return new List<string>(warnings);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Force immediate health check
        /// </summary>
        [ContextMenu("Force Health Check")]
        public void ForceHealthCheck()
        {
            PerformHealthCheck();
            Log("Manual health check completed");
        }

        /// <summary>
        /// Record a state change for monitoring
        /// </summary>
        public void RecordStateChange(string stateInfo)
        {
            recentStateChanges.Enqueue($"{Time.time}:{stateInfo}");
        }

        /// <summary>
        /// Get system health report
        /// </summary>
        public string GetHealthReport()
        {
            var report = $"=== SYSTEM HEALTH REPORT ===\n";
            report += $"Overall Status: {currentMetrics.OverallHealthStatus}\n";
            report += $"Health Score: {currentMetrics.HealthScore}/100\n";
            report += $"Last Check: {Time.time - lastMonitorTime:F1}s ago\n\n";

            report += $"CRITICAL ISSUES ({criticalIssues.Count}):\n";
            foreach (var issue in criticalIssues)
            {
                report += $"  ❌ {issue}\n";
            }

            report += $"\nWARNINGS ({warnings.Count}):\n";
            foreach (var warning in warnings)
            {
                report += $"  ⚠️ {warning}\n";
            }

            report += $"\nSYSTEM METRICS:\n";
            report += $"  Projectile Pools: {currentMetrics.ProjectilePoolCount}\n";
            report += $"  Movement Controllers: {currentMetrics.MovementControllerCount}\n";
            report += $"  Pool Objects: {currentMetrics.ActivePoolObjects}/{currentMetrics.TotalPoolObjects}\n";
            report += $"  Average Speed: {currentMetrics.AverageSpeed:F1} m/s\n";
            report += $"  Max Speed: {currentMetrics.MaxRecordedSpeed:F1} m/s\n";
            report += $"  FPS: {currentMetrics.CurrentFPS:F1}\n";
            report += $"  Memory: {currentMetrics.MemoryUsage:F1} MB\n";

            return report;
        }

        #endregion

        #region Utilities

        private void Log(string message)
        {
            Debug.Log($"[SystemHealthMonitor] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SystemHealthMonitor] {message}");
        }

        private void OnGUI()
        {
            if (!showDebugUI) return;

            // Health status display
            GUILayout.BeginArea(new Rect(10, 500, 500, 300));
            
            var color = currentMetrics.OverallHealthStatus switch
            {
                HealthStatus.Critical => Color.red,
                HealthStatus.Warning => Color.yellow,
                _ => Color.green
            };

            var style = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            style.normal.textColor = color;
            
            GUILayout.Label($"SYSTEM HEALTH: {currentMetrics.OverallHealthStatus}", style);
            GUILayout.Label($"Score: {currentMetrics.HealthScore}/100");
            GUILayout.Label($"Critical Issues: {criticalIssues.Count} | Warnings: {warnings.Count}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Force Health Check"))
            {
                ForceHealthCheck();
            }
            
            if (GUILayout.Button("Refresh Components"))
            {
                RefreshComponentReferences();
            }

            if (GUILayout.Button("Generate Full Report"))
            {
                Debug.Log(GetHealthReport());
            }

            // Show recent issues
            if (criticalIssues.Count > 0)
            {
                GUILayout.Label("Recent Critical Issues:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                for (int i = 0; i < Mathf.Min(3, criticalIssues.Count); i++)
                {
                    GUILayout.Label($"❌ {criticalIssues[i]}", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } });
                }
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (enableRealTimeMonitoring)
            {
                CancelInvoke(nameof(PerformHealthCheck));
            }
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public struct HealthMetrics
    {
        public HealthStatus OverallHealthStatus;
        public int HealthScore;
        
        // Pool Metrics
        public int ProjectilePoolCount;
        public int TotalPoolObjects;
        public int ActivePoolObjects;
        public int NullPrefabCount;
        public int MissingComponentCount;
        public int LowSuccessRatePools;
        public int TotalComponentFixes;
        
        // Movement Metrics
        public int MovementControllerCount;
        public float AverageSpeed;
        public float MaxRecordedSpeed;
        public int ExtremeSpeedCount;
        
        // System Metrics
        public int NullComponentCount;
        public int RapidStateChangeCount;
        public float CurrentFPS;
        public float MemoryUsage;
        public int AutoFixesApplied;
    }

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical
    }

    #endregion
}
