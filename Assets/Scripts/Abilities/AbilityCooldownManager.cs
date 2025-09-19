using UnityEngine;
using System.Collections.Generic;
using MOBA.Debugging;

namespace MOBA.Abilities
{
    /// <summary>
    /// Manages ability cooldowns, cooldown reduction, and global cooldown mechanics.
    /// Responsible for tracking cooldown timers and applying cooldown reduction modifiers.
    /// </summary>
    public class AbilityCooldownManager : AbilityManagerComponent
    {
        #region Configuration
        
        [Header("Cooldown Settings")]
        [SerializeField, Tooltip("Global cooldown applied to all abilities")]
        private float globalCooldown = 0.1f;
        
        [SerializeField, Tooltip("Enable cooldown reduction mechanics")]
        private bool enableCooldownReduction = true;
        
        [SerializeField, Tooltip("Maximum cooldown reduction percentage (0.4 = 40%)")]
        private float maxCooldownReduction = 0.4f;
        
        [SerializeField, Tooltip("Enable detailed cooldown logging")]
        private bool logCooldownEvents = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Tracks cooldown timers for each ability by index
        /// </summary>
        private readonly Dictionary<int, float> cooldowns = new Dictionary<int, float>();
        
        /// <summary>
        /// Tracks whether abilities are locked (during channeling/casting)
        /// </summary>
        private readonly Dictionary<int, bool> abilityLocked = new Dictionary<int, bool>();
        
        /// <summary>
        /// Cache list for iterating cooldown keys without allocation
        /// </summary>
        private readonly List<int> cooldownKeyCache = new List<int>(8);
        
        /// <summary>
        /// Current cooldown reduction modifier (0.0 to maxCooldownReduction)
        /// </summary>
        private float currentCooldownReduction = 0f;
        
        /// <summary>
        /// Time of the last ability cast for global cooldown
        /// </summary>
        private float lastCastTime = 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when an ability cooldown updates. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int, float> OnCooldownUpdate; // ability index, remaining time
        
        /// <summary>
        /// Raised when an ability comes off cooldown
        /// </summary>
        public System.Action<int> OnCooldownComplete; // ability index
        
        /// <summary>
        /// Raised when cooldown reduction changes
        /// </summary>
        public System.Action<float> OnCooldownReductionChanged; // new CDR value
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current cooldown reduction percentage (0.0 to maxCooldownReduction)
        /// </summary>
        public float CooldownReduction => currentCooldownReduction;
        
        /// <summary>
        /// Whether global cooldown is currently active
        /// </summary>
        public bool IsGlobalCooldownActive => Time.time - lastCastTime < globalCooldown;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(EnhancedAbilitySystem abilitySystem)
        {
            base.Initialize(abilitySystem);
            
            // Initialize cooldown tracking for all abilities
            InitializeCooldownTracking();
            
            if (logCooldownEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Cooldown),
                    "Cooldown manager initialized.",
                    ("GlobalCooldown", globalCooldown),
                    ("MaxCDR", maxCooldownReduction));
            }
        }
        
        public override void Shutdown()
        {
            cooldowns.Clear();
            abilityLocked.Clear();
            cooldownKeyCache.Clear();
            
            OnCooldownUpdate = null;
            OnCooldownComplete = null;
            OnCooldownReductionChanged = null;
            
            if (logCooldownEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Cooldown),
                    "Cooldown manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        /// <summary>
        /// Initialize cooldown tracking for the available abilities
        /// </summary>
        private void InitializeCooldownTracking()
        {
            if (enhancedAbilitySystem?.Abilities == null)
            {
                return;
            }
            
            for (int i = 0; i < enhancedAbilitySystem.Abilities.Length; i++)
            {
                cooldowns[i] = 0f;
                abilityLocked[i] = false;
            }
        }
        
        #endregion
        
        #region Update Loop
        
        public override void UpdateComponent()
        {
            if (!IsInitialized())
            {
                return;
            }
            
            UpdateCooldowns();
        }
        
        /// <summary>
        /// Updates all ability cooldowns
        /// </summary>
        private void UpdateCooldowns()
        {
            cooldownKeyCache.Clear();
            
            // Collect keys to avoid modification during iteration
            foreach (var kvp in cooldowns)
            {
                if (kvp.Value > 0)
                {
                    cooldownKeyCache.Add(kvp.Key);
                }
            }
            
            // Update cooldowns
            foreach (int abilityIndex in cooldownKeyCache)
            {
                float remainingTime = cooldowns[abilityIndex] - Time.deltaTime;
                cooldowns[abilityIndex] = Mathf.Max(0f, remainingTime);
                
                // Trigger cooldown update event
                OnCooldownUpdate?.Invoke(abilityIndex, cooldowns[abilityIndex]);
                
                // Check if cooldown completed
                if (remainingTime > 0f && cooldowns[abilityIndex] <= 0f)
                {
                    OnCooldownComplete?.Invoke(abilityIndex);
                    
                    if (logCooldownEvents)
                    {
                        GameDebug.Log(
                            BuildContext(GameDebugMechanicTag.Cooldown),
                            "Ability cooldown completed.",
                            ("AbilityIndex", abilityIndex));
                    }
                }
            }
        }
        
        #endregion
        
        #region Cooldown Management
        
        /// <summary>
        /// Checks if the specified ability is ready to cast (off cooldown and not locked)
        /// </summary>
        /// <param name="abilityIndex">Ability index to check</param>
        /// <returns>True if ability is ready to cast</returns>
        public bool IsAbilityReady(int abilityIndex)
        {
            if (!cooldowns.ContainsKey(abilityIndex) || !abilityLocked.ContainsKey(abilityIndex))
            {
                return false;
            }
            
            return cooldowns[abilityIndex] <= 0f && !abilityLocked[abilityIndex] && !IsGlobalCooldownActive;
        }
        
        /// <summary>
        /// Gets the remaining cooldown time for the specified ability
        /// </summary>
        /// <param name="abilityIndex">Ability index to query</param>
        /// <returns>Remaining cooldown time in seconds</returns>
        public float GetCooldownRemaining(int abilityIndex)
        {
            return cooldowns.TryGetValue(abilityIndex, out float remaining) ? remaining : 0f;
        }
        
        /// <summary>
        /// Starts cooldown for the specified ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="cooldownTime">Base cooldown time</param>
        public void StartCooldown(int abilityIndex, float cooldownTime)
        {
            if (cooldownTime <= 0f)
            {
                return;
            }
            
            // Apply cooldown reduction
            float modifiedCooldown = cooldownTime;
            if (enableCooldownReduction && currentCooldownReduction > 0f)
            {
                modifiedCooldown *= (1f - currentCooldownReduction);
            }
            
            cooldowns[abilityIndex] = modifiedCooldown;
            lastCastTime = Time.time;
            
            OnCooldownUpdate?.Invoke(abilityIndex, modifiedCooldown);
            
            if (logCooldownEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Cooldown),
                    "Ability cooldown started.",
                    ("AbilityIndex", abilityIndex),
                    ("BaseCooldown", cooldownTime),
                    ("ModifiedCooldown", modifiedCooldown),
                    ("CDR", currentCooldownReduction));
            }
        }
        
        /// <summary>
        /// Resets cooldown for the specified ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        public void ResetCooldown(int abilityIndex)
        {
            if (cooldowns.ContainsKey(abilityIndex))
            {
                cooldowns[abilityIndex] = 0f;
                OnCooldownUpdate?.Invoke(abilityIndex, 0f);
                OnCooldownComplete?.Invoke(abilityIndex);
                
                if (logCooldownEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Ability cooldown reset.",
                        ("AbilityIndex", abilityIndex));
                }
            }
        }
        
        /// <summary>
        /// Reduces cooldown for the specified ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="reduction">Time to reduce in seconds</param>
        public void ReduceCooldown(int abilityIndex, float reduction)
        {
            if (cooldowns.ContainsKey(abilityIndex) && reduction > 0f)
            {
                float previousCooldown = cooldowns[abilityIndex];
                cooldowns[abilityIndex] = Mathf.Max(0f, previousCooldown - reduction);
                
                OnCooldownUpdate?.Invoke(abilityIndex, cooldowns[abilityIndex]);
                
                // Check if cooldown completed due to reduction
                if (previousCooldown > 0f && cooldowns[abilityIndex] <= 0f)
                {
                    OnCooldownComplete?.Invoke(abilityIndex);
                }
                
                if (logCooldownEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Ability cooldown reduced.",
                        ("AbilityIndex", abilityIndex),
                        ("Reduction", reduction),
                        ("Remaining", cooldowns[abilityIndex]));
                }
            }
        }
        
        #endregion
        
        #region Ability Locking
        
        /// <summary>
        /// Locks an ability (prevents casting during channeling)
        /// </summary>
        /// <param name="abilityIndex">Ability index to lock</param>
        public void LockAbility(int abilityIndex)
        {
            if (abilityLocked.ContainsKey(abilityIndex))
            {
                abilityLocked[abilityIndex] = true;
                
                if (logCooldownEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Ability locked.",
                        ("AbilityIndex", abilityIndex));
                }
            }
        }
        
        /// <summary>
        /// Unlocks an ability (allows casting)
        /// </summary>
        /// <param name="abilityIndex">Ability index to unlock</param>
        public void UnlockAbility(int abilityIndex)
        {
            if (abilityLocked.ContainsKey(abilityIndex))
            {
                abilityLocked[abilityIndex] = false;
                
                if (logCooldownEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Ability unlocked.",
                        ("AbilityIndex", abilityIndex));
                }
            }
        }
        
        /// <summary>
        /// Checks if an ability is currently locked
        /// </summary>
        /// <param name="abilityIndex">Ability index to check</param>
        /// <returns>True if ability is locked</returns>
        public bool IsAbilityLocked(int abilityIndex)
        {
            return abilityLocked.TryGetValue(abilityIndex, out bool locked) && locked;
        }
        
        #endregion
        
        #region Cooldown Reduction
        
        /// <summary>
        /// Sets the cooldown reduction modifier
        /// </summary>
        /// <param name="reduction">Cooldown reduction percentage (0.0 to maxCooldownReduction)</param>
        public void SetCooldownReduction(float reduction)
        {
            float clampedReduction = Mathf.Clamp(reduction, 0f, maxCooldownReduction);
            
            if (!Mathf.Approximately(currentCooldownReduction, clampedReduction))
            {
                currentCooldownReduction = clampedReduction;
                OnCooldownReductionChanged?.Invoke(currentCooldownReduction);
                
                if (logCooldownEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Cooldown reduction updated.",
                        ("CDR", currentCooldownReduction),
                        ("Percentage", currentCooldownReduction * 100f));
                }
            }
        }
        
        /// <summary>
        /// Adds to the current cooldown reduction
        /// </summary>
        /// <param name="additionalReduction">Additional CDR to add</param>
        public void AddCooldownReduction(float additionalReduction)
        {
            SetCooldownReduction(currentCooldownReduction + additionalReduction);
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Updates cooldown configuration at runtime
        /// </summary>
        /// <param name="newGlobalCooldown">New global cooldown time</param>
        /// <param name="newMaxCDR">New maximum cooldown reduction</param>
        public void UpdateConfiguration(float newGlobalCooldown, float newMaxCDR)
        {
            globalCooldown = newGlobalCooldown;
            maxCooldownReduction = Mathf.Clamp01(newMaxCDR);
            
            // Clamp current CDR to new maximum
            if (currentCooldownReduction > maxCooldownReduction)
            {
                SetCooldownReduction(maxCooldownReduction);
            }
            
            if (logCooldownEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Cooldown),
                    "Cooldown configuration updated.",
                    ("GlobalCooldown", globalCooldown),
                    ("MaxCDR", maxCooldownReduction));
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Gets current cooldown configuration
        /// </summary>
        /// <returns>Cooldown configuration data</returns>
        public CooldownConfig GetConfiguration()
        {
            return new CooldownConfig
            {
                GlobalCooldown = globalCooldown,
                EnableCooldownReduction = enableCooldownReduction,
                MaxCooldownReduction = maxCooldownReduction,
                CurrentCooldownReduction = currentCooldownReduction
            };
        }
        
        /// <summary>
        /// Gets all current cooldown states for debugging
        /// </summary>
        /// <returns>Dictionary of ability index to remaining cooldown</returns>
        public Dictionary<int, float> GetAllCooldowns()
        {
            return new Dictionary<int, float>(cooldowns);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for cooldown logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                tag,
                subsystem: nameof(AbilityCooldownManager),
                actor: gameObject?.name);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration data structure for cooldown settings
    /// </summary>
    [System.Serializable]
    public struct CooldownConfig
    {
        public float GlobalCooldown;
        public bool EnableCooldownReduction;
        public float MaxCooldownReduction;
        public float CurrentCooldownReduction;
    }
    
    #endregion
}