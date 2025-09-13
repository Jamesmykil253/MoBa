using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MOBA
{
    /// <summary>
    /// Unified Event System that consolidates EventBus and NetworkEventBus
    /// Supports both local and network events with proper separation
    /// Thread-safe implementation with defensive programming patterns
    /// </summary>
    public static class UnifiedEventSystem
    {
        // Thread-safe event storage
        private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> localSubscribers = new();
        private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> networkSubscribers = new();
        private static readonly object lockObject = new object();

        #region Local Event Handling

        /// <summary>
        /// Subscribe to local events (non-networked)
        /// </summary>
        public static void SubscribeLocal<T>(Action<T> handler) where T : ILocalEvent
        {
            if (handler == null) return;
            
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!localSubscribers.ContainsKey(eventType))
                {
                    localSubscribers[eventType] = new ConcurrentBag<object>();
                }
                
                localSubscribers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from local events
        /// </summary>
        public static void UnsubscribeLocal<T>(Action<T> handler) where T : ILocalEvent
        {
            if (handler == null) return;
            
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (localSubscribers.ContainsKey(eventType))
                {
                    // Note: ConcurrentBag doesn't support direct removal, 
                    // so we'll recreate without the handler
                    var oldBag = localSubscribers[eventType];
                    var newBag = new ConcurrentBag<object>();
                    
                    foreach (var item in oldBag)
                    {
                        if (!item.Equals(handler))
                        {
                            newBag.Add(item);
                        }
                    }
                    
                    localSubscribers[eventType] = newBag;
                }
            }
        }

        /// <summary>
        /// Publish local events
        /// </summary>
        public static void PublishLocal<T>(T eventData) where T : ILocalEvent
        {
            if (eventData == null) return;

            var eventType = typeof(T);
            if (localSubscribers.TryGetValue(eventType, out var subscribers))
            {
                var deadHandlers = new List<object>();
                
                foreach (var handler in subscribers)
                {
                    try
                    {
                        if (handler is Action<T> typedHandler)
                        {
                            typedHandler.Invoke(eventData);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UnifiedEventSystem] Error handling local event {eventType.Name}: {e.Message}");
                        deadHandlers.Add(handler);
                    }
                }
                
                // Clean up dead handlers
                if (deadHandlers.Count > 0)
                {
                    CleanupLocalHandlers(eventType, deadHandlers);
                }
            }
        }

        #endregion

        #region Network Event Handling

        /// <summary>
        /// Subscribe to network events
        /// </summary>
        public static void SubscribeNetwork<T>(Action<T> handler) where T : INetworkEvent
        {
            if (handler == null) return;
            
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!networkSubscribers.ContainsKey(eventType))
                {
                    networkSubscribers[eventType] = new ConcurrentBag<object>();
                }
                
                networkSubscribers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from network events
        /// </summary>
        public static void UnsubscribeNetwork<T>(Action<T> handler) where T : INetworkEvent
        {
            if (handler == null) return;
            
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (networkSubscribers.ContainsKey(eventType))
                {
                    var oldBag = networkSubscribers[eventType];
                    var newBag = new ConcurrentBag<object>();
                    
                    foreach (var item in oldBag)
                    {
                        if (!item.Equals(handler))
                        {
                            newBag.Add(item);
                        }
                    }
                    
                    networkSubscribers[eventType] = newBag;
                }
            }
        }

        /// <summary>
        /// Publish network events (server authority)
        /// </summary>
        public static void PublishNetwork<T>(T eventData) where T : INetworkEvent
        {
            if (eventData == null) return;

            // Network events should only be published by server or host
            if (NetworkManager.Singleton == null || 
                (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost))
            {
                Debug.LogWarning($"[UnifiedEventSystem] Network event {typeof(T).Name} can only be published by server/host");
                return;
            }

            var eventType = typeof(T);
            if (networkSubscribers.TryGetValue(eventType, out var subscribers))
            {
                var deadHandlers = new List<object>();
                
                foreach (var handler in subscribers)
                {
                    try
                    {
                        if (handler is Action<T> typedHandler)
                        {
                            typedHandler.Invoke(eventData);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UnifiedEventSystem] Error handling network event {eventType.Name}: {e.Message}");
                        deadHandlers.Add(handler);
                    }
                }
                
                // Clean up dead handlers
                if (deadHandlers.Count > 0)
                {
                    CleanupNetworkHandlers(eventType, deadHandlers);
                }
            }
        }

        #endregion

        #region Utility Methods

        private static void CleanupLocalHandlers(Type eventType, List<object> deadHandlers)
        {
            lock (lockObject)
            {
                if (localSubscribers.TryGetValue(eventType, out var oldBag))
                {
                    var newBag = new ConcurrentBag<object>();
                    
                    foreach (var item in oldBag)
                    {
                        if (!deadHandlers.Contains(item))
                        {
                            newBag.Add(item);
                        }
                    }
                    
                    localSubscribers[eventType] = newBag;
                }
            }
        }

        private static void CleanupNetworkHandlers(Type eventType, List<object> deadHandlers)
        {
            lock (lockObject)
            {
                if (networkSubscribers.TryGetValue(eventType, out var oldBag))
                {
                    var newBag = new ConcurrentBag<object>();
                    
                    foreach (var item in oldBag)
                    {
                        if (!deadHandlers.Contains(item))
                        {
                            newBag.Add(item);
                        }
                    }
                    
                    networkSubscribers[eventType] = newBag;
                }
            }
        }

        /// <summary>
        /// Clear all local event subscriptions
        /// </summary>
        public static void ClearLocalSubscriptions()
        {
            lock (lockObject)
            {
                localSubscribers.Clear();
                Debug.Log("[UnifiedEventSystem] Cleared all local subscriptions");
            }
        }

        /// <summary>
        /// Clear all network event subscriptions
        /// </summary>
        public static void ClearNetworkSubscriptions()
        {
            lock (lockObject)
            {
                networkSubscribers.Clear();
                Debug.Log("[UnifiedEventSystem] Cleared all network subscriptions");
            }
        }

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            ClearLocalSubscriptions();
            ClearNetworkSubscriptions();
        }

        /// <summary>
        /// Get subscription statistics for debugging
        /// </summary>
        public static (int localEventTypes, int networkEventTypes, int totalSubscribers) GetStats()
        {
            int totalSubscribers = 0;
            
            foreach (var bag in localSubscribers.Values)
            {
                totalSubscribers += bag.Count;
            }
            
            foreach (var bag in networkSubscribers.Values)
            {
                totalSubscribers += bag.Count;
            }
            
            return (localSubscribers.Count, networkSubscribers.Count, totalSubscribers);
        }

        #endregion
    }

    #region Event Interfaces and Base Events

    /// <summary>
    /// Base interface for all events
    /// </summary>
    public interface IEvent { }

    /// <summary>
    /// Interface for local (non-networked) events
    /// </summary>
    public interface ILocalEvent : IEvent { }

    /// <summary>
    /// Interface for network events (require server authority)
    /// </summary>
    public interface INetworkEvent : IEvent { }

    #endregion

    #region Local Game Events

    /// <summary>
    /// Damage dealt event (local)
    /// </summary>
    public class DamageDealtEvent : ILocalEvent
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

    /// <summary>
    /// Character defeated event (local)
    /// </summary>
    public class CharacterDefeatedEvent : ILocalEvent
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

    /// <summary>
    /// State transition event (local)
    /// </summary>
    public class StateTransitionEvent : ILocalEvent
    {
        public GameObject Character { get; }
        public string PreviousState { get; }
        public string NewState { get; }
        public float TransitionTime { get; }

        public StateTransitionEvent(GameObject character, string previousState, 
                                  string newState, float transitionTime)
        {
            Character = character;
            PreviousState = previousState;
            NewState = newState;
            TransitionTime = transitionTime;
        }
    }

    #endregion

    #region Network Events

    /// <summary>
    /// Player health changed event (network)
    /// </summary>
    public class PlayerHealthChangedEvent : INetworkEvent
    {
        public ulong PlayerId { get; }
        public float NewHealth { get; }
        public float MaxHealth { get; }
        public float PreviousHealth { get; }

        public PlayerHealthChangedEvent(ulong playerId, float newHealth, float maxHealth, float previousHealth)
        {
            PlayerId = playerId;
            NewHealth = newHealth;
            MaxHealth = maxHealth;
            PreviousHealth = previousHealth;
        }
    }

    /// <summary>
    /// Player position update event (network)
    /// </summary>
    public class PlayerPositionChangedEvent : INetworkEvent
    {
        public ulong PlayerId { get; }
        public Vector3 NewPosition { get; }
        public Vector3 PreviousPosition { get; }
        public float Timestamp { get; }

        public PlayerPositionChangedEvent(ulong playerId, Vector3 newPosition, Vector3 previousPosition, float timestamp)
        {
            PlayerId = playerId;
            NewPosition = newPosition;
            PreviousPosition = previousPosition;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Network game state event
    /// </summary>
    public class GameStateChangedEvent : INetworkEvent
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }
        public float Timestamp { get; }

        public GameStateChangedEvent(GameState previousState, GameState newState, float timestamp)
        {
            PreviousState = previousState;
            NewState = newState;
            Timestamp = timestamp;
        }
    }

    #endregion

    #region Supporting Types

    public enum GameState
    {
        Lobby,
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Healing
    }

    [System.Serializable]
    public struct DamageResult
    {
        public float FinalDamage;
        public bool IsCritical;
        public float RiskFactor;
        public float SkillMultiplier;
        public DamageType DamageType;
    }

    [System.Serializable]
    public class AbilityData
    {
        public string abilityName;
        public float cooldown;
        public float manaCost;
        public float damage;
        public float range;
    }

    #endregion
}
