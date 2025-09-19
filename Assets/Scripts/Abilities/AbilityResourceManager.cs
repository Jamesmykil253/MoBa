using UnityEngine;
using MOBA.Debugging;

namespace MOBA.Abilities
{
    /// <summary>
    /// Manages ability resources like mana, energy, and resource regeneration.
    /// Responsible for resource validation, consumption, and restoration.
    /// </summary>
    public class AbilityResourceManager : AbilityManagerComponent
    {
        #region Configuration
        
        [Header("Resource Settings")]
        [SerializeField, Tooltip("Maximum mana pool")]
        private float maxMana = 100f;
        
        [SerializeField, Tooltip("Mana regeneration per second")]
        private float manaRegenPerSecond = 5f;
        
        [SerializeField, Tooltip("Multiplier for out-of-combat mana regeneration")]
        private float outOfCombatManaMultiplier = 2f;
        
        [SerializeField, Tooltip("Enable detailed resource logging")]
        private bool logResourceEvents = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Current mana amount
        /// </summary>
        private float currentMana = 100f;
        
        /// <summary>
        /// Reference to combat manager for combat state checks
        /// </summary>
        private AbilityCombatManager combatManager;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when mana changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<float, float> OnManaChanged; // current, max
        
        /// <summary>
        /// Raised when mana is insufficient for an ability
        /// </summary>
        public System.Action<float, float> OnInsufficientMana; // required, available
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current mana amount
        /// </summary>
        public float CurrentMana => currentMana;
        
        /// <summary>
        /// Maximum mana amount
        /// </summary>
        public float MaxMana => maxMana;
        
        /// <summary>
        /// Current mana as a percentage (0.0 to 1.0)
        /// </summary>
        public float ManaPercentage => maxMana > 0 ? currentMana / maxMana : 0f;
        
        /// <summary>
        /// Whether mana is at maximum
        /// </summary>
        public bool IsFullMana => currentMana >= maxMana;
        
        /// <summary>
        /// Whether mana is depleted
        /// </summary>
        public bool IsOutOfMana => currentMana <= 0f;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(EnhancedAbilitySystem abilitySystem)
        {
            base.Initialize(abilitySystem);
            
            // Initialize mana to max
            currentMana = maxMana;
            
            if (logResourceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Resource),
                    "Resource manager initialized.",
                    ("MaxMana", maxMana),
                    ("RegenRate", manaRegenPerSecond));
            }
        }
        
        public override void Shutdown()
        {
            OnManaChanged = null;
            OnInsufficientMana = null;
            combatManager = null;
            
            if (logResourceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Resource),
                    "Resource manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        /// <summary>
        /// Set reference to combat manager for combat state checks
        /// </summary>
        /// <param name="combatMgr">Combat manager reference</param>
        public void SetCombatManager(AbilityCombatManager combatMgr)
        {
            combatManager = combatMgr;
        }
        
        #endregion
        
        #region Update Loop
        
        public override void UpdateComponent()
        {
            if (!IsInitialized())
            {
                return;
            }
            
            UpdateManaRegeneration();
        }
        
        /// <summary>
        /// Updates mana regeneration based on combat state
        /// </summary>
        private void UpdateManaRegeneration()
        {
            if (IsFullMana)
            {
                return;
            }
            
            float regenAmount = manaRegenPerSecond * Time.deltaTime;
            
            // Apply out-of-combat multiplier
            if (combatManager != null && !combatManager.IsInCombat)
            {
                regenAmount *= outOfCombatManaMultiplier;
            }
            
            RestoreMana(regenAmount, false);
        }
        
        #endregion
        
        #region Resource Management
        
        /// <summary>
        /// Checks if sufficient mana is available for an ability
        /// </summary>
        /// <param name="manaCost">Required mana amount</param>
        /// <returns>True if sufficient mana is available</returns>
        public bool HasSufficientMana(float manaCost)
        {
            return currentMana >= manaCost;
        }
        
        /// <summary>
        /// Attempts to consume mana for an ability
        /// </summary>
        /// <param name="manaCost">Mana amount to consume</param>
        /// <returns>True if mana was successfully consumed</returns>
        public bool TryConsumeMana(float manaCost)
        {
            if (!HasSufficientMana(manaCost))
            {
                OnInsufficientMana?.Invoke(manaCost, currentMana);
                
                if (logResourceEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Resource),
                        "Insufficient mana for ability.",
                        ("Required", manaCost),
                        ("Available", currentMana));
                }
                
                return false;
            }
            
            currentMana = Mathf.Max(0f, currentMana - manaCost);
            OnManaChanged?.Invoke(currentMana, maxMana);
            
            if (logResourceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Resource),
                    "Mana consumed.",
                    ("Cost", manaCost),
                    ("Remaining", currentMana));
            }
            
            return true;
        }
        
        /// <summary>
        /// Restores mana by the specified amount
        /// </summary>
        /// <param name="amount">Amount of mana to restore</param>
        /// <param name="triggerEvents">Whether to trigger mana changed events</param>
        public void RestoreMana(float amount, bool triggerEvents = true)
        {
            if (amount <= 0f)
            {
                return;
            }
            
            float previousMana = currentMana;
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            
            if (triggerEvents && !Mathf.Approximately(previousMana, currentMana))
            {
                OnManaChanged?.Invoke(currentMana, maxMana);
            }
            
            if (logResourceEvents && triggerEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Resource),
                    "Mana restored.",
                    ("Amount", amount),
                    ("Current", currentMana));
            }
        }
        
        /// <summary>
        /// Sets current mana to the specified amount
        /// </summary>
        /// <param name="amount">New mana amount</param>
        public void SetMana(float amount)
        {
            currentMana = Mathf.Clamp(amount, 0f, maxMana);
            OnManaChanged?.Invoke(currentMana, maxMana);
            
            if (logResourceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Resource),
                    "Mana set.",
                    ("Amount", currentMana));
            }
        }
        
        /// <summary>
        /// Restores mana to maximum
        /// </summary>
        public void RestoreFullMana()
        {
            SetMana(maxMana);
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Updates resource configuration at runtime
        /// </summary>
        /// <param name="newMaxMana">New maximum mana amount</param>
        /// <param name="regenRate">New mana regeneration rate per second</param>
        /// <param name="outOfCombatMultiplier">New out-of-combat regeneration multiplier</param>
        public void UpdateConfiguration(float newMaxMana, float regenRate, float outOfCombatMultiplier)
        {
            float manaPercentage = ManaPercentage;
            maxMana = newMaxMana;
            manaRegenPerSecond = regenRate;
            outOfCombatManaMultiplier = outOfCombatMultiplier;
            
            // Maintain mana percentage when max mana changes
            currentMana = maxMana * manaPercentage;
            OnManaChanged?.Invoke(currentMana, maxMana);
            
            if (logResourceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Resource),
                    "Resource configuration updated.",
                    ("MaxMana", maxMana),
                    ("RegenRate", regenRate),
                    ("OutOfCombatMultiplier", outOfCombatMultiplier));
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Gets current resource configuration
        /// </summary>
        /// <returns>Resource configuration data</returns>
        public ResourceConfig GetConfiguration()
        {
            return new ResourceConfig
            {
                MaxMana = maxMana,
                ManaRegenPerSecond = manaRegenPerSecond,
                OutOfCombatManaMultiplier = outOfCombatManaMultiplier
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for resource logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                tag,
                subsystem: nameof(AbilityResourceManager),
                actor: gameObject?.name);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration data structure for resource settings
    /// </summary>
    [System.Serializable]
    public struct ResourceConfig
    {
        public float MaxMana;
        public float ManaRegenPerSecond;
        public float OutOfCombatManaMultiplier;
    }
    
    #endregion
}