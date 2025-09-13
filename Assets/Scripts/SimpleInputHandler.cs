using UnityEngine;
using UnityEngine.InputSystem;

namespace MOBA
{
    /// <summary>
    /// Simple input handler for basic MOBA controls
    /// </summary>
    public class SimpleInputHandler : MonoBehaviour
    {
        [SerializeField] private SimpleAbilitySystem abilitySystem;

        private void Awake()
        {
            if (abilitySystem == null)
            {
                abilitySystem = GetComponent<SimpleAbilitySystem>();
            }
        }

        private void Update()
        {
            // Basic keyboard input for abilities
            if (Input.GetKeyDown(KeyCode.Q))
            {
                abilitySystem?.TryCastAbility(0);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                abilitySystem?.TryCastAbility(1);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                abilitySystem?.TryCastAbility(2);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                abilitySystem?.TryCastAbility(3);
            }
        }
    }
}
