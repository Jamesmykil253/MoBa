using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace MOBA.ErrorHandling
{
    /// <summary>
    /// Comprehensive error handling and validation system
    /// Provides centralized error management, logging, and recovery
    /// </summary>
    public static class ErrorHandler {
    // Thread safety lock for error logging and statistics
    private static readonly object errorLock = new object();
    // Rate limiting and batching for log file writes
    private static readonly List<ErrorLog> logBuffer = new List<ErrorLog>();
    private static DateTime lastFlushTime = DateTime.MinValue;
    private static float flushIntervalSeconds = 1.0f; // Flush every 1 second
    private static int maxLogsPerFlush = 10; // Max logs written per flush
        /// <summary>
        /// Cleans up static event subscriptions (call on application quit or domain reload).
        /// </summary>
        public static void Dispose()
        {
            Application.logMessageReceived -= OnUnityLogMessage;
            isInitialized = false;
        }
        private static List<ErrorLog> errorHistory = new List<ErrorLog>();
        private static int maxErrorHistory = 1000;
        private static string logFilePath;
        private static bool isInitialized = false;
        private static bool isHandlingUnityLogMessage = false;
        
        // Error thresholds
        private static readonly Dictionary<ErrorSeverity, int> errorThresholds = new Dictionary<ErrorSeverity, int>
        {
            { ErrorSeverity.Warning, 10 },
            { ErrorSeverity.Error, 5 },
            { ErrorSeverity.Critical, 1 }
        };
        
        // Error counts for monitoring
        private static Dictionary<ErrorSeverity, int> errorCounts = new Dictionary<ErrorSeverity, int>();
        
    /// <summary>
    /// Raised when an error is logged. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public static event Action<ErrorLog> OnErrorLogged;
    /// <summary>
    /// Raised when an error threshold is exceeded. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public static event Action<ErrorSeverity> OnErrorThresholdExceeded;
        
        static ErrorHandler()
        {
            Initialize();
            OnErrorThresholdExceeded += DefaultEscalationOrRecovery;
        }
        
        public static void Initialize()
        {
            if (isInitialized) return;
            
            logFilePath = Path.Combine(Application.persistentDataPath, "error_log.txt");
            
            // Initialize error counts
            foreach (ErrorSeverity severity in Enum.GetValues(typeof(ErrorSeverity)))
            {
                errorCounts[severity] = 0;
            }
            
            // Set up Unity log callback
            Application.logMessageReceived += OnUnityLogMessage;

            // Start periodic flush (Unity update not available in static, so use timer)
            System.Timers.Timer flushTimer = new System.Timers.Timer(flushIntervalSeconds * 1000f);
            flushTimer.Elapsed += (s, e) => FlushLogBuffer();
            flushTimer.AutoReset = true;
            flushTimer.Start();

            isInitialized = true;
            LogInfo("ErrorHandler", "Error handling system initialized");
        }
        
        #region Public Logging Methods
        
        public static void LogInfo(string context, string message)
        {
            LogError(ErrorSeverity.Info, context, message, null);
        }
        
        public static void LogWarning(string context, string message)
        {
            LogError(ErrorSeverity.Warning, context, message, null);
        }
        
        public static void LogError(string context, string message, Exception exception = null)
        {
            LogError(ErrorSeverity.Error, context, message, exception);
        }
        
        public static void LogCritical(string context, string message, Exception exception = null)
        {
            LogError(ErrorSeverity.Critical, context, message, exception);
        }
        
        public static void LogError(ErrorSeverity severity, string context, string message, Exception exception)
        {
            var errorLog = new ErrorLog
            {
                Severity = severity,
                Context = context,
                Message = message,
                Exception = exception,
                Timestamp = DateTime.Now,
                StackTrace = exception?.StackTrace ?? Environment.StackTrace,
                SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                GameObjectName = GetCurrentGameObjectName()
            };

            lock (errorLock)
            {
                // Add to history
                errorHistory.Add(errorLog);
                if (errorHistory.Count > maxErrorHistory)
                {
                    errorHistory.RemoveAt(0);
                }

                // Update counts
                errorCounts[severity]++;

                // Add to log buffer for batching
                logBuffer.Add(errorLog);
            }

            // Try to flush if needed
            TryFlushLogBuffer();

            // Trigger events
            OnErrorLogged?.Invoke(errorLog);

            // Check thresholds
            bool thresholdExceeded = false;
            lock (errorLock)
            {
                if (errorThresholds.ContainsKey(severity) && errorCounts[severity] >= errorThresholds[severity])
                {
                    thresholdExceeded = true;
                }
            }
            if (thresholdExceeded)
            {
                OnErrorThresholdExceeded?.Invoke(severity);
            }

            // Console output with color coding
            LogToConsole(errorLog);
        }
        
        #endregion
        
        #region Validation Methods
        
        public static bool ValidateNotNull<T>(T obj, string context, string parameterName) where T : class
        {
            if (obj == null)
            {
                LogError(context, $"Null reference: {parameterName}");
                return false;
            }
            return true;
        }
        
        public static bool ValidateRange(float value, float min, float max, string context, string parameterName)
        {
            if (value < min || value > max)
            {
                LogError(context, $"Value out of range: {parameterName} = {value}, expected [{min}, {max}]");
                return false;
            }
            return true;
        }
        
        public static bool ValidateComponent<T>(GameObject gameObject, string context) where T : Component
        {
            if (gameObject == null)
            {
                LogError(context, $"GameObject is null when checking for component {typeof(T).Name}");
                return false;
            }
            
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                LogError(context, $"Missing required component {typeof(T).Name} on GameObject {gameObject.name}");
                return false;
            }
            
            return true;
        }
        
        public static bool ValidateArrayIndex<T>(T[] array, int index, string context, string arrayName)
        {
            if (array == null)
            {
                LogError(context, $"Array is null: {arrayName}");
                return false;
            }
            
            if (index < 0 || index >= array.Length)
            {
                LogError(context, $"Array index out of bounds: {arrayName}[{index}], length = {array.Length}");
                return false;
            }
            
            return true;
        }
        
        public static T SafeGetComponent<T>(GameObject gameObject, string context) where T : Component
        {
            if (!ValidateNotNull(gameObject, context, "gameObject"))
                return null;
                
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                LogWarning(context, $"Component {typeof(T).Name} not found on {gameObject.name}");
            }
            
            return component;
        }
        
        #endregion
        
        #region Recovery Methods
        
        public static void AttemptRecovery(string context, Action recoveryAction, string recoveryDescription)
        {
            try
            {
                LogInfo(context, $"Attempting recovery: {recoveryDescription}");
                recoveryAction?.Invoke();
                LogInfo(context, $"Recovery successful: {recoveryDescription}");
            }
            catch (Exception e)
            {
                LogError(context, $"Recovery failed: {recoveryDescription}", e);
            }
        }
        
        public static T SafeExecute<T>(string context, Func<T> action, T defaultValue = default(T))
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                LogError(context, $"Safe execution failed, returning default value", e);
                return defaultValue;
            }
        }
        
        public static void SafeExecute(string context, Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                LogError(context, "Safe execution failed", e);
            }
        }
        
        #endregion
        
        #region Internal Methods
        
        private static void OnUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            if (isHandlingUnityLogMessage)
            {
                return;
            }

            if (string.IsNullOrEmpty(logString) || logString.StartsWith("[ErrorHandler]"))
            {
                return;
            }

            isHandlingUnityLogMessage = true;
            try
            {
                string context = "UnityLog";
                string message = string.IsNullOrEmpty(stackTrace) ? logString : $"{logString}\n{stackTrace}";

                switch (type)
                {
                    case LogType.Warning:
                        LogWarning(context, message);
                        break;
                    case LogType.Error:
                    case LogType.Assert:
                        LogError(context, message);
                        break;
                    case LogType.Exception:
                        LogCritical(context, message);
                        break;
                    default:
                        // Ignore regular info logs to avoid noise
                        break;
                }
            }
            finally
            {
                isHandlingUnityLogMessage = false;
            }
        }
        
        // Write a batch of error logs to file (rate limited)
        private static void WriteToLogFileBatch(List<ErrorLog> logs)
        {
            try
            {
                if (logs == null || logs.Count == 0) return;
                var lines = new System.Text.StringBuilder();
                foreach (var errorLog in logs)
                {
                    string logLine = $"[{errorLog.Timestamp:yyyy-MM-dd HH:mm:ss}] [{errorLog.Severity}] [{errorLog.Context}] {errorLog.Message}";
                    if (errorLog.Exception != null)
                    {
                        logLine += $"\nException: {errorLog.Exception}";
                    }
                    logLine += "\n";
                    lines.Append(logLine);
                }
                File.AppendAllText(logFilePath, lines.ToString());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ErrorHandler] Failed to write to log file: {e.Message}");
            }
        }

        // Try to flush log buffer if interval has passed or buffer is large
        private static void TryFlushLogBuffer()
        {
            lock (errorLock)
            {
                var now = DateTime.Now;
                if ((now - lastFlushTime).TotalSeconds >= flushIntervalSeconds || logBuffer.Count >= maxLogsPerFlush)
                {
                    FlushLogBuffer();
                }
            }
        }

        // Flushes the log buffer to disk (rate limited)
        private static void FlushLogBuffer()
        {
            lock (errorLock)
            {
                if (logBuffer.Count == 0) return;
                int countToWrite = Math.Min(logBuffer.Count, maxLogsPerFlush);
                var logsToWrite = logBuffer.Take(countToWrite).ToList();
                logBuffer.RemoveRange(0, countToWrite);
                WriteToLogFileBatch(logsToWrite);
                lastFlushTime = DateTime.Now;
            }
        }
        
        private static void LogToConsole(ErrorLog errorLog)
        {
            string message = $"[ErrorHandler] [{errorLog.Context}] {errorLog.Message}";
            
            switch (errorLog.Severity)
            {
                case ErrorSeverity.Info:
                    UnityEngine.Debug.Log(message);
                    break;
                case ErrorSeverity.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case ErrorSeverity.Error:
                case ErrorSeverity.Critical:
                    UnityEngine.Debug.LogError(message);
                    if (errorLog.Exception != null)
                    {
                        UnityEngine.Debug.LogException(errorLog.Exception);
                    }
                    break;
            }
        }
        
        private static string GetCurrentGameObjectName()
        {
            // Try to get current GameObject from stack trace
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
            
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method.DeclaringType != null && method.DeclaringType.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    return method.DeclaringType.Name;
                }
            }
            
            return "Unknown";
        }
        
        // Default escalation/recovery for critical error thresholds
        private static void DefaultEscalationOrRecovery(ErrorSeverity severity)
        {
            string context = "ErrorHandler";
            if (severity == ErrorSeverity.Critical)
            {
                LogInfo(context, "Critical error threshold exceeded. Attempting default recovery action.");
                // Example: Attempt to save state, notify user, or restart subsystem
                AttemptRecovery(context, () => {
                    // Insert custom recovery logic here (e.g., reload scene, reset network, etc.)
                    UnityEngine.Debug.LogWarning("[ErrorHandler] Default recovery: No custom action defined.");
                }, "Default critical error recovery");
            }
            else if (severity == ErrorSeverity.Error)
            {
                LogInfo(context, "Error threshold exceeded. Escalating to admin or logging for review.");
                // Example: Send alert, log to server, etc.
            }
            else if (severity == ErrorSeverity.Warning)
            {
                LogInfo(context, "Warning threshold exceeded. Monitoring for further issues.");
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public static List<ErrorLog> GetErrorHistory() => new List<ErrorLog>(errorHistory);
        
        public static Dictionary<ErrorSeverity, int> GetErrorCounts() => new Dictionary<ErrorSeverity, int>(errorCounts);
        
        public static void ClearErrorHistory()
        {
            errorHistory.Clear();
            foreach (var key in errorCounts.Keys.ToArray())
            {
                errorCounts[key] = 0;
            }
            LogInfo("ErrorHandler", "Error history cleared");
        }
        
        public static void SetErrorThreshold(ErrorSeverity severity, int threshold)
        {
            errorThresholds[severity] = threshold;
            LogInfo("ErrorHandler", $"Error threshold for {severity} set to {threshold}");
        }
        
        public static string GetLogFilePath() => logFilePath;
        #endregion
    }
    
    /// <summary>
    /// MonoBehaviour wrapper for ErrorHandler to provide easy access in Unity
    /// </summary>
    public class ErrorHandlerMonoBehaviour : MonoBehaviour
    {
        [Header("Error Monitoring")]
        [SerializeField] private bool enableErrorThresholdNotifications = true;

        private void Start()
        {
            ErrorHandler.Initialize();
            
            if (enableErrorThresholdNotifications)
            {
                ErrorHandler.OnErrorThresholdExceeded += OnErrorThresholdExceeded;
            }
        }
        
        private void OnDestroy()
        {
            ErrorHandler.OnErrorThresholdExceeded -= OnErrorThresholdExceeded;
            ErrorHandler.Dispose();
        }
        
        private void OnErrorThresholdExceeded(ErrorSeverity severity)
        {
            UnityEngine.Debug.LogWarning($"[ErrorHandlerMonoBehaviour] Error threshold exceeded for {severity}");
            
            // You could trigger additional actions here, like:
            // - Sending telemetry data
            // - Showing user notifications
            // - Triggering automatic recovery procedures
        }
        
    }
    
    #region Supporting Types
    
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    [Serializable]
    public class ErrorLog
    {
        public ErrorSeverity Severity;
        public string Context;
        public string Message;
        public Exception Exception;
        public DateTime Timestamp;
        public string StackTrace;
        public string SceneName;
        public string GameObjectName;
    }
    
    /// <summary>
    /// Utility class for common validation scenarios
    /// </summary>
    public static class ValidationHelper
    {
        public static bool ValidatePlayerStats(CharacterStats stats, string context)
        {
            bool isValid = true;
            
            if (!ErrorHandler.ValidateRange(stats.MaxHP, 1f, 10000f, context, "MaxHP"))
                isValid = false;
            
            if (!ErrorHandler.ValidateRange(stats.CurrentHP, 0f, stats.MaxHP, context, "CurrentHP"))
                isValid = false;
            
            if (!ErrorHandler.ValidateRange(stats.Speed, 0f, 1000f, context, "Speed"))
                isValid = false;
            
            return isValid;
        }
        
        public static bool ValidateNetworkObject(GameObject obj, string context)
        {
            if (!ErrorHandler.ValidateNotNull(obj, context, "NetworkObject"))
                return false;
            
            if (!ErrorHandler.ValidateComponent<Unity.Netcode.NetworkObject>(obj, context))
                return false;
            
            return true;
        }
        
        public static bool ValidateAbilityData(MOBA.Abilities.EnhancedAbility ability, string context)
        {
            if (!ErrorHandler.ValidateNotNull(ability, context, "ability"))
                return false;
            
            bool isValid = true;
            
            if (!ErrorHandler.ValidateRange(ability.damage, 0f, 10000f, context, "damage"))
                isValid = false;
            
            if (!ErrorHandler.ValidateRange(ability.range, 0f, 100f, context, "range"))
                isValid = false;
            
            if (!ErrorHandler.ValidateRange(ability.cooldown, 0f, 300f, context, "cooldown"))
                isValid = false;
            
            return isValid;
        }
    }
    

}
    #endregion
