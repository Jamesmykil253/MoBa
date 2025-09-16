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

        public bool TryCastAbility(int abilityIndex)
        {
            EnsureEnhancedSystem();
            return enhancedSystem != null && enhancedSystem.TryCastAbility(abilityIndex);
        }

        public bool IsAbilityReady(int abilityIndex)
        {
            EnsureEnhancedSystem();
            return enhancedSystem != null && enhancedSystem.IsAbilityReady(abilityIndex);
        }

        public float GetCooldownRemaining(int abilityIndex)
        {
            EnsureEnhancedSystem();
            return enhancedSystem != null ? enhancedSystem.GetCooldownRemaining(abilityIndex) : 0f;
        }

        public void SetInputEnabled(bool enabled)
        {
            EnsureEnhancedSystem();
            enhancedSystem?.SetInputEnabled(enabled);
        }

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
