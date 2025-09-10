using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Command for casting abilities with undo support
    /// </summary>
    public class AbilityCommand : ICommand
    {
        private readonly AbilitySystem abilitySystem;
        private readonly AbilityData ability;
        private readonly Vector2 targetPosition;
        private bool wasExecuted;

        public AbilityCommand(AbilitySystem abilitySystem, AbilityData ability, Vector2 target)
        {
            this.abilitySystem = abilitySystem;
            this.ability = ability;
            this.targetPosition = target;
            this.wasExecuted = false;
        }

        public void Execute()
        {
            if (CanExecute())
            {
                abilitySystem.CastAbility(ability, targetPosition);
                wasExecuted = true;
            }
        }

        public void Undo()
        {
            if (wasExecuted)
            {
                abilitySystem.CancelAbility(ability);
                wasExecuted = false;
            }
        }

        public bool CanExecute()
        {
            return abilitySystem != null && ability != null && abilitySystem.CanCastAbility(ability);
        }
    }

}