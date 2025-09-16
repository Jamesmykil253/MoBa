using System;
using System.Reflection;
using UnityEngine;

namespace MOBA.Initialization
{
    /// <summary>
    /// Disables Adaptive Performance auto-start when no provider is configured to avoid noisy warnings.
    /// </summary>
    internal static class AdaptivePerformanceDisabler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableIfNoProvider()
        {
            try
            {
                var generalSettingsType = Type.GetType("UnityEngine.AdaptivePerformance.AdaptivePerformanceGeneralSettings, Unity.AdaptivePerformance");
                if (generalSettingsType == null)
                    return;

                var instance = generalSettingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (instance == null)
                    return;

                var manager = generalSettingsType.GetProperty("Manager", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
                if (manager == null)
                    return;

                var activeLoader = manager.GetType().GetProperty("activeLoader", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manager);
                if (activeLoader != null)
                    return; // Provider already configured

                // Disable any automatic initialization flags so Unity stops trying to start without a provider.
                SetBoolMember(manager, "initializeOnStartup", false);
                SetBoolMember(manager, "automaticLoading", false);
                SetBoolMember(manager, "automaticRunning", false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdaptivePerformanceDisabler] Failed to adjust settings: {ex.Message}");
            }
        }

        private static void SetBoolMember(object target, string memberName, bool value)
        {
            if (target == null)
                return;

            var type = target.GetType();
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(target, value);
            }

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
            {
                property.SetValue(target, value);
            }
        }
    }
}
