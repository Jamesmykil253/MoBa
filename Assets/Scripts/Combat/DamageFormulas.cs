using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Static class containing all damage calculation formulas
    /// Based on GAMEPLAY.md balance requirements and TECHNICAL.md patterns
    /// </summary>
    public static class DamageFormulas
    {
        // Core Constants
        public const float DEFENSE_CAP = 0.75f; // 75% resistance cap
        public const float TECH_DEFENSE_CAP = 0.75f;
        public const float CRIT_CHANCE_BASE = 0.10f; // 10% base crit chance
        public const float CRIT_MULTIPLIER = 1.5f;
        public const float HP_REGEN_CAP_PERCENT = 0.10f; // 10% max HP per tick
        public const float MIN_ACTION_SPEED = 1f; // Minimum action speed to prevent infinite delays

        /// <summary>
        /// Physical Damage Dealt Formula
        /// Physical Damage Dealt = (Base Attack + Bonus Attack) * (1 - Enemy Physical Def / (Enemy Physical Def + 100)) * Critical Multiplier
        /// </summary>
        public static float CalculatePhysicalDamage(
            float baseAttack,
            float bonusAttack,
            float enemyPhysicalDef,
            bool isCritical = false,
            float critMultiplier = CRIT_MULTIPLIER)
        {
            float totalAttack = baseAttack + bonusAttack;
            float mitigation = enemyPhysicalDef / (enemyPhysicalDef + 100f);
            float damage = totalAttack * (1f - mitigation);

            if (isCritical)
                damage *= critMultiplier;

            return Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Magical Damage Dealt Formula
        /// Magical Damage Dealt = (Base Tech Attack + Bonus Tech Attack) * (1 - Enemy Tech Def / (Enemy Tech Def + 100)) * Critical Multiplier
        /// </summary>
        public static float CalculateMagicalDamage(
            float baseTechAttack,
            float bonusTechAttack,
            float enemyTechDef,
            bool isCritical = false,
            float critMultiplier = CRIT_MULTIPLIER)
        {
            float totalTechAttack = baseTechAttack + bonusTechAttack;
            float mitigation = enemyTechDef / (enemyTechDef + 100f);
            float damage = totalTechAttack * (1f - mitigation);

            if (isCritical)
                damage *= critMultiplier;

            return Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Damage Taken with Resistance and Flat Reduction
        /// Damage Taken = Incoming Damage * (1 - Resistance) - Flat Reduction
        /// </summary>
        public static float ApplyDamageMitigation(
            float incomingDamage,
            float resistance,
            float flatReduction)
        {
            // Cap resistance at 75%
            resistance = Mathf.Min(resistance, DEFENSE_CAP);

            float mitigatedDamage = incomingDamage * (1f - resistance);
            return Mathf.Max(0f, mitigatedDamage - flatReduction);
        }

        /// <summary>
        /// HP Regeneration Formula
        /// HP Regeneration = Base Regen + Items + Abilities (capped at 10% max HP per tick)
        /// </summary>
        public static float CalculateHPRegeneration(
            float baseRegen,
            float itemRegen,
            float abilityRegen,
            float maxHP)
        {
            float totalRegen = baseRegen + itemRegen + abilityRegen;
            float regenCap = maxHP * HP_REGEN_CAP_PERCENT;
            return Mathf.Min(totalRegen, regenCap);
        }

        /// <summary>
        /// Action Speed Priority Formula
        /// Action Speed Priority = Base Speed + Buffs - Debuffs
        /// Used to determine turn order in combat state machine
        /// </summary>
        public static float CalculateActionPriority(
            float baseSpeed,
            float speedBuffs,
            float speedDebuffs)
        {
            return Mathf.Max(MIN_ACTION_SPEED, baseSpeed + speedBuffs - speedDebuffs);
        }

        /// <summary>
        /// Critical Hit Chance Calculation
        /// </summary>
        public static bool RollCriticalHit(float critChanceBonus = 0f)
        {
            float totalCritChance = CRIT_CHANCE_BASE + critChanceBonus;
            totalCritChance = Mathf.Clamp(totalCritChance, 0f, 1f);
            return Random.value < totalCritChance;
        }

        /// <summary>
        /// Calculate damage over time (DoT) effects
        /// </summary>
        public static float CalculateDoTDamage(
            float baseDamage,
            float duration,
            float tickInterval,
            float enemyResistance)
        {
            int ticks = Mathf.FloorToInt(duration / tickInterval);
            float damagePerTick = baseDamage / ticks;
            return ApplyDamageMitigation(damagePerTick, enemyResistance, 0f);
        }

        /// <summary>
        /// Calculate healing amount with possible critical healing
        /// </summary>
        public static float CalculateHealing(
            float baseHealing,
            float healingBonus,
            bool isCritical = false,
            float critMultiplier = CRIT_MULTIPLIER)
        {
            float totalHealing = baseHealing + healingBonus;

            if (isCritical)
                totalHealing *= critMultiplier;

            return totalHealing;
        }

        /// <summary>
        /// Calculate ability cooldown reduction
        /// </summary>
        public static float CalculateCooldownReduction(
            float baseCooldown,
            float cooldownReductionPercent)
        {
            float reduction = baseCooldown * Mathf.Clamp(cooldownReductionPercent, 0f, 0.75f); // Max 75% reduction
            return Mathf.Max(0.1f, baseCooldown - reduction); // Minimum 0.1s cooldown
        }

        /// <summary>
        /// Calculate damage reflection
        /// </summary>
        public static float CalculateDamageReflection(
            float incomingDamage,
            float reflectionPercent)
        {
            return incomingDamage * Mathf.Clamp(reflectionPercent, 0f, 1f);
        }

        /// <summary>
        /// Calculate lifesteal amount
        /// </summary>
        public static float CalculateLifesteal(
            float damageDealt,
            float lifestealPercent)
        {
            return damageDealt * Mathf.Clamp(lifestealPercent, 0f, 0.50f); // Max 50% lifesteal
        }

        /// <summary>
        /// Calculate shield absorption
        /// </summary>
        public static float CalculateShieldAbsorption(
            float shieldAmount,
            float incomingDamage)
        {
            return Mathf.Min(shieldAmount, incomingDamage);
        }

        /// <summary>
        /// Calculate stun duration with resistance
        /// </summary>
        public static float CalculateStunDuration(
            float baseStunDuration,
            float stunResistance)
        {
            float resistance = Mathf.Clamp(stunResistance, 0f, 0.75f); // Max 75% stun resistance
            return baseStunDuration * (1f - resistance);
        }

        /// <summary>
        /// Calculate knockback distance
        /// </summary>
        public static float CalculateKnockbackDistance(
            float baseKnockback,
            float knockbackResistance)
        {
            float resistance = Mathf.Clamp(knockbackResistance, 0f, 1f);
            return baseKnockback * (1f - resistance);
        }

        /// <summary>
        /// Calculate ability range with modifications
        /// </summary>
        public static float CalculateAbilityRange(
            float baseRange,
            float rangeBonus,
            float rangeMalus)
        {
            return Mathf.Max(1f, baseRange + rangeBonus - rangeMalus); // Minimum 1 unit range
        }

        /// <summary>
        /// Calculate projectile speed with modifications
        /// </summary>
        public static float CalculateProjectileSpeed(
            float baseSpeed,
            float speedBonus,
            float speedMalus)
        {
            return Mathf.Max(1f, baseSpeed + speedBonus - speedMalus); // Minimum 1 unit/s speed
        }
    }
}