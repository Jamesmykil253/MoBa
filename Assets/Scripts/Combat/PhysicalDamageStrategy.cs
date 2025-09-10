using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Physical damage calculation strategy
    /// Uses Attack stat for damage, Physical Def for mitigation
    /// Implements critical hits and lifesteal
    /// </summary>
    public class PhysicalDamageStrategy : MonoBehaviour, IDamageStrategy
    {
        [SerializeField] private float baseCritChanceBonus = 0f;
        [SerializeField] private float baseLifestealPercent = 0f;
        [SerializeField] private float baseDamageReflection = 0f;

        public DamageResult CalculateDamage(DamageContext context)
        {
            // Calculate critical hit
            var critResult = CriticalHitSystem.CalculateAdvancedCriticalHit(
                context.AttackerStats,
                context.DefenderStats,
                context.AbilityData
            );

            // Add strategy-specific crit chance bonus
            critResult.CritChance += baseCritChanceBonus;
            critResult.CritChance = Mathf.Clamp(critResult.CritChance, 0f, 1f);

            bool isCritical = context.IsCritical || (Random.value < critResult.CritChance);

            // Calculate raw damage
            float rawDamage = DamageFormulas.CalculatePhysicalDamage(
                context.AttackerStats.TotalAttack,
                0f, // Bonus attack already included in TotalAttack
                context.DefenderStats.TotalPhysicalDefense,
                isCritical,
                critResult.DamageMultiplier
            );

            // Apply ability modifiers
            if (context.AbilityData != null)
            {
                rawDamage *= context.AbilityData.damage;

                // Apply ability-specific bonuses
                if (context.AbilityData.critChanceBonus > 0)
                {
                    rawDamage *= (1f + context.AbilityData.critChanceBonus);
                }
            }

            // Apply bonus multiplier (from buffs, etc.)
            rawDamage *= context.BonusMultiplier;

            // Apply mitigation
            float mitigatedDamage = DamageFormulas.ApplyDamageMitigation(
                rawDamage,
                context.DefenderStats.PhysicalResistance,
                context.DefenderStats.FlatPhysicalReduction
            );

            // Calculate overkill
            float overkillAmount = Mathf.Max(0f, mitigatedDamage - context.DefenderStats.CurrentHP);

            // Calculate lifesteal
            float lifestealAmount = 0f;
            float totalLifesteal = baseLifestealPercent + context.AttackerStats.LifestealPercent;
            if (totalLifesteal > 0f)
            {
                lifestealAmount = DamageFormulas.CalculateLifesteal(mitigatedDamage, totalLifesteal);
            }

            // Calculate damage reflection
            float reflectionAmount = 0f;
            float totalReflection = baseDamageReflection + context.DefenderStats.DamageReflection;
            if (totalReflection > 0f)
            {
                reflectionAmount = DamageFormulas.CalculateDamageReflection(mitigatedDamage, totalReflection);
            }

            return DamageResult.Create(
                rawDamage,
                mitigatedDamage,
                DamageType.Physical,
                isCritical,
                overkillAmount,
                lifestealAmount,
                reflectionAmount
            );
        }

        public DamageType GetDamageType() => DamageType.Physical;

        // Utility methods for external configuration
        public void SetCritChanceBonus(float bonus) => baseCritChanceBonus = bonus;
        public void SetLifestealPercent(float percent) => baseLifestealPercent = percent;
        public void SetDamageReflection(float reflection) => baseDamageReflection = reflection;
    }
}