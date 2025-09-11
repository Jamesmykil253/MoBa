using UnityEngine;

namespace MOBA.Core
{
    /// <summary>
    /// Optimized logging system to replace Debug.Log calls
    /// Addresses performance issues from excessive logging in production
    /// </summary>
    public static class MOBALogger
    {
        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
            Debug = 4,
            Verbose = 5
        }

        // Current log level - can be changed at runtime
        public static LogLevel currentLogLevel = LogLevel.Info;

        // Performance tracking
        private static int logCount = 0;
        private static float lastLogTime = 0f;
        private static readonly int MAX_LOGS_PER_SECOND = 30;

        /// <summary>
        /// Optimized logging with level filtering and rate limiting
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            // Early exit if log level is too low
            if (level > currentLogLevel) return;

            // Rate limiting to prevent performance issues
            if (Time.time - lastLogTime < 1f)
            {
                logCount++;
                if (logCount > MAX_LOGS_PER_SECOND)
                {
                    return; // Skip this log to prevent spam
                }
            }
            else
            {
                logCount = 0;
                lastLogTime = Time.time;
            }

            // Format message with level prefix
            string formattedMessage = $"[{level}] {message}";

            // Output based on level
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                case LogLevel.Verbose:
                    Debug.Log(formattedMessage, context);
                    break;
            }
        }

        /// <summary>
        /// Convenience methods for different log levels
        /// </summary>
        public static void LogError(string message, Object context = null)
        {
            Log(message, LogLevel.Error, context);
        }

        public static void LogWarning(string message, Object context = null)
        {
            Log(message, LogLevel.Warning, context);
        }

        public static void LogInfo(string message, Object context = null)
        {
            Log(message, LogLevel.Info, context);
        }

        public static void LogDebug(string message, Object context = null)
        {
            Log(message, LogLevel.Debug, context);
        }

        public static void LogVerbose(string message, Object context = null)
        {
            Log(message, LogLevel.Verbose, context);
        }

        /// <summary>
        /// Conditional logging - only logs in editor or development builds
        /// </summary>
        public static void LogConditional(string message, LogLevel level = LogLevel.Debug)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Log(message, level);
            #endif
        }

        /// <summary>
        /// Performance-aware logging with frame-based throttling
        /// </summary>
        public static void LogThrottled(string message, int frameInterval = 60, LogLevel level = LogLevel.Debug)
        {
            if (Time.frameCount % frameInterval == 0)
            {
                Log(message, level);
            }
        }

        /// <summary>
        /// Set log level at runtime
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            currentLogLevel = level;
            Log($"Log level changed to: {level}", LogLevel.Info);
        }

        /// <summary>
        /// Get current performance stats
        /// </summary>
        public static string GetPerformanceStats()
        {
            return $"Logs this second: {logCount}/{MAX_LOGS_PER_SECOND}, Level: {currentLogLevel}";
        }
    }

    /// <summary>
    /// Log level configuration for different build types
    /// </summary>
    public class LogLevelManager : MonoBehaviour
    {
        [Header("Log Level Configuration")]
        [SerializeField] private MOBALogger.LogLevel editorLogLevel = MOBALogger.LogLevel.Verbose;
        [SerializeField] private MOBALogger.LogLevel developmentLogLevel = MOBALogger.LogLevel.Debug;
        [SerializeField] private MOBALogger.LogLevel releaseLogLevel = MOBALogger.LogLevel.Warning;

        private void Awake()
        {
            ConfigureLogLevel();
        }

        private void ConfigureLogLevel()
        {
            #if UNITY_EDITOR
            MOBALogger.SetLogLevel(editorLogLevel);
            #elif DEVELOPMENT_BUILD
            MOBALogger.SetLogLevel(developmentLogLevel);
            #else
            MOBALogger.SetLogLevel(releaseLogLevel);
            #endif
            
            // Ensure fields are marked as used for compiler
            _ = developmentLogLevel;
            _ = releaseLogLevel;
        }

        /// <summary>
        /// Runtime log level adjustment
        /// </summary>
        public void SetLogLevel(int levelIndex)
        {
            MOBALogger.SetLogLevel((MOBALogger.LogLevel)levelIndex);
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 120));
            GUILayout.Label("MOBA Logger Control");
            GUILayout.Label(MOBALogger.GetPerformanceStats());
            
            if (GUILayout.Button($"Level: {MOBALogger.currentLogLevel}"))
            {
                // Cycle through log levels
                int nextLevel = ((int)MOBALogger.currentLogLevel + 1) % 6;
                MOBALogger.SetLogLevel((MOBALogger.LogLevel)nextLevel);
            }
            
            GUILayout.EndArea();
        }
    }
}
