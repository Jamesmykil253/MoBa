using UnityEngine;
using MOBA.Debugging;

namespace MOBA.Abilities
{
    /// <summary>
    /// Manages combat state tracking and combat-related mechanics.
    /// Responsible for determining when players enter/exit combat and related state changes.
    /// </summary>
    public class AbilityCombatManager : AbilityManagerComponent
    {
        #region Configuration
        
        [Header("Combat Settings")]
        [SerializeField, Tooltip("Duration to stay in combat after last action")]
        private float combatDuration = 5f;
        
        [SerializeField, Tooltip("Enable detailed combat logging")]
        private bool logCombatEvents = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Whether the player is currently in combat
        /// </summary>
        private bool isInCombat = false;
        
        /// <summary>
        /// Time of the last combat action
        /// </summary>
        private float lastCombatTime = 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when combat state changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<bool> OnCombatStateChanged; // in combat
        
        /// <summary>
        /// Raised when entering combat
        /// </summary>
        public System.Action OnCombatEntered;
        
        /// <summary>
        /// Raised when exiting combat
        /// </summary>
        public System.Action OnCombatExited;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Whether the player is currently in combat
        /// </summary>
        public bool IsInCombat => isInCombat;
        
        /// <summary>
        /// Time remaining until combat state expires
        /// </summary>
        public float CombatTimeRemaining => isInCombat ? Mathf.Max(0f, combatDuration - (Time.time - lastCombatTime)) : 0f;
        
        /// <summary>
        /// How long the player has been in the current combat state
        /// </summary>
        public float TimeSinceLastCombatAction => Time.time - lastCombatTime;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(EnhancedAbilitySystem abilitySystem)
        {
            base.Initialize(abilitySystem);
            
            // Initialize combat state
            isInCombat = false;
            lastCombatTime = Time.time - combatDuration;
            
            if (logCombatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Combat manager initialized.",
                    ("CombatDuration", combatDuration));
            }
        }
        
        public override void Shutdown()
        {
            OnCombatStateChanged = null;
            OnCombatEntered = null;
            OnCombatExited = null;
            
            if (logCombatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Combat manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Update Loop
        
        public override void UpdateComponent()
        {
            if (!IsInitialized())
            {
                return;
            }
            
            UpdateCombatState();
        }
        
        /// <summary>
        /// Updates combat state based on time since last combat action
        /// </summary>
        private void UpdateCombatState()
        {
            if (isInCombat && Time.time - lastCombatTime >= combatDuration)
            {
                ExitCombat();
            }
        }
        
        #endregion
        
        #region Combat State Management
        
        /// <summary>
        /// Enters combat state (called when performing combat actions)
        /// </summary>
        public void EnterCombat()
        {
            lastCombatTime = Time.time;
            
            if (!isInCombat)
            {
                isInCombat = true;
                OnCombatStateChanged?.Invoke(true);
                OnCombatEntered?.Invoke();
                
                if (logCombatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Combat),
                        "Entered combat state.");
                }
            }
        }
        
        /// <summary>
        /// Exits combat state (called when combat duration expires)
        /// </summary>
        private void ExitCombat()
        {
            if (isInCombat)
            {
                isInCombat = false;
                OnCombatStateChanged?.Invoke(false);
                OnCombatExited?.Invoke();
                
                if (logCombatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Combat),
                        "Exited combat state.");
                }
            }
        }
        
        /// <summary>
        /// Forces entry into combat state (called by external systems like taking damage)
        /// </summary>
        public void ForceEnterCombat()
        {
            EnterCombat();
            
            if (logCombatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Combat state forced by external system.");
            }
        }
        
        /// <summary>
        /// Forces exit from combat state (called by special abilities or effects)
        /// </summary>
        public void ForceExitCombat()
        {
            ExitCombat();
            
            if (logCombatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Combat state forcibly ended by external system.");
            }
        }
        
        /// <summary>
        /// Refreshes combat state timer (extends combat duration)
        /// </summary>
        public void RefreshCombatTimer()
        {
            if (isInCombat)
            {
                lastCombatTime = Time.time;
                
                if (logCombatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Combat),
                        "Combat timer refreshed.");
                }
            }
        }
        
        #endregion
        
        #region Combat Actions
        
        /// <summary>
        /// Called when an ability is cast to enter combat
        /// </summary>
        public void OnAbilityCast()
        {
            EnterCombat();
        }
        
        /// <summary>
        /// Called when taking damage to enter combat
        /// </summary>
        /// <param name="damage">Damage amount received</param>
        public void OnDamageTaken(float damage)
        {
            if (damage > 0f)
            {
                EnterCombat();
                
                if (logCombatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Combat),
                        "Entered combat due to damage taken.",
                        ("Damage", damage));
                }
            }
        }
        
        /// <summary>
        /// Called when dealing damage to enter combat
        /// </summary>
        /// <param name="damage">Damage amount dealt</param>
        public void OnDamageDealt(float damage)
        {
            if (damage > 0f)
            {
                EnterCombat();
                
                if (logCombatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Combat),
                        "Entered combat due to damage dealt.",
                        ("Damage", damage));
                }
            }
        }
        
        /// <summary>
        /// Called when healing to potentially enter combat
        /// </summary>
        /// <param name="healAmount">Healing amount</param>
        /// <param name="targetIsSelf">Whether the heal target is self</param>
        public void OnHealing(float healAmount, bool targetIsSelf)
        {
            // Only enter combat for healing others, not self-healing
            if (healAmount > 0f && !targetIsSelf)
            {
                EnterCombat();
                
                if (logCombatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Combat),
                        "Entered combat due to healing others.",
                        ("HealAmount", healAmount));
                }
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Updates combat configuration at runtime
        /// </summary>
        /// <param name="newCombatDuration">New combat duration in seconds</param>
        public void UpdateConfiguration(float newCombatDuration)
        {
            combatDuration = Mathf.Max(0f, newCombatDuration);
            
            if (logCombatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Combat),
                    "Combat configuration updated.",
                    ("CombatDuration", combatDuration));
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Gets current combat configuration
        /// </summary>
        /// <returns>Combat configuration data</returns>
        public CombatConfig GetConfiguration()
        {
            return new CombatConfig
            {
                CombatDuration = combatDuration,
                IsInCombat = isInCombat,
                LastCombatTime = lastCombatTime
            };
        }
        
        /// <summary>
        /// Checks if a given action would cause combat entry
        /// </summary>
        /// <param name="actionType">Type of action being checked</param>
        /// <returns>True if action would enter combat</returns>
        public bool WouldEnterCombat(CombatActionType actionType)
        {
            switch (actionType)
            {
                case CombatActionType.DamageAbility:
                case CombatActionType.DealDamage:
                case CombatActionType.TakeDamage:
                case CombatActionType.HealOthers:
                    return true;
                
                case CombatActionType.SelfHeal:
                case CombatActionType.BuffSelf:
                case CombatActionType.Movement:
                    return false;
                
                default:
                    return false;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for combat logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                tag,
                subsystem: nameof(AbilityCombatManager),
                actor: gameObject?.name);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration data structure for combat settings
    /// </summary>
    [System.Serializable]
    public struct CombatConfig
    {
        public float CombatDuration;
        public bool IsInCombat;
        public float LastCombatTime;
    }
    
    /// <summary>
    /// Types of actions that can affect combat state
    /// </summary>
    public enum CombatActionType
    {
        DamageAbility,
        SelfHeal,
        HealOthers,
        BuffSelf,
        DealDamage,
        TakeDamage,
        Movement
    }
    
    #endregion
}