using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Magical damage calculation strategy
    /// Uses Tech Attack stat for damage, Tech Def for mitigation
    /// Implements critical hits, spell amplification, and magic penetration
    /// </summary>
    public class MagicalDamageStrategy : MonoBehaviour, IDamageStrategy
    {
        [SerializeField] private float baseCritChanceBonus = 0f;
        [SerializeField] private float baseSpellAmplification = 0f; // Increases magic damage
        [SerializeField] private float baseMagicPenetration = 0f;   // Reduces enemy magic resistance

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
            float rawDamage = DamageFormulas.CalculateMagicalDamage(
                context.AttackerStats.TotalTechAttack,
                0f, // Bonus tech attack already included in TotalTechAttack
                context.DefenderStats.TotalTechDefense,
                isCritical,
                critResult.DamageMultiplier
            );

            // Apply spell amplification
            float totalAmplification = baseSpellAmplification;
            if (context.AbilityData != null && context.AbilityData.spellAmpBonus > 0)
            {
                totalAmplification += context.AbilityData.spellAmpBonus;
            }
            rawDamage *= (1f + totalAmplification);

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

            // Apply magic penetration (reduces enemy resistance)
            float effectiveResistance = context.DefenderStats.TechResistance;
            effectiveResistance = Mathf.Max(0f, effectiveResistance - baseMagicPenetration);

            // Apply mitigation
            float mitigatedDamage = DamageFormulas.ApplyDamageMitigation(
                rawDamage,
                effectiveResistance,
                context.DefenderStats.FlatTechReduction
            );

            // Calculate overkill
            float overkillAmount = Mathf.Max(0f, mitigatedDamage - context.DefenderStats.CurrentHP);

            // Magical damage typically doesn't have lifesteal, but can have mana restoration
            float lifestealAmount = 0f;

            // Calculate damage reflection (magical reflection)
            float reflectionAmount = 0f;
            float totalReflection = context.DefenderStats.DamageReflection;
            if (totalReflection > 0f)
            {
                reflectionAmount = DamageFormulas.CalculateDamageReflection(mitigatedDamage, totalReflection);
            }

            return DamageResult.Create(
                rawDamage,
                mitigatedDamage,
                DamageType.Magical,
                isCritical,
                overkillAmount,
                lifestealAmount,
                reflectionAmount
            );
        }

        public DamageType GetDamageType() => DamageType.Magical;

        // Utility methods for external configuration
        public void SetCritChanceBonus(float bonus) => baseCritChanceBonus = bonus;
        public void SetSpellAmplification(float amp) => baseSpellAmplification = amp;
        public void SetMagicPenetration(float pen) => baseMagicPenetration = pen;
    }
}