using UnityEngine;
using System.Diagnostics;

namespace MOBA
{
    /// <summary>
    /// Centralized logging system with conditional compilation
    /// Replaces direct Debug.Log calls throughout codebase
    /// Based on Clean Code principles - no debug code in production
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public static class Logger
    {
        private static LogLevel minimumLogLevel = LogLevel.Info;

        /// <summary>
        /// Set minimum log level for filtering
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            minimumLogLevel = level;
        }

        /// <summary>
        /// Log debug information - only in editor/development builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogDebug(string message, UnityEngine.Object context = null)
        {
            if (minimumLogLevel <= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"[DEBUG] {message}", context);
            }
        }

        /// <summary>
        /// Log general information
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string message, UnityEngine.Object context = null)
        {
            if (minimumLogLevel <= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"[INFO] {message}", context);
            }
        }

        /// <summary>
        /// Log warnings - always shown
        /// </summary>
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            if (minimumLogLevel <= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"[WARNING] {message}", context);
            }
        }

        /// <summary>
        /// Log errors - always shown
        /// </summary>
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError($"[ERROR] {message}", context);
        }

        /// <summary>
        /// Performance-sensitive logging with string interpolation
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogDebugFormat(string format, params object[] args)
        {
            if (minimumLogLevel <= LogLevel.Debug)
            {
                UnityEngine.Debug.LogFormat($"[DEBUG] {format}", args);
            }
        }

        /// <summary>
        /// Component initialization logging
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogComponentInit(string componentName, string message = "initialized")
        {
            if (minimumLogLevel <= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"[INIT] {componentName} {message}");
            }
        }

        /// <summary>
        /// Network operation logging
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogNetwork(string message)
        {
            if (minimumLogLevel <= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"[NETWORK] {message}");
            }
        }

        /// <summary>
        /// Performance logging
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPerformance(string message)
        {
            if (minimumLogLevel <= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"[PERF] {message}");
            }
        }
    }
}
