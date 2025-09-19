using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
using MOBA.Debugging;

namespace MOBA.Networking
{
    /// <summary>
    /// Handles network reconnection logic and automatic connection recovery.
    /// Responsible for managing reconnection attempts, cooldown periods, and
    /// client recovery after network disconnections or failures.
    /// </summary>
    public class NetworkReconnectionManager : NetworkManagerComponent
    {
        #region Configuration
        
        [Header("Reconnection Settings")]
        [SerializeField, Tooltip("Enable automatic reconnection on disconnect")]
        private bool enableReconnection = true;
        
        [SerializeField, Tooltip("Interval between reconnection attempts in seconds")]
        private float reconnectionInterval = 5f;
        
        [SerializeField, Tooltip("Maximum number of reconnection attempts")]
        private int maxReconnectionAttempts = 3;
        
        [SerializeField, Tooltip("Cooldown period before allowing new reconnection attempts")]
        private float reconnectCooldown = 3f;
        
        [SerializeField, Tooltip("Enable detailed reconnection logging")]
        private bool logReconnectionEvents = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Current number of reconnection attempts
        /// </summary>
        private int reconnectionAttempts = 0;
        
        /// <summary>
        /// Active reconnection coroutine
        /// </summary>
        private Coroutine reconnectionCoroutine;
        
        /// <summary>
        /// Time of last disconnect for cooldown calculation
        /// </summary>
        private float lastDisconnectTime = 0f;
        
        /// <summary>
        /// Reference to the main production network manager
        /// </summary>
        private ProductionNetworkManager productionNetworkManager;
        
        /// <summary>
        /// Current reconnection state
        /// </summary>
        private ReconnectionState reconnectionState = ReconnectionState.Idle;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when reconnection attempt starts
        /// </summary>
        public System.Action<int, int> OnReconnectionStarted; // (currentAttempt, maxAttempts)
        
        /// <summary>
        /// Raised when reconnection succeeds
        /// </summary>
        public System.Action OnReconnectionSucceeded;
        
        /// <summary>
        /// Raised when all reconnection attempts are exhausted
        /// </summary>
        public System.Action<string> OnReconnectionFailed;
        
        /// <summary>
        /// Raised when reconnection state changes
        /// </summary>
        public System.Action<ReconnectionState> OnReconnectionStateChanged;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(ProductionNetworkManager networkManager)
        {
            base.Initialize(networkManager);
            productionNetworkManager = networkManager;
            reconnectionState = ReconnectionState.Idle;
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Initialization"),
                    "Reconnection manager initialized.",
                    ("Enabled", enableReconnection),
                    ("Interval", reconnectionInterval),
                    ("MaxAttempts", maxReconnectionAttempts));
            }
        }
        
        public override void Shutdown()
        {
            StopReconnection();
            
            OnReconnectionStarted = null;
            OnReconnectionSucceeded = null;
            OnReconnectionFailed = null;
            OnReconnectionStateChanged = null;
            
            SetReconnectionState(ReconnectionState.Idle);
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Shutdown"),
                    "Reconnection manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Reconnection Control
        
        /// <summary>
        /// Starts the reconnection process if conditions are met
        /// </summary>
        /// <param name="reason">Reason for the reconnection attempt</param>
        public void StartReconnection(string reason = "Connection lost")
        {
            if (!enableReconnection)
            {
                if (logReconnectionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Control"),
                        "Reconnection disabled; not attempting.",
                        ("Reason", reason));
                }
                return;
            }
            
            if (reconnectionState == ReconnectionState.Reconnecting)
            {
                if (logReconnectionEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Control"),
                        "Reconnection already in progress.");
                }
                return;
            }
            
            if (reconnectionAttempts >= maxReconnectionAttempts)
            {
                string errorMessage = $"Max reconnection attempts ({maxReconnectionAttempts}) reached";
                OnReconnectionFailed?.Invoke(errorMessage);
                
                if (logReconnectionEvents)
                {
                    GameDebug.LogError(
                        BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Control"),
                        "Maximum reconnection attempts reached.",
                        ("Attempts", reconnectionAttempts),
                        ("MaxAttempts", maxReconnectionAttempts));
                }
                return;
            }
            
            ScheduleReconnection();
        }
        
        /// <summary>
        /// Stops any active reconnection attempts
        /// </summary>
        public void StopReconnection()
        {
            if (reconnectionCoroutine != null)
            {
                productionNetworkManager.StopCoroutine(reconnectionCoroutine);
                reconnectionCoroutine = null;
            }
            
            SetReconnectionState(ReconnectionState.Idle);
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Control"),
                    "Reconnection attempts stopped.");
            }
        }
        
        /// <summary>
        /// Resets reconnection attempt counter
        /// </summary>
        public void ResetReconnectionAttempts()
        {
            reconnectionAttempts = 0;
            SetReconnectionState(ReconnectionState.Idle);
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Control"),
                    "Reconnection attempts reset.");
            }
        }
        
        #endregion
        
        #region Cooldown Management
        
        /// <summary>
        /// Checks if reconnection is currently in cooldown period
        /// </summary>
        /// <returns>True if in cooldown, false if ready to reconnect</returns>
        public bool IsInCooldown()
        {
            return Time.time - lastDisconnectTime < reconnectCooldown;
        }
        
        /// <summary>
        /// Gets remaining cooldown time in seconds
        /// </summary>
        /// <returns>Remaining cooldown time, or 0 if not in cooldown</returns>
        public float GetRemainingCooldown()
        {
            return Mathf.Max(0f, reconnectCooldown - (Time.time - lastDisconnectTime));
        }
        
        /// <summary>
        /// Marks disconnect time for cooldown calculation
        /// </summary>
        public void MarkDisconnectTime()
        {
            lastDisconnectTime = Time.time;
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Cooldown"),
                    "Disconnect time marked for cooldown.",
                    ("CooldownPeriod", reconnectCooldown));
            }
        }
        
        /// <summary>
        /// Checks if rapid reconnect attempt is being made
        /// </summary>
        /// <returns>True if attempting to reconnect too quickly</returns>
        public bool IsRapidReconnectAttempt()
        {
            if (IsInCooldown())
            {
                float remaining = GetRemainingCooldown();
                
                if (logReconnectionEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Cooldown"),
                        "Rapid reconnect attempt detected.",
                        ("RemainingCooldown", remaining));
                }
                
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region Internal Reconnection Logic
        
        /// <summary>
        /// Schedules a reconnection attempt with proper timing
        /// </summary>
        private void ScheduleReconnection()
        {
            if (reconnectionCoroutine != null)
            {
                productionNetworkManager.StopCoroutine(reconnectionCoroutine);
            }
            
            reconnectionCoroutine = productionNetworkManager.StartCoroutine(ReconnectionCoroutine());
        }
        
        /// <summary>
        /// Coroutine that handles the reconnection process
        /// </summary>
        private IEnumerator ReconnectionCoroutine()
        {
            reconnectionAttempts++;
            SetReconnectionState(ReconnectionState.Reconnecting);
            
            OnReconnectionStarted?.Invoke(reconnectionAttempts, maxReconnectionAttempts);
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Reconnection"),
                    "Attempting reconnection.",
                    ("Attempt", reconnectionAttempts),
                    ("MaxAttempts", maxReconnectionAttempts));
            }

            yield return new WaitForSeconds(reconnectionInterval);

            // Attempt to reconnect
            if (productionNetworkManager != null)
            {
                productionNetworkManager.StartClient();
            }
            
            reconnectionCoroutine = null;
        }
        
        /// <summary>
        /// Called when a connection attempt succeeds
        /// </summary>
        public void OnConnectionSucceeded()
        {
            if (reconnectionState == ReconnectionState.Reconnecting)
            {
                ResetReconnectionAttempts();
                OnReconnectionSucceeded?.Invoke();
                
                if (logReconnectionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Success"),
                        "Reconnection succeeded.");
                }
            }
        }
        
        /// <summary>
        /// Called when a connection attempt fails
        /// </summary>
        /// <param name="error">Error message from failed connection</param>
        public void OnConnectionFailed(string error)
        {
            if (reconnectionState == ReconnectionState.Reconnecting)
            {
                if (reconnectionAttempts >= maxReconnectionAttempts)
                {
                    string errorMessage = $"Max reconnection attempts ({maxReconnectionAttempts}) reached: {error}";
                    SetReconnectionState(ReconnectionState.Failed);
                    OnReconnectionFailed?.Invoke(errorMessage);
                    
                    if (logReconnectionEvents)
                    {
                        GameDebug.LogError(
                            BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Failure"),
                            "Reconnection failed after maximum attempts.",
                            ("Error", error),
                            ("Attempts", reconnectionAttempts));
                    }
                }
                else
                {
                    // Schedule next attempt
                    ScheduleReconnection();
                }
            }
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Sets the current reconnection state and triggers events
        /// </summary>
        /// <param name="newState">New reconnection state</param>
        private void SetReconnectionState(ReconnectionState newState)
        {
            if (reconnectionState != newState)
            {
                reconnectionState = newState;
                OnReconnectionStateChanged?.Invoke(reconnectionState);
                
                if (logReconnectionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Recovery, subsystem: "State"),
                        "Reconnection state changed.",
                        ("State", reconnectionState));
                }
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Update reconnection configuration at runtime
        /// </summary>
        /// <param name="enabled">Enable/disable reconnection</param>
        /// <param name="interval">Interval between attempts</param>
        /// <param name="maxAttempts">Maximum reconnection attempts</param>
        /// <param name="cooldown">Cooldown period before new attempts</param>
        public void UpdateConfiguration(bool enabled, float interval, int maxAttempts, float cooldown)
        {
            enableReconnection = enabled;
            reconnectionInterval = interval;
            maxReconnectionAttempts = maxAttempts;
            reconnectCooldown = cooldown;
            
            if (logReconnectionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Configuration"),
                    "Reconnection configuration updated.",
                    ("Enabled", enabled),
                    ("Interval", interval),
                    ("MaxAttempts", maxAttempts),
                    ("Cooldown", cooldown));
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get current reconnection state
        /// </summary>
        /// <returns>Current reconnection state</returns>
        public ReconnectionState GetReconnectionState()
        {
            return reconnectionState;
        }
        
        /// <summary>
        /// Get current reconnection attempt count
        /// </summary>
        /// <returns>Number of reconnection attempts made</returns>
        public int GetReconnectionAttempts()
        {
            return reconnectionAttempts;
        }
        
        /// <summary>
        /// Check if reconnection is currently enabled
        /// </summary>
        /// <returns>True if reconnection is enabled</returns>
        public bool IsReconnectionEnabled()
        {
            return enableReconnection;
        }
        
        /// <summary>
        /// Check if currently attempting to reconnect
        /// </summary>
        /// <returns>True if reconnection is in progress</returns>
        public bool IsReconnecting()
        {
            return reconnectionState == ReconnectionState.Reconnecting;
        }
        
        /// <summary>
        /// Get current reconnection configuration
        /// </summary>
        /// <returns>Reconnection configuration data</returns>
        public ReconnectionConfig GetConfiguration()
        {
            return new ReconnectionConfig
            {
                EnableReconnection = enableReconnection,
                ReconnectionInterval = reconnectionInterval,
                MaxReconnectionAttempts = maxReconnectionAttempts,
                ReconnectCooldown = reconnectCooldown
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for reconnection logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag, string subsystem = null)
        {
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                tag,
                subsystem ?? "Reconnection");
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// States for the reconnection system
    /// </summary>
    public enum ReconnectionState
    {
        Idle,           // Not attempting reconnection
        Reconnecting,   // Currently attempting to reconnect
        Failed          // Reconnection failed after maximum attempts
    }
    
    /// <summary>
    /// Configuration data structure for reconnection settings
    /// </summary>
    [System.Serializable]
    public struct ReconnectionConfig
    {
        public bool EnableReconnection;
        public float ReconnectionInterval;
        public int MaxReconnectionAttempts;
        public float ReconnectCooldown;
    }
    
    #endregion
}