using System;
using UnityEngine;
using Unity.Netcode;
using MOBA.Debugging;
using MOBA.Networking;

namespace MOBA.Abilities
{
    /// <summary>
    /// Enhanced ability system with component-based architecture for maintainability and single responsibility.
    /// Now delegates specialized responsibilities to focused manager components.
    /// </summary>
    public class EnhancedAbilitySystem : MonoBehaviour
    {
        #region Configuration
        
        [Header("Abilities")]
        [SerializeField] private EnhancedAbility[] abilities = new EnhancedAbility[4];
        
        [Header("Component Configuration")]
        [SerializeField, Tooltip("Enable detailed system logging")]
        private bool logSystemEvents = true;
        
        #endregion
        
        #region Manager Components
        
        /// <summary>
        /// Handles mana and resource management
        /// </summary>
        public AbilityResourceManager ResourceManager { get; private set; }
        
        /// <summary>
        /// Handles cooldown tracking and reduction
        /// </summary>
        public AbilityCooldownManager CooldownManager { get; private set; }
        
        /// <summary>
        /// Handles combat state tracking
        /// </summary>
        public AbilityCombatManager CombatManager { get; private set; }
        
        /// <summary>
        /// Handles ability execution and effects
        /// </summary>
        public AbilityExecutionManager ExecutionManager { get; private set; }
        
        /// <summary>
        /// Handles input processing and key bindings
        /// </summary>
        public AbilityInputManager InputManager { get; private set; }
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Network bridge for ability synchronization
        /// </summary>
        private AbilityNetworkController networkBridge;
        
        /// <summary>
        /// Whether the system is fully initialized
        /// </summary>
        private bool isInitialized = false;
        
        #endregion
        
        #region Events (Delegated from Components)
        
        /// <summary>
        /// Raised when an ability is cast. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnAbilityCast; // ability index
        
        /// <summary>
        /// Raised when mana changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<float, float> OnManaChanged; // current, max
        
        /// <summary>
        /// Raised when an ability cooldown updates. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int, float> OnCooldownUpdate; // ability index, remaining time
        
        /// <summary>
        /// Raised when combat state changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<bool> OnCombatStateChanged; // in combat
        
        #endregion
        
        #region Properties (Delegated to Components)
        
        /// <summary>
        /// Current mana amount
        /// </summary>
        public float CurrentMana => ResourceManager?.CurrentMana ?? 0f;
        
        /// <summary>
        /// Maximum mana amount
        /// </summary>
        public float MaxMana => ResourceManager?.MaxMana ?? 0f;
        
        /// <summary>
        /// Mana as percentage of maximum
        /// </summary>
        public float ManaPercentage => ResourceManager?.ManaPercentage ?? 0f;
        
        /// <summary>
        /// Whether currently in combat
        /// </summary>
        public bool IsInCombat => CombatManager?.IsInCombat ?? false;
        
        /// <summary>
        /// Current cooldown reduction percentage
        /// </summary>
        public float CooldownReduction => CooldownManager?.CooldownReduction ?? 0f;
        
        /// <summary>
        /// Public access to abilities array
        /// </summary>
        public EnhancedAbility[] Abilities => abilities;
        
        #endregion
        
        #region Unity Lifecycle
        
        void Awake()
        {
            InitializeComponents();
        }

        void OnEnable()
        {
            if (isInitialized)
            {
                EnableAllComponents();
            }
        }

        void Start()
        {
            if (!isInitialized)
            {
                InitializeSystem();
            }
            
            ConnectEvents();
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(),
                    "Enhanced ability system started.",
                    ("AbilityCount", abilities.Length),
                    ("ComponentsInitialized", isInitialized));
            }
        }
        
        void Update()
        {
            if (!isInitialized)
            {
                return;
            }
            
            // Update all manager components
            ResourceManager?.UpdateComponent();
            CooldownManager?.UpdateComponent();
            CombatManager?.UpdateComponent();
            ExecutionManager?.UpdateComponent();
            InputManager?.UpdateComponent();
        }
        
        void OnDisable()
        {
            DisableAllComponents();
        }
        
        void OnDestroy()
        {
            ShutdownSystem();
        }
        
        #endregion
        
        #region Component Management
        
        /// <summary>
        /// Initialize all ability manager components
        /// </summary>
        private void InitializeComponents()
        {
            // Create components if they don't exist
            ResourceManager = GetComponent<AbilityResourceManager>() ?? gameObject.AddComponent<AbilityResourceManager>();
            CooldownManager = GetComponent<AbilityCooldownManager>() ?? gameObject.AddComponent<AbilityCooldownManager>();
            CombatManager = GetComponent<AbilityCombatManager>() ?? gameObject.AddComponent<AbilityCombatManager>();
            ExecutionManager = GetComponent<AbilityExecutionManager>() ?? gameObject.AddComponent<AbilityExecutionManager>();
            InputManager = GetComponent<AbilityInputManager>() ?? gameObject.AddComponent<AbilityInputManager>();
            
            // Find network bridge
            networkBridge = GetComponent<AbilityNetworkController>();
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(),
                    "Components initialized.",
                    ("NetworkBridge", networkBridge != null));
            }
        }
        
        /// <summary>
        /// Initialize the complete ability system
        /// </summary>
        private void InitializeSystem()
        {
            try
            {
                // Initialize each component with this ability system
                ResourceManager?.Initialize(this);
                CooldownManager?.Initialize(this);
                CombatManager?.Initialize(this);
                ExecutionManager?.Initialize(this);
                InputManager?.Initialize(this);
                
                // Set cross-component references
                ExecutionManager?.SetManagers(ResourceManager, CooldownManager, CombatManager);
                InputManager?.SetManagers(ExecutionManager, CooldownManager);
                
                // Attach network bridge if available
                if (networkBridge != null)
                {
                    ExecutionManager?.AttachNetworkBridge(networkBridge);
                }
                
                isInitialized = true;
                
                if (logSystemEvents)
                {
                    GameDebug.Log(
                        BuildContext(),
                        "System initialization completed.");
                }
            }
            catch (System.Exception ex)
            {
                GameDebug.LogError(
                    BuildContext(),
                    "System initialization failed.",
                    ("Error", ex.Message));
                
                isInitialized = false;
            }
        }
        
        /// <summary>
        /// Connect component events to main system events
        /// </summary>
        private void ConnectEvents()
        {
            // Resource manager events
            if (ResourceManager != null)
            {
                ResourceManager.OnManaChanged += (current, max) => OnManaChanged?.Invoke(current, max);
            }
            
            // Cooldown manager events
            if (CooldownManager != null)
            {
                CooldownManager.OnCooldownUpdate += (index, remaining) => OnCooldownUpdate?.Invoke(index, remaining);
            }
            
            // Combat manager events
            if (CombatManager != null)
            {
                CombatManager.OnCombatStateChanged += (inCombat) => OnCombatStateChanged?.Invoke(inCombat);
            }
            
            // Execution manager events
            if (ExecutionManager != null)
            {
                ExecutionManager.OnAbilityCast += (index) => OnAbilityCast?.Invoke(index);
            }
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(),
                    "Component events connected.");
            }
        }
        
        /// <summary>
        /// Enable all manager components
        /// </summary>
        private void EnableAllComponents()
        {
            InputManager?.EnableInput();
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(),
                    "All components enabled.");
            }
        }
        
        /// <summary>
        /// Disable all manager components
        /// </summary>
        private void DisableAllComponents()
        {
            InputManager?.DisableInput();
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(),
                    "All components disabled.");
            }
        }
        
        /// <summary>
        /// Shutdown the complete ability system
        /// </summary>
        private void ShutdownSystem()
        {
            try
            {
                // Shutdown all components
                ResourceManager?.Shutdown();
                CooldownManager?.Shutdown();
                CombatManager?.Shutdown();
                ExecutionManager?.Shutdown();
                InputManager?.Shutdown();
                
                // Clear events
                OnAbilityCast = null;
                OnManaChanged = null;
                OnCooldownUpdate = null;
                OnCombatStateChanged = null;
                
                // Clear references
                networkBridge = null;
                isInitialized = false;
                
                if (logSystemEvents)
                {
                    GameDebug.Log(
                        BuildContext(),
                        "System shutdown completed.");
                }
            }
            catch (System.Exception ex)
            {
                GameDebug.LogError(
                    BuildContext(),
                    "System shutdown failed.",
                    ("Error", ex.Message));
            }
        }
        
        #endregion
        
        #region Public API (Delegates to Components)
        
        /// <summary>
        /// Attempt to cast an ability
        /// </summary>
        /// <param name="abilityIndex">Index of ability to cast</param>
        /// <returns>True if casting was initiated successfully</returns>
        public bool TryCastAbility(int abilityIndex)
        {
            if (!isInitialized || ExecutionManager == null)
            {
                return false;
            }
            
            return ExecutionManager.TryCastAbility(abilityIndex);
        }
        
        /// <summary>
        /// Check if an ability is ready to cast
        /// </summary>
        /// <param name="abilityIndex">Index of ability to check</param>
        /// <returns>True if ability is ready</returns>
        public bool IsAbilityReady(int abilityIndex)
        {
            if (!isInitialized || CooldownManager == null)
            {
                return false;
            }
            
            return CooldownManager.IsAbilityReady(abilityIndex);
        }
        
        /// <summary>
        /// Get remaining cooldown for an ability
        /// </summary>
        /// <param name="abilityIndex">Index of ability</param>
        /// <returns>Remaining cooldown in seconds</returns>
        public float GetAbilityCooldown(int abilityIndex)
        {
            if (!isInitialized || CooldownManager == null)
            {
                return 0f;
            }
            
            return CooldownManager.GetCooldownRemaining(abilityIndex);
        }
        
        /// <summary>
        /// Check if sufficient mana is available for an ability
        /// </summary>
        /// <param name="manaCost">Mana cost to check</param>
        /// <returns>True if sufficient mana</returns>
        public bool HasSufficientMana(float manaCost)
        {
            if (!isInitialized || ResourceManager == null)
            {
                return false;
            }
            
            return ResourceManager.HasSufficientMana(manaCost);
        }
        
        /// <summary>
        /// Force enter combat state
        /// </summary>
        public void ForceEnterCombat()
        {
            if (isInitialized && CombatManager != null)
            {
                CombatManager.ForceEnterCombat();
            }
        }
        
        /// <summary>
        /// Enable input processing
        /// </summary>
        public void EnableInput()
        {
            if (isInitialized && InputManager != null)
            {
                InputManager.EnableInput();
            }
        }
        
        /// <summary>
        /// Disable input processing
        /// </summary>
        public void DisableInput()
        {
            if (isInitialized && InputManager != null)
            {
                InputManager.DisableInput();
            }
        }
        
        /// <summary>
        /// Set key binding for an ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="keyCode">Key to bind</param>
        public void SetKeyBinding(int abilityIndex, KeyCode keyCode)
        {
            if (isInitialized && InputManager != null)
            {
                InputManager.SetKeyBinding(abilityIndex, keyCode);
            }
        }
        
        /// <summary>
        /// Set current cooldown reduction
        /// </summary>
        /// <param name="reduction">Cooldown reduction percentage (0-1)</param>
        public void SetCooldownReduction(float reduction)
        {
            if (isInitialized && CooldownManager != null)
            {
                CooldownManager.SetCooldownReduction(reduction);
            }
        }
        
        /// <summary>
        /// Restore mana amount
        /// </summary>
        /// <param name="amount">Amount to restore</param>
        public void RestoreMana(float amount)
        {
            if (isInitialized && ResourceManager != null)
            {
                ResourceManager.RestoreMana(amount);
            }
        }
        
        /// <summary>
        /// Set ability at given index
        /// </summary>
        /// <param name="abilityIndex">Index to set ability at</param>
        /// <param name="ability">Ability to set</param>
        public void SetAbility(int abilityIndex, EnhancedAbility ability)
        {
            if (abilityIndex >= 0 && abilityIndex < abilities.Length)
            {
                abilities[abilityIndex] = ability;
            }
        }
        
        /// <summary>
        /// Get remaining cooldown for an ability
        /// </summary>
        /// <param name="abilityIndex">Index of ability</param>
        /// <returns>Remaining cooldown in seconds</returns>
        public float GetCooldownRemaining(int abilityIndex)
        {
            if (!isInitialized || CooldownManager == null)
            {
                return 0f;
            }
            
            return CooldownManager.GetCooldownRemaining(abilityIndex);
        }
        
        /// <summary>
        /// Set input processing enabled state
        /// </summary>
        /// <param name="enabled">True to enable input, false to disable</param>
        public void SetInputEnabled(bool enabled)
        {
            if (!isInitialized || InputManager == null)
            {
                return;
            }
            
            if (enabled)
            {
                InputManager.EnableInput();
            }
            else
            {
                InputManager.DisableInput();
            }
        }
        
        /// <summary>
        /// Check if has sufficient mana for cost
        /// </summary>
        /// <param name="manaCost">Mana cost to check</param>
        /// <returns>True if has sufficient mana</returns>
        public bool HasMana(float manaCost)
        {
            if (!isInitialized || ResourceManager == null)
            {
                return false;
            }
            
            return ResourceManager.HasSufficientMana(manaCost);
        }
        
        /// <summary>
        /// Consume mana amount
        /// </summary>
        /// <param name="amount">Amount to consume</param>
        /// <returns>True if mana was consumed successfully</returns>
        public bool ConsumeMana(float amount)
        {
            if (!isInitialized || ResourceManager == null)
            {
                return false;
            }
            
            return ResourceManager.TryConsumeMana(amount);
        }
        
        /// <summary>
        /// Set maximum mana
        /// </summary>
        /// <param name="maxMana">New maximum mana value</param>
        public void SetMaxMana(float maxMana)
        {
            if (isInitialized && ResourceManager != null)
            {
                ResourceManager.UpdateConfiguration(maxMana, 5f, 2f); // Use default values for other params
            }
        }
        
        /// <summary>
        /// Reset all ability cooldowns
        /// </summary>
        public void ResetAllCooldowns()
        {
            if (isInitialized && CooldownManager != null)
            {
                // Reset cooldowns for all abilities
                for (int i = 0; i < abilities.Length; i++)
                {
                    CooldownManager.ResetCooldown(i);
                }
            }
        }
        
        #endregion
        
        #region Network Integration
        
        /// <summary>
        /// Attach network bridge for ability synchronization
        /// </summary>
        /// <param name="bridge">Network controller bridge</param>
        internal void AttachNetworkBridge(AbilityNetworkController bridge)
        {
            networkBridge = bridge;
            
            if (isInitialized && ExecutionManager != null)
            {
                ExecutionManager.AttachNetworkBridge(bridge);
            }
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(),
                    "Network bridge attached.");
            }
        }
        
        /// <summary>
        /// Detach network bridge
        /// </summary>
        /// <param name="bridge">Network controller bridge</param>
        internal void DetachNetworkBridge(AbilityNetworkController bridge)
        {
            if (networkBridge == bridge)
            {
                networkBridge = null;
                
                if (logSystemEvents)
                {
                    GameDebug.Log(
                        BuildContext(),
                        "Network bridge detached.");
                }
            }
        }
        
        /// <summary>
        /// Handle ability cast request from network
        /// </summary>
        /// <param name="request">Network ability cast request</param>
        /// <param name="senderClientId">Client ID of sender</param>
        internal void HandleAbilityCastRequest(AbilityCastRequest request, ulong senderClientId)
        {
            if (!isInitialized)
            {
                return;
            }
            
            // Validate request and attempt to cast
            if (request.AbilityIndex >= 0 && request.AbilityIndex < abilities.Length)
            {
                TryCastAbility(request.AbilityIndex);
            }
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network ability cast request received.",
                    ("AbilityIndex", request.AbilityIndex),
                    ("SenderClientId", senderClientId));
            }
        }
        
        /// <summary>
        /// Apply ability result from network
        /// </summary>
        /// <param name="result">Network ability cast result</param>
        /// <param name="isServerAuthority">Whether this is server authoritative</param>
        internal void ApplyAbilityResult(AbilityCastResult result, bool isServerAuthority)
        {
            if (!isInitialized)
            {
                return;
            }
            
            // Apply result based on server authority
            // This would be implemented based on specific network requirements
            
            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network ability result applied.",
                    ("AbilityIndex", result.AbilityIndex),
                    ("Success", result.Approved),
                    ("ServerAuthority", isServerAuthority));
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for system logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                tag,
                subsystem: nameof(EnhancedAbilitySystem),
                actor: gameObject?.name);
        }

        #endregion

        #region Pokemon Unite Evolution Support

        /// <summary>
        /// Modify ability damage for evolution upgrades
        /// </summary>
        public void ModifyAbilityDamage(int abilityIndex, float multiplier)
        {
            if (!IsValidAbilityIndex(abilityIndex)) return;

            var ability = abilities[abilityIndex];
            ability.damage *= (1f + multiplier);

            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.General),
                    "Ability damage modified by evolution.",
                    ("AbilityIndex", abilityIndex),
                    ("Multiplier", multiplier),
                    ("NewDamage", ability.damage));
            }
        }

        /// <summary>
        /// Modify ability cooldown for evolution upgrades
        /// </summary>
        public void ModifyAbilityCooldown(int abilityIndex, float multiplier)
        {
            if (!IsValidAbilityIndex(abilityIndex)) return;

            var ability = abilities[abilityIndex];
            ability.cooldown *= (1f + multiplier);
            ability.cooldown = Mathf.Max(0.1f, ability.cooldown); // Minimum cooldown

            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.General),
                    "Ability cooldown modified by evolution.",
                    ("AbilityIndex", abilityIndex),
                    ("Multiplier", multiplier),
                    ("NewCooldown", ability.cooldown));
            }
        }

        /// <summary>
        /// Modify ability range for evolution upgrades
        /// </summary>
        public void ModifyAbilityRange(int abilityIndex, float multiplier)
        {
            if (!IsValidAbilityIndex(abilityIndex)) return;

            var ability = abilities[abilityIndex];
            ability.range *= (1f + multiplier);

            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.General),
                    "Ability range modified by evolution.",
                    ("AbilityIndex", abilityIndex),
                    ("Multiplier", multiplier),
                    ("NewRange", ability.range));
            }
        }

        /// <summary>
        /// Modify ability duration for evolution upgrades
        /// </summary>
        public void ModifyAbilityDuration(int abilityIndex, float multiplier)
        {
            if (!IsValidAbilityIndex(abilityIndex)) return;

            var ability = abilities[abilityIndex];
            // Duration could be stored in a custom field or effect duration
            // For now, we'll modify cast time as a placeholder
            ability.castTime *= (1f + multiplier);

            if (logSystemEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.General),
                    "Ability duration modified by evolution.",
                    ("AbilityIndex", abilityIndex),
                    ("Multiplier", multiplier),
                    ("NewDuration", ability.castTime));
            }
        }

        /// <summary>
        /// Add new effect to ability through evolution
        /// </summary>
        public void AddAbilityEffect(int abilityIndex, string effectName)
        {
            if (!IsValidAbilityIndex(abilityIndex)) return;

            // In a full implementation, this would add new effects to the ability
            // For now, we'll log the addition and modify some properties based on effect name
            var ability = abilities[abilityIndex];

            switch (effectName)
            {
                case "DamageReflection":
                    ability.effectColor = Color.red; // Visual indicator
                    if (logSystemEvents)
                    {
                        GameDebug.Log(
                            BuildContext(GameDebugMechanicTag.General),
                            "Added damage reflection effect to ability.",
                            ("AbilityIndex", abilityIndex));
                    }
                    break;
                case "LifeSteal":
                    ability.effectColor = Color.green; // Visual indicator
                    if (logSystemEvents)
                    {
                        GameDebug.Log(
                            BuildContext(GameDebugMechanicTag.General),
                            "Added life steal effect to ability.",
                            ("AbilityIndex", abilityIndex));
                    }
                    break;
                case "AreaOfEffect":
                    ability.abilityType = AbilityType.Area;
                    ability.maxTargets *= 2; // Double target count
                    if (logSystemEvents)
                    {
                        GameDebug.Log(
                            BuildContext(GameDebugMechanicTag.General),
                            "Added area of effect to ability.",
                            ("AbilityIndex", abilityIndex),
                            ("NewMaxTargets", ability.maxTargets));
                    }
                    break;
                default:
                    if (logSystemEvents)
                    {
                        GameDebug.Log(
                            BuildContext(GameDebugMechanicTag.General),
                            "Added custom effect to ability.",
                            ("AbilityIndex", abilityIndex),
                            ("EffectName", effectName));
                    }
                    break;
            }
        }

        /// <summary>
        /// Validate ability index is within valid range and ability exists
        /// </summary>
        private bool IsValidAbilityIndex(int index)
        {
            return index >= 0 && index < abilities.Length && abilities[index] != null;
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Enhanced ability data with configuration for effects, costs, and behavior
    /// </summary>
    [CreateAssetMenu(fileName = "EnhancedAbility", menuName = "MOBA/Abilities/Enhanced Ability")]
    public class EnhancedAbility : ScriptableObject
    {
        [Header("Basic Properties")]
        public string abilityName = "New Ability";
        [TextArea(2, 4)]
        public string description = "Ability description";
        public Sprite icon;
        
        [Header("Costs")]
        public float manaCost = 50f;
        public float cooldown = 5f;
        public float castTime = 0f; // Channel time before effect
        
        [Header("Effects")]
        public float damage = 100f;
        public float range = 5f;
        public int maxTargets = 5;
        
        [Header("Visual")]
        public bool enableParticleEffect = true;
        public Color effectColor = Color.blue;
        public bool enableScreenShake = false;
        public float screenShakeIntensity = 0.1f;
        
        [Header("Audio")]
        public AudioClip castSound;
        public AudioClip hitSound;
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Header("Advanced")]
        public AbilityType abilityType = AbilityType.Instant;
        public TargetType targetType = TargetType.Enemy;
        public bool canCritical = true;
        public float criticalChance = 0.05f;
        public float criticalMultiplier = 1.5f;
    }
    
    /// <summary>
    /// Type of ability effect
    /// </summary>
    public enum AbilityType
    {
        Instant,    // Immediate effect
        Channeled,  // Requires channeling
        Projectile, // Fires a projectile
        Area,       // Area of effect
        Buff,       // Applies buff/debuff
        Heal        // Healing ability
    }
    
    /// <summary>
    /// Valid target types for abilities
    /// </summary>
    public enum TargetType
    {
        Enemy,
        Ally,
        Self,
        All
    }
    
    #endregion
}