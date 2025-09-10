using UnityEngine;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// Critical Hit System implementing Strategy Pattern for different crit mechanics
    /// Based on Game Programming Patterns - Strategy Pattern
    /// </summary>
    public static class CriticalHitSystem
    {
        /// <summary>
        /// Calculates critical hit chance and damage multiplier
        /// </summary>
        public static CriticalHitResult CalculateCriticalHit(
            float baseCritChance,
            float critChanceBonus,
            float baseCritMultiplier = DamageFormulas.CRIT_MULTIPLIER)
        {
            float totalCritChance = Mathf.Clamp(baseCritChance + critChanceBonus, 0f, 1f);
            bool isCritical = Random.value < totalCritChance;

            return new CriticalHitResult
            {
                IsCritical = isCritical,
                DamageMultiplier = isCritical ? baseCritMultiplier : 1f,
                CritChance = totalCritChance
            };
        }

        /// <summary>
        /// Advanced critical hit calculation with multiple factors
        /// </summary>
        public static CriticalHitResult CalculateAdvancedCriticalHit(
            CharacterStats attacker,
            CharacterStats defender,
            AbilityData ability = null)
        {
            float baseCritChance = DamageFormulas.CRIT_CHANCE_BASE;

            // Add attacker crit chance bonuses
            float critChanceBonus = attacker.CritChanceBonus;

            // Add ability-specific crit chance
            if (ability != null && ability.critChanceBonus > 0)
            {
                critChanceBonus += ability.critChanceBonus;
            }

            // Reduce crit chance based on defender's crit resistance
            critChanceBonus -= defender.CritResistance;

            // Calculate crit multiplier
            float critMultiplier = DamageFormulas.CRIT_MULTIPLIER;

            // Add attacker crit damage bonuses
            critMultiplier += attacker.CritDamageBonus;

            // Add ability-specific crit damage
            if (ability != null && ability.critDamageBonus > 0)
            {
                critMultiplier += ability.critDamageBonus;
            }

            var result = CalculateCriticalHit(baseCritChance, critChanceBonus, critMultiplier);
            result.CritDamageBonus = attacker.CritDamageBonus + (ability?.critDamageBonus ?? 0f);
            result.CritResistance = defender.CritResistance;

            return result;
        }

        /// <summary>
        /// Calculates critical hit for projectile-based attacks
        /// </summary>
        public static CriticalHitResult CalculateProjectileCriticalHit(
            ProjectileFlyweight projectileData,
            CharacterStats attacker,
            CharacterStats defender)
        {
            float baseCritChance = projectileData.critChance;
            float critChanceBonus = attacker.CritChanceBonus - defender.CritResistance;
            float critMultiplier = projectileData.critMultiplier + attacker.CritDamageBonus;

            return CalculateCriticalHit(baseCritChance, critChanceBonus, critMultiplier);
        }
    }

    /// <summary>
    /// Result of critical hit calculation
    /// </summary>
    public struct CriticalHitResult
    {
        public bool IsCritical;
        public float DamageMultiplier;
        public float CritChance;
        public float CritDamageBonus;
        public float CritResistance;

        public override string ToString()
        {
            return IsCritical ?
                $"CRITICAL HIT! ({DamageMultiplier}x damage, {CritChance * 100:F1}% chance)" :
                $"Normal Hit ({CritChance * 100:F1}% crit chance)";
        }
    }

    /// <summary>
    /// Extensions for CharacterStats to include crit properties
    /// </summary>
    public static class CharacterStatsCritExtensions
    {
        public static float GetCritChanceBonus(this CharacterStats stats)
        {
            // Calculate crit chance bonus from items, abilities, etc.
            return 0f; // Placeholder - would be calculated from equipped items/abilities
        }

        public static float GetCritDamageBonus(this CharacterStats stats)
        {
            // Calculate crit damage bonus from items, abilities, etc.
            return 0f; // Placeholder - would be calculated from equipped items/abilities
        }

        public static float GetCritResistance(this CharacterStats stats)
        {
            // Calculate crit resistance from items, abilities, etc.
            return 0f; // Placeholder - would be calculated from equipped items/abilities
        }
    }
}