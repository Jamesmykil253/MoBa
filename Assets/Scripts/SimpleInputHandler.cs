using UnityEngine;
using UnityEngine.InputSystem;

namespace MOBA
{
    /// <summary>
    /// Bridges Input System actions to <see cref="SimpleAbilitySystem"/> casts.
    /// </summary>
    public class SimpleInputHandler : MonoBehaviour
    {
        [SerializeField] private SimpleAbilitySystem abilitySystem;
        [SerializeField] private InputActionReference[] abilityActions = new InputActionReference[4];

        private System.Action<InputAction.CallbackContext>[] actionHandlers;

        private void Awake()
        {
            if (abilitySystem == null)
            {
                abilitySystem = GetComponent<SimpleAbilitySystem>();
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
                    actionHandlers[i] = ctx => abilitySystem?.TryCastAbility(abilityIndex);
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
