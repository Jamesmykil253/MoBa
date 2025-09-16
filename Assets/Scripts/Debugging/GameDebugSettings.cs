using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA.Debugging
{
    /// <summary>
    /// ScriptableObject that controls runtime debug logging behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "MOBA/Debug Settings", order = 0)]
    public class GameDebugSettings : ScriptableObject
    {
        [Header("Global")]
        public bool logEverything = true;
        public bool suppressWarnings = false;

        [Header("Categories")]
        public bool logGameLifecycle = true;
        public bool logPlayer = true;
        public bool logCamera = true;
        public bool logAbility = true;
        public bool logEnemy = true;
        public bool logNetworking = true;
        public bool logInput = true;
        public bool logPooling = true;
        public bool logUI = true;
        public bool logPerformance = true;
        public bool logErrorHandling = true;
        public bool logConfiguration = true;
        public bool logInitialization = true;
        public bool logScene = true;
        public bool logMovement = true;
        public bool logCombat = true;
        public bool logHealth = true;
        public bool logResource = true;
        public bool logProjectile = true;
        public bool logAI = true;
        public bool logAudio = true;
        public bool logDiagnostics = true;

        [Header("Systems")]
        [SerializeField] private List<SystemToggle> systemToggles = new();

        [Header("Mechanics")]
        [SerializeField] private List<MechanicToggle> mechanicToggles = new();

        [Header("Subsystem Overrides")]
        [SerializeField] private List<SubsystemToggle> subsystemToggles = new();

        private Dictionary<GameDebugSystemTag, bool> systemLookup;
        private Dictionary<GameDebugMechanicTag, bool> mechanicLookup;
        private Dictionary<string, bool> subsystemLookup;

        [Serializable]
        private class SystemToggle
        {
            public GameDebugSystemTag system;
            public bool enabled = true;
        }

        [Serializable]
        private class MechanicToggle
        {
            public GameDebugMechanicTag mechanic;
            public bool enabled = true;
        }

        [Serializable]
        private class SubsystemToggle
        {
            public string name = string.Empty;
            public bool enabled = true;
        }

        private void OnEnable()
        {
            BuildLookups();
        }

        private void OnValidate()
        {
            BuildLookups();
        }

        /// <summary>
        /// Determines whether the specified category should be logged.
        /// </summary>
        public bool IsEnabled(GameDebugCategory category)
        {
            if (logEverything)
            {
                return true;
            }

            return category switch
            {
                GameDebugCategory.GameLifecycle => logGameLifecycle,
                GameDebugCategory.Player => logPlayer,
                GameDebugCategory.Camera => logCamera,
                GameDebugCategory.Ability => logAbility,
                GameDebugCategory.Enemy => logEnemy,
                GameDebugCategory.Networking => logNetworking,
                GameDebugCategory.Input => logInput,
                GameDebugCategory.Pooling => logPooling,
                GameDebugCategory.UI => logUI,
                GameDebugCategory.Performance => logPerformance,
                GameDebugCategory.ErrorHandling => logErrorHandling,
                GameDebugCategory.Configuration => logConfiguration,
                GameDebugCategory.Initialization => logInitialization,
                GameDebugCategory.Scene => logScene,
                GameDebugCategory.Movement => logMovement,
                GameDebugCategory.Combat => logCombat,
                GameDebugCategory.Health => logHealth,
                GameDebugCategory.Resource => logResource,
                GameDebugCategory.Projectile => logProjectile,
                GameDebugCategory.AI => logAI,
                GameDebugCategory.Audio => logAudio,
                GameDebugCategory.Diagnostics => logDiagnostics,
                _ => logEverything
            };
        }

        /// <summary>
        /// Determines whether the specified system is allowed to log.
        /// </summary>
        public bool IsSystemEnabled(GameDebugSystemTag system)
        {
            if (logEverything)
            {
                return true;
            }

            EnsureLookups();
            return systemLookup.TryGetValue(system, out bool enabled) ? enabled : true;
        }

        /// <summary>
        /// Determines whether the specified mechanic is allowed to log.
        /// </summary>
        public bool IsMechanicEnabled(GameDebugMechanicTag mechanic)
        {
            if (logEverything)
            {
                return true;
            }

            EnsureLookups();
            return mechanicLookup.TryGetValue(mechanic, out bool enabled) ? enabled : true;
        }

        /// <summary>
        /// Determines whether the specified subsystem (optional string identifier) is allowed to log.
        /// </summary>
        public bool IsSubsystemEnabled(string subsystem)
        {
            if (logEverything || string.IsNullOrWhiteSpace(subsystem))
            {
                return true;
            }

            EnsureLookups();
            return subsystemLookup.TryGetValue(subsystem, out bool enabled) ? enabled : true;
        }

        private void EnsureLookups()
        {
            if (systemLookup == null || mechanicLookup == null || subsystemLookup == null)
            {
                BuildLookups();
            }
        }

        private void BuildLookups()
        {
            EnsureSystemCoverage();
            EnsureMechanicCoverage();

            systemLookup = new Dictionary<GameDebugSystemTag, bool>();
            foreach (var toggle in systemToggles)
            {
                systemLookup[toggle.system] = toggle.enabled;
            }

            mechanicLookup = new Dictionary<GameDebugMechanicTag, bool>();
            foreach (var toggle in mechanicToggles)
            {
                mechanicLookup[toggle.mechanic] = toggle.enabled;
            }

            subsystemLookup = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var toggle in subsystemToggles)
            {
                if (string.IsNullOrWhiteSpace(toggle.name))
                {
                    continue;
                }

                subsystemLookup[toggle.name] = toggle.enabled;
            }
        }

        private void EnsureSystemCoverage()
        {
            if (systemToggles == null)
            {
                systemToggles = new List<SystemToggle>();
            }

            foreach (GameDebugSystemTag value in Enum.GetValues(typeof(GameDebugSystemTag)))
            {
                if (!systemToggles.Exists(t => t.system == value))
                {
                    systemToggles.Add(new SystemToggle { system = value, enabled = true });
                }
            }
        }

        private void EnsureMechanicCoverage()
        {
            if (mechanicToggles == null)
            {
                mechanicToggles = new List<MechanicToggle>();
            }

            foreach (GameDebugMechanicTag value in Enum.GetValues(typeof(GameDebugMechanicTag)))
            {
                if (!mechanicToggles.Exists(t => t.mechanic == value))
                {
                    mechanicToggles.Add(new MechanicToggle { mechanic = value, enabled = true });
                }
            }
        }
    }
}
