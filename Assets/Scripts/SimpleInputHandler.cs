using System;
using System.Collections.Generic;
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

        private readonly List<(InputAction action, Action<InputAction.CallbackContext> handler)> registeredHandlers = new();

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
            registeredHandlers.Clear();

            if (abilityActions == null)
            {
                return;
            }

            for (int i = 0; i < abilityActions.Length; i++)
            {
                var reference = abilityActions[i];
                var action = reference != null ? reference.action : null;

                if (action == null)
                {
                    continue;
                }

                if (!TryResolveAbilityIndex(action, out int abilityIndex))
                {
                    continue;
                }

                Action<InputAction.CallbackContext> handler = ctx =>
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

                action.performed += handler;
                if (!action.enabled)
                {
                    action.Enable();
                }

                registeredHandlers.Add((action, handler));
            }
        }

        private void OnDisable()
        {
            foreach (var (action, handler) in registeredHandlers)
            {
                if (action != null)
                {
                    action.performed -= handler;
                    if (action.enabled)
                    {
                        action.Disable();
                    }
                }
            }

            registeredHandlers.Clear();
        }

        private bool TryResolveAbilityIndex(InputAction action, out int abilityIndex)
        {
            abilityIndex = -1;
            if (action == null)
            {
                return false;
            }

            const string prefix = "Ability";
            if (!action.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var suffix = action.name.Substring(prefix.Length);
            if (int.TryParse(suffix, out int oneBasedIndex))
            {
                abilityIndex = Mathf.Max(0, oneBasedIndex - 1);
                return true;
            }

            return false;
        }
    }
}
