using UnityEngine;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// RSB (Risk-Skill-Balance) Combat Formula System
    /// Standardizes combat calculations with manual aim damage bonus integration
    /// Based on GAMEPLAY.md specifications and Clean Code principles
    /// </summary>
    public class RSBCombatSystem : MonoBehaviour
    {
        [Header("RSB Base Configuration")]
        [SerializeField] private float baseDamageMultiplier = 1.0f;
        [SerializeField] private float baseRiskFactor = 0.5f; // 0-1 risk scaling
        [SerializeField] private float baseSkillFactor = 0.3f; // 0-1 skill scaling
        [SerializeField] private float baseBalanceFactor = 0.2f; // 0-1 balance scaling
        
        [Header("Manual Aim Integration")]
        [SerializeField] private float manualAimDamageBonus = 0.20f; // 20% bonus per CONTROLS.md
        [SerializeField] private float aimAccuracyThreshold = 0.8f; // 80% accuracy required for full bonus
        [SerializeField] private float aimTimeWindow = 2f; // Time window for aim accuracy calculation
        
        [Header("Distance Scaling")]
        [SerializeField] private float optimalRange = 8f; // Optimal combat range
        [SerializeField] private float maxEffectiveRange = 15f; // Maximum effective range
        [SerializeField] private AnimationCurve distanceDamageCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.5f);
        
        [Header("Ability Type Modifiers")]
        [SerializeField] private float projectileRiskMultiplier = 1.2f; // Higher risk for projectiles
        [SerializeField] private float meleeSkillMultiplier = 1.3f; // Higher skill for melee
        [SerializeField] private float areaBalanceMultiplier = 0.8f; // Area abilities are more balanced
        
        // Cached components for performance
        private HoldToAimSystem holdToAimSystem;
        private Dictionary<string, RSBAbilityProfile> abilityProfiles;
        
        // Aim tracking for time window validation
        private float aimStartTime = 0f;
        private bool isCurrentlyAiming = false;
        
        // Combat tracking for analytics
        private List<CombatEvent> recentCombatEvents = new List<CombatEvent>();
        private const int maxCombatEventHistory = 100;

        private void Awake()
        {
            InitializeAbilityProfiles();
            CacheComponents();
        }

        /// <summary>
        /// Initialize RSB profiles for different ability types
        /// Based on documented ability specifications from GAMEPLAY.md
        /// </summary>
        private void InitializeAbilityProfiles()
        {
            abilityProfiles = new Dictionary<string, RSBAbilityProfile>
            {
                // Primary attack - balanced risk/skill/balance
                ["PrimaryAttack"] = new RSBAbilityProfile
                {
                    baseRisk = 0.3f,
                    baseSkill = 0.4f,
                    baseBalance = 0.3f,
                    abilityType = AbilityType.Projectile,
                    manualAimRequired = false,
                    optimalRange = 6f
                },
                
                // Ability1 - high skill, medium risk (per hold-to-aim mechanics)
                ["Ability1"] = new RSBAbilityProfile
                {
                    baseRisk = 0.4f,
                    baseSkill = 0.5f,
                    baseBalance = 0.1f,
                    abilityType = AbilityType.Projectile,
                    manualAimRequired = true,
                    optimalRange = 8f
                },
                
                // Ability2 - high risk, high skill (per hold-to-aim mechanics)
                ["Ability2"] = new RSBAbilityProfile
                {
                    baseRisk = 0.6f,
                    baseSkill = 0.6f,
                    baseBalance = 0.2f,
                    abilityType = AbilityType.Projectile,
                    manualAimRequired = true,
                    optimalRange = 12f
                },
                
                // Ultimate - high balance, medium skill
                ["Ultimate"] = new RSBAbilityProfile
                {
                    baseRisk = 0.2f,
                    baseSkill = 0.3f,
                    baseBalance = 0.8f,
                    abilityType = AbilityType.Area,
                    manualAimRequired = false,
                    optimalRange = 10f
                }
            };
        }

        /// <summary>
        /// Cache component references for performance
        /// Following Clean Code dependency injection principles
        /// </summary>
        private void CacheComponents()
        {
            holdToAimSystem = FindAnyObjectByType<HoldToAimSystem>();
            if (holdToAimSystem == null)
            {
                Debug.LogWarning("[RSBCombatSystem] HoldToAimSystem not found - manual aim bonuses disabled");
            }
        }

        /// <summary>
        /// Calculate final damage using RSB formula with manual aim integration
        /// Core combat calculation method implementing the standardized formula
        /// </summary>
        /// <param name="baseDamage">Base ability damage</param>
        /// <param name="abilityName">Name of the ability being used</param>
        /// <param name="attackerPosition">Position of the attacker</param>
        /// <param name="targetPosition">Position of the target</param>
        /// <param name="wasManuallyAimed">Whether the ability was manually aimed</param>
        /// <param name="aimAccuracy">Accuracy of manual aim (0-1)</param>
        /// <returns>Final calculated damage</returns>
        public float CalculateDamage(
            float baseDamage, 
            string abilityName, 
            Vector3 attackerPosition, 
            Vector3 targetPosition, 
            bool wasManuallyAimed = false, 
            float aimAccuracy = 0f)
        {
            // Get ability profile
            if (!abilityProfiles.TryGetValue(abilityName, out RSBAbilityProfile profile))
            {
                Debug.LogWarning($"[RSBCombatSystem] No profile found for ability '{abilityName}', using default");
                profile = GetDefaultProfile();
            }

            // Calculate distance factor
            float distance = Vector3.Distance(attackerPosition, targetPosition);
            float distanceFactor = CalculateDistanceFactor(distance, profile.optimalRange);

            // Calculate RSB factors
            float riskFactor = CalculateRiskFactor(profile, distance);
            float skillFactor = CalculateSkillFactor(profile, wasManuallyAimed, aimAccuracy);
            float balanceFactor = CalculateBalanceFactor(profile, distance);

            // Apply manual aim bonus if applicable
            float manualAimMultiplier = CalculateManualAimBonus(profile, wasManuallyAimed, aimAccuracy);

            // RSB Formula: FinalDamage = BaseDamage × (Risk + Skill + Balance) × Distance × ManualAim × Base
            float rsbMultiplier = (riskFactor + skillFactor + balanceFactor);
            float finalDamage = baseDamage * rsbMultiplier * distanceFactor * manualAimMultiplier * baseDamageMultiplier;

            // Log combat event for analytics
            LogCombatEvent(abilityName, baseDamage, finalDamage, distance, wasManuallyAimed, aimAccuracy, rsbMultiplier);

            Debug.Log($"[RSBCombatSystem] {abilityName}: Base={baseDamage:F1} → Final={finalDamage:F1} " +
                     $"(RSB={rsbMultiplier:F2}, Dist={distanceFactor:F2}, Aim={manualAimMultiplier:F2})");

            return finalDamage;
        }

        /// <summary>
        /// Calculate risk factor based on ability profile and situational context
        /// Risk increases with distance for projectiles, decreases for area abilities
        /// </summary>
        private float CalculateRiskFactor(RSBAbilityProfile profile, float distance)
        {
            float baseFactor = profile.baseRisk * baseRiskFactor;
            
            // Apply ability type modifiers
            switch (profile.abilityType)
            {
                case AbilityType.Projectile:
                    // Higher risk at longer distances (projectiles can miss)
                    float distanceRisk = Mathf.Clamp01(distance / maxEffectiveRange) * 0.3f;
                    return baseFactor * projectileRiskMultiplier + distanceRisk;
                    
                case AbilityType.Melee:
                    // Higher risk at close range (exposure)
                    float meleeRisk = Mathf.Clamp01(1f - (distance / 3f)) * 0.4f;
                    return baseFactor + meleeRisk;
                    
                case AbilityType.Area:
                    // Lower risk due to area coverage
                    return baseFactor * 0.7f;
                    
                default:
                    return baseFactor;
            }
        }

        /// <summary>
        /// Calculate skill factor with manual aim integration
        /// Higher skill factor for manual aim and ability type considerations
        /// </summary>
        private float CalculateSkillFactor(RSBAbilityProfile profile, bool wasManuallyAimed, float aimAccuracy)
        {
            float baseFactor = profile.baseSkill * baseSkillFactor;
            
            // Manual aim skill bonus
            if (profile.manualAimRequired && wasManuallyAimed)
            {
                float aimSkillBonus = aimAccuracy * 0.4f; // Up to 40% skill bonus for perfect aim
                baseFactor += aimSkillBonus;
            }
            
            // Apply ability type modifiers
            switch (profile.abilityType)
            {
                case AbilityType.Melee:
                    return baseFactor * meleeSkillMultiplier;
                case AbilityType.Projectile:
                    return baseFactor * (wasManuallyAimed ? 1.2f : 1.0f);
                default:
                    return baseFactor;
            }
        }

        /// <summary>
        /// Calculate balance factor based on ability design and context
        /// Ensures abilities remain balanced across different situations
        /// </summary>
        private float CalculateBalanceFactor(RSBAbilityProfile profile, float distance)
        {
            float baseFactor = profile.baseBalance * baseBalanceFactor;
            
            // Apply ability type balance modifiers
            switch (profile.abilityType)
            {
                case AbilityType.Area:
                    return baseFactor * areaBalanceMultiplier;
                default:
                    return baseFactor;
            }
        }

        /// <summary>
        /// Calculate distance-based damage falloff using animation curve
        /// </summary>
        private float CalculateDistanceFactor(float distance, float optimalRange)
        {
            float normalizedDistance = distance / optimalRange;
            return distanceDamageCurve.Evaluate(normalizedDistance);
        }

        /// <summary>
        /// Calculate manual aim damage bonus per CONTROLS.md specification
        /// 20% damage bonus for manual aim with accuracy threshold
        /// </summary>
        private float CalculateManualAimBonus(RSBAbilityProfile profile, bool wasManuallyAimed, float aimAccuracy)
        {
            if (!profile.manualAimRequired || !wasManuallyAimed)
            {
                return 1f; // No bonus
            }

            // Scale bonus based on aim accuracy
            if (aimAccuracy >= aimAccuracyThreshold)
            {
                return 1f + manualAimDamageBonus; // Full 20% bonus
            }
            else
            {
                // Partial bonus based on accuracy
                float accuracyRatio = aimAccuracy / aimAccuracyThreshold;
                return 1f + (manualAimDamageBonus * accuracyRatio);
            }
        }

        /// <summary>
        /// Get default RSB profile for unknown abilities
        /// </summary>
        private RSBAbilityProfile GetDefaultProfile()
        {
            return new RSBAbilityProfile
            {
                baseRisk = 0.3f,
                baseSkill = 0.3f,
                baseBalance = 0.4f,
                abilityType = AbilityType.Projectile,
                manualAimRequired = false,
                optimalRange = optimalRange
            };
        }

        /// <summary>
        /// Log combat event for analytics and balance tuning
        /// </summary>
        private void LogCombatEvent(string abilityName, float baseDamage, float finalDamage, 
                                  float distance, bool wasManuallyAimed, float aimAccuracy, float rsbMultiplier)
        {
            var combatEvent = new CombatEvent
            {
                timestamp = Time.time,
                abilityName = abilityName,
                baseDamage = baseDamage,
                finalDamage = finalDamage,
                distance = distance,
                wasManuallyAimed = wasManuallyAimed,
                aimAccuracy = aimAccuracy,
                rsbMultiplier = rsbMultiplier
            };

            recentCombatEvents.Add(combatEvent);

            // Maintain event history limit
            if (recentCombatEvents.Count > maxCombatEventHistory)
            {
                recentCombatEvents.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get combat analytics for balance tuning
        /// </summary>
        public CombatAnalytics GetCombatAnalytics()
        {
            if (recentCombatEvents.Count == 0)
            {
                return new CombatAnalytics();
            }

            float totalDamage = 0f;
            float totalManualAimDamage = 0f;
            int manualAimCount = 0;
            float maxDistance = 0f;
            float avgAccuracy = 0f;
            int accuracyCount = 0;

            foreach (var evt in recentCombatEvents)
            {
                totalDamage += evt.finalDamage;
                maxDistance = Mathf.Max(maxDistance, evt.distance);
                
                if (evt.wasManuallyAimed)
                {
                    totalManualAimDamage += evt.finalDamage;
                    manualAimCount++;
                    avgAccuracy += evt.aimAccuracy;
                    accuracyCount++;
                }
            }

            return new CombatAnalytics
            {
                eventCount = recentCombatEvents.Count,
                averageDamage = totalDamage / recentCombatEvents.Count,
                averageManualAimDamage = manualAimCount > 0 ? totalManualAimDamage / manualAimCount : 0f,
                manualAimUsageRate = (float)manualAimCount / recentCombatEvents.Count,
                maxCombatDistance = maxDistance,
                averageAimAccuracy = accuracyCount > 0 ? avgAccuracy / accuracyCount : 0f
            };
        }

        /// <summary>
        /// Update RSB configuration at runtime for balance tuning
        /// </summary>
        public void UpdateRSBConfiguration(float riskWeight, float skillWeight, float balanceWeight)
        {
            baseRiskFactor = riskWeight;
            baseSkillFactor = skillWeight;
            baseBalanceFactor = balanceWeight;
            
            Debug.Log($"[RSBCombatSystem] Updated RSB weights: Risk={riskWeight:F2}, Skill={skillWeight:F2}, Balance={balanceWeight:F2}");
        }

        /// <summary>
        /// Public API for external systems to calculate damage
        /// Simplified interface for common use cases
        /// </summary>
        public float CalculateAbilityDamage(AbilityData abilityData, Vector3 attackerPos, Vector3 targetPos)
        {
            bool wasManuallyAimed = false;
            float aimAccuracy = 0f;

            // Check if this ability was manually aimed through HoldToAimSystem
            if (holdToAimSystem != null)
            {
                var aimInfo = holdToAimSystem.GetAimInfo();
                if (aimInfo.isAiming)
                {
                    wasManuallyAimed = aimInfo.hasManualAim;
                    float baseAccuracy = aimInfo.accuracy;
                    
                    // Apply time window modifier - accuracy degrades if aiming too long
                    if (isCurrentlyAiming)
                    {
                        float aimDuration = Time.time - aimStartTime;
                        float timeModifier = Mathf.Clamp01(1f - (aimDuration / aimTimeWindow));
                        aimAccuracy = baseAccuracy * timeModifier;
                    }
                    else
                    {
                        aimAccuracy = baseAccuracy;
                        aimStartTime = Time.time;
                        isCurrentlyAiming = true;
                    }
                }
                else
                {
                    isCurrentlyAiming = false;
                }
            }

            return CalculateDamage(abilityData.damage, abilityData.name, attackerPos, targetPos, wasManuallyAimed, aimAccuracy);
        }
    }

    /// <summary>
    /// RSB profile defining risk, skill, and balance factors for each ability
    /// </summary>
    [System.Serializable]
    public struct RSBAbilityProfile
    {
        public float baseRisk;      // 0-1 base risk factor
        public float baseSkill;     // 0-1 base skill factor  
        public float baseBalance;   // 0-1 base balance factor
        public AbilityType abilityType;
        public bool manualAimRequired;
        public float optimalRange;
    }

    /// <summary>
    /// Ability type enumeration for RSB calculations
    /// </summary>
    public enum AbilityType
    {
        Projectile,
        Melee,
        Area,
        Buff,
        Debuff,
        PrimaryAttack,
        Ability1,
        Ability2,
        Ultimate
    }

    /// <summary>
    /// Combat event data structure for analytics
    /// </summary>
    [System.Serializable]
    public struct CombatEvent
    {
        public float timestamp;
        public string abilityName;
        public float baseDamage;
        public float finalDamage;
        public float distance;
        public bool wasManuallyAimed;
        public float aimAccuracy;
        public float rsbMultiplier;
    }

    /// <summary>
    /// Combat analytics data structure
    /// </summary>
    [System.Serializable]
    public struct CombatAnalytics
    {
        public int eventCount;
        public float averageDamage;
        public float averageManualAimDamage;
        public float manualAimUsageRate;
        public float maxCombatDistance;
        public float averageAimAccuracy;
    }
}
