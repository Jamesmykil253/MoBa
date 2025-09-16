using System;
using System.Text;
using UnityEngine;

namespace MOBA.Debugging
{
    /// <summary>
    /// Centralised debug helper that respects <see cref="GameDebugSettings"/> configuration.
    /// Supports filtering by category, system, mechanic, and optional subsystem identifiers.
    /// </summary>
    public static class GameDebug
    {
        private const string SettingsResourceName = "DebugSettings";
        private static GameDebugSettings cachedSettings;
        private static bool attemptedLoad;

        private static bool useAdvancedFiltering;
        private static GameDebugSettings Settings
        {
            get
            {
                if (cachedSettings != null)
                {
                    return cachedSettings;
                }

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

        /// <summary>
        /// Enables or disables the advanced filtering workflow. When disabled (default) all messages log immediately with a short prefix.
        /// </summary>
        public static void UseAdvancedFiltering(bool enable)
        {
            useAdvancedFiltering = enable;
        }

        /// <summary>
        /// Logs an informational message with the specified context.
        /// </summary>
        public static void Log(GameDebugContext context, string message, params (string Key, object Value)[] details)
        {
            if (!ShouldLog(context))
            {
                return;
            }

            Debug.Log(Format(context, message, details));
        }

        /// <summary>
        /// Logs an informational message with a category only (legacy overload).
        /// </summary>
        public static void Log(string message, GameDebugCategory category = GameDebugCategory.General)
        {
            Log(new GameDebugContext(category), message);
        }

        /// <summary>
        /// Logs a warning with the specified context.
        /// </summary>
        public static void LogWarning(GameDebugContext context, string message, params (string Key, object Value)[] details)
        {
            if (!ShouldLog(context) || Settings.suppressWarnings)
            {
                return;
            }

            Debug.LogWarning(Format(context, message, details));
        }

        /// <summary>
        /// Logs a warning with a category only (legacy overload).
        /// </summary>
        public static void LogWarning(string message, GameDebugCategory category = GameDebugCategory.General)
        {
            LogWarning(new GameDebugContext(category), message);
        }

        /// <summary>
        /// Logs an error with the specified context.
        /// </summary>
        public static void LogError(GameDebugContext context, string message, params (string Key, object Value)[] details)
        {
            if (!ShouldLog(context))
            {
                return;
            }

            Debug.LogError(Format(context, message, details));
        }

        /// <summary>
        /// Logs an error with a category only (legacy overload).
        /// </summary>
        public static void LogError(string message, GameDebugCategory category = GameDebugCategory.General)
        {
            LogError(new GameDebugContext(category), message);
        }

        /// <summary>
        /// Logs an exception with the specified context.
        /// </summary>
        public static void LogException(Exception exception, GameDebugContext context)
        {
            if (!ShouldLog(context))
            {
                return;
            }

            Debug.LogException(exception);
        }

        /// <summary>
        /// Logs an exception with a category only (legacy overload).
        /// </summary>
        public static void LogException(Exception exception, GameDebugCategory category = GameDebugCategory.General)
        {
            LogException(exception, new GameDebugContext(category));
        }

        /// <summary>
        /// Logs a state snapshot; syntactic sugar for <see cref="Log(GameDebugContext, string, (string Key, object Value)[])"/>.
        /// </summary>
        public static void LogState(GameDebugContext context, string message, params (string Key, object Value)[] stateEntries)
        {
            Log(context, message, stateEntries);
        }

        private static bool ShouldLog(GameDebugContext context)
        {
            if (!useAdvancedFiltering)
            {
                return true;
            }

            if (!Settings.IsEnabled(context.Category))
            {
                return false;
            }

            if (!Settings.IsSystemEnabled(context.System))
            {
                return false;
            }

            if (!Settings.IsMechanicEnabled(context.Mechanic))
            {
                return false;
            }

            if (!Settings.IsSubsystemEnabled(context.Subsystem))
            {
                return false;
            }

            return true;
        }

        private static string Format(GameDebugContext context, string message, params (string Key, object Value)[] details)
        {
            if (!useAdvancedFiltering)
            {
                if (details == null || details.Length == 0)
                {
                    return $"[{context.Category}] {message}";
                }

                var simpleBuilder = new StringBuilder();
                simpleBuilder.Append('[').Append(context.Category).Append("] ").Append(message);
                simpleBuilder.Append(" | ");
                for (int i = 0; i < details.Length; i++)
                {
                    var (key, value) = details[i];
                    simpleBuilder.Append(key).Append('=').Append(value ?? "null");
                    if (i < details.Length - 1)
                    {
                        simpleBuilder.Append(", ");
                    }
                }

                return simpleBuilder.ToString();
            }

            var builder = new StringBuilder();
            builder.Append('[').Append(context.Category).Append(']');
            builder.Append("[SYS:").Append(context.System).Append(']');
            builder.Append("[MECH:").Append(context.Mechanic).Append(']');

            if (!string.IsNullOrWhiteSpace(context.Subsystem))
            {
                builder.Append("[SUB:").Append(context.Subsystem).Append(']');
            }

            if (!string.IsNullOrWhiteSpace(context.Actor))
            {
                builder.Append("[ACT:").Append(context.Actor).Append(']');
            }

            builder.Append(' ').Append(message);

            if (details != null && details.Length > 0)
            {
                builder.Append(" | ");
                for (int i = 0; i < details.Length; i++)
                {
                    var (key, value) = details[i];
                    builder.Append(key).Append('=').Append(value ?? "null");
                    if (i < details.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            return builder.ToString();
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
