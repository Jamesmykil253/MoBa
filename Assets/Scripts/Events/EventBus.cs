using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Custom EventBus implementation for damage system notifications
    /// Observer Pattern based on Game Programming Patterns
    /// Enhanced with proper cleanup and memory management
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<object>> subscribers = new();
        private static readonly object lockObject = new object(); // Thread safety

        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null) return;
            
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!subscribers.ContainsKey(eventType))
                {
                    subscribers[eventType] = new List<object>();
                }
                
                // Prevent duplicate subscriptions
                if (!subscribers[eventType].Contains(handler))
                {
                    subscribers[eventType].Add(handler);
                }
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null) return;
            
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (subscribers.ContainsKey(eventType))
                {
                    subscribers[eventType].Remove(handler);
                    
                    // Clean up empty lists to prevent memory leaks
                    if (subscribers[eventType].Count == 0)
                    {
                        subscribers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribe all handlers for a specific event type
        /// </summary>
        public static void UnsubscribeAll<T>() where T : IEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (subscribers.ContainsKey(eventType))
                {
                    subscribers[eventType].Clear();
                    subscribers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Clear all subscriptions - useful for cleanup
        /// </summary>
        public static void ClearAll()
        {
            lock (lockObject)
            {
                foreach (var list in subscribers.Values)
                {
                    list.Clear();
                }
                subscribers.Clear();
            }
        }

        public static void Publish<T>(T eventData) where T : IEvent
        {
            if (eventData == null) return;
            
            List<object> eventSubscribers = null;
            var eventType = typeof(T);
            
            // Copy subscribers list to avoid modification during iteration
            lock (lockObject)
            {
                if (subscribers.ContainsKey(eventType))
                {
                    eventSubscribers = new List<object>(subscribers[eventType]);
                }
            }
            
            if (eventSubscribers != null)
            {
                var deadHandlers = new List<object>();
                
                foreach (var handler in eventSubscribers)
                {
                    try
                    {
                        if (handler is Action<T> typedHandler)
                        {
                            typedHandler.Invoke(eventData);
                        }
                        else
                        {
                            deadHandlers.Add(handler);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error handling event {eventType.Name}: {e.Message}");
                        deadHandlers.Add(handler); // Remove problematic handlers
                    }
                }
                
                // Clean up dead handlers
                if (deadHandlers.Count > 0)
                {
                    lock (lockObject)
                    {
                        if (subscribers.ContainsKey(eventType))
                        {
                            foreach (var deadHandler in deadHandlers)
                            {
                                subscribers[eventType].Remove(deadHandler);
                            }
                            
                            if (subscribers[eventType].Count == 0)
                            {
                                subscribers.Remove(eventType);
                            }
                        }
                    }
                }
            }
        }
    }

    public interface IEvent { }

    // Damage Events
    public class DamageDealtEvent : IEvent
    {
        public GameObject Attacker { get; }
        public GameObject Defender { get; }
        public DamageResult DamageResult { get; }
        public AbilityData AbilityData { get; }
        public Vector2 AttackPosition { get; }

        public DamageDealtEvent(GameObject attacker, GameObject defender,
                              DamageResult damageResult, AbilityData abilityData = null,
                              Vector2 attackPosition = default)
        {
            Attacker = attacker;
            Defender = defender;
            DamageResult = damageResult;
            AbilityData = abilityData;
            AttackPosition = attackPosition;
        }
    }

    public class DamageMitigatedEvent : IEvent
    {
        public GameObject Defender { get; }
        public float RawDamage { get; }
        public float MitigatedDamage { get; }
        public float MitigationPercentage { get; }
        public DamageType DamageType { get; }

        public DamageMitigatedEvent(GameObject defender, float rawDamage,
                                  float mitigatedDamage, DamageType damageType)
        {
            Defender = defender;
            RawDamage = rawDamage;
            MitigatedDamage = mitigatedDamage;
            DamageType = damageType;
            MitigationPercentage = rawDamage > 0 ? (1f - mitigatedDamage / rawDamage) * 100f : 0f;
        }
    }

    public class CharacterDefeatedEvent : IEvent
    {
        public GameObject DefeatedCharacter { get; }
        public GameObject Attacker { get; }
        public float OverkillDamage { get; }
        public DamageType KillingBlowType { get; }

        public CharacterDefeatedEvent(GameObject defeatedCharacter, GameObject attacker,
                                    float overkillDamage, DamageType killingBlowType)
        {
            DefeatedCharacter = defeatedCharacter;
            Attacker = attacker;
            OverkillDamage = overkillDamage;
            KillingBlowType = killingBlowType;
        }
    }

    public class CriticalHitEvent : IEvent
    {
        public GameObject Attacker { get; }
        public GameObject Defender { get; }
        public float CritMultiplier { get; }
        public float CritDamage { get; }
        public DamageType DamageType { get; }

        public CriticalHitEvent(GameObject attacker, GameObject defender,
                              float critMultiplier, float critDamage, DamageType damageType)
        {
            Attacker = attacker;
            Defender = defender;
            CritMultiplier = critMultiplier;
            CritDamage = critDamage;
            DamageType = damageType;
        }
    }

    public class HPRegeneratedEvent : IEvent
    {
        public GameObject Character { get; }
        public float RegeneratedAmount { get; }
        public float NewHP { get; }
        public float MaxHP { get; }
        public string RegenSource { get; }

        public HPRegeneratedEvent(GameObject character, float regeneratedAmount,
                                float newHP, float maxHP, string regenSource = "passive")
        {
            Character = character;
            RegeneratedAmount = regeneratedAmount;
            NewHP = newHP;
            MaxHP = maxHP;
            RegenSource = regenSource;
        }
    }

    public class StateTransitionEvent : IEvent
    {
        public GameObject Character { get; }
        public string PreviousState { get; }
        public string NewState { get; }
        public string Reason { get; }
        public float TransitionTime { get; }

        public StateTransitionEvent(GameObject character, string previousState,
                                  string newState, string reason = "", float transitionTime = 0f)
        {
            Character = character;
            PreviousState = previousState;
            NewState = newState;
            Reason = reason;
            TransitionTime = transitionTime;
        }
    }

    public class AbilityExecutedEvent : IEvent
    {
        public GameObject Caster { get; }
        public AbilityData AbilityData { get; }
        public Vector2 TargetPosition { get; }
        public float CooldownRemaining { get; }

        public AbilityExecutedEvent(GameObject caster, AbilityData abilityData,
                                  Vector2 targetPosition, float cooldownRemaining)
        {
            Caster = caster;
            AbilityData = abilityData;
            TargetPosition = targetPosition;
            CooldownRemaining = cooldownRemaining;
        }
    }

    public class LifestealEvent : IEvent
    {
        public GameObject Character { get; }
        public float LifestealAmount { get; }
        public float NewHP { get; }
        public DamageType SourceDamageType { get; }

        public LifestealEvent(GameObject character, float lifestealAmount,
                            float newHP, DamageType sourceDamageType)
        {
            Character = character;
            LifestealAmount = lifestealAmount;
            NewHP = newHP;
            SourceDamageType = sourceDamageType;
        }
    }

    // UI Update Events
    public class UIUpdateEvent : IEvent
    {
        public string UpdateType { get; }
        public object Data { get; }

        public UIUpdateEvent(string updateType, object data = null)
        {
            UpdateType = updateType;
            Data = data;
        }
    }

    // Combat Events
    public class CombatStartEvent : IEvent
    {
        public GameObject Initiator { get; }
        public GameObject Target { get; }
        public string CombatType { get; }

        public CombatStartEvent(GameObject initiator, GameObject target, string combatType = "pvp")
        {
            Initiator = initiator;
            Target = target;
            CombatType = combatType;
        }
    }

    public class CombatEndEvent : IEvent
    {
        public GameObject Winner { get; }
        public GameObject Loser { get; }
        public string EndReason { get; }
        public float CombatDuration { get; }

        public CombatEndEvent(GameObject winner, GameObject loser,
                            string endReason, float combatDuration)
        {
            Winner = winner;
            Loser = loser;
            EndReason = endReason;
            CombatDuration = combatDuration;
        }
    }
}