using System;
using UnityEngine;

namespace MOBA.Services
{
    /// <summary>
    /// Pokemon Unite authentic damage calculation system
    /// Based on Pokemon Unite mechanics: Attack/Special Attack vs Defense/Special Defense
    /// Reference: https://game8.co/games/Pokemon-UNITE/archives/335865
    /// </summary>
    public sealed class PokemonUniteCombatResolver : ICombatResolver
    {
        [Header("Pokemon Unite Formula Constants")]
        [SerializeField] private float baseMultiplier = 1.0f;
        [SerializeField] private float levelScaling = 0.05f;      // 5% per level above 1
        [SerializeField] private float criticalMultiplier = 2.0f; // Pokemon Unite default
        [SerializeField] private float minimumDamagePercent = 0.1f; // Always deal at least 10% of raw damage

        public CombatResult ResolveCombat(in CombatRequest request)
        {
            var attacker = request.Attacker ?? CharacterStats.DefaultStats;
            var defender = request.Defender ?? CharacterStats.DefaultStats;

            // Pokemon Unite uses different stats based on damage type
            float attackStat = GetAttackStat(attacker, request.DamageType);
            float defenseStat = GetDefenseStat(defender, request.DamageType);

            // Calculate base damage (Move Base Power + Attack Stat)
            float baseDamage = request.AbilityDamage + (request.BaseAttack > 0 ? request.BaseAttack : attackStat);

            // Pokemon Unite level scaling (both attacker and defender)
            float attackerLevel = Mathf.Max(1f, attacker.Level);
            float defenderLevel = Mathf.Max(1f, defender.Level);
            float levelDifference = (attackerLevel - defenderLevel) * levelScaling;

            // Pokemon Unite damage formula: 
            // Damage = (Base Damage × Level Scaling) × (Attack / Defense) × Type Effectiveness
            float levelScalingFactor = 1.0f + (attackerLevel - 1f) * levelScaling + levelDifference;
            float attackDefenseRatio = CalculateAttackDefenseRatio(attackStat, defenseStat);
            
            float rawDamage = baseDamage * levelScalingFactor * attackDefenseRatio * baseMultiplier;

            // Apply minimum damage threshold (Pokemon Unite always deals some damage)
            float minimumDamage = baseDamage * minimumDamagePercent;
            float finalDamage = Mathf.Max(minimumDamage, rawDamage);

            // Handle critical hits (Pokemon Unite style)
            bool isCritical = false;
            if (request.AllowCritical && CanCriticalHit(attacker, defender, request.CriticalChance))
            {
                isCritical = true;
                float critMultiplier = request.CriticalMultiplier > 0 ? request.CriticalMultiplier : criticalMultiplier;
                finalDamage *= critMultiplier;
            }

            return new CombatResult
            {
                FinalDamage = Mathf.Max(1f, finalDamage), // Pokemon Unite always deals at least 1 damage
                DamageType = request.DamageType,
                IsCritical = isCritical,
                RiskFactor = levelDifference, // Level advantage as risk factor
                SkillFactor = attackDefenseRatio // Stat advantage as skill factor
            };
        }

        /// <summary>
        /// Get the appropriate attack stat based on damage type (Pokemon Unite style)
        /// </summary>
        private float GetAttackStat(CharacterStats stats, DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Physical => stats.TotalAttack,         // Attack stat for Physical moves
                DamageType.Magical => stats.TotalTechAttack,      // Special Attack stat for Magical moves
                _ => stats.TotalAttack                            // Default to Attack
            };
        }

        /// <summary>
        /// Get the appropriate defense stat based on damage type (Pokemon Unite style)
        /// </summary>
        private float GetDefenseStat(CharacterStats stats, DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Physical => stats.TotalPhysicalDefense, // Defense stat for Physical moves
                DamageType.Magical => stats.TotalTechDefense,      // Special Defense stat for Magical moves
                DamageType.True => 0f,                             // True damage ignores defense
                _ => stats.TotalPhysicalDefense
            };
        }

        /// <summary>
        /// Calculate Attack/Defense ratio with Pokemon Unite scaling
        /// Formula approximated from Pokemon Unite damage testing
        /// </summary>
        private float CalculateAttackDefenseRatio(float attack, float defense)
        {
            if (defense <= 0f) return 2.0f; // Maximum ratio when no defense

            // Pokemon Unite uses a formula that prevents defense from completely negating damage
            // Approximation: Attack / (Defense + Attack * 0.5)
            float ratio = attack / (defense + attack * 0.5f);
            
            // Clamp to reasonable bounds (0.1x to 3.0x damage)
            return Mathf.Clamp(ratio, 0.1f, 3.0f);
        }

        /// <summary>
        /// Pokemon Unite critical hit calculation
        /// </summary>
        private bool CanCriticalHit(CharacterStats attacker, CharacterStats defender, float baseCritChance)
        {
            float effectiveCritChance = baseCritChance + attacker.CritChanceBonus - defender.CritResistance;
            effectiveCritChance = Mathf.Clamp01(effectiveCritChance);

            // Pokemon Unite uses deterministic crits based on stats for balance
            if (effectiveCritChance >= 1f) return true;
            if (effectiveCritChance <= 0f) return false;

            // Use a deterministic threshold based on relative stats
            float statAdvantage = (attacker.TotalAttack + attacker.TotalTechAttack) / 
                                  (defender.TotalPhysicalDefense + defender.TotalTechDefense + 1f);
            float critThreshold = Mathf.Clamp01(statAdvantage * 0.5f); // 50% of stat advantage
            
            return effectiveCritChance >= critThreshold;
        }
    }

    /// <summary>
    /// Extension methods for Pokemon Unite style stat calculations
    /// </summary>
    public static class PokemonUniteStatsExtensions
    {
        /// <summary>
        /// Get Pokemon Unite style level 15 stats (reference level for balancing)
        /// Based on Game8 Pokemon Unite stats table
        /// </summary>
        public static CharacterStats GetPokemonUniteStats(this CharacterStats baseStats, int level, PokemonUniteRole role)
        {
            var stats = baseStats;
            stats.Level = level;

            // Pokemon Unite level scaling (levels 1-15)
            float levelMultiplier = 1f + (level - 1f) * 0.067f; // ~6.7% per level to reach 2x at level 15

            switch (role)
            {
                case PokemonUniteRole.Attacker:
                    // High Attack/Special Attack, Low HP/Defense (like Cinderace, Pikachu)
                    stats.MaxHP = Mathf.RoundToInt(baseStats.MaxHP * levelMultiplier * 0.85f);     // 5400-6300 HP at 15
                    stats.Attack = Mathf.RoundToInt(baseStats.Attack * levelMultiplier * 2.0f);    // ~400-700 Attack
                    stats.TechAttack = Mathf.RoundToInt(baseStats.TechAttack * levelMultiplier * 2.0f);
                    stats.PhysicalDefense = Mathf.RoundToInt(baseStats.PhysicalDefense * levelMultiplier * 1.0f); // ~150-300 Defense
                    stats.TechDefense = Mathf.RoundToInt(baseStats.TechDefense * levelMultiplier * 1.0f);
                    break;

                case PokemonUniteRole.AllRounder:
                    // Balanced stats (like Charizard, Lucario)
                    stats.MaxHP = Mathf.RoundToInt(baseStats.MaxHP * levelMultiplier * 1.15f);     // 7000-8000 HP
                    stats.Attack = Mathf.RoundToInt(baseStats.Attack * levelMultiplier * 1.5f);    // ~400-600 Attack
                    stats.TechAttack = Mathf.RoundToInt(baseStats.TechAttack * levelMultiplier * 1.5f);
                    stats.PhysicalDefense = Mathf.RoundToInt(baseStats.PhysicalDefense * levelMultiplier * 1.3f); // ~300-500 Defense
                    stats.TechDefense = Mathf.RoundToInt(baseStats.TechDefense * levelMultiplier * 1.3f);
                    break;

                case PokemonUniteRole.Defender:
                    // High HP/Defense, Low Attack (like Snorlax, Crustle)
                    stats.MaxHP = Mathf.RoundToInt(baseStats.MaxHP * levelMultiplier * 1.5f);       // 9000-10000 HP
                    stats.Attack = Mathf.RoundToInt(baseStats.Attack * levelMultiplier * 0.8f);     // ~300-400 Attack
                    stats.TechAttack = Mathf.RoundToInt(baseStats.TechAttack * levelMultiplier * 0.8f);
                    stats.PhysicalDefense = Mathf.RoundToInt(baseStats.PhysicalDefense * levelMultiplier * 2.0f); // ~400-600 Defense
                    stats.TechDefense = Mathf.RoundToInt(baseStats.TechDefense * levelMultiplier * 2.0f);
                    break;

                case PokemonUniteRole.Supporter:
                    // Utility focused (like Eldegoss, Blissey)
                    stats.MaxHP = Mathf.RoundToInt(baseStats.MaxHP * levelMultiplier * 1.2f);       // 6000-8000 HP
                    stats.Attack = Mathf.RoundToInt(baseStats.Attack * levelMultiplier * 1.0f);     // ~300-400 Attack
                    stats.TechAttack = Mathf.RoundToInt(baseStats.TechAttack * levelMultiplier * 1.3f); // Focus on Special Attack
                    stats.PhysicalDefense = Mathf.RoundToInt(baseStats.PhysicalDefense * levelMultiplier * 1.1f); // ~200-300 Defense
                    stats.TechDefense = Mathf.RoundToInt(baseStats.TechDefense * levelMultiplier * 1.1f);
                    break;

                case PokemonUniteRole.Speedster:
                    // High mobility, burst damage (like Zeraora, Gengar)
                    stats.MaxHP = Mathf.RoundToInt(baseStats.MaxHP * levelMultiplier * 0.9f);       // 6000-7000 HP
                    stats.Attack = Mathf.RoundToInt(baseStats.Attack * levelMultiplier * 1.8f);     // ~500-700 Attack
                    stats.TechAttack = Mathf.RoundToInt(baseStats.TechAttack * levelMultiplier * 1.8f);
                    stats.PhysicalDefense = Mathf.RoundToInt(baseStats.PhysicalDefense * levelMultiplier * 0.9f); // ~200-300 Defense
                    stats.TechDefense = Mathf.RoundToInt(baseStats.TechDefense * levelMultiplier * 0.9f);
                    stats.Speed = Mathf.RoundToInt(baseStats.Speed * levelMultiplier * 1.2f); // Higher speed
                    break;
            }

            stats.CurrentHP = stats.MaxHP;
            return stats;
        }
    }

    /// <summary>
    /// Pokemon Unite roles (Battle Types)
    /// Reference: https://game8.co/games/Pokemon-UNITE/archives/335865
    /// </summary>
    public enum PokemonUniteRole
    {
        Attacker,   // High damage, low endurance (Pikachu, Cinderace)
        AllRounder, // Balanced offense and endurance (Charizard, Lucario)
        Defender,   // High endurance, protection focused (Snorlax, Crustle)
        Supporter,  // Team support, healing and buffs (Eldegoss, Blissey)
        Speedster   // High mobility and burst damage (Zeraora, Gengar)
    }
}