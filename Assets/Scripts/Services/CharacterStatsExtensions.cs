using UnityEngine;

namespace MOBA.Services
{
    public static class CharacterStatsExtensions
    {
        public static float RiskFactor(this CharacterStats attacker, CharacterStats defender)
        {
            float attackPower = attacker.TotalAttack + attacker.TotalTechAttack + attacker.DamageReflection;
            float defensePower = defender.TotalPhysicalDefense + defender.TotalTechDefense + defender.DamageReflection;
            if (defensePower <= 0f)
            {
                return 1f;
            }
            return Mathf.Clamp01((attackPower - defensePower) / (defensePower + 1f));
        }

        public static float SkillFactor(this CharacterStats stats)
        {
            float levelFactor = Mathf.Clamp01(stats.Level / 20f);
            return Mathf.Clamp01(levelFactor + stats.CooldownReduction);
        }

        public static float TotalCritMultiplier(this CharacterStats stats)
        {
            return stats.TotalCritMultiplier;
        }
    }
}
