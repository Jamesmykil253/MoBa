using System;
using UnityEngine;

namespace MOBA.Services
{
    public interface ICombatResolver
    {
        CombatResult ResolveCombat(in CombatRequest request);
    }

    [Serializable]
    public struct CombatRequest
    {
        public CharacterStats? Attacker;
        public CharacterStats? Defender;
        public float AbilityDamage;
        public float BaseAttack;
        public DamageType DamageType;
        public bool AllowCritical;
        public float CriticalChance;
        public float CriticalMultiplier;

        public CombatRequest(CharacterStats? attacker, CharacterStats? defender, float abilityDamage, DamageType damageType, bool allowCritical, float criticalChance, float criticalMultiplier)
        {
            Attacker = attacker;
            Defender = defender;
            AbilityDamage = abilityDamage;
            DamageType = damageType;
            AllowCritical = allowCritical;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            BaseAttack = 0f;
        }
    }

    [Serializable]
    public struct CombatResult
    {
        public float FinalDamage;
        public bool IsCritical;
        public float RiskFactor;
        public float SkillFactor;
        public DamageType DamageType;

        public static CombatResult FromDamage(float damage, DamageType type)
        {
            return new CombatResult
            {
                FinalDamage = Mathf.Max(0f, damage),
                DamageType = type,
                RiskFactor = 0f,
                SkillFactor = 0f,
                IsCritical = false
            };
        }
    }

    public sealed class RiskSkillCombatResolver : ICombatResolver
    {
        public CombatResult ResolveCombat(in CombatRequest request)
        {
            var attacker = request.Attacker ?? CharacterStats.DefaultStats;
            var defender = request.Defender ?? CharacterStats.DefaultStats;

            float attackPower = request.DamageType == DamageType.Physical ? attacker.TotalAttack : attacker.TotalTechAttack;
            float defensePower = request.DamageType == DamageType.Physical ? defender.TotalPhysicalDefense : defender.TotalTechDefense;
            float resistance = request.DamageType == DamageType.Physical ? defender.PhysicalResistance : defender.TechResistance;

            float baseDamage = Mathf.Max(0f, request.AbilityDamage + attackPower - defensePower);
            float resistanceFactor = Mathf.Clamp01(1f - resistance);
            float riskFactor = Mathf.Clamp01(attacker.RiskFactor(defender));
            float skillFactor = Mathf.Clamp01(attacker.SkillFactor());

            float damage = baseDamage * resistanceFactor;
            damage *= 1f + (riskFactor * 0.25f) + (skillFactor * 0.15f);

            bool isCritical = false;
            if (request.AllowCritical)
            {
                float effectiveChance = Mathf.Clamp01(request.CriticalChance + attacker.CritChanceBonus - defender.CritResistance);
                if (effectiveChance >= 1f)
                {
                    isCritical = true;
                }
                else if (effectiveChance > 0f)
                {
                    // Deterministic threshold: compare against combined stats ratio
                    float threshold = Mathf.Clamp01((attacker.TotalAttack + attacker.TotalTechAttack) / (defender.TotalPhysicalDefense + defender.TotalTechDefense + 1f));
                    isCritical = effectiveChance >= threshold;
                }

                if (isCritical)
                {
                    float multiplier = Mathf.Max(1f, request.CriticalMultiplier + attacker.TotalCritMultiplier - 1f);
                    damage *= multiplier;
                }
            }

            return new CombatResult
            {
                FinalDamage = Mathf.Max(0f, damage),
                DamageType = request.DamageType,
                IsCritical = isCritical,
                RiskFactor = riskFactor,
                SkillFactor = skillFactor
            };
        }
    }
}
