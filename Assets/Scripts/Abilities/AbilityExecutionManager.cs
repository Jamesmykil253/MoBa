using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using MOBA.Debugging;
using MOBA.Effects;
using MOBA.Networking;

namespace MOBA.Abilities
{
    /// <summary>
    /// Manages ability execution, effects, and network coordination.
    /// Responsible for ability casting, effect execution, hit detection, and network synchronization.
    /// </summary>
    public class AbilityExecutionManager : AbilityManagerComponent
    {
        #region Configuration
        
        [Header("Execution Settings")]
        [SerializeField, Tooltip("Enable detailed execution logging")]
        private bool logExecutionEvents = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Hit result buffers pooled to avoid per-cast allocations
        /// </summary>
        private readonly Stack<List<AbilityHitResult>> hitBufferPool = new Stack<List<AbilityHitResult>>();
        
        /// <summary>
        /// Static buffer for non-alloc overlap sphere operations
        /// </summary>
        private static readonly Collider[] overlapBuffer = new Collider[32];
        
        /// <summary>
        /// Network bridge for ability synchronization
        /// </summary>
        private AbilityNetworkController networkBridge;
        
        /// <summary>
        /// Reference to resource manager for mana consumption
        /// </summary>
        private AbilityResourceManager resourceManager;
        
        /// <summary>
        /// Reference to cooldown manager for cooldown management
        /// </summary>
        private AbilityCooldownManager cooldownManager;
        
        /// <summary>
        /// Reference to combat manager for combat state updates
        /// </summary>
        private AbilityCombatManager combatManager;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when an ability is cast. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnAbilityCast; // ability index
        
        /// <summary>
        /// Raised when an ability execution completes
        /// </summary>
        public System.Action<int, bool> OnAbilityExecuted; // ability index, success
        
        /// <summary>
        /// Raised when an ability fails to execute
        /// </summary>
        public System.Action<int, AbilityFailureCode> OnAbilityFailed; // ability index, failure reason
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(EnhancedAbilitySystem abilitySystem)
        {
            base.Initialize(abilitySystem);
            
            if (logExecutionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AbilityUse),
                    "Execution manager initialized.");
            }
        }
        
        public override void Shutdown()
        {
            // Clear hit buffer pool
            while (hitBufferPool.Count > 0)
            {
                hitBufferPool.Pop().Clear();
            }
            
            OnAbilityCast = null;
            OnAbilityExecuted = null;
            OnAbilityFailed = null;
            
            networkBridge = null;
            resourceManager = null;
            cooldownManager = null;
            combatManager = null;
            
            if (logExecutionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AbilityUse),
                    "Execution manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        /// <summary>
        /// Set references to other ability managers
        /// </summary>
        public void SetManagers(AbilityResourceManager resMgr, AbilityCooldownManager cdMgr, AbilityCombatManager combatMgr)
        {
            resourceManager = resMgr;
            cooldownManager = cdMgr;
            combatManager = combatMgr;
        }
        
        /// <summary>
        /// Attach network bridge for ability synchronization
        /// </summary>
        /// <param name="bridge">Network controller bridge</param>
        public void AttachNetworkBridge(AbilityNetworkController bridge)
        {
            networkBridge = bridge;
        }
        
        #endregion
        
        #region Ability Execution
        
        /// <summary>
        /// Attempts to cast the specified ability
        /// </summary>
        /// <param name="abilityIndex">Index of ability to cast</param>
        /// <returns>True if ability casting was initiated successfully</returns>
        public bool TryCastAbility(int abilityIndex)
        {
            // Validate ability index
            if (abilityIndex < 0 || abilityIndex >= enhancedAbilitySystem.Abilities.Length)
            {
                HandleAbilityFailure(abilityIndex, AbilityFailureCode.InvalidIndex);
                return false;
            }
            
            var ability = enhancedAbilitySystem.Abilities[abilityIndex];
            if (ability == null)
            {
                HandleAbilityFailure(abilityIndex, AbilityFailureCode.AbilityNotFound);
                return false;
            }
            
            // Validate ability readiness
            var validationResult = ValidateAbility(abilityIndex, ability);
            if (validationResult != AbilityFailureCode.None)
            {
                HandleAbilityFailure(abilityIndex, validationResult);
                return false;
            }
            
            // Execute ability
            return ExecuteAbility(abilityIndex, ability);
        }
        
        /// <summary>
        /// Validates if an ability can be cast without consuming resources
        /// </summary>
        /// <param name="abilityIndex">Index of ability to validate</param>
        /// <returns>Failure code if validation fails, None if valid</returns>
        public AbilityFailureCode ValidateAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= enhancedAbilitySystem.Abilities.Length)
            {
                return AbilityFailureCode.InvalidIndex;
            }
            
            var ability = enhancedAbilitySystem.Abilities[abilityIndex];
            if (ability == null)
            {
                return AbilityFailureCode.AbilityNotFound;
            }
            
            return ValidateAbility(abilityIndex, ability);
        }
        
        /// <summary>
        /// Internal validation logic for ability casting
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="ability">Ability data</param>
        /// <returns>Failure code if validation fails</returns>
        private AbilityFailureCode ValidateAbility(int abilityIndex, EnhancedAbility ability)
        {
            // Check if ability is ready (cooldown and lock state)
            if (cooldownManager != null && !cooldownManager.IsAbilityReady(abilityIndex))
            {
                if (cooldownManager.IsAbilityLocked(abilityIndex))
                {
                    return AbilityFailureCode.AbilityLocked;
                }
                return AbilityFailureCode.OnCooldown;
            }
            
            // Check mana cost
            if (resourceManager != null && !resourceManager.HasSufficientMana(ability.manaCost))
            {
                return AbilityFailureCode.InsufficientMana;
            }
            
            return AbilityFailureCode.None;
        }
        
        /// <summary>
        /// Executes the validated ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="ability">Ability data</param>
        /// <returns>True if execution started successfully</returns>
        private bool ExecuteAbility(int abilityIndex, EnhancedAbility ability)
        {
            // Consume resources
            if (resourceManager != null && !resourceManager.TryConsumeMana(ability.manaCost))
            {
                HandleAbilityFailure(abilityIndex, AbilityFailureCode.InsufficientMana);
                return false;
            }
            
            // Start cooldown
            cooldownManager?.StartCooldown(abilityIndex, ability.cooldown);
            
            // Enter combat
            combatManager?.OnAbilityCast();
            
            // Handle cast time
            if (ability.castTime > 0f)
            {
                StartCoroutine(CastAbilityWithDelay(abilityIndex, ability));
            }
            else
            {
                // Instant cast
                ExecuteAbilityEffect(abilityIndex, ability);
            }
            
            // Trigger event
            OnAbilityCast?.Invoke(abilityIndex);
            
            if (logExecutionEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AbilityUse),
                    "Ability cast initiated.",
                    ("AbilityIndex", abilityIndex),
                    ("AbilityName", ability.abilityName),
                    ("ManaCost", ability.manaCost),
                    ("CastTime", ability.castTime));
            }
            
            return true;
        }
        
        /// <summary>
        /// Handles ability casting with cast time delay
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="ability">Ability data</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator CastAbilityWithDelay(int abilityIndex, EnhancedAbility ability)
        {
            // Lock ability during cast
            cooldownManager?.LockAbility(abilityIndex);
            
            // Wait for cast time
            yield return new WaitForSeconds(ability.castTime);
            
            // Unlock ability
            cooldownManager?.UnlockAbility(abilityIndex);
            
            // Execute effect
            ExecuteAbilityEffect(abilityIndex, ability);
        }
        
        /// <summary>
        /// Executes the actual ability effect
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="ability">Ability data</param>
        private void ExecuteAbilityEffect(int abilityIndex, EnhancedAbility ability)
        {
            try
            {
                // Get hit buffer from pool
                var hitBuffer = GetHitBuffer();
                
                // Perform hit detection
                PerformHitDetection(ability, hitBuffer);
                
                // Apply effects to targets
                ApplyAbilityEffects(ability, hitBuffer);
                
                // Create visual effects
                CreateAbilityEffects(ability);
                
                // Network synchronization (commented out until method is implemented)
                // if (networkBridge != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                // {
                //     networkBridge.SynchronizeAbilityExecution(abilityIndex, hitBuffer);
                // }
                
                // Return buffer to pool
                ReturnHitBuffer(hitBuffer);
                
                // Trigger completion event
                OnAbilityExecuted?.Invoke(abilityIndex, true);
                
                if (logExecutionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AbilityUse),
                        "Ability effect executed.",
                        ("AbilityIndex", abilityIndex),
                        ("TargetsHit", hitBuffer.Count));
                }
            }
            catch (System.Exception ex)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.AbilityUse),
                    "Error executing ability effect.",
                    ("AbilityIndex", abilityIndex),
                    ("Error", ex.Message));
                
                OnAbilityExecuted?.Invoke(abilityIndex, false);
            }
        }
        
        #endregion
        
        #region Hit Detection
        
        /// <summary>
        /// Performs hit detection for the ability
        /// </summary>
        /// <param name="ability">Ability data</param>
        /// <param name="hitBuffer">Buffer to store hit results</param>
        private void PerformHitDetection(EnhancedAbility ability, List<AbilityHitResult> hitBuffer)
        {
            // Simple sphere-based hit detection
            Vector3 center = transform.position;
            float range = ability.range;
            
            // Use Physics.OverlapSphereNonAlloc for performance
            int hitCount = Physics.OverlapSphereNonAlloc(center, range, overlapBuffer);
            
            for (int i = 0; i < hitCount && hitBuffer.Count < ability.maxTargets; i++)
            {
                var collider = overlapBuffer[i];
                
                // Skip self
                if (collider.transform == transform)
                {
                    continue;
                }
                
                // Check target type compatibility
                if (!IsValidTarget(ability, collider))
                {
                    continue;
                }
                
                // Calculate damage (with critical chance)
                float damage = CalculateAbilityDamage(ability);
                
                // Add hit result
                hitBuffer.Add(new AbilityHitResult
                {
                    Target = collider.transform,
                    Damage = damage,
                    Position = collider.transform.position,
                    IsCritical = damage > ability.damage
                });
            }
        }
        
        /// <summary>
        /// Checks if a target is valid for the ability
        /// </summary>
        /// <param name="ability">Ability data</param>
        /// <param name="targetCollider">Target collider</param>
        /// <returns>True if target is valid</returns>
        private bool IsValidTarget(EnhancedAbility ability, Collider targetCollider)
        {
            // Simple target validation - can be extended with team checking, etc.
            switch (ability.targetType)
            {
                case TargetType.Enemy:
                    // Check for enemy tag or component
                    return targetCollider.CompareTag("Enemy");
                    
                case TargetType.Ally:
                    // Check for ally tag or component
                    return targetCollider.CompareTag("Ally");
                    
                case TargetType.Self:
                    // Only target self
                    return targetCollider.transform == transform;
                    
                case TargetType.All:
                    // Target anything with a valid component
                    return targetCollider.GetComponent<IDamageable>() != null;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Calculates damage for an ability including critical hits
        /// </summary>
        /// <param name="ability">Ability data</param>
        /// <returns>Calculated damage amount</returns>
        private float CalculateAbilityDamage(EnhancedAbility ability)
        {
            float baseDamage = ability.damage;
            
            // Check for critical hit
            if (ability.canCritical && Random.value < ability.criticalChance)
            {
                return baseDamage * ability.criticalMultiplier;
            }
            
            return baseDamage;
        }
        
        #endregion
        
        #region Effect Application
        
        /// <summary>
        /// Applies ability effects to all hit targets
        /// </summary>
        /// <param name="ability">Ability data</param>
        /// <param name="hitBuffer">List of hit results</param>
        private void ApplyAbilityEffects(EnhancedAbility ability, List<AbilityHitResult> hitBuffer)
        {
            foreach (var hitResult in hitBuffer)
            {
                ApplyEffectToTarget(ability, hitResult);
            }
        }
        
        /// <summary>
        /// Applies ability effect to a single target
        /// </summary>
        /// <param name="ability">Ability data</param>
        /// <param name="hitResult">Hit result data</param>
        private void ApplyEffectToTarget(EnhancedAbility ability, AbilityHitResult hitResult)
        {
            // Apply damage or healing based on ability type
            switch (ability.abilityType)
            {
                case AbilityType.Instant:
                case AbilityType.Area:
                case AbilityType.Projectile:
                    ApplyDamage(hitResult);
                    break;
                    
                case AbilityType.Heal:
                    ApplyHealing(hitResult);
                    break;
                    
                case AbilityType.Buff:
                    ApplyBuff(hitResult);
                    break;
            }
            
            // Create hit effect
            CreateHitEffect(hitResult.Position);
            
            // Update combat state
            if (hitResult.Damage > 0f)
            {
                combatManager?.OnDamageDealt(hitResult.Damage);
            }
        }
        
        /// <summary>
        /// Applies damage to a target
        /// </summary>
        /// <param name="hitResult">Hit result data</param>
        private void ApplyDamage(AbilityHitResult hitResult)
        {
            var damageable = hitResult.Target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(hitResult.Damage);
                
                if (logExecutionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AbilityUse),
                        "Damage applied to target.",
                        ("Target", hitResult.Target.name),
                        ("Damage", hitResult.Damage),
                        ("Critical", hitResult.IsCritical));
                }
            }
        }
        
        /// <summary>
        /// Applies healing to a target
        /// </summary>
        /// <param name="hitResult">Hit result data</param>
        private void ApplyHealing(AbilityHitResult hitResult)
        {
            var healable = hitResult.Target.GetComponent<IHealable>();
            if (healable != null)
            {
                healable.Heal(hitResult.Damage); // Reuse damage field for heal amount
                
                // Update combat state for healing others
                bool isTargetSelf = hitResult.Target == transform;
                combatManager?.OnHealing(hitResult.Damage, isTargetSelf);
                
                if (logExecutionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AbilityUse),
                        "Healing applied to target.",
                        ("Target", hitResult.Target.name),
                        ("HealAmount", hitResult.Damage));
                }
            }
        }
        
        /// <summary>
        /// Applies buff effects to a target
        /// </summary>
        /// <param name="hitResult">Hit result data</param>
        private void ApplyBuff(AbilityHitResult hitResult)
        {
            var buffable = hitResult.Target.GetComponent<IBuffable>();
            if (buffable != null)
            {
                // Apply generic buff - can be extended with specific buff types
                buffable.ApplyBuff("AbilityBuff", hitResult.Damage, 10f); // 10 second duration
                
                if (logExecutionEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AbilityUse),
                        "Buff applied to target.",
                        ("Target", hitResult.Target.name),
                        ("BuffValue", hitResult.Damage));
                }
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        /// <summary>
        /// Creates visual effects for ability casting
        /// </summary>
        /// <param name="ability">Ability data</param>
        private void CreateAbilityEffects(EnhancedAbility ability)
        {
            if (ability.enableParticleEffect)
            {
                CreateCastEffect(ability);
            }
            
            // Screen shake effect (commented out until CameraShake component is implemented)
            // if (ability.enableScreenShake)
            // {
            //     Camera.main?.GetComponent<CameraShake>()?.Shake(ability.screenShakeIntensity, 0.2f);
            // }
            
            // Audio effects
            if (ability.castSound != null)
            {
                AudioSource.PlayClipAtPoint(ability.castSound, transform.position, ability.volume);
            }
        }
        
        /// <summary>
        /// Creates cast effect for ability
        /// </summary>
        /// <param name="ability">Ability data</param>
        private void CreateCastEffect(EnhancedAbility ability)
        {
            EffectPoolService.SpawnSphereBurst(
                transform.position,
                ability.effectColor,
                ability.range * 0.5f,
                5,
                0.75f,
                0.1f,
                null);
        }
        
        /// <summary>
        /// Creates hit effect at position
        /// </summary>
        /// <param name="position">Effect position</param>
        private void CreateHitEffect(Vector3 position)
        {
            EffectPoolService.SpawnSphereEffect(position, Color.red, 0.3f, 0.4f, null);
        }
        
        #endregion
        
        #region Buffer Pool Management
        
        /// <summary>
        /// Gets a hit buffer from the pool
        /// </summary>
        /// <returns>Hit result buffer</returns>
        private List<AbilityHitResult> GetHitBuffer()
        {
            if (hitBufferPool.Count > 0)
            {
                var buffer = hitBufferPool.Pop();
                buffer.Clear();
                return buffer;
            }
            
            return new List<AbilityHitResult>(8);
        }
        
        /// <summary>
        /// Returns a hit buffer to the pool
        /// </summary>
        /// <param name="buffer">Buffer to return</param>
        private void ReturnHitBuffer(List<AbilityHitResult> buffer)
        {
            if (buffer == null)
            {
                return;
            }
            
            buffer.Clear();
            
            // Clamp capacity to avoid unbounded growth
            if (buffer.Capacity > 128)
            {
                buffer.Capacity = 128;
            }
            
            hitBufferPool.Push(buffer);
        }
        
        #endregion
        
        #region Error Handling
        
        /// <summary>
        /// Handles ability execution failures
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="failureCode">Reason for failure</param>
        private void HandleAbilityFailure(int abilityIndex, AbilityFailureCode failureCode)
        {
            OnAbilityFailed?.Invoke(abilityIndex, failureCode);
            
            if (logExecutionEvents)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.AbilityUse),
                    "Ability execution failed.",
                    ("AbilityIndex", abilityIndex),
                    ("FailureCode", failureCode));
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for execution logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                tag,
                subsystem: nameof(AbilityExecutionManager),
                actor: gameObject?.name);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Result data for ability hit detection
    /// </summary>
    [System.Serializable]
    public struct AbilityHitResult
    {
        public Transform Target;
        public float Damage;
        public Vector3 Position;
        public bool IsCritical;
    }
    
    /// <summary>
    /// Failure codes for ability execution
    /// </summary>
    public enum AbilityFailureCode
    {
        None,
        InvalidIndex,
        AbilityNotFound,
        OnCooldown,
        InsufficientMana,
        AbilityLocked,
        InvalidTarget,
        OutOfRange,
        NetworkError
    }
    
    /// <summary>
    /// Interface for objects that can take damage
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
    
    /// <summary>
    /// Interface for objects that can be healed
    /// </summary>
    public interface IHealable
    {
        void Heal(float amount);
    }
    
    /// <summary>
    /// Interface for objects that can receive buffs
    /// </summary>
    public interface IBuffable
    {
        void ApplyBuff(string buffType, float value, float duration);
    }
    
    #endregion
}