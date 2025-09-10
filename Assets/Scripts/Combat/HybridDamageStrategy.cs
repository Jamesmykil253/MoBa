using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Hybrid damage calculation strategy
    /// Combines physical and magical damage calculations
    /// Useful for abilities that deal mixed damage types
    /// </summary>
    public class HybridDamageStrategy : MonoBehaviour, IDamageStrategy
    {
        [SerializeField] private PhysicalDamageStrategy physicalStrategy;
        [SerializeField] private MagicalDamageStrategy magicalStrategy;
        [SerializeField] private float physicalRatio = 0.5f; // 50/50 split by default
        [SerializeField] private bool useWeightedCrit = true; // Use weighted crit calculation

        private void Awake()
        {
            // Create strategies if not assigned
            if (physicalStrategy == null)
            {
                physicalStrategy = gameObject.AddComponent<PhysicalDamageStrategy>();
            }
            if (magicalStrategy == null)
            {
                magicalStrategy = gameObject.AddComponent<MagicalDamageStrategy>();
            }
        }

        public DamageResult CalculateDamage(DamageContext context)
        {
            // Calculate both damage types
            var physicalResult = physicalStrategy.CalculateDamage(context);
            var magicalResult = magicalStrategy.CalculateDamage(context);

            // Determine critical hit for hybrid
            bool isCritical;
            if (useWeightedCrit)
            {
                // Weighted crit based on damage ratio
                float physicalWeight = physicalRatio;
                float magicalWeight = 1f - physicalRatio;

                float weightedCritChance = (physicalResult.IsCritical ? physicalWeight : 0f) +
                                         (magicalResult.IsCritical ? magicalWeight : 0f);

                isCritical = Random.value < weightedCritChance;
            }
            else
            {
                // Either damage type being critical makes the whole attack critical
                isCritical = physicalResult.IsCritical || magicalResult.IsCritical;
            }

            // Combine damage values
            float combinedRawDamage = (physicalResult.RawDamage * physicalRatio) +
                                    (magicalResult.RawDamage * (1f - physicalRatio));

            float combinedMitigatedDamage = (physicalResult.MitigatedDamage * physicalRatio) +
                                          (magicalResult.MitigatedDamage * (1f - physicalRatio));

            // Use the higher overkill amount
            float overkillAmount = Mathf.Max(physicalResult.OverkillAmount, magicalResult.OverkillAmount);

            // Combine lifesteal (only from physical component)
            float lifestealAmount = physicalResult.LifestealAmount * physicalRatio;

            // Combine reflection (average of both types)
            float reflectionAmount = (physicalResult.ReflectionAmount + magicalResult.ReflectionAmount) * 0.5f;

            return DamageResult.Create(
                combinedRawDamage,
                combinedMitigatedDamage,
                DamageType.Hybrid,
                isCritical,
                overkillAmount,
                lifestealAmount,
                reflectionAmount
            );
        }

        public DamageType GetDamageType() => DamageType.Hybrid;

        // Configuration methods
        public void SetPhysicalRatio(float ratio)
        {
            physicalRatio = Mathf.Clamp(ratio, 0f, 1f);
        }

        public void SetWeightedCrit(bool weighted)
        {
            useWeightedCrit = weighted;
        }

        public void ConfigureStrategies(
            float physicalCritBonus = 0f,
            float physicalLifesteal = 0f,
            float magicalCritBonus = 0f,
            float magicalAmp = 0f,
            float magicalPen = 0f)
        {
            if (physicalStrategy != null)
            {
                physicalStrategy.SetCritChanceBonus(physicalCritBonus);
                physicalStrategy.SetLifestealPercent(physicalLifesteal);
            }

            if (magicalStrategy != null)
            {
                magicalStrategy.SetCritChanceBonus(magicalCritBonus);
                magicalStrategy.SetSpellAmplification(magicalAmp);
                magicalStrategy.SetMagicPenetration(magicalPen);
            }
        }

        // Get individual strategy results for detailed analysis
        public (DamageResult physical, DamageResult magical) GetIndividualResults(DamageContext context)
        {
            var physicalResult = physicalStrategy.CalculateDamage(context);
            var magicalResult = magicalStrategy.CalculateDamage(context);
            return (physicalResult, magicalResult);
        }
    }
}