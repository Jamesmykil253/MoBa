using UnityEngine;
// ...existing code...
using System.Collections.Generic;
using System.Collections;
using MOBA;

namespace MOBA.Abilities
{
    /// <summary>
    /// Enhanced ability system with resource management, effects, and proper state tracking
    /// Replaces SimpleAbilitySystem with production-ready implementation
    /// </summary>
    public class EnhancedAbilitySystem : MonoBehaviour
    {
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
        
        // Runtime state
        private float lastCastTime;
        private float lastCombatTime;
        private bool isInCombat = false;
        private Dictionary<int, float> cooldowns = new Dictionary<int, float>();
        private Dictionary<int, bool> abilityLocked = new Dictionary<int, bool>(); // For channeling/casting
        
        // Stats modifiers
        private float currentCooldownReduction = 0f;
        
        // Events
        public System.Action<int> OnAbilityCast; // ability index
        public System.Action<float, float> OnManaChanged; // current, max
        public System.Action<int, float> OnCooldownUpdate; // ability index, remaining time
        public System.Action<bool> OnCombatStateChanged; // in combat
        
        // Properties
        public float CurrentMana => currentMana;
        public float MaxMana => maxMana;
        public float ManaPercentage => maxMana > 0 ? currentMana / maxMana : 0f;
        public bool IsInCombat => isInCombat;
        public float CooldownReduction => currentCooldownReduction;
        
        #region Unity Lifecycle
        
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
            HandleInput();
        }
        
        void OnDestroy()
        {
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
            var keys = new List<int>(cooldowns.Keys);
            foreach (var key in keys)
            {
                if (cooldowns[key] > 0)
                {
                    float previousCooldown = cooldowns[key];
                    cooldowns[key] = Mathf.Max(0f, cooldowns[key] - Time.deltaTime);
                    
                    if (cooldowns[key] != previousCooldown)
                    {
                        OnCooldownUpdate?.Invoke(key, cooldowns[key]);
                    }
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
        
        #region Input Handling
        
        private void HandleInput()
        {
            // Q, W, E, R for abilities
            if (Input.GetKeyDown(KeyCode.Q)) TryCastAbility(0);
            if (Input.GetKeyDown(KeyCode.W)) TryCastAbility(1);
            if (Input.GetKeyDown(KeyCode.E)) TryCastAbility(2);
            if (Input.GetKeyDown(KeyCode.R)) TryCastAbility(3);
        }
        
        #endregion
        
        #region Ability Casting
        
        public bool TryCastAbility(int abilityIndex)
        {
            if (!CanCastAbility(abilityIndex)) return false;
            
            var ability = abilities[abilityIndex];
            return StartCoroutine(CastAbilityCoroutine(abilityIndex, ability)) != null;
        }
        
        public bool CanCastAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return false;
            if (abilities[abilityIndex] == null) return false;
            
            var ability = abilities[abilityIndex];
            
            // Check global cooldown
            if (Time.time - lastCastTime < globalCooldown) return false;
            
            // Check ability cooldown
            if (cooldowns[abilityIndex] > 0) return false;
            
            // Check mana cost
            if (!HasMana(ability.manaCost)) return false;
            
            // Check if ability is locked (channeling)
            if (abilityLocked[abilityIndex]) return false;
            
            return true;
        }
        
        private IEnumerator CastAbilityCoroutine(int abilityIndex, EnhancedAbility ability)
        {
            // Lock the ability
            abilityLocked[abilityIndex] = true;
            
            // Consume mana
            if (!ConsumeMana(ability.manaCost))
            {
                abilityLocked[abilityIndex] = false;
                yield break;
            }
            
            // Enter combat
            EnterCombat();
            
            // Cast time delay
            if (ability.castTime > 0)
            {
                yield return new WaitForSeconds(ability.castTime);
            }
            
            // Execute ability effect
            ExecuteAbilityEffect(abilityIndex, ability);
            
            // Set cooldown
            float modifiedCooldown = GetModifiedCooldown(ability.cooldown);
            cooldowns[abilityIndex] = modifiedCooldown;
            lastCastTime = Time.time;
            
            // Unlock ability
            abilityLocked[abilityIndex] = false;
            
            // Trigger events
            OnAbilityCast?.Invoke(abilityIndex);
            OnCooldownUpdate?.Invoke(abilityIndex, cooldowns[abilityIndex]);
            
            Debug.Log($"[EnhancedAbilitySystem] Cast {ability.abilityName} - Damage: {ability.damage}, Range: {ability.range}, Mana: {currentMana}/{maxMana}");
        }
        
        private void ExecuteAbilityEffect(int abilityIndex, EnhancedAbility ability)
        {
            // Apply damage to enemies in range
            Collider[] targets = Physics.OverlapSphere(transform.position, ability.range);
            int hitCount = 0;
            
            foreach (var target in targets)
            {
                if (target.CompareTag("Enemy") && target != this.GetComponent<Collider>())
                {
                    var damageable = target.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(ability.damage);
                        hitCount++;
                        
                        // Create hit effect
                        CreateHitEffect(target.transform.position);
                        
                        // Break if we hit max targets
                        if (hitCount >= ability.maxTargets)
                            break;
                    }
                }
            }
            
            // Create cast effect
            CreateCastEffect(ability);
            
            // Publish ability used event
            UnifiedEventSystem.PublishLocal(new AbilityUsedEvent(
                gameObject, ability.abilityName, ability.manaCost, ability.cooldown, 
                transform.position, null));
        }
        
        #endregion
        
        #region Effects
        
        private void CreateCastEffect(EnhancedAbility ability)
        {
            // Simple visual effect for ability casting
            if (ability.enableParticleEffect)
            {
                // Create a simple particle burst
                for (int i = 0; i < 5; i++)
                {
                    GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    particle.transform.position = transform.position + Random.insideUnitSphere * ability.range * 0.5f;
                    particle.transform.localScale = Vector3.one * 0.1f;
                    
                    var renderer = particle.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = ability.effectColor;
                    }
                    
                    // Add movement
                    var rb = particle.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(Random.insideUnitSphere * 100f);
                    }
                    
                    Destroy(particle, 1f);
                }
            }
        }
        
        private void CreateHitEffect(Vector3 position)
        {
            // Simple hit effect
            GameObject hitEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitEffect.transform.position = position;
            hitEffect.transform.localScale = Vector3.one * 0.3f;
            
            var renderer = hitEffect.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
            
            Destroy(hitEffect, 0.5f);
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
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return null;
            return abilities[abilityIndex];
        }
        
        public void SetAbility(int abilityIndex, EnhancedAbility ability)
        {
            if (abilityIndex < 0 || abilityIndex >= abilities.Length) return;
            abilities[abilityIndex] = ability;
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
        
        #endregion
        
        #region Debug
        
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUI.Label(new Rect(10, 10, 300, 20), $"Mana: {currentMana:F1}/{maxMana:F1} ({ManaPercentage:P0})");
            GUI.Label(new Rect(10, 30, 300, 20), $"Combat: {(isInCombat ? "Yes" : "No")} | CDR: {currentCooldownReduction:P0}");
            
            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] != null)
                {
                    string status = IsAbilityReady(i) ? "Ready" : $"CD: {GetCooldownRemaining(i):F1}s";
                    GUI.Label(new Rect(10, 50 + i * 20, 300, 20), 
                        $"{i + 1}. {abilities[i].abilityName} - {status}");
                }
            }
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
