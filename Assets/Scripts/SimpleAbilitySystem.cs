using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// Simple ability system with basic cooldowns
    /// </summary>
    public class SimpleAbilitySystem : MonoBehaviour
    {
        [Header("Abilities")]
        public SimpleAbility[] abilities = new SimpleAbility[4];

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string[] abilityActionNames = new[] { "Ability1", "Ability2", "Ability3", "Ability4" };

        [Header("Settings")]
        public float globalCooldown = 0.1f;

        private float lastCastTime;
        private Dictionary<int, float> cooldowns = new Dictionary<int, float>();
        private InputAction[] abilityActions;
        private System.Action<InputAction.CallbackContext>[] abilityCallbacks;
        private bool inputInitialized;
        private bool inputEnabled = true;

        void Awake()
        {
            InitializeInput();
        }

        void OnEnable()
        {
            ApplyInputState(true);
        }

        void Start()
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                cooldowns[i] = 0f;
            }
        }

        void Update()
        {
            UpdateCooldowns();
        }

        void OnDisable()
        {
            ApplyInputState(false);
        }

        void OnDestroy()
        {
            CleanupInput();
        }
        
        void UpdateCooldowns()
        {
            var keys = new List<int>(cooldowns.Keys);
            foreach (var key in keys)
            {
                if (cooldowns[key] > 0)
                    cooldowns[key] -= Time.deltaTime;
            }
        }
        
        public void TryCastAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return;
            if (abilities[abilityIndex] == null) return;
            
            // Check global cooldown
            if (Time.time - lastCastTime < globalCooldown) return;
            
            // Check ability cooldown
            if (cooldowns[abilityIndex] > 0) return;
            
            // Cast ability
            CastAbility(abilityIndex);
        }
        
        void CastAbility(int abilityIndex)
        {
            var ability = abilities[abilityIndex];
            
            // Apply damage to nearby enemies
            Collider[] enemies = Physics.OverlapSphere(transform.position, ability.range);
            foreach (var enemy in enemies)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    var damageable = enemy.GetComponent<IDamageable>();
                    damageable?.TakeDamage(ability.damage);
                }
            }
            
            // Set cooldowns
            cooldowns[abilityIndex] = ability.cooldown;
            lastCastTime = Time.time;
            
            Debug.Log($"Cast {ability.abilityName} - Damage: {ability.damage}, Range: {ability.range}");
        }
        
        public bool IsAbilityReady(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return false;
            return cooldowns[abilityIndex] <= 0;
        }
        
        public float GetCooldownRemaining(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return 0f;
            return Mathf.Max(0f, cooldowns[abilityIndex]);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!isActiveAndEnabled) return;
            ApplyInputState(enabled);
        }

        private void InitializeInput()
        {
            if (inputInitialized)
            {
                return;
            }

            if (abilities == null)
            {
                abilities = new SimpleAbility[4];
            }

            if (abilityActionNames == null || abilityActionNames.Length == 0)
            {
                abilityActionNames = new[] { "Ability1", "Ability2", "Ability3", "Ability4" };
            }

            if (inputActions == null)
            {
                var playerInput = GetComponentInParent<PlayerInput>();
                if (playerInput != null)
                {
                    inputActions = playerInput.actions;
                }
            }

            int count = abilities.Length;
            abilityActions = new InputAction[count];
            abilityCallbacks = new System.Action<InputAction.CallbackContext>[count];

            if (inputActions != null)
            {
                int mapped = Mathf.Min(count, abilityActionNames.Length);
                for (int i = 0; i < mapped; i++)
                {
                    string actionName = abilityActionNames[i];
                    if (string.IsNullOrEmpty(actionName))
                    {
                        continue;
                    }

                    var action = inputActions.FindAction(actionName, throwIfNotFound: false);
                    if (action == null)
                    {
                        Debug.LogWarning($"[SimpleAbilitySystem] Input action '{actionName}' not found.");
                        continue;
                    }

                    abilityActions[i] = action;
                    int abilityIndex = i;
                    abilityCallbacks[i] = ctx =>
                    {
                        if (!inputEnabled || !isActiveAndEnabled) return;
                        TryCastAbility(abilityIndex);
                    };
                    action.performed += abilityCallbacks[i];
                }
            }

            inputInitialized = true;
        }

        private void ApplyInputState(bool enable)
        {
            if (abilityActions == null) return;

            foreach (var action in abilityActions)
            {
                if (action == null) continue;
                if (enable)
                {
                    if (!action.enabled) action.Enable();
                }
                else if (action.enabled)
                {
                    action.Disable();
                }
            }
        }

        private void CleanupInput()
        {
            if (abilityActions == null || abilityCallbacks == null) return;

            for (int i = 0; i < abilityActions.Length; i++)
            {
                if (abilityActions[i] != null && abilityCallbacks[i] != null)
                {
                    abilityActions[i].performed -= abilityCallbacks[i];
                }
            }
        }
    }
}
