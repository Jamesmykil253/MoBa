using UnityEngine;
using System.Collections.Generic;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Prototype Pattern implementation for ability templates
    /// Based on GoF Prototype Pattern and Game Programming Patterns
    /// Allows efficient cloning of ability configurations
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityPrototype", menuName = "MOBA/Ability Prototype")]
    public class AbilityPrototype : ScriptableObject
    {
        [Header("Basic Properties")]
        public string abilityName;
        public string description;
        public Sprite icon;
        public AbilityType abilityType;

        [Header("Damage & Effects")]
        public float baseDamage = 50f;
        public float damageMultiplier = 1f;
        public DamageType damageType = DamageType.Physical;
        public float range = 5f;
        public float areaOfEffect = 0f;

        [Header("Timing")]
        public float castTime = 0f;
        public float cooldown = 5f;
        public float duration = 0f;

        [Header("Visual Effects")]
        public GameObject castEffect;
        public GameObject impactEffect;
        public AudioClip castSound;
        public AudioClip impactSound;

        [Header("Advanced Properties")]
        public bool isHoming = false;
        public float projectileSpeed = 10f;
        public LayerMask hitLayers = -1;
        public bool canCrit = true;
        public float critChanceBonus = 0f;

        [Header("Scaling")]
        public float damagePerLevel = 10f;
        public float rangePerLevel = 0.5f;
        public float cooldownReductionPerLevel = 0.1f;

        /// <summary>
        /// Creates a deep clone of this ability prototype
        /// </summary>
        public AbilityPrototype Clone()
        {
            var clone = CreateInstance<AbilityPrototype>();

            // Copy all basic properties
            clone.abilityName = abilityName;
            clone.description = description;
            clone.icon = icon;
            clone.abilityType = abilityType;

            // Copy damage and effects
            clone.baseDamage = baseDamage;
            clone.damageMultiplier = damageMultiplier;
            clone.damageType = damageType;
            clone.range = range;
            clone.areaOfEffect = areaOfEffect;

            // Copy timing
            clone.castTime = castTime;
            clone.cooldown = cooldown;
            clone.duration = duration;

            // Copy visual effects (shallow copy for shared assets)
            clone.castEffect = castEffect;
            clone.impactEffect = impactEffect;
            clone.castSound = castSound;
            clone.impactSound = impactSound;

            // Copy advanced properties
            clone.isHoming = isHoming;
            clone.projectileSpeed = projectileSpeed;
            clone.hitLayers = hitLayers;
            clone.canCrit = canCrit;
            clone.critChanceBonus = critChanceBonus;

            // Copy scaling
            clone.damagePerLevel = damagePerLevel;
            clone.rangePerLevel = rangePerLevel;
            clone.cooldownReductionPerLevel = cooldownReductionPerLevel;

            return clone;
        }

        /// <summary>
        /// Creates a modified clone with runtime adjustments
        /// </summary>
        public AbilityPrototype CloneWithModifiers(
            float damageModifier = 1f,
            float rangeModifier = 1f,
            float cooldownModifier = 1f)
        {
            var clone = Clone();

            // Apply modifiers
            clone.baseDamage *= damageModifier;
            clone.range *= rangeModifier;
            clone.cooldown *= cooldownModifier;

            // Generate unique name for modified ability
            clone.abilityName = $"{abilityName}_Modified_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

            return clone;
        }

        /// <summary>
        /// Creates a leveled version of the ability
        /// </summary>
        public AbilityPrototype CreateLeveledVersion(int level)
        {
            var leveledClone = Clone();

            // Apply level scaling
            leveledClone.baseDamage += damagePerLevel * (level - 1);
            leveledClone.range += rangePerLevel * (level - 1);
            leveledClone.cooldown *= Mathf.Pow(1f - cooldownReductionPerLevel, level - 1);

            leveledClone.abilityName = $"{abilityName} (Level {level})";

            return leveledClone;
        }

        /// <summary>
        /// Converts prototype to AbilityData for runtime use
        /// </summary>
        public AbilityData ToAbilityData()
        {
            return new AbilityData
            {
                name = abilityName,
                description = description,
                damage = baseDamage,
                range = range,
                speed = projectileSpeed,
                cooldown = cooldown,
                icon = icon,
                effectColor = Color.white,
                // DISABLED: Projectile system removed for development
                // isProjectile = true,
                isInstantCast = true, // Replaced projectile with instant cast for development
                homing = isHoming,
                hitLayers = -1,
                damageType = damageType,
                critChanceBonus = critChanceBonus,
                critDamageBonus = 0f,
                spellAmpBonus = 0f,
                lifestealPercent = 0f,
                damageReflection = 0f,
                stunDuration = 0f,
                knockbackForce = 0f,
                healingAmount = 0f,
                isAreaEffect = areaOfEffect > 0f,
                areaRadius = areaOfEffect,
                areaDamageFalloff = 0f,
                dotDamage = 0f,
                dotDuration = duration,
                dotTickInterval = 1f,
                attackScaling = 1f,
                techAttackScaling = 0f,
                defenseScaling = 0f,
                cooldownReduction = 0f
            };
        }

        /// <summary>
        /// Validates the prototype configuration
        /// </summary>
        public bool IsValid()
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(abilityName))
            {
                Debug.LogError($"AbilityPrototype '{name}': abilityName is required");
                isValid = false;
            }

            if (baseDamage < 0)
            {
                Debug.LogError($"AbilityPrototype '{name}': baseDamage cannot be negative");
                isValid = false;
            }

            if (cooldown < 0)
            {
                Debug.LogError($"AbilityPrototype '{name}': cooldown cannot be negative");
                isValid = false;
            }

            if (range < 0)
            {
                Debug.LogError($"AbilityPrototype '{name}': range cannot be negative");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Gets a human-readable summary of the ability
        /// </summary>
        public string GetSummary()
        {
            return $"{abilityName}: {damageType} damage ({baseDamage}), Range: {range}, Cooldown: {cooldown}s";
        }
    }

    /// <summary>
    /// Manager for ability prototypes
    /// Provides centralized access to ability templates
    /// </summary>
    public class AbilityPrototypeManager : MonoBehaviour
    {
        [SerializeField] private List<AbilityPrototype> abilityPrototypes = new();

        private Dictionary<string, AbilityPrototype> prototypeCache = new();

        // Network synchronization for multiplayer
        private void Awake()
        {
            InitializeCache();

            // REMOVED: MemoryManager was removed during cleanup
            // Memory management is now handled by Unity's garbage collector
            
            // Initialize ability cache for performance
            foreach (var prototype in abilityPrototypes)
            {
                if (prototype != null)
                {
                    // Memory tracking removed - Unity handles GC automatically
                    Debug.Log($"Initialized ability prototype: {prototype.name}");
                }
            }
        }

        /// <summary>
        /// Memory manager setup removed - Unity handles memory automatically
        /// </summary>
        public void SetMemoryManager(object memoryManager)
        {
            // Memory management removed - Unity handles this automatically
            UnityEngine.Debug.Log("[AbilityPrototype] Memory management handled by Unity - no manual setup needed");
        }

        private void InitializeCache()
        {
            foreach (var prototype in abilityPrototypes)
            {
                if (prototype != null && !string.IsNullOrEmpty(prototype.abilityName))
                {
                    prototypeCache[prototype.abilityName] = prototype;
                }
            }

            Debug.Log($"AbilityPrototypeManager initialized with {prototypeCache.Count} prototypes");
        }

        /// <summary>
        /// Gets an ability prototype by name
        /// </summary>
        public AbilityPrototype GetPrototype(string name)
        {
            if (prototypeCache.TryGetValue(name, out AbilityPrototype prototype))
            {
                return prototype;
            }

            Debug.LogWarning($"Ability prototype '{name}' not found");
            return null;
        }

        /// <summary>
        /// Creates a clone of an ability prototype
        /// </summary>
        public AbilityPrototype CreateAbility(string prototypeName)
        {
            var prototype = GetPrototype(prototypeName);
            if (prototype == null) return null;

            return prototype.Clone();
        }

        /// <summary>
        /// Creates a leveled version of an ability
        /// </summary>
        public AbilityPrototype CreateLeveledAbility(string prototypeName, int level)
        {
            var prototype = GetPrototype(prototypeName);
            if (prototype == null) return null;

            return prototype.CreateLeveledVersion(level);
        }

        /// <summary>
        /// Gets all available prototype names
        /// </summary>
        public List<string> GetAvailablePrototypes()
        {
            return new List<string>(prototypeCache.Keys);
        }

        /// <summary>
        /// Validates all prototypes
        /// </summary>
        public bool ValidateAllPrototypes()
        {
            bool allValid = true;

            foreach (var prototype in abilityPrototypes)
            {
                if (prototype != null && !prototype.IsValid())
                {
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>
        /// Adds a prototype at runtime
        /// </summary>
        public void AddPrototype(AbilityPrototype prototype)
        {
            if (prototype != null && !string.IsNullOrEmpty(prototype.abilityName))
            {
                abilityPrototypes.Add(prototype);
                prototypeCache[prototype.abilityName] = prototype;
                Debug.Log($"Added ability prototype: {prototype.abilityName}");
            }
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUI.Label(new Rect(10, 60, 300, 20), $"Ability Prototypes: {prototypeCache.Count}");

            int y = 80;
            foreach (var kvp in prototypeCache)
            {
                GUI.Label(new Rect(10, y, 400, 20), $"{kvp.Key}: {kvp.Value.GetSummary()}");
                y += 20;
                if (y > 200) break; // Limit display
            }
        }
    }

}