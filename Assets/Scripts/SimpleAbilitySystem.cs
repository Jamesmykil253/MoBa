using UnityEngine;
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
        
        [Header("Settings")]
        public float globalCooldown = 0.1f;
        
        private float lastCastTime;
        private Dictionary<int, float> cooldowns = new Dictionary<int, float>();
        
        void Start()
        {
            // Initialize cooldowns
            for (int i = 0; i < abilities.Length; i++)
            {
                cooldowns[i] = 0f;
            }
        }
        
        void Update()
        {
            UpdateCooldowns();
            HandleInput();
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
        
        void HandleInput()
        {
            // Q, W, E, R for abilities
            if (Input.GetKeyDown(KeyCode.Q)) TryCastAbility(0);
            if (Input.GetKeyDown(KeyCode.W)) TryCastAbility(1);
            if (Input.GetKeyDown(KeyCode.E)) TryCastAbility(2);
            if (Input.GetKeyDown(KeyCode.R)) TryCastAbility(3);
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
    }
}
