using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Debugging;
using MOBA.Effects;
using MOBA.Networking;

namespace MOBA.Abilities
{
    /// <summary>
    /// Enhanced ability system with resource management, effects, and proper state tracking
    /// Replaces SimpleAbilitySystem with production-ready implementation
    /// </summary>
    public class EnhancedAbilitySystem : MonoBehaviour
    {
        // Static buffer for non-alloc overlap sphere
        private static Collider[] overlapBuffer = new Collider[32];
        [Header("Abilities")]
        [SerializeField] private EnhancedAbility[] abilities = new EnhancedAbility[4];
        
        [Header("Resource System")]
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float currentMana = 100f;
        [SerializeField] private float manaRegenPerSecond = 5f;
        [SerializeField] private float outOfCombatManaMultiplier = 2f;
        
        [Header("Global Settings")]
        [SerializeField] private float globalCooldown = 0.1f;
        [SerializeField] private bool enableCooldownReduction = true;
        [SerializeField] private float maxCooldownReduction = 0.4f; // 40% max CDR

        [Header("Combat State")]
        [SerializeField] private float combatDuration = 5f; // Time to stay in combat after last action

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string[] abilityActionNames = new[] { "Ability1", "Ability2", "Ability3", "Ability4" };
        [SerializeField, Tooltip("When enabled the system binds directly to the Input Action Asset. Disable if a legacy handler (e.g. SimpleInputHandler) should own input wiring.")]
        private bool enableInternalInputActions = true;

        // Runtime state
        private float lastCastTime;
        private float lastCombatTime;
        private bool isInCombat = false;
        private readonly Dictionary<int, float> cooldowns = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> abilityLocked = new Dictionary<int, bool>(); // For channeling/casting
        private readonly List<int> cooldownKeyCache = new List<int>(8);
        private AbilityNetworkController networkBridge;

        // Hit result buffers are pooled to avoid per-cast allocations.
        private readonly Stack<List<AbilityHitResult>> serverHitBufferPool = new Stack<List<AbilityHitResult>>();

        // Stats modifiers
        private float currentCooldownReduction = 0f;

        // Input state
        private InputAction[] abilityInputActions;
        private System.Action<InputAction.CallbackContext>[] abilityActionHandlers;
        private bool inputActionsInitialized;
        private bool inputEnabled = true;
        
    // Events
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

        // Properties
        public float CurrentMana => currentMana;
        public float MaxMana => maxMana;
        public float ManaPercentage => maxMana > 0 ? currentMana / maxMana : 0f;
        public bool IsInCombat => isInCombat;
        public float CooldownReduction => currentCooldownReduction;

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                mechanic,
                subsystem: nameof(EnhancedAbilitySystem),
                actor: gameObject != null ? gameObject.name : null);
        }
        
        internal void AttachNetworkBridge(AbilityNetworkController bridge)
        {
            networkBridge = bridge;
        }

        internal void DetachNetworkBridge(AbilityNetworkController bridge)
        {
            if (networkBridge == bridge)
            {
                networkBridge = null;
            }
        }
        
        #region Unity Lifecycle
        
        void Awake()
        {
            DetectLegacyInputHandler();
            InitializeInputActions();
            if (networkBridge == null)
            {
                networkBridge = GetComponent<AbilityNetworkController>();
            }
        }

        void OnEnable()
        {
            ApplyInputActionState(inputEnabled);
        }

        void Start()
        {
            // Initialize cooldowns and locks
            for (int i = 0; i < abilities.Length; i++)
            {
                cooldowns[i] = 0f;
                abilityLocked[i] = false;
            }
            
            currentMana = maxMana;
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
        
        void Update()
        {
            UpdateCooldowns();
            UpdateManaRegeneration();
            UpdateCombatState();
        }
        
        void OnDisable()
        {
            ApplyInputActionState(false);
        }

        void OnDestroy()
        {
            CleanupInputActions();
            // Clean up events
            OnAbilityCast = null;
            OnManaChanged = null;
            OnCooldownUpdate = null;
            OnCombatStateChanged = null;
        }
        
        #endregion
        
        #region Resource Management
        
        private void UpdateManaRegeneration()
        {
            if (currentMana >= maxMana) return;
            
            float regenRate = manaRegenPerSecond;
            if (!isInCombat)
            {
                regenRate *= outOfCombatManaMultiplier;
            }
            
            float previousMana = currentMana;
            currentMana = Mathf.Min(maxMana, currentMana + regenRate * Time.deltaTime);
            
            if (currentMana != previousMana)
            {
                OnManaChanged?.Invoke(currentMana, maxMana);
            }
        }
        
        public bool HasMana(float cost)
        {
            return currentMana >= cost;
        }
        
        public bool ConsumeMana(float cost)
        {
            if (!HasMana(cost)) return false;
            
            currentMana = Mathf.Max(0f, currentMana - cost);
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }
        
        public void RestoreMana(float amount)
        {
            float previousMana = currentMana;
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            
            if (currentMana != previousMana)
            {
                OnManaChanged?.Invoke(currentMana, maxMana);
            }
        }
        
        #endregion
        
        #region Cooldown Management
        
        private void UpdateCooldowns()
        {
            if (cooldowns.Count == 0)
            {
                return;
            }

            cooldownKeyCache.Clear();
            cooldownKeyCache.AddRange(cooldowns.Keys);

            float deltaTime = Time.deltaTime;

            for (int i = 0; i < cooldownKeyCache.Count; i++)
            {
                int key = cooldownKeyCache[i];
                if (!cooldowns.TryGetValue(key, out float remaining) || remaining <= 0f)
                {
                    continue;
                }

                float updated = Mathf.Max(0f, remaining - deltaTime);
                if (!Mathf.Approximately(updated, remaining))
                {
                    cooldowns[key] = updated;
                    OnCooldownUpdate?.Invoke(key, updated);
                }
            }
        }
        
        private float GetModifiedCooldown(float baseCooldown)
        {
            if (!enableCooldownReduction) return baseCooldown;
            
            float reduction = Mathf.Clamp(currentCooldownReduction, 0f, maxCooldownReduction);
            return baseCooldown * (1f - reduction);
        }
        
        public void SetCooldownReduction(float reduction)
        {
            currentCooldownReduction = Mathf.Clamp(reduction, 0f, maxCooldownReduction);
        }
        
        #endregion
        
        #region Combat State
        
        private void UpdateCombatState()
        {
            bool wasInCombat = isInCombat;
            isInCombat = Time.time - lastCombatTime < combatDuration;
            
            if (wasInCombat != isInCombat)
            {
                OnCombatStateChanged?.Invoke(isInCombat);
            }
        }
        
        private void EnterCombat()
        {
            lastCombatTime = Time.time;
            if (!isInCombat)
            {
                isInCombat = true;
                OnCombatStateChanged?.Invoke(true);
            }
        }
        
        #endregion
        
        #region Ability Casting
        
        public bool TryCastAbility(int abilityIndex)
        {
            Vector3 targetPosition = transform.position;
            Vector3 targetDirection = transform.forward;

            var precheck = ValidateAbilityCast(abilityIndex);
            if (networkBridge != null && networkBridge.IsSpawned)
            {
                if (precheck != AbilityFailureCode.None && !networkBridge.HasNetworkAuthority)
                {
                    HandleLocalAbilityFailure(abilityIndex, precheck);
                    return false;
                }

                return networkBridge.RequestAbilityCast(abilityIndex, targetPosition, targetDirection);
            }

            if (precheck != AbilityFailureCode.None)
            {
                HandleLocalAbilityFailure(abilityIndex, precheck);
                return false;
            }

            var request = new AbilityCastRequest
            {
                RequestId = 0,
                AbilityIndex = (ushort)Mathf.Max(0, abilityIndex),
                TargetPosition = targetPosition,
                TargetDirection = targetDirection
            };

            HandleAbilityCastRequest(request, NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0);
            return true;
        }
        
        public bool CanCastAbility(int abilityIndex)
        {
            return ValidateAbilityCast(abilityIndex) == AbilityFailureCode.None;
        }

        private AbilityFailureCode ValidateAbilityCast(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length)
            {
                return AbilityFailureCode.InvalidAbility;
            }

            var ability = abilities[abilityIndex];
            if (ability == null)
            {
                return AbilityFailureCode.InvalidAbility;
            }

            if (abilityLocked.TryGetValue(abilityIndex, out bool locked) && locked)
            {
                return AbilityFailureCode.AbilityLocked;
            }

            if (cooldowns.TryGetValue(abilityIndex, out float cooldownRemaining) && cooldownRemaining > 0f)
            {
                return AbilityFailureCode.OnCooldown;
            }

            if (Time.time - lastCastTime < globalCooldown)
            {
                return AbilityFailureCode.GlobalCooldown;
            }

            if (!HasMana(ability.manaCost))
            {
                return AbilityFailureCode.InsufficientMana;
            }

            return AbilityFailureCode.None;
        }

        private void HandleLocalAbilityFailure(int abilityIndex, AbilityFailureCode failureCode)
        {
            if (failureCode == AbilityFailureCode.None)
            {
                return;
            }

            if (abilityIndex < 0 || abilityIndex >= abilities.Length)
            {
                return;
            }

            var ability = abilities[abilityIndex];
            if (ability == null)
            {
                return;
            }

            switch (failureCode)
            {
                case AbilityFailureCode.InsufficientMana:
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Resource),
                        "Insufficient mana to cast ability.",
                        ("Ability", ability.abilityName),
                        ("Required", ability.manaCost),
                        ("Current", currentMana));
                    break;
                case AbilityFailureCode.OnCooldown:
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Ability on cooldown.",
                        ("Ability", ability.abilityName));
                    break;
                case AbilityFailureCode.GlobalCooldown:
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Global cooldown active.",
                        ("Ability", ability.abilityName));
                    break;
                case AbilityFailureCode.AbilityLocked:
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Cooldown),
                        "Ability is currently channeling.",
                        ("Ability", ability.abilityName));
                    break;
                default:
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Validation),
                        "Ability cast rejected.",
                        ("Ability", ability.abilityName),
                        ("Reason", failureCode));
                    break;
            }
        }

        internal void HandleAbilityCastRequest(AbilityCastRequest request, ulong senderClientId)
        {
            int abilityIndex = request.AbilityIndex;
            var failure = ValidateAbilityCast(abilityIndex);

            if (failure != AbilityFailureCode.None)
            {
                var failureResult = AbilityCastResult.CreateFailure(request.RequestId, abilityIndex, failure, currentMana);
                if (networkBridge != null && networkBridge.HasNetworkAuthority)
                {
                    networkBridge.NotifyAbilityCastResult(failureResult);
                }
                else
                {
                    ApplyAbilityResult(failureResult, true);
                }
                return;
            }

            var ability = abilities[abilityIndex];
            StartCoroutine(ServerCastAbilityCoroutine(request, ability, senderClientId));
        }

        private IEnumerator ServerCastAbilityCoroutine(AbilityCastRequest request, EnhancedAbility ability, ulong senderClientId)
        {
            int abilityIndex = request.AbilityIndex;
            abilityLocked[abilityIndex] = true;

            if (!ConsumeMana(ability.manaCost))
            {
                abilityLocked[abilityIndex] = false;
                var failureResult = AbilityCastResult.CreateFailure(request.RequestId, abilityIndex, AbilityFailureCode.InsufficientMana, currentMana);
                if (networkBridge != null && networkBridge.HasNetworkAuthority)
                {
                    networkBridge.NotifyAbilityCastResult(failureResult);
                }
                else
                {
                    ApplyAbilityResult(failureResult, true);
                }
                yield break;
            }

            EnterCombat();

            if (ability.castTime > 0f)
            {
                yield return new WaitForSeconds(ability.castTime);
            }

            var hitBuffer = RentHitBuffer();
            ExecuteAbilityEffect(abilityIndex, ability, hitBuffer);

            float modifiedCooldown = GetModifiedCooldown(ability.cooldown);
            cooldowns[abilityIndex] = modifiedCooldown;
            lastCastTime = Time.time;
            abilityLocked[abilityIndex] = false;

            OnAbilityCast?.Invoke(abilityIndex);
            OnCooldownUpdate?.Invoke(abilityIndex, cooldowns[abilityIndex]);

            var result = AbilityCastResult.CreateSuccess(request.RequestId, abilityIndex, currentMana, modifiedCooldown, globalCooldown, hitBuffer);
            if (networkBridge != null && networkBridge.HasNetworkAuthority)
            {
                networkBridge.NotifyAbilityCastResult(result);
            }
            else
            {
                ApplyAbilityResult(result, true);
            }

            ReturnHitBuffer(hitBuffer);
        }

        internal void ApplyAbilityResult(AbilityCastResult result, bool isServerAuthority)
        {
            int abilityIndex = result.AbilityIndex;
            var ability = abilityIndex >= 0 && abilityIndex < abilities.Length ? abilities[abilityIndex] : null;

            if (result.Approved)
            {
                currentMana = result.CurrentMana;
                OnManaChanged?.Invoke(currentMana, maxMana);

                cooldowns[abilityIndex] = result.CooldownSeconds;
                if (!isServerAuthority)
                {
                    OnCooldownUpdate?.Invoke(abilityIndex, cooldowns[abilityIndex]);
                }

                lastCastTime = Time.time;

                if (!isServerAuthority)
                {
                    EnterCombat();
                    OnAbilityCast?.Invoke(abilityIndex);

                    if (ability != null)
                    {
                        CreateCastEffect(ability);
                        UnifiedEventSystem.PublishLocal(new AbilityUsedEvent(
                            gameObject, ability.abilityName, ability.manaCost, ability.cooldown,
                            transform.position, null));
                    }
                }
            }
            else if (!isServerAuthority)
            {
                HandleLocalAbilityFailure(abilityIndex, result.FailureCode);
            }
        }

        private void ExecuteAbilityEffect(int abilityIndex, EnhancedAbility ability, List<AbilityHitResult> hitBuffer = null)
        {
            // Apply damage to enemies in range
            int numTargets = Physics.OverlapSphereNonAlloc(transform.position, ability.range, overlapBuffer);
            int hitCount = 0;
            for (int i = 0; i < numTargets; i++)
            {
                var target = overlapBuffer[i];
                if (target.CompareTag("Enemy") && target != this.GetComponent<Collider>())
                {
                    var damageable = target.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(ability.damage);
                        if (hitBuffer != null)
                        {
                            var networkObject = target.GetComponent<NetworkObject>();
                            if (networkObject != null)
                            {
                                hitBuffer.Add(new AbilityHitResult
                                {
                                    TargetNetworkId = networkObject.NetworkObjectId,
                                    AppliedDamage = ability.damage,
                                    RemainingHealth = damageable.GetHealth(),
                                    IsDead = damageable.IsDead()
                                });
                            }
                        }
                        hitCount++;
                        // Create hit effect
                        CreateHitEffect(target.transform.position);
                        if (hitCount >= ability.maxTargets)
                            break;
                    }
                }
            }
            
            // Create cast effect
            CreateCastEffect(ability);
            UnifiedEventSystem.PublishLocal(new AbilityUsedEvent(
                gameObject, ability.abilityName, ability.manaCost, ability.cooldown,
                transform.position, null));
        }

        #endregion

        private List<AbilityHitResult> RentHitBuffer()
        {
            if (serverHitBufferPool.Count > 0)
            {
                var buffer = serverHitBufferPool.Pop();
                buffer.Clear();
                return buffer;
            }

            return new List<AbilityHitResult>(8);
        }

        private void ReturnHitBuffer(List<AbilityHitResult> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();

            // Clamp capacity to avoid unbounded growth if a large hit list occurs once.
            if (buffer.Capacity > 128)
            {
                buffer.Capacity = 128;
            }

            serverHitBufferPool.Push(buffer);
        }

        #region Effects
        
        private void CreateCastEffect(EnhancedAbility ability)
        {
            // Simple visual effect for ability casting
            if (ability.enableParticleEffect)
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
        }

        private void CreateHitEffect(Vector3 position)
        {
            EffectPoolService.SpawnSphereEffect(position, Color.red, 0.3f, 0.4f, null);
        }
        
        #endregion
        
        #region Public Interface
        
        public bool IsAbilityReady(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return false;
            return cooldowns[abilityIndex] <= 0 && !abilityLocked[abilityIndex];
        }
        
        public float GetCooldownRemaining(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return 0f;
            return Mathf.Max(0f, cooldowns[abilityIndex]);
        }
        
        public EnhancedAbility GetAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "GetAbility called with invalid index.",
                    ("Index", abilityIndex));
                return null;
            }
            return abilities[abilityIndex];
        }

        public void SetAbility(int abilityIndex, EnhancedAbility ability)
        {
            if (abilityIndex < 0)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "SetAbility called with negative index.",
                    ("Index", abilityIndex));
                return;
            }
            // Dynamically resize array if needed
            bool resized = false;

            if (abilityIndex >= abilities.Length)
            {
                int oldLen = abilities.Length;
                System.Array.Resize(ref abilities, abilityIndex + 1);
                for (int i = oldLen; i < abilities.Length; i++)
                {
                    abilities[i] = null;
                    cooldowns[i] = 0f;
                    abilityLocked[i] = false;
                }
                resized = true;
            }
            abilities[abilityIndex] = ability;

            if (resized)
            {
                HandleAbilityLayoutChanged();
            }
        }

        public void RemoveAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "RemoveAbility called with invalid index.",
                    ("Index", abilityIndex));
                return;
            }
            abilities[abilityIndex] = null;
            cooldowns[abilityIndex] = 0f;
            abilityLocked[abilityIndex] = false;
            HandleAbilityLayoutChanged();
        }

        public int AddAbility(EnhancedAbility ability)
        {
            // Find first empty slot or expand
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == null)
                {
                    abilities[i] = ability;
                    return i;
                }
            }
            // Expand array
            int newIndex = abilities.Length;
            System.Array.Resize(ref abilities, newIndex + 1);
            abilities[newIndex] = ability;
            cooldowns[newIndex] = 0f;
            abilityLocked[newIndex] = false;
            HandleAbilityLayoutChanged();
            return newIndex;
        }
        
        public void ResetAllCooldowns()
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                cooldowns[i] = 0f;
                OnCooldownUpdate?.Invoke(i, 0f);
            }
        }

        public void SetMaxMana(float newMaxMana)
        {
            float percentage = ManaPercentage;
            maxMana = newMaxMana;
            currentMana = maxMana * percentage;
            OnManaChanged?.Invoke(currentMana, maxMana);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!isActiveAndEnabled) return;
            ApplyInputActionState(enabled);
        }

        #endregion

        #region Input Integration

        private void InitializeInputActions()
        {
            if (!enableInternalInputActions)
            {
                return;
            }

            if (abilities == null)
            {
                abilities = new EnhancedAbility[4];
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

            if (inputActionsInitialized)
            {
                HandleAbilityLayoutChanged();
                return;
            }

            inputActionsInitialized = true;
            RebuildAbilityInputBindings();
        }

        private void ApplyInputActionState(bool enable)
        {
            if (!enableInternalInputActions || abilityInputActions == null)
            {
                return;
            }

            foreach (var action in abilityInputActions)
            {
                if (action == null) continue;
                if (enable)
                {
                    if (!action.enabled)
                    {
                        action.Enable();
                    }
                }
                else if (action.enabled)
                {
                    action.Disable();
                }
            }
        }

        private void CleanupInputActions()
        {
            if (!enableInternalInputActions)
            {
                abilityInputActions = null;
                abilityActionHandlers = null;
                return;
            }

            if (abilityInputActions != null && abilityActionHandlers != null)
            {
                int length = Mathf.Min(abilityInputActions.Length, abilityActionHandlers.Length);
                for (int i = 0; i < length; i++)
                {
                    if (abilityInputActions[i] != null && abilityActionHandlers[i] != null)
                    {
                        abilityInputActions[i].performed -= abilityActionHandlers[i];
                    }
                }
            }

            abilityInputActions = null;
            abilityActionHandlers = null;
        }

        private void RebuildAbilityInputBindings()
        {
            if (!enableInternalInputActions)
            {
                return;
            }

            CleanupInputActions();

            if (abilities == null)
            {
                abilities = new EnhancedAbility[4];
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

            int abilityCount = abilities.Length;
            abilityInputActions = new InputAction[abilityCount];
            abilityActionHandlers = new System.Action<InputAction.CallbackContext>[abilityCount];

            if (inputActions == null)
            {
                return;
            }

            for (int i = 0; i < abilityCount; i++)
            {
                if (abilityActionNames == null || i >= abilityActionNames.Length)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Input),
                        "No input action configured for ability slot.",
                        ("Slot", i));
                    continue;
                }

                string actionName = abilityActionNames[i];

                if (string.IsNullOrEmpty(actionName))
                {
                    continue;
                }

                var action = inputActions.FindAction(actionName, throwIfNotFound: false);
                if (action == null)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Input),
                        "Input action not found for enhanced ability binding.",
                        ("Action", actionName));
                    continue;
                }

                abilityInputActions[i] = action;
                int abilityIndex = i;
                abilityActionHandlers[i] = ctx =>
                {
                    if (!inputEnabled || !isActiveAndEnabled)
                    {
                        return;
                    }
                    TryCastAbility(abilityIndex);
                };
                action.performed += abilityActionHandlers[i];
            }

            ApplyInputActionState(inputEnabled);
        }

        private void HandleAbilityLayoutChanged()
        {
            if (!enableInternalInputActions || !inputActionsInitialized)
            {
                return;
            }

            if (abilityInputActions != null && abilityInputActions.Length == abilities.Length)
            {
                return;
            }

            RebuildAbilityInputBindings();
        }

        #endregion

        private void DetectLegacyInputHandler()
        {
            if (!enableInternalInputActions)
            {
                return;
            }

            if (TryGetComponent<SimpleInputHandler>(out _))
            {
                enableInternalInputActions = false;
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Input),
                    "Detected SimpleInputHandler; internal input bindings disabled to avoid duplicate listeners.");
            }
        }

        #region External Combat Triggers

        /// <summary>
        /// Allows external systems (e.g., being attacked) to force the player into combat state.
        /// </summary>
        public void ForceEnterCombat()
        {
            EnterCombat();
        }

        #endregion
    }
    
    /// <summary>
    /// Enhanced ability data with more features than SimpleAbility
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
    
    public enum AbilityType
    {
        Instant,    // Immediate effect
        Channeled,  // Requires channeling
        Projectile, // Fires a projectile
        Area,       // Area of effect
        Buff,       // Applies buff/debuff
        Heal        // Healing ability
    }
    
    public enum TargetType
    {
        Enemy,
        Ally,
        Self,
        All
    }
}
