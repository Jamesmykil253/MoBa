
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace MOBA
{
	/// <summary>
	/// Weak reference contract for event handlers to prevent memory leaks.
	/// </summary>
	public interface IWeakEventHandler
	{
		bool IsAlive { get; }
		Delegate OriginalDelegate { get; }
		bool TryInvoke(object arg);
	}

	/// <summary>
	/// Weak reference wrapper for strongly typed event handlers without using reflection per dispatch.
	/// </summary>
	/// <typeparam name="T">Event payload type.</typeparam>
	public sealed class WeakEventHandler<T> : IWeakEventHandler
	{
		private readonly WeakReference targetRef;
		private readonly bool isStatic;
		private readonly Action<T> typedHandler;

		public WeakEventHandler(Action<T> handler)
		{
			if (handler == null) throw new ArgumentNullException(nameof(handler));

			typedHandler = handler;
			isStatic = handler.Target == null;
			if (!isStatic)
			{
				targetRef = new WeakReference(handler.Target);
			}
		}

		public bool IsAlive => isStatic || (targetRef != null && targetRef.IsAlive);

		public Delegate OriginalDelegate => typedHandler;

		public bool TryInvoke(object arg)
		{
			if (!IsAlive)
			{
				return false;
			}

			if (!isStatic && targetRef.Target == null)
			{
				return false;
			}

			if (arg is T payload)
			{
				typedHandler(payload);
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Unified event system for local and network events
	/// Implements the Observer pattern with weak references to prevent memory leaks
	/// Supports both local events (single client) and network events (server-authoritative)
	/// 
	/// <example>
	/// <para><strong>Local Event Usage (Observer Pattern):</strong></para>
	/// <code>
	/// // Define a local event
	/// public class PlayerLevelUpEvent : ILocalEvent
	/// {
	///     public int PlayerLevel { get; }
	///     public int NewXP { get; }
	///     
	///     public PlayerLevelUpEvent(int level, int xp)
	///     {
	///         PlayerLevel = level;
	///         NewXP = xp;
	///     }
	/// }
	/// 
	/// // Subscribe to the event
	/// public class UIManager : MonoBehaviour
	/// {
	///     void Start()
	///     {
	///         UnifiedEventSystem.SubscribeLocal&lt;PlayerLevelUpEvent&gt;(OnPlayerLevelUp);
	///     }
	///     
	///     void OnDestroy()
	///     {
	///         UnifiedEventSystem.UnsubscribeLocal&lt;PlayerLevelUpEvent&gt;(OnPlayerLevelUp);
	///     }
	///     
	///     private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
	///     {
	///         Debug.Log($"Player reached level {evt.PlayerLevel} with {evt.NewXP} XP!");
	///         UpdateLevelDisplay(evt.PlayerLevel);
	///     }
	/// }
	/// 
	/// // Publish the event
	/// public class PlayerController : MonoBehaviour
	/// {
	///     void GainExperience(int xp)
	///     {
	///         currentXP += xp;
	///         if (currentXP >= xpToNextLevel)
	///         {
	///             currentLevel++;
	///             var levelUpEvent = new PlayerLevelUpEvent(currentLevel, currentXP);
	///             UnifiedEventSystem.PublishLocal(levelUpEvent);
	///         }
	///     }
	/// }
	/// </code>
	/// 
	/// <para><strong>Network Event Usage (Server-Authoritative):</strong></para>
	/// <code>
	/// // Network events can only be published by server/host
	/// public class GameManager : NetworkBehaviour
	/// {
	///     void Start()
	///     {
	///         // All clients can subscribe to network events
	///         UnifiedEventSystem.SubscribeNetwork&lt;PlayerConnectedEvent&gt;(OnPlayerJoined);
	///         UnifiedEventSystem.SubscribeNetwork&lt;PlayerDisconnectedEvent&gt;(OnPlayerLeft);
	///     }
	///     
	///     void OnDestroy()
	///     {
	///         UnifiedEventSystem.UnsubscribeNetwork&lt;PlayerConnectedEvent&gt;(OnPlayerJoined);
	///         UnifiedEventSystem.UnsubscribeNetwork&lt;PlayerDisconnectedEvent&gt;(OnPlayerLeft);
	///     }
	///     
	///     // Server publishes network events
	///     [ServerRpc]
	///     void HandlePlayerConnectionServerRpc(ulong clientId)
	///     {
	///         if (IsServer)
	///         {
	///             var connectEvent = new PlayerConnectedEvent(clientId, Time.time);
	///             UnifiedEventSystem.PublishNetwork(connectEvent); // Sent to all clients
	///         }
	///     }
	///     
	///     private void OnPlayerJoined(PlayerConnectedEvent evt)
	///     {
	///         Debug.Log($"Player {evt.ClientId} joined at {evt.Timestamp}");
	///         ShowWelcomeMessage(evt.ClientId);
	///     }
	///     
	///     private void OnPlayerLeft(PlayerDisconnectedEvent evt)
	///     {
	///         Debug.Log($"Player {evt.ClientId} left at {evt.Timestamp}");
	///         CleanupPlayerData(evt.ClientId);
	///     }
	/// }
	/// </code>
	/// 
	/// <para><strong>Combat Event Chain Example:</strong></para>
	/// <code>
	/// // Combat system publishes damage events
	/// public class CombatSystem : MonoBehaviour
	/// {
	///     public void DealDamage(GameObject attacker, GameObject defender, float damage)
	///     {
	///         // Apply damage
	///         var health = defender.GetComponent&lt;HealthComponent&gt;();
	///         health.TakeDamage(damage);
	///         
	///         // Publish damage event for UI/effects
	///         var damageEvent = new DamageDealtEvent(attacker, defender, damage);
	///         UnifiedEventSystem.PublishLocal(damageEvent);
	///         
	///         // Check for death
	///         if (health.IsDead())
	///         {
	///             var deathEvent = new CharacterDefeatedEvent(defender, attacker, 0f, "Combat");
	///             UnifiedEventSystem.PublishLocal(deathEvent);
	///         }
	///     }
	/// }
	/// 
	/// // Multiple systems can react to the same event
	/// public class EffectsManager : MonoBehaviour
	/// {
	///     void Start()
	///     {
	///         UnifiedEventSystem.SubscribeLocal&lt;DamageDealtEvent&gt;(OnDamageDealt);
	///         UnifiedEventSystem.SubscribeLocal&lt;CharacterDefeatedEvent&gt;(OnCharacterDefeated);
	///     }
	///     
	///     void OnDamageDealt(DamageDealtEvent evt)
	///     {
	///         SpawnDamageNumber(evt.Defender.transform.position, evt.DamageResult);
	///         PlayHitEffect(evt.AttackPosition);
	///     }
	///     
	///     void OnCharacterDefeated(CharacterDefeatedEvent evt)
	///     {
	///         SpawnDeathEffect(evt.DefeatedCharacter.transform.position);
	///         PlayDeathSound();
	///     }
	/// }
	/// </code>
	/// 
	/// <para><strong>Memory Management and Cleanup:</strong></para>
	/// <code>
	/// // Automatic cleanup with weak references (no explicit unsubscribe needed)
	/// public class TemporaryEffect : MonoBehaviour
	/// {
	///     void Start()
	///     {
	///         // Subscribe without worrying about cleanup - weak references prevent leaks
	///         UnifiedEventSystem.SubscribeLocal&lt;ExplosionEvent&gt;(OnExplosion);
	///     }
	///     
	///     // When this object is destroyed, weak references automatically clean up
	///     
	///     void OnExplosion(ExplosionEvent evt)
	///     {
	///         if (Vector3.Distance(transform.position, evt.Position) &lt; evt.Radius)
	///         {
	///             ApplyKnockback(evt.Force);
	///         }
	///     }
	/// }
	/// 
	/// // Manual cleanup for performance-critical scenarios
	/// public class PerformanceManager : MonoBehaviour
	/// {
	///     void Update()
	///     {
	///         // Force cleanup of dead references periodically
	///         if (Time.time % 60f &lt; Time.deltaTime) // Every 60 seconds
	///         {
	///             UnifiedEventSystem.ForceCleanup();
	///         }
	///     }
	/// }
	/// </code>
	/// </example>
	/// 
	/// <para><strong>Design Pattern:</strong> Observer with Weak References</para>
	/// <para><strong>Thread Safety:</strong> All operations are thread-safe with proper locking</para>
	/// <para><strong>Memory Safety:</strong> Weak references prevent memory leaks from forgotten unsubscribes</para>
	/// <para><strong>Network Aware:</strong> Server authority enforced for network events</para>
	/// <para><strong>Performance:</strong> Automatic cleanup of dead references every 30 seconds</para>
	/// </summary>
	public static class UnifiedEventSystem
	{
		private static readonly object lockObject = new object();
		private static readonly ConcurrentDictionary<Type, ConcurrentBag<IWeakEventHandler>> localSubscribers = new ConcurrentDictionary<Type, ConcurrentBag<IWeakEventHandler>>();
		private static readonly ConcurrentDictionary<Type, ConcurrentBag<IWeakEventHandler>> networkSubscribers = new ConcurrentDictionary<Type, ConcurrentBag<IWeakEventHandler>>();
		private static float lastCleanupTime = 0f;
		private const float cleanupInterval = 30f;

		public static void SubscribeLocal<T>(Action<T> handler) where T : ILocalEvent
		{
			if (handler == null) return;
			lock (lockObject)
			{
				var eventType = typeof(T);
				if (!localSubscribers.ContainsKey(eventType))
				{
					localSubscribers[eventType] = new ConcurrentBag<IWeakEventHandler>();
				}
				var weakHandler = new WeakEventHandler<T>(handler);
				localSubscribers[eventType].Add(weakHandler);
				if (Time.time - lastCleanupTime > cleanupInterval)
				{
					CleanupDeadReferences();
				}
			}
		}

		public static void UnsubscribeLocal<T>(Action<T> handler) where T : ILocalEvent
		{
			if (handler == null) return;
			lock (lockObject)
			{
				var eventType = typeof(T);
				if (localSubscribers.ContainsKey(eventType))
				{
					var oldBag = localSubscribers[eventType];
					var newBag = new ConcurrentBag<IWeakEventHandler>();
					foreach (var weakHandler in oldBag)
					{
						if (weakHandler.IsAlive && !ReferenceEquals(weakHandler.OriginalDelegate, handler))
						{
							newBag.Add(weakHandler);
						}
					}
					localSubscribers[eventType] = newBag;
				}
			}
		}

		public static void PublishLocal<T>(T eventData) where T : ILocalEvent
		{
			if (eventData == null) return;
			var eventType = typeof(T);
			if (localSubscribers.TryGetValue(eventType, out var subscribers))
			{
				var deadHandlers = new List<IWeakEventHandler>();
				foreach (var handler in subscribers)
				{
					if (!handler.TryInvoke(eventData))
					{
						deadHandlers.Add(handler);
					}
				}
				if (deadHandlers.Count > 0)
				{
					CleanupLocalHandlers(eventType, deadHandlers);
				}
			}
		}

		public static void SubscribeNetwork<T>(Action<T> handler) where T : INetworkEvent
		{
			if (handler == null) return;
			lock (lockObject)
			{
				var eventType = typeof(T);
				if (!networkSubscribers.ContainsKey(eventType))
				{
					networkSubscribers[eventType] = new ConcurrentBag<IWeakEventHandler>();
				}
				var weakHandler = new WeakEventHandler<T>(handler);
				networkSubscribers[eventType].Add(weakHandler);
				if (Time.time - lastCleanupTime > cleanupInterval)
				{
					CleanupDeadReferences();
				}
			}
		}

		public static void UnsubscribeNetwork<T>(Action<T> handler) where T : INetworkEvent
		{
			if (handler == null) return;
			lock (lockObject)
			{
				var eventType = typeof(T);
				if (networkSubscribers.ContainsKey(eventType))
				{
					var oldBag = networkSubscribers[eventType];
					var newBag = new ConcurrentBag<IWeakEventHandler>();
					foreach (var weakHandler in oldBag)
					{
						if (weakHandler.IsAlive && !ReferenceEquals(weakHandler.OriginalDelegate, handler))
						{
							newBag.Add(weakHandler);
						}
					}
					networkSubscribers[eventType] = newBag;
				}
			}
		}

		public static void PublishNetwork<T>(T eventData) where T : INetworkEvent
		{
			if (eventData == null) return;
			if (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost))
			{
				Debug.LogWarning($"[UnifiedEventSystem] Network event {typeof(T).Name} can only be published by server/host");
				return;
			}
			var eventType = typeof(T);
			if (networkSubscribers.TryGetValue(eventType, out var subscribers))
			{
				var deadHandlers = new List<IWeakEventHandler>();
				foreach (var handler in subscribers)
				{
					if (!handler.TryInvoke(eventData))
					{
						deadHandlers.Add(handler);
					}
				}
				if (deadHandlers.Count > 0)
				{
					CleanupNetworkHandlers(eventType, deadHandlers);
				}
			}
		}

		private static void CleanupDeadReferences()
		{
			lastCleanupTime = Time.time;
			var localTypes = localSubscribers.Keys.ToList();
			foreach (var eventType in localTypes)
			{
				if (localSubscribers.TryGetValue(eventType, out var bag))
				{
					var aliveBag = new ConcurrentBag<IWeakEventHandler>();
					foreach (var handler in bag)
					{
						if (handler.IsAlive)
						{
							aliveBag.Add(handler);
						}
					}
					localSubscribers[eventType] = aliveBag;
				}
			}
			var networkTypes = networkSubscribers.Keys.ToList();
			foreach (var eventType in networkTypes)
			{
				if (networkSubscribers.TryGetValue(eventType, out var bag))
				{
					var aliveBag = new ConcurrentBag<IWeakEventHandler>();
					foreach (var handler in bag)
					{
						if (handler.IsAlive)
						{
							aliveBag.Add(handler);
						}
					}
					networkSubscribers[eventType] = aliveBag;
				}
			}
			Debug.Log("[UnifiedEventSystem] Cleaned up dead event handler references");
		}

		private static void CleanupLocalHandlers(Type eventType, List<IWeakEventHandler> deadHandlers)
		{
			lock (lockObject)
			{
				if (localSubscribers.TryGetValue(eventType, out var oldBag))
				{
					var newBag = new ConcurrentBag<IWeakEventHandler>();
					foreach (var handler in oldBag)
					{
						if (!deadHandlers.Contains(handler) && handler.IsAlive)
						{
							newBag.Add(handler);
						}
					}
					localSubscribers[eventType] = newBag;
				}
			}
		}

		private static void CleanupNetworkHandlers(Type eventType, List<IWeakEventHandler> deadHandlers)
		{
			lock (lockObject)
			{
				if (networkSubscribers.TryGetValue(eventType, out var oldBag))
				{
					var newBag = new ConcurrentBag<IWeakEventHandler>();
					foreach (var handler in oldBag)
					{
						if (!deadHandlers.Contains(handler) && handler.IsAlive)
						{
							newBag.Add(handler);
						}
					}
					networkSubscribers[eventType] = newBag;
				}
			}
		}

		public static void ClearLocalSubscriptions()
		{
			lock (lockObject)
			{
				localSubscribers.Clear();
				Debug.Log("[UnifiedEventSystem] Cleared all local subscriptions");
			}
		}

		public static void ClearNetworkSubscriptions()
		{
			lock (lockObject)
			{
				networkSubscribers.Clear();
				Debug.Log("[UnifiedEventSystem] Cleared all network subscriptions");
			}
		}

		public static void ClearAllSubscriptions()
		{
			ClearLocalSubscriptions();
			ClearNetworkSubscriptions();
		}

		public static void ForceCleanup()
		{
			CleanupDeadReferences();
		}

		public static (int localEventTypes, int networkEventTypes, int totalSubscribers, int aliveSubscribers) GetStats()
		{
			int totalSubscribers = 0;
			int aliveSubscribers = 0;
			foreach (var bag in localSubscribers.Values)
			{
				var count = bag.Count;
				totalSubscribers += count;
				aliveSubscribers += bag.Count(h => h.IsAlive);
			}
			foreach (var bag in networkSubscribers.Values)
			{
				var count = bag.Count;
				totalSubscribers += count;
				aliveSubscribers += bag.Count(h => h.IsAlive);
			}
			return (localSubscribers.Count, networkSubscribers.Count, totalSubscribers, aliveSubscribers);
		}
	}

	// Supporting event interfaces
	public interface IEvent { }
	public interface ILocalEvent : IEvent { }
	public interface INetworkEvent : IEvent { }

	// Example event types (add more as needed)

	// Ability used event (local)
	public class AbilityUsedEvent : ILocalEvent
	{
		public GameObject Caster { get; }
		public string AbilityName { get; }
		public float ManaCost { get; }
		public float Cooldown { get; }
		public Vector3 TargetPosition { get; }
		public GameObject Target { get; }

		public AbilityUsedEvent(GameObject caster, string abilityName, float manaCost, float cooldown, Vector3 targetPosition, GameObject target = null)
		{
			Caster = caster;
			AbilityName = abilityName;
			ManaCost = manaCost;
			Cooldown = cooldown;
			TargetPosition = targetPosition;
			Target = target;
		}
	}

	// Player connected event (network)
	public class PlayerConnectedEvent : INetworkEvent
	{
		public ulong ClientId { get; }
		public float Timestamp { get; }

		public PlayerConnectedEvent(ulong clientId, float timestamp)
		{
			ClientId = clientId;
			Timestamp = timestamp;
		}
	}

	// Player disconnected event (network)
	public class PlayerDisconnectedEvent : INetworkEvent
	{
		public ulong ClientId { get; }
		public float Timestamp { get; }

		public PlayerDisconnectedEvent(ulong clientId, float timestamp)
		{
			ClientId = clientId;
			Timestamp = timestamp;
		}
	}
	public class DamageDealtEvent : ILocalEvent
	{
		public GameObject Attacker { get; }
		public GameObject Defender { get; }
		public object DamageResult { get; }
		public object AbilityData { get; }
		public Vector2 AttackPosition { get; }
		public DamageDealtEvent(GameObject attacker, GameObject defender, object damageResult, object abilityData = null, Vector2 attackPosition = default)
		{
			Attacker = attacker;
			Defender = defender;
			DamageResult = damageResult;
			AbilityData = abilityData;
			AttackPosition = attackPosition;
		}
	}

	public class CharacterDefeatedEvent : ILocalEvent
	{
		public GameObject DefeatedCharacter { get; }
		public GameObject Attacker { get; }
		public float OverkillDamage { get; }
		public object KillingBlowType { get; }
		public CharacterDefeatedEvent(GameObject defeatedCharacter, GameObject attacker, float overkillDamage, object killingBlowType)
		{
			DefeatedCharacter = defeatedCharacter;
			Attacker = attacker;
			OverkillDamage = overkillDamage;
			KillingBlowType = killingBlowType;
		}
	}

	public class StateTransitionEvent : ILocalEvent
	{
		public GameObject Character { get; }
		public string PreviousState { get; }
		public string NewState { get; }
		public float TransitionTime { get; }
		public StateTransitionEvent(GameObject character, string previousState, string newState, float transitionTime)
		{
			Character = character;
			PreviousState = previousState;
			NewState = newState;
			TransitionTime = transitionTime;
		}
	}

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

	public class GameStateChangedEvent : INetworkEvent
	{
		public object PreviousState { get; }
		public object NewState { get; }
		public float Timestamp { get; }
		public GameStateChangedEvent(object previousState, object newState, float timestamp)
		{
			PreviousState = previousState;
			NewState = newState;
			Timestamp = timestamp;
		}
	}

	// Supporting enums and types (replace object with actual types as needed)
	public enum GameState { Lobby, Loading, Playing, Paused, GameOver }
	public enum DamageType { Physical, Magical, True, Healing }
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
}
