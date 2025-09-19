using System;
using MOBA.Abilities;

namespace MOBA.Services
{
    public interface IAbilityResourceService
    {
        float CurrentMana { get; }
        float MaxMana { get; }
        bool HasMana(float cost);
        bool TryConsume(float cost);
        void Restore(float amount);
        void SetMaxMana(float newMax);
    }

    public interface IAbilityCooldownService
    {
        void SetCooldownReduction(float reduction);
        float GetCooldownRemaining(int abilityIndex);
        bool IsAbilityReady(int abilityIndex);
        void ResetAllCooldowns();
    }

    public class AbilityResourceService : IAbilityResourceService
    {
        private readonly EnhancedAbilitySystem abilitySystem;

        public AbilityResourceService(EnhancedAbilitySystem abilitySystem)
        {
            this.abilitySystem = abilitySystem ?? throw new ArgumentNullException(nameof(abilitySystem));
        }

        public float CurrentMana => abilitySystem.CurrentMana;
        public float MaxMana => abilitySystem.MaxMana;

        public bool HasMana(float cost)
        {
            return abilitySystem.HasMana(cost);
        }

        public bool TryConsume(float cost)
        {
            return abilitySystem.ConsumeMana(cost);
        }

        public void Restore(float amount)
        {
            abilitySystem.RestoreMana(amount);
        }

        public void SetMaxMana(float newMax)
        {
            abilitySystem.SetMaxMana(newMax);
        }
    }

    public class AbilityCooldownService : IAbilityCooldownService
    {
        private readonly EnhancedAbilitySystem abilitySystem;

        public AbilityCooldownService(EnhancedAbilitySystem abilitySystem)
        {
            this.abilitySystem = abilitySystem ?? throw new ArgumentNullException(nameof(abilitySystem));
        }

        public void SetCooldownReduction(float reduction)
        {
            abilitySystem.SetCooldownReduction(reduction);
        }

        public float GetCooldownRemaining(int abilityIndex)
        {
            return abilitySystem.GetCooldownRemaining(abilityIndex);
        }

        public bool IsAbilityReady(int abilityIndex)
        {
            return abilitySystem.IsAbilityReady(abilityIndex);
        }

        public void ResetAllCooldowns()
        {
            abilitySystem.ResetAllCooldowns();
        }
    }
}
