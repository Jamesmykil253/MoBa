using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Complete character statistics structure for damage system
    /// Used by damage calculations, state machine, and UI
    /// </summary>
    [System.Serializable]
    public struct CharacterStats
    {
        // Core Stats
        [Header("Core Stats")]
        public float MaxHP;
        public float CurrentHP;
        public float Attack;           // Physical damage stat
        public float TechAttack;      // Magical damage stat
        public float PhysicalDefense; // Physical damage reduction
        public float TechDefense;     // Magical damage reduction
        public float Speed;           // Action speed and movement speed

        // Bonus Stats (from items/buffs)
        [Header("Bonus Stats")]
        public float BonusAttack;
        public float BonusTechAttack;
        public float BonusPhysicalDefense;
        public float BonusTechDefense;
        public float BonusSpeed;

        // Resistances & Reductions (0-0.75, capped at 75%)
        [Header("Resistances")]
        public float PhysicalResistance; // Percentage reduction (0-0.75)
        public float TechResistance;     // Percentage reduction (0-0.75)
        public float FlatPhysicalReduction; // Flat damage reduction
        public float FlatTechReduction;

        // Critical Hit Stats
        [Header("Critical Hit")]
        public float CritChanceBonus;  // Additional crit chance (0-1)
        public float CritDamageBonus;  // Additional crit multiplier
        public float CritResistance;   // Reduces enemy crit chance

        // Regeneration
        [Header("Regeneration")]
        public float BaseHPRegen;
        public float ItemHPRegen;
        public float AbilityHPRegen;

        // Combat State
        [Header("Combat State")]
        public bool IsAlive;
        public bool IsStunned;
        public float StunDuration;
        public int Level;

        // Advanced Stats
        [Header("Advanced Stats")]
        public float LifestealPercent;     // Percentage of damage dealt healed
        public float CooldownReduction;    // Percentage cooldown reduction
        public float DamageReflection;     // Percentage of damage reflected
        public float StunResistance;       // Reduces stun duration
        public float KnockbackResistance;  // Reduces knockback distance
        public float HealingReceivedBonus; // Increases healing received

        // Computed Properties
        public float TotalAttack => Attack + BonusAttack;
        public float TotalTechAttack => TechAttack + BonusTechAttack;
        public float TotalPhysicalDefense => PhysicalDefense + BonusPhysicalDefense;
        public float TotalTechDefense => TechDefense + BonusTechDefense;
        public float TotalSpeed => Mathf.Max(1f, Speed + BonusSpeed);

        public float TotalHPRegen => BaseHPRegen + ItemHPRegen + AbilityHPRegen;

        public float TotalCritChance => Mathf.Clamp(0.05f + CritChanceBonus, 0f, 1f); // 5% base crit
        public float TotalCritMultiplier => 1.5f + CritDamageBonus; // 1.5x base crit multiplier

        // Utility Methods
        public void TakeDamage(float damage)
        {
            CurrentHP = Mathf.Max(0f, CurrentHP - damage);
            IsAlive = CurrentHP > 0f;
        }

        public void Heal(float amount)
        {
            float actualHealing = amount * (1f + HealingReceivedBonus);
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + actualHealing);
        }

        public void ApplyStun(float duration)
        {
            float actualDuration = duration * (1f - StunResistance * 0.01f); // Simple stun resistance calculation
            IsStunned = true;
            StunDuration = actualDuration;
        }

        public void UpdateStun(float deltaTime)
        {
            if (IsStunned)
            {
                StunDuration -= deltaTime;
                if (StunDuration <= 0f)
                {
                    IsStunned = false;
                    StunDuration = 0f;
                }
            }
        }

        public float GetHPPercentage() => CurrentHP / MaxHP;

        public bool IsLowHP(float threshold = 0.20f) => GetHPPercentage() < threshold;

        public bool IsFullHP() => CurrentHP >= MaxHP;

        public float GetMissingHP() => MaxHP - CurrentHP;

        // Stat Modification Methods
        public void AddBonusAttack(float amount) => BonusAttack += amount;
        public void AddBonusTechAttack(float amount) => BonusTechAttack += amount;
        public void AddBonusPhysicalDefense(float amount) => BonusPhysicalDefense += amount;
        public void AddBonusTechDefense(float amount) => BonusTechDefense += amount;
        public void AddBonusSpeed(float amount) => BonusSpeed += amount;

        public void AddPhysicalResistance(float amount) => PhysicalResistance = Mathf.Min(75f, PhysicalResistance + amount); // 75% cap
        public void AddTechResistance(float amount) => TechResistance = Mathf.Min(75f, TechResistance + amount); // 75% cap

        public void AddFlatPhysicalReduction(float amount) => FlatPhysicalReduction += amount;
        public void AddFlatTechReduction(float amount) => FlatTechReduction += amount;

        public void AddCritChanceBonus(float amount) => CritChanceBonus += amount;
        public void AddCritDamageBonus(float amount) => CritDamageBonus += amount;
        public void AddCritResistance(float amount) => CritResistance += amount;

        // Reset Methods
        public void ResetBonuses()
        {
            BonusAttack = 0f;
            BonusTechAttack = 0f;
            BonusPhysicalDefense = 0f;
            BonusTechDefense = 0f;
            BonusSpeed = 0f;

            PhysicalResistance = 0f;
            TechResistance = 0f;
            FlatPhysicalReduction = 0f;
            FlatTechReduction = 0f;

            CritChanceBonus = 0f;
            CritDamageBonus = 0f;
            CritResistance = 0f;

            ItemHPRegen = 0f;
            AbilityHPRegen = 0f;

            LifestealPercent = 0f;
            CooldownReduction = 0f;
            DamageReflection = 0f;
            StunResistance = 0f;
            KnockbackResistance = 0f;
            HealingReceivedBonus = 0f;
        }

        // Default Stats
        public static CharacterStats DefaultStats => new CharacterStats
        {
            MaxHP = 1000f,
            CurrentHP = 1000f,
            Attack = 100f,
            TechAttack = 100f,
            PhysicalDefense = 50f,
            TechDefense = 50f,
            Speed = 100f,
            IsAlive = true,
            Level = 1
        };

        // Level Scaling
        public static CharacterStats GetStatsForLevel(int level, CharacterArchetype archetype = CharacterArchetype.Balanced)
        {
            float levelMultiplier = 1f + (level - 1) * 0.1f; // 10% increase per level

            var stats = DefaultStats;
            stats.Level = level;
            stats.MaxHP *= levelMultiplier;
            stats.CurrentHP = stats.MaxHP;

            switch (archetype)
            {
                case CharacterArchetype.PhysicalDPS:
                    stats.Attack *= levelMultiplier * 1.5f;
                    stats.PhysicalDefense *= levelMultiplier * 0.8f;
                    break;
                case CharacterArchetype.MagicalDPS:
                    stats.TechAttack *= levelMultiplier * 1.5f;
                    stats.TechDefense *= levelMultiplier * 0.8f;
                    break;
                case CharacterArchetype.Tank:
                    stats.MaxHP *= levelMultiplier * 1.5f;
                    stats.CurrentHP = stats.MaxHP;
                    stats.PhysicalDefense *= levelMultiplier * 1.3f;
                    stats.TechDefense *= levelMultiplier * 1.3f;
                    stats.Speed *= 0.9f;
                    break;
                case CharacterArchetype.Support:
                    stats.TechAttack *= levelMultiplier * 1.2f;
                    stats.MaxHP *= levelMultiplier * 1.1f;
                    stats.CurrentHP = stats.MaxHP;
                    break;
                default: // Balanced
                    stats.Attack *= levelMultiplier;
                    stats.TechAttack *= levelMultiplier;
                    stats.PhysicalDefense *= levelMultiplier;
                    stats.TechDefense *= levelMultiplier;
                    break;
            }

            return stats;
        }
    }

    public enum CharacterArchetype
    {
        Balanced,
        PhysicalDPS,
        MagicalDPS,
        Tank,
        Support
    }
}