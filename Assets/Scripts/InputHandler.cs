using UnityEngine;
using UnityEngine.InputSystem;

namespace MOBA
{
    /// <summary>
    /// Input handler that uses the Command Pattern to process inputs
    /// Demonstrates separation of input capture from action execution
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private AbilitySystem commandManager;
        [SerializeField] private AbilitySystem abilitySystem;

        private PlayerInput playerInput;
        private Vector2 currentTargetPosition;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            if (commandManager == null)
            {
                commandManager = GetComponent<AbilitySystem>();
            }
        }

        private void OnEnable()
        {
            if (playerInput != null)
            {
                playerInput.actions["Ability1"].performed += OnAbility1;
                playerInput.actions["Ability2"].performed += OnAbility2;
                playerInput.actions["Ultimate"].performed += OnUltimate;
            }
        }

        private void OnDisable()
        {
            if (playerInput != null)
            {
                playerInput.actions["Ability1"].performed -= OnAbility1;
                playerInput.actions["Ability2"].performed -= OnAbility2;
                playerInput.actions["Ultimate"].performed -= OnUltimate;
            }
        }

        private void Update()
        {
            // Update target position based on mouse/touch input using new Input System
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                currentTargetPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            }
        }

        private void OnAbility1(InputAction.CallbackContext context)
        {
            if (abilitySystem != null)
            {
                var ability = new AbilityData { name = "Ability1" };
                abilitySystem.CastAbility(ability, currentTargetPosition);
            }
        }

        private void OnAbility2(InputAction.CallbackContext context)
        {
            if (abilitySystem != null)
            {
                var ability = new AbilityData { name = "Ability2" };
                abilitySystem.CastAbility(ability, currentTargetPosition);
            }
        }

        private void OnUltimate(InputAction.CallbackContext context)
        {
            if (abilitySystem != null)
            {
                var ability = new AbilityData { name = "Ultimate" };
                abilitySystem.CastAbility(ability, currentTargetPosition);
            }
        }

        /// <summary>
        /// Creates and executes an ability programmatically
        /// </summary>
        public void ExecuteAbilityCommand(string abilityName, Vector2 target)
        {
            if (abilitySystem != null)
            {
                var ability = new AbilityData { name = abilityName };
                abilitySystem.CastAbility(ability, target);
            }
        }
    }
}