using UnityEngine;
using MOBA.Configuration;
using MOBA.Debugging;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Simple configuration data structure
    /// </summary>
    public class GameConfiguration
    {
        public float matchDuration;
        public int scoreToWin;
        public bool autoStartOnEnable;
    }

    /// <summary>
    /// Manages game configuration, settings validation, and editor integration.
    /// Handles default config application and runtime configuration updates.
    /// </summary>
    public class GameConfigurationManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("Configuration Settings")]
        [SerializeField, Tooltip("Default game configuration asset")]
        private GameConfig defaultGameConfig;
        
        [SerializeField, Tooltip("Lock game settings to config values")]
        private bool lockGameSettingsToConfig = true;
        
        [SerializeField, Tooltip("Enable detailed configuration logging")]
        private bool logConfigurationEvents = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current default game configuration
        /// </summary>
        public GameConfig DefaultGameConfig => defaultGameConfig;
        
        /// <summary>
        /// Whether settings are locked to configuration
        /// </summary>
        public bool LockGameSettingsToConfig => lockGameSettingsToConfig;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            ApplyConfiguredDefaultsIfNeeded();
            
            if (logConfigurationEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Configuration manager initialized.",
                    ("DefaultConfig", defaultGameConfig != null),
                    ("LockSettings", lockGameSettingsToConfig));
            }
        }
        
        public override void Shutdown()
        {
            if (logConfigurationEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Configuration manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Configuration Management
        
        /// <summary>
        /// Apply configured defaults if needed and validation passes
        /// </summary>
        public void ApplyConfiguredDefaultsIfNeeded()
        {
            if (!lockGameSettingsToConfig || defaultGameConfig == null)
            {
                return;
            }
            
#if UNITY_EDITOR
            // Validate configuration in editor mode
            if (!Application.isPlaying)
            {
                defaultGameConfig.Validate();
            }
#endif
            
            if (simpleGameManager == null)
            {
                if (logConfigurationEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Configuration),
                        "Cannot apply configuration - SimpleGameManager not initialized.");
                }
                return;
            }
            
            // Apply configuration values
            ApplyGameSettings();
            ApplySpawnSettings();
            
            if (logConfigurationEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Configuration applied.",
                    ("MaxPlayers", simpleGameManager.maxPlayers),
                    ("GameTime", simpleGameManager.gameTime),
                    ("ScoreToWin", simpleGameManager.scoreToWin));
            }
        }
        
        /// <summary>
        /// Apply game settings from configuration
        /// </summary>
        private void ApplyGameSettings()
        {
            simpleGameManager.maxPlayers = Mathf.Clamp(defaultGameConfig.maxPlayers, 1, 20);
            simpleGameManager.gameTime = Mathf.Max(1f, defaultGameConfig.gameTimeInSeconds);
            simpleGameManager.scoreToWin = Mathf.Max(1, defaultGameConfig.scoreToWin);
            
            // Update match lifecycle service if available
            var matchService = simpleGameManager.GetMatchLifecycleService();
            matchService?.Configure(simpleGameManager.gameTime, simpleGameManager.scoreToWin);
        }
        
        /// <summary>
        /// Apply spawn settings from configuration
        /// </summary>
        private void ApplySpawnSettings()
        {
            // Apply spawn-related configuration if needed
            // This can be extended based on GameConfig properties
        }
        
        /// <summary>
        /// Set new default configuration
        /// </summary>
        /// <param name="newConfig">New configuration to apply</param>
        public void SetDefaultConfiguration(GameConfig newConfig)
        {
            defaultGameConfig = newConfig;
            
            if (lockGameSettingsToConfig)
            {
                ApplyConfiguredDefaultsIfNeeded();
            }
            
            if (logConfigurationEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Default configuration updated.",
                    ("ConfigName", newConfig?.name ?? "null"));
            }
        }
        
        /// <summary>
        /// Toggle settings lock to configuration
        /// </summary>
        /// <param name="locked">Whether to lock settings</param>
        public void SetConfigurationLock(bool locked)
        {
            lockGameSettingsToConfig = locked;
            
            if (locked)
            {
                ApplyConfiguredDefaultsIfNeeded();
            }
            
            if (logConfigurationEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Configuration lock changed.",
                    ("Locked", locked));
            }
        }
        
        /// <summary>
        /// Validate current configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration()
        {
            if (defaultGameConfig == null)
            {
                if (logConfigurationEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Validation),
                        "No default configuration assigned.");
                }
                return false;
            }
            
            try
            {
                defaultGameConfig.Validate();
                
                if (logConfigurationEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Validation),
                        "Configuration validation passed.");
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "Configuration validation failed.",
                    ("Error", ex.Message));
                
                return false;
            }
        }
        
        /// <summary>
        /// Get configuration value by name
        /// </summary>
        /// <param name="propertyName">Name of configuration property</param>
        /// <returns>Configuration value or null if not found</returns>
        public object GetConfigurationValue(string propertyName)
        {
            if (defaultGameConfig == null)
            {
                return null;
            }
            
            // Use reflection to get property value
            var property = defaultGameConfig.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(defaultGameConfig);
            }
            
            var field = defaultGameConfig.GetType().GetField(propertyName);
            if (field != null)
            {
                return field.GetValue(defaultGameConfig);
            }
            
            if (logConfigurationEvents)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Configuration property not found.",
                    ("PropertyName", propertyName));
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if a valid configuration is available
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool HasValidConfig()
        {
            return simpleGameManager != null && 
                   simpleGameManager.gameTime > 0 && 
                   simpleGameManager.scoreToWin > 0;
        }
        
        /// <summary>
        /// Get the current game configuration
        /// </summary>
        /// <returns>Current configuration or null if invalid</returns>
        public GameConfiguration GetCurrentConfig()
        {
            if (!HasValidConfig())
            {
                return null;
            }
            
            return new GameConfiguration
            {
                matchDuration = simpleGameManager.gameTime,
                scoreToWin = simpleGameManager.scoreToWin,
                autoStartOnEnable = simpleGameManager.AutoStartOnEnable
            };
        }

        #endregion
        
        #region Editor Integration
        
#if UNITY_EDITOR
        /// <summary>
        /// Called when inspector values change in editor
        /// </summary>
        private void OnValidate()
        {
            if (isInitialized)
            {
                ApplyConfiguredDefaultsIfNeeded();
            }
        }
#endif
        
        #endregion
    }
}