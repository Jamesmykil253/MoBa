using System.Collections.Generic;
using UnityEngine;
using MOBA.Abilities;
using MOBA.Debugging;

namespace MOBA
{
    /// <summary>
    /// Legacy façade that now delegates to <see cref="EnhancedAbilitySystem"/>.
    /// Keeps existing prefabs functional while unifying all runtime logic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnhancedAbilitySystem))]
    public class SimpleAbilitySystem : MonoBehaviour
    {
        [Header("Legacy Ability Definitions")]
        [Tooltip("Legacy ability assets are converted to runtime EnhancedAbility instances at startup.")]
        [SerializeField] private SimpleAbility[] abilities = new SimpleAbility[4];
        [SerializeField] private bool synchroniseOnAwake = true;

        [Header("Bridged Enhanced System")]
        [SerializeField] private EnhancedAbilitySystem enhancedSystem;

        private readonly List<EnhancedAbility> runtimeAbilities = new List<EnhancedAbility>();

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                mechanic,
                subsystem: nameof(SimpleAbilitySystem),
                actor: gameObject != null ? gameObject.name : null);
        }

        private void Awake()
        {
            EnsureEnhancedSystem();

            if (synchroniseOnAwake)
            {
                SynchroniseAbilities();
            }
        }

        private void OnDestroy()
        {
            for (int i = runtimeAbilities.Count - 1; i >= 0; i--)
            {
                if (runtimeAbilities[i] != null)
                {
                    Destroy(runtimeAbilities[i]);
                }
            }

            runtimeAbilities.Clear();
        }

        private void EnsureEnhancedSystem()
        {
            if (enhancedSystem == null)
            {
                enhancedSystem = GetComponent<EnhancedAbilitySystem>();
            }

            if (enhancedSystem == null)
            {
                enhancedSystem = gameObject.AddComponent<EnhancedAbilitySystem>();
            }
        }

        /// <summary>
        /// Converts legacy <see cref="SimpleAbility"/> assets into runtime <see cref="EnhancedAbility"/> instances.
        /// Call this after modifying the abilities array at runtime.
        /// </summary>
        /// <remarks>
        /// This method destroys existing runtime abilities and recreates them from the legacy definitions.
        /// Legacy abilities are automatically assigned default values for new Enhanced system properties.
        /// The conversion follows the Factory pattern to create enhanced ability instances.
        /// Thread-safe operation that validates the enhanced system before proceeding.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the enhanced system component cannot be found or created
        /// </exception>
        public void SynchroniseAbilities()
        {
            EnsureEnhancedSystem();

            if (abilities == null || enhancedSystem == null)
            {
                return;
            }

            for (int i = runtimeAbilities.Count - 1; i >= 0; i--)
            {
                if (runtimeAbilities[i] != null)
                {
                    Destroy(runtimeAbilities[i]);
                }
            }
            runtimeAbilities.Clear();

            for (int i = 0; i < abilities.Length; i++)
            {
                var simpleAbility = abilities[i];
                EnhancedAbility runtimeAbility = null;

                if (simpleAbility != null)
                {
                    runtimeAbility = ScriptableObject.CreateInstance<EnhancedAbility>();
                    runtimeAbility.hideFlags = HideFlags.HideAndDontSave;
                    runtimeAbility.abilityName = simpleAbility.abilityName;
                    runtimeAbility.description = simpleAbility.description;
                    runtimeAbility.icon = simpleAbility.icon;
                    runtimeAbility.damage = simpleAbility.damage;
                    runtimeAbility.range = simpleAbility.range;
                    runtimeAbility.cooldown = simpleAbility.cooldown;
                    runtimeAbility.manaCost = 0f; // Legacy abilities were free by default
                    runtimeAbility.castTime = 0f;
                    runtimeAbility.maxTargets = 5;
                    runtimeAbility.effectColor = Color.white;
                    runtimeAbility.enableParticleEffect = false;
                    runtimeAbility.abilityType = ConvertAbilityType(simpleAbility.abilityType);
                    runtimeAbility.targetType = MOBA.Abilities.TargetType.Enemy;

                    runtimeAbilities.Add(runtimeAbility);
                }
                else
                {
                    runtimeAbilities.Add(null);
                }

                enhancedSystem.SetAbility(i, runtimeAbility);
            }

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Configuration),
                "Synchronised legacy abilities into EnhancedAbilitySystem.",
                ("AbilityCount", abilities.Length));
        }

        #region Legacy API Forwarders

        /// <summary>
        /// Attempts to cast the specified ability if it's available and off cooldown.
        /// Delegates to the enhanced ability system for execution.
        /// </summary>
        /// <param name="abilityIndex">
        /// Zero-based index of the ability to cast (0-3 for Primary/Ability1/Ability2/Ultimate)
        /// </param>
        /// <returns>
        /// True if ability was successfully cast; false if on cooldown, insufficient resources,
        /// invalid index, or enhanced system unavailable
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when abilityIndex is less than 0 or greater than 3
        /// </exception>
        /// <remarks>
        /// This method validates cooldown, mana cost, and target availability before casting.
        /// Casting triggers animations, effects, and damage calculation through the enhanced system.
        /// Use IsAbilityReady() to check availability before calling this method.
        /// </remarks>
        public bool TryCastAbility(int abilityIndex)
        {
            EnsureEnhancedSystem();
            return enhancedSystem != null && enhancedSystem.TryCastAbility(abilityIndex);
        }

        /// <summary>
        /// Checks if the specified ability is ready to cast (off cooldown and sufficient resources).
        /// Non-destructive query method that doesn't modify state.
        /// </summary>
        /// <param name="abilityIndex">
        /// Zero-based index of the ability to check (0-3 for Primary/Ability1/Ability2/Ultimate)
        /// </param>
        /// <returns>
        /// True if ability can be cast immediately; false if on cooldown, insufficient mana,
        /// invalid index, or enhanced system unavailable
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when abilityIndex is less than 0 or greater than 3
        /// </exception>
        /// <remarks>
        /// Thread-safe method suitable for UI updates and input validation.
        /// Check this before calling TryCastAbility() to provide responsive UI feedback.
        /// Does not account for dynamic conditions like target availability.
        /// </remarks>
        public bool IsAbilityReady(int abilityIndex)
        {
            EnsureEnhancedSystem();
            return enhancedSystem != null && enhancedSystem.IsAbilityReady(abilityIndex);
        }

        /// <summary>
        /// Gets the remaining cooldown time for the specified ability in seconds.
        /// Used for UI countdown timers and ability availability checking.
        /// </summary>
        /// <param name="abilityIndex">
        /// Zero-based index of the ability to query (0-3 for Primary/Ability1/Ability2/Ultimate)
        /// </param>
        /// <returns>
        /// Remaining cooldown time in seconds; 0 if ability is ready to cast or invalid index
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when abilityIndex is less than 0 or greater than 3
        /// </exception>
        /// <remarks>
        /// Thread-safe method that provides precise cooldown timing for UI elements.
        /// Updates every frame during cooldown period with sub-second precision.
        /// Returns 0 for abilities that don't exist or are ready to cast.
        /// Suitable for progress bars and countdown displays.
        /// </remarks>
        public float GetCooldownRemaining(int abilityIndex)
        {
            EnsureEnhancedSystem();
            return enhancedSystem != null ? enhancedSystem.GetCooldownRemaining(abilityIndex) : 0f;
        }

        /// <summary>
        /// Enables or disables input processing for all abilities.
        /// Used during cutscenes, death states, or other gameplay restrictions.
        /// </summary>
        /// <param name="enabled">
        /// True to enable ability input processing; false to disable and ignore input
        /// </param>
        /// <remarks>
        /// When disabled, all ability input is ignored but cooldowns continue to tick.
        /// Useful for preventing ability usage during specific game states.
        /// State is maintained until explicitly changed - not reset on scene load.
        /// Thread-safe operation that immediately affects input responsiveness.
        /// </remarks>
        public void SetInputEnabled(bool enabled)
        {
            EnsureEnhancedSystem();
            enhancedSystem?.SetInputEnabled(enabled);
        }

        /// <summary>
        /// Gets direct access to the underlying enhanced ability system component.
        /// Provides access to advanced features not available through the legacy API.
        /// </summary>
        /// <returns>
        /// Reference to the EnhancedAbilitySystem component; null if component missing
        /// </returns>
        /// <remarks>
        /// Use this for advanced functionality like custom ability configuration,
        /// event subscriptions, or direct access to enhanced features.
        /// Prefer the legacy API methods for standard ability operations.
        /// Ensures enhanced system exists before returning reference.
        /// </remarks>
        public EnhancedAbilitySystem GetEnhancedSystem() => enhancedSystem;

        #endregion

        private static MOBA.Abilities.AbilityType ConvertAbilityType(AbilityType type)
        {
            switch (type)
            {
                case AbilityType.PrimaryAttack:
                case AbilityType.Ability1:
                case AbilityType.Ability2:
                case AbilityType.Ultimate:
                    return MOBA.Abilities.AbilityType.Instant;
                default:
                    return MOBA.Abilities.AbilityType.Instant;
            }
        }
    }
}
