using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Strategy Pattern interface for damage calculation algorithms
    /// Based on Game Programming Patterns - Strategy Pattern
    /// Extends MonoBehaviour for Unity integration
    /// </summary>
    public interface IDamageStrategy
    {
        DamageResult CalculateDamage(DamageContext context);
        DamageType GetDamageType();
    }

    public enum DamageType
    {
        Physical,
        Magical,
        Hybrid,
        True,     // Ignores all mitigation
        Healing,  // Restorative damage
        Shield    // Shield damage
    }

    public struct DamageContext
    {
        public CharacterStats AttackerStats;
        public CharacterStats DefenderStats;
        public AbilityData AbilityData;
        public bool IsCritical;
        public float BonusMultiplier;
        public Vector2? TargetPosition; // For area effects
        public float Distance; // Distance between attacker and defender

        public static DamageContext Create(
            CharacterStats attacker,
            CharacterStats defender,
            AbilityData ability = null,
            bool isCritical = false,
            float bonusMultiplier = 1f,
            Vector2? targetPosition = null,
            float distance = 0f)
        {
            return new DamageContext
            {
                AttackerStats = attacker,
                DefenderStats = defender,
                AbilityData = ability,
                IsCritical = isCritical,
                BonusMultiplier = bonusMultiplier,
                TargetPosition = targetPosition,
                Distance = distance
            };
        }
    }

    public struct DamageResult
    {
        public float RawDamage;
        public float MitigatedDamage;
        public bool IsCritical;
        public DamageType DamageType;
        public float OverkillAmount;
        public float LifestealAmount;
        public float ReflectionAmount;
        public bool IsKill;
        public float ActualHealing; // For healing abilities

        public static DamageResult Create(
            float rawDamage,
            float mitigatedDamage,
            DamageType damageType,
            bool isCritical = false,
            float overkillAmount = 0f,
            float lifestealAmount = 0f,
            float reflectionAmount = 0f,
            float actualHealing = 0f)
        {
            return new DamageResult
            {
                RawDamage = rawDamage,
                MitigatedDamage = mitigatedDamage,
                IsCritical = isCritical,
                DamageType = damageType,
                OverkillAmount = overkillAmount,
                LifestealAmount = lifestealAmount,
                ReflectionAmount = reflectionAmount,
                IsKill = overkillAmount > 0f,
                ActualHealing = actualHealing
            };
        }

        public override string ToString()
        {
            if (DamageType == DamageType.Healing)
            {
                return $"Healing: {ActualHealing:F0} HP";
            }

            string critText = IsCritical ? " CRITICAL!" : "";
            string killText = IsKill ? " (KILL)" : "";

            return $"{DamageType} Damage: {MitigatedDamage:F0} ({RawDamage:F0} raw){critText}{killText}";
        }
    }
}