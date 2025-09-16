using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Abilities;

namespace MOBA
{
    /// <summary>
    /// Bridges Input System actions to ability casts.
    /// </summary>
    public class SimpleInputHandler : MonoBehaviour
    {
        [SerializeField] private EnhancedAbilitySystem enhancedAbilitySystem;
        [SerializeField] private SimpleAbilitySystem legacyAbilitySystem;
        [SerializeField] private InputActionReference[] abilityActions = new InputActionReference[4];

        private System.Action<InputAction.CallbackContext>[] actionHandlers;

        private void Awake()
        {
            if (enhancedAbilitySystem == null)
            {
                enhancedAbilitySystem = GetComponent<EnhancedAbilitySystem>();
            }

            if (enhancedAbilitySystem == null && legacyAbilitySystem == null)
            {
                legacyAbilitySystem = GetComponent<SimpleAbilitySystem>();
            }

            if (enhancedAbilitySystem == null && legacyAbilitySystem != null)
            {
                enhancedAbilitySystem = legacyAbilitySystem.GetEnhancedSystem();
            }
        }

        private void OnEnable()
        {
            if (abilityActions == null) return;

            if (actionHandlers == null || actionHandlers.Length != abilityActions.Length)
            {
                actionHandlers = new System.Action<InputAction.CallbackContext>[abilityActions.Length];
            }

            for (int i = 0; i < abilityActions.Length; i++)
            {
                var reference = abilityActions[i];
                if (reference == null) continue;
                var action = reference.action;
                if (action == null) continue;

                if (actionHandlers[i] == null)
                {
                    int abilityIndex = i;
                    actionHandlers[i] = ctx =>
                    {
                        if (enhancedAbilitySystem != null)
                        {
                            enhancedAbilitySystem.TryCastAbility(abilityIndex);
                        }
                        else
                        {
                            legacyAbilitySystem?.TryCastAbility(abilityIndex);
                        }
                    };
                }

                action.performed += actionHandlers[i];
                if (!action.enabled)
                {
                    action.Enable();
                }
            }
        }

        private void OnDisable()
        {
            if (abilityActions == null) return;

            for (int i = 0; i < abilityActions.Length; i++)
            {
                var reference = abilityActions[i];
                if (reference == null) continue;
                var action = reference.action;
                if (action == null) continue;

                if (actionHandlers != null && actionHandlers.Length > i && actionHandlers[i] != null)
                {
                    action.performed -= actionHandlers[i];
                }

                if (action.enabled)
                {
                    action.Disable();
                }
            }
        }
    }
}
