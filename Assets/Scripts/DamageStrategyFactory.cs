using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Factory Pattern implementation for damage strategy creation
    /// Based on GoF Factory Pattern and Strategy Pattern from Game Programming Patterns
    /// Provides centralized creation of damage calculation strategies
    /// </summary>
    public static class DamageStrategyFactory
    {
        /// <summary>
        /// Creates a damage strategy based on the specified type
        /// </summary>
        public static IDamageStrategy Create(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return CreatePhysicalStrategy();
                case DamageType.Magical:
                    return CreateMagicalStrategy();
                case DamageType.Hybrid:
                    return CreateHybridStrategy();
                case DamageType.True:
                    return CreateTrueDamageStrategy();
                case DamageType.Healing:
                    return CreateHealingStrategy();
                case DamageType.Shield:
                    return CreateShieldStrategy();
                default:
                    Debug.LogWarning($"Unknown damage type: {damageType}, using Physical");
                    return CreatePhysicalStrategy();
            }
        }

        /// <summary>
        /// Creates a physical damage strategy with default settings
        /// </summary>
        public static IDamageStrategy CreatePhysicalStrategy()
        {
            var strategy = CreateStrategyGameObject<PhysicalDamageStrategy>("PhysicalDamageStrategy");
            return strategy;
        }

        /// <summary>
        /// Creates a magical damage strategy with default settings
        /// </summary>
        public static IDamageStrategy CreateMagicalStrategy()
        {
            var strategy = CreateStrategyGameObject<MagicalDamageStrategy>("MagicalDamageStrategy");
            return strategy;
        }

        /// <summary>
        /// Creates a hybrid damage strategy with default settings
        /// </summary>
        public static IDamageStrategy CreateHybridStrategy()
        {
            var strategy = CreateStrategyGameObject<HybridDamageStrategy>("HybridDamageStrategy");
            return strategy;
        }

        /// <summary>
        /// Creates a true damage strategy (ignores all mitigation)
        /// </summary>
        public static IDamageStrategy CreateTrueDamageStrategy()
        {
            var strategy = CreateStrategyGameObject<TrueDamageStrategy>("TrueDamageStrategy");
            return strategy;
        }

        /// <summary>
        /// Creates a healing strategy
        /// </summary>
        public static IDamageStrategy CreateHealingStrategy()
        {
            var strategy = CreateStrategyGameObject<HealingStrategy>("HealingStrategy");
            return strategy;
        }

        /// <summary>
        /// Creates a shield damage strategy
        /// </summary>
        public static IDamageStrategy CreateShieldStrategy()
        {
            var strategy = CreateStrategyGameObject<ShieldDamageStrategy>("ShieldDamageStrategy");
            return strategy;
        }

        /// <summary>
        /// Creates a customized physical damage strategy
        /// </summary>
        public static IDamageStrategy CreateCustomPhysicalStrategy(
            float critBonus = 0f,
            float lifestealPercent = 0f,
            float damageReflection = 0f)
        {
            var strategy = CreateStrategyGameObject<PhysicalDamageStrategy>("CustomPhysicalStrategy");

            if (strategy is PhysicalDamageStrategy physicalStrategy)
            {
                physicalStrategy.SetCritChanceBonus(critBonus);
                physicalStrategy.SetLifestealPercent(lifestealPercent);
                physicalStrategy.SetDamageReflection(damageReflection);
            }

            return strategy;
        }

        /// <summary>
        /// Creates a customized magical damage strategy
        /// </summary>
        public static IDamageStrategy CreateCustomMagicalStrategy(
            float critBonus = 0f,
            float spellAmp = 0f,
            float magicPen = 0f)
        {
            var strategy = CreateStrategyGameObject<MagicalDamageStrategy>("CustomMagicalStrategy");

            if (strategy is MagicalDamageStrategy magicalStrategy)
            {
                magicalStrategy.SetCritChanceBonus(critBonus);
                magicalStrategy.SetSpellAmplification(spellAmp);
                magicalStrategy.SetMagicPenetration(magicPen);
            }

            return strategy;
        }

        /// <summary>
        /// Creates a customized hybrid damage strategy
        /// </summary>
        public static IDamageStrategy CreateCustomHybridStrategy(
            float physicalRatio = 0.5f,
            bool weightedCrit = true,
            float physicalCritBonus = 0f,
            float magicalCritBonus = 0f,
            float spellAmp = 0f,
            float magicPen = 0f)
        {
            var strategy = CreateStrategyGameObject<HybridDamageStrategy>("CustomHybridStrategy");

            if (strategy is HybridDamageStrategy hybridStrategy)
            {
                hybridStrategy.SetPhysicalRatio(physicalRatio);
                hybridStrategy.SetWeightedCrit(weightedCrit);

                hybridStrategy.ConfigureStrategies(
                    physicalCritBonus,
                    0f, // lifesteal
                    magicalCritBonus,
                    spellAmp,
                    magicPen
                );
            }

            return strategy;
        }

        /// <summary>
        /// Gets the recommended strategy for an ability type
        /// </summary>
        public static IDamageStrategy GetRecommendedStrategyForAbility(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.PrimaryAttack:
                    return CreatePhysicalStrategy();
                case AbilityType.Ability1:
                    return CreateCustomHybridStrategy(0.7f, true, 0.1f, 0.05f, 0.1f, 0f);
                case AbilityType.Ability2:
                    return CreateMagicalStrategy();
                case AbilityType.Ultimate:
                    return CreateCustomHybridStrategy(0.5f, true, 0.2f, 0.15f, 0.25f, 0.1f);
                default:
                    return CreatePhysicalStrategy();
            }
        }

        /// <summary>
        /// Creates a strategy GameObject with the specified component
        /// </summary>
        private static T CreateStrategyGameObject<T>(string name) where T : MonoBehaviour, IDamageStrategy
        {
            var go = new GameObject(name);
            var strategy = go.AddComponent<T>();

            // Set as child of a strategies container for organization
            var container = GameObject.Find("DamageStrategies");
            if (container == null)
            {
                container = new GameObject("DamageStrategies");
            }
            go.transform.SetParent(container.transform);

            return strategy;
        }

        /// <summary>
        /// Validates a damage strategy implementation
        /// </summary>
        public static bool ValidateStrategy(IDamageStrategy strategy)
        {
            if (strategy == null)
            {
                Debug.LogError("Damage strategy is null");
                return false;
            }

            try
            {
                var damageType = strategy.GetDamageType();

                // Test with a basic damage context
                var testContext = DamageContext.Create(
                    new CharacterStats { Attack = 100, PhysicalDefense = 50, MaxHP = 1000, CurrentHP = 1000, IsAlive = true },
                    new CharacterStats { Attack = 80, PhysicalDefense = 40, MaxHP = 1000, CurrentHP = 1000, IsAlive = true }
                );

                var result = strategy.CalculateDamage(testContext);

                if (result.RawDamage < 0 || result.MitigatedDamage < 0)
                {
                    Debug.LogError($"Strategy {strategy.GetType().Name} produced negative damage");
                    return false;
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Strategy validation failed for {strategy.GetType().Name}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets performance statistics for strategy creation
        /// </summary>
        public static void LogCreationStats()
        {
            var strategies = GameObject.FindGameObjectsWithTag("DamageStrategy");
            Debug.Log($"Active damage strategies: {strategies.Length}");

            foreach (var strategy in strategies)
            {
                var damageStrategy = strategy.GetComponent<IDamageStrategy>();
                if (damageStrategy != null)
                {
                    Debug.Log($"{strategy.name}: {damageStrategy.GetDamageType()}");
                }
            }
        }
    }

    // Additional strategy implementations for completeness

    /// <summary>
    /// True damage strategy - ignores all mitigation
    /// </summary>
    public class TrueDamageStrategy : MonoBehaviour, IDamageStrategy
    {
        public DamageResult CalculateDamage(DamageContext context)
        {
            float rawDamage = context.AttackerStats.TotalAttack * context.BonusMultiplier;
            rawDamage *= context.AbilityData?.damage ?? 1f;

            return DamageResult.Create(
                rawDamage,
                rawDamage, // No mitigation
                DamageType.True,
                context.IsCritical,
                0f, 0f, 0f
            );
        }

        public DamageType GetDamageType() => DamageType.True;
    }

    /// <summary>
    /// Healing strategy for restorative abilities
    /// </summary>
    public class HealingStrategy : MonoBehaviour, IDamageStrategy
    {
        [SerializeField] private float baseHealingMultiplier = 1f;
        [SerializeField] private float critHealingBonus = 0.5f;

        public DamageResult CalculateDamage(DamageContext context)
        {
            float rawHealing = context.AttackerStats.TotalTechAttack * baseHealingMultiplier * context.BonusMultiplier;
            rawHealing *= context.AbilityData?.damage ?? 1f;

            bool isCritical = context.IsCritical || (Random.value < 0.1f); // 10% crit chance
            if (isCritical)
            {
                rawHealing *= (1f + critHealingBonus);
            }

            return DamageResult.Create(
                rawHealing,
                rawHealing,
                DamageType.Healing,
                isCritical,
                0f, 0f, 0f,
                rawHealing
            );
        }

        public DamageType GetDamageType() => DamageType.Healing;
    }

    /// <summary>
    /// Shield damage strategy for shield-based abilities
    /// </summary>
    public class ShieldDamageStrategy : MonoBehaviour, IDamageStrategy
    {
        [SerializeField] private float shieldEfficiency = 0.8f; // 80% of damage becomes shield

        public DamageResult CalculateDamage(DamageContext context)
        {
            float rawDamage = context.AttackerStats.TotalAttack * context.BonusMultiplier;
            rawDamage *= context.AbilityData?.damage ?? 1f;

            float shieldValue = rawDamage * shieldEfficiency;

            return DamageResult.Create(
                rawDamage,
                rawDamage,
                DamageType.Shield,
                context.IsCritical,
                0f, 0f, 0f,
                shieldValue
            );
        }

        public DamageType GetDamageType() => DamageType.Shield;
    }
}