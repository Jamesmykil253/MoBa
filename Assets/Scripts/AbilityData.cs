using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Data structure for ability information
    /// Used by AbilitySystem, AbilityCommand, and damage calculation
    /// </summary>
    [System.Serializable]
    public class AbilityData
    {
        [Header("Basic Info")]
        public string name;
        public string description;

        [Header("Core Stats")]
        public float damage = 50f;
        public float range = 10f;
        public float speed = 10f;
        public float cooldown = 5f;

        [Header("Visual")]
        public Sprite icon;
        public Color effectColor = Color.white;

        [Header("Behavior")]
        // DISABLED: Projectile system removed for development
        // public bool isProjectile = true;
        public bool isInstantCast = true; // Replaced projectile with instant cast for development
        public bool homing = false;
        public LayerMask hitLayers;

        [Header("Damage Type")]
        public DamageType damageType = DamageType.Physical;

        [Header("Critical Hit")]
        public float critChanceBonus = 0f;    // Additional crit chance (0-1)
        public float critDamageBonus = 0f;    // Additional crit multiplier

        [Header("Special Effects")]
        public float spellAmpBonus = 0f;      // Spell amplification for magic abilities
        public float lifestealPercent = 0f;   // Lifesteal percentage
        public float damageReflection = 0f;   // Damage reflection percentage
        public float stunDuration = 0f;       // Stun duration on hit
        public float knockbackForce = 0f;     // Knockback force
        public float healingAmount = 0f;      // Healing amount (for healing abilities)

        [Header("Area Effects")]
        public bool isAreaEffect = false;
        public float areaRadius = 0f;
        public float areaDamageFalloff = 0f;  // Damage reduction at edge of area

        [Header("Status Effects")]
        public float dotDamage = 0f;          // Damage over time
        public float dotDuration = 0f;        // DoT duration
        public float dotTickInterval = 1f;    // DoT tick interval

        [Header("Scaling")]
        public float attackScaling = 1f;      // How much ability scales with Attack stat
        public float techAttackScaling = 0f;  // How much ability scales with Tech Attack stat
        public float defenseScaling = 0f;     // How much ability scales with Defense stats

        [Header("Cooldown")]
        public float cooldownReduction = 0f;  // Cooldown reduction percentage

        /// <summary>
        /// Creates a copy of this ability data
        /// </summary>
        public AbilityData Clone()
        {
            return new AbilityData
            {
                name = this.name,
                description = this.description,
                damage = this.damage,
                range = this.range,
                speed = this.speed,
                cooldown = this.cooldown,
                icon = this.icon,
                effectColor = this.effectColor,
                // DISABLED: Projectile system removed for development
                // isProjectile = this.isProjectile,
                isInstantCast = this.isInstantCast, // Replaced projectile with instant cast
                homing = this.homing,
                hitLayers = this.hitLayers,
                damageType = this.damageType,
                critChanceBonus = this.critChanceBonus,
                critDamageBonus = this.critDamageBonus,
                spellAmpBonus = this.spellAmpBonus,
                lifestealPercent = this.lifestealPercent,
                damageReflection = this.damageReflection,
                stunDuration = this.stunDuration,
                knockbackForce = this.knockbackForce,
                healingAmount = this.healingAmount,
                isAreaEffect = this.isAreaEffect,
                areaRadius = this.areaRadius,
                areaDamageFalloff = this.areaDamageFalloff,
                dotDamage = this.dotDamage,
                dotDuration = this.dotDuration,
                dotTickInterval = this.dotTickInterval,
                attackScaling = this.attackScaling,
                techAttackScaling = this.techAttackScaling,
                defenseScaling = this.defenseScaling,
                cooldownReduction = this.cooldownReduction
            };
        }

        /// <summary>
        /// Gets the effective cooldown after reductions
        /// </summary>
        public float GetEffectiveCooldown(float additionalReduction = 0f)
        {
            float totalReduction = Mathf.Clamp(cooldownReduction + additionalReduction, 0f, 0.75f);
            return DamageFormulas.CalculateCooldownReduction(cooldown, totalReduction);
        }

        /// <summary>
        /// Checks if this is a healing ability
        /// </summary>
        public bool IsHealingAbility()
        {
            return damageType == DamageType.Healing || healingAmount > 0f;
        }

        /// <summary>
        /// Checks if this ability applies damage over time
        /// </summary>
        public bool HasDamageOverTime()
        {
            return dotDamage > 0f && dotDuration > 0f;
        }

        /// <summary>
        /// Gets the total damage including scaling
        /// </summary>
        public float GetScaledDamage(CharacterStats casterStats)
        {
            float scaledDamage = damage;

            // Apply stat scaling
            scaledDamage += casterStats.TotalAttack * attackScaling;
            scaledDamage += casterStats.TotalTechAttack * techAttackScaling;
            scaledDamage += casterStats.TotalPhysicalDefense * defenseScaling;
            scaledDamage += casterStats.TotalTechDefense * defenseScaling;

            return scaledDamage;
        }
    }
}