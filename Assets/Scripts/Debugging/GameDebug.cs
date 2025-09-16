using System;
using UnityEngine;

namespace MOBA.Debugging
{
    /// <summary>
    /// Centralised debug helper that respects <see cref="GameDebugSettings"/> configuration.
    /// </summary>
    public static class GameDebug
    {
        private const string SettingsResourceName = "DebugSettings";
        private static GameDebugSettings cachedSettings;
        private static bool attemptedLoad;

        private static GameDebugSettings Settings
        {
            get
            {
                if (cachedSettings != null)
                    return cachedSettings;

                if (!attemptedLoad)
                {
                    attemptedLoad = true;
                    cachedSettings = Resources.Load<GameDebugSettings>(SettingsResourceName);
                    if (cachedSettings == null)
                    {
                        cachedSettings = ScriptableObject.CreateInstance<GameDebugSettings>();
                        Debug.LogWarning("[GameDebug] DebugSettings asset not found in Resources. Using default verbose configuration.");
                    }
                }

                return cachedSettings;
            }
        }

        public static void Log(string message, GameDebugCategory category = GameDebugCategory.General)
        {
            if (!ShouldLog(category)) return;
            Debug.Log(Format(message, category));
        }

        public static void LogWarning(string message, GameDebugCategory category = GameDebugCategory.General)
        {
            if (!ShouldLog(category)) return;
            if (Settings.suppressWarnings) return;
            Debug.LogWarning(Format(message, category));
        }

        public static void LogError(string message, GameDebugCategory category = GameDebugCategory.General)
        {
            if (!ShouldLog(category)) return;
            Debug.LogError(Format(message, category));
        }

        public static void LogException(Exception exception, GameDebugCategory category = GameDebugCategory.General)
        {
            if (!ShouldLog(category)) return;
            Debug.LogException(exception);
        }

        private static bool ShouldLog(GameDebugCategory category)
        {
            return Settings.IsEnabled(category);
        }

        private static string Format(string message, GameDebugCategory category)
        {
            return $"[{category}] {message}";
        }

        /// <summary>
        /// Forces reloading the debug settings (useful when changes were made in editor at runtime).
        /// </summary>
        public static void ReloadSettings()
        {
            attemptedLoad = false;
            cachedSettings = null;
        }
    }
}
