using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Basic ability system for the MOBA game
    /// Manages ability casting, cooldowns, and effects
    /// Designed for future network integration
    /// Integrated with RSBCombatSystem for standardized damage calculations
    /// </summary>
    public class AbilitySystem : MonoBehaviour
    {
        [Header("Ability Settings")]
        [SerializeField] private float globalCooldown = 0.1f;

        [Header("Combat System Integration")]
        [SerializeField] private RSBCombatSystem rsbCombatSystem;

        // Ability cooldown tracking
        private float lastAbilityCastTime;
        private System.Collections.Generic.Dictionary<string, float> abilityCooldowns = new();

        private void Start()
        {
            // Initialize ability cooldowns
            abilityCooldowns["PrimaryAttack"] = 0f;
            abilityCooldowns["Ability1"] = 0f;
            abilityCooldowns["Ability2"] = 0f;
            abilityCooldowns["Ultimate"] = 0f;
            
            // Cache RSB Combat System for damage calculations
            if (rsbCombatSystem == null)
            {
                rsbCombatSystem = FindAnyObjectByType<RSBCombatSystem>();
                if (rsbCombatSystem == null)
                {
                    Debug.LogWarning("[AbilitySystem] RSBCombatSystem not found - using default damage calculations");
                }
            }
        }

        private void Update()
        {
            // Update cooldowns
            UpdateCooldowns();
        }

        private void UpdateCooldowns()
        {
            var keys = new System.Collections.Generic.List<string>(abilityCooldowns.Keys);
            foreach (var key in keys)
            {
                if (abilityCooldowns[key] > 0)
                {
                    abilityCooldowns[key] -= Time.deltaTime;
                    if (abilityCooldowns[key] < 0)
                    {
                        abilityCooldowns[key] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Casts an ability at the specified target position
        /// Integrated with RSBCombatSystem for standardized damage calculations
        /// </summary>
        public void CastAbility(AbilityData ability, Vector2 targetPosition)
        {
            if (!CanCastAbility(ability)) return;

            // Calculate final damage using RSB combat system
            float finalDamage = ability.damage; // Default fallback
            if (rsbCombatSystem != null)
            {
                Vector3 attackerPos = transform.position;
                Vector3 targetPos = new Vector3(targetPosition.x, targetPosition.y, attackerPos.z);
                finalDamage = rsbCombatSystem.CalculateAbilityDamage(ability, attackerPos, targetPos);
            }

            // Update ability data with calculated damage
            AbilityData enhancedAbility = new AbilityData
            {
                name = ability.name,
                damage = finalDamage,
                range = ability.range,
                speed = ability.speed
            };

            Debug.Log($"Casting {ability.name} at {targetPosition} (Base: {ability.damage:F1} → Final: {finalDamage:F1})");

            // Set cooldown
            if (abilityCooldowns.ContainsKey(ability.name))
            {
                abilityCooldowns[ability.name] = GetAbilityCooldown(ability.name);
            }

            lastAbilityCastTime = Time.time;

            // Spawn ability effect with enhanced damage
            SpawnAbilityEffect(enhancedAbility, targetPosition);

            // Network: Send ability cast to server
            SendAbilityCastToServer(ability.name, targetPosition);
        }

        /// <summary>
        /// Cancels an ability if possible
        /// </summary>
        public void CancelAbility(AbilityData ability)
        {
            Debug.Log($"Cancelling {ability.name}");

            // Network: Send ability cancel to server
            SendAbilityCancelToServer(ability.name);
        }

        /// <summary>
        /// Checks if an ability can be cast
        /// </summary>
        public bool CanCastAbility(AbilityData ability)
        {
            // Check global cooldown
            if (Time.time - lastAbilityCastTime < globalCooldown)
            {
                return false;
            }

            // Check ability-specific cooldown
            if (abilityCooldowns.ContainsKey(ability.name))
            {
                return abilityCooldowns[ability.name] <= 0;
            }

            return true;
        }

        /// <summary>
        /// Gets the remaining cooldown for an ability
        /// </summary>
        public float GetAbilityCooldown(string abilityName)
        {
            switch (abilityName)
            {
                case "PrimaryAttack": return 0.5f;
                case "Ability1": return 8f;
                case "Ability2": return 12f;
                case "Ultimate": return 60f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Gets the remaining cooldown time for an ability
        /// </summary>
        public float GetRemainingCooldown(string abilityName)
        {
            if (abilityCooldowns.ContainsKey(abilityName))
            {
                return abilityCooldowns[abilityName];
            }
            return 0f;
        }

        private void SpawnAbilityEffect(AbilityData ability, Vector2 targetPosition)
        {
            // Use object pool for effects
            var pool = Object.FindAnyObjectByType<ProjectilePool>();
            if (pool != null)
            {
                // Spawn projectile based on ability type
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

                // Try to use flyweight for this ability
                string flyweightName = GetFlyweightNameForAbility(ability.name);
                var availableFlyweights = pool.GetAvailableFlyweightNames();

                if (availableFlyweights.Contains(flyweightName))
                {
                    pool.SpawnProjectileWithFlyweight(transform.position, direction, flyweightName);
                }
                else
                {
                    // Fallback to default projectile
                    pool.SpawnProjectile(transform.position, direction, 10f, ability.damage, 3f);
                }
            }
            else
            {
                // Fallback: create simple effect
                Debug.Log($"Ability {ability.name} effect spawned at {targetPosition}");
            }
        }

        private string GetFlyweightNameForAbility(string abilityName)
        {
            // Map ability names to flyweight names
            switch (abilityName.ToLower())
            {
                case "primaryattack":
                    return "BasicProjectile";
                case "ability1":
                    return "FastProjectile";
                case "ability2":
                    return "HeavyProjectile";
                case "ultimate":
                    return "HomingProjectile";
                default:
                    return "BasicProjectile";
            }
        }

        // Network placeholder methods
        private void SendAbilityCastToServer(string abilityName, Vector2 targetPosition)
        {
            // Placeholder for network implementation
            // This would send the ability cast to the server for validation
        }

        private void SendAbilityCancelToServer(string abilityName)
        {
            // Placeholder for network implementation
            // This would notify the server of ability cancellation
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 200, 300, 20), "Ability System Status:");
            GUI.Label(new Rect(10, 220, 300, 20), $"Global CD: {(Time.time - lastAbilityCastTime < globalCooldown ? "Active" : "Ready")}");

            int y = 240;
            foreach (var kvp in abilityCooldowns)
            {
                GUI.Label(new Rect(10, y, 300, 20), $"{kvp.Key}: {(kvp.Value > 0 ? $"{kvp.Value:F1}s" : "Ready")}");
                y += 20;
            }
        }
    }
}