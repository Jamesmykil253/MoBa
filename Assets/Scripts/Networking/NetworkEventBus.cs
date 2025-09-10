using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace MOBA.Networking
{
    /// <summary>
    /// Centralized event system for network components using Observer pattern
    /// Provides decoupled communication between networking systems
    /// </summary>
    public class NetworkEventBus : MonoBehaviour
    {
        private static NetworkEventBus _instance;
        public static NetworkEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("NetworkEventBus");
                    _instance = go.AddComponent<NetworkEventBus>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Player events
        public event Action<NetworkPlayerController, float> OnPlayerHealthChanged;
        public event Action<NetworkPlayerController, Vector3> OnPlayerPositionChanged;
        public event Action<NetworkPlayerController, int> OnPlayerCoinsChanged;
        public event Action<NetworkPlayerController> OnPlayerDeath;
        public event Action<NetworkPlayerController> OnPlayerRespawn;

        // Ability events
        public event Action<NetworkPlayerController, AbilityType, Vector3> OnAbilityCast;
        public event Action<NetworkPlayerController, AbilityType> OnAbilityCooldownReady;
        public event Action<ulong, AbilityType> OnAbilityRateLimitExceeded;

        // Projectile events
        public event Action<NetworkProjectile, Vector3> OnProjectileHit;
        public event Action<NetworkProjectile> OnProjectileDestroyed;
        public event Action<NetworkProjectile, Vector3> OnProjectileCreated;

        // Game state events
        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
        public event Action OnGameStarted;
        public event Action OnGameEnded;

        // Network events
        public event Action<float> OnNetworkLatencyUpdate;
        public event Action<int> OnNetworkMessageSent;
        public event Action<int> OnNetworkMessageReceived;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        // Player event publishers
        public void PublishPlayerHealthChanged(NetworkPlayerController player, float newHealth)
        {
            OnPlayerHealthChanged?.Invoke(player, newHealth);
        }

        public void PublishPlayerPositionChanged(NetworkPlayerController player, Vector3 newPosition)
        {
            OnPlayerPositionChanged?.Invoke(player, newPosition);
        }

        public void PublishPlayerCoinsChanged(NetworkPlayerController player, int newCoins)
        {
            OnPlayerCoinsChanged?.Invoke(player, newCoins);
        }

        public void PublishPlayerDeath(NetworkPlayerController player)
        {
            OnPlayerDeath?.Invoke(player);
        }

        public void PublishPlayerRespawn(NetworkPlayerController player)
        {
            OnPlayerRespawn?.Invoke(player);
        }

        // Ability event publishers
        public void PublishAbilityCast(NetworkPlayerController player, AbilityType ability, Vector3 target)
        {
            OnAbilityCast?.Invoke(player, ability, target);
        }

        public void PublishAbilityCooldownReady(NetworkPlayerController player, AbilityType ability)
        {
            OnAbilityCooldownReady?.Invoke(player, ability);
        }

        public void PublishAbilityRateLimitExceeded(ulong clientId, AbilityType ability)
        {
            OnAbilityRateLimitExceeded?.Invoke(clientId, ability);
        }

        // Projectile event publishers
        public void PublishProjectileHit(NetworkProjectile projectile, Vector3 hitPoint)
        {
            OnProjectileHit?.Invoke(projectile, hitPoint);
        }

        public void PublishProjectileDestroyed(NetworkProjectile projectile)
        {
            OnProjectileDestroyed?.Invoke(projectile);
        }

        public void PublishProjectileCreated(NetworkProjectile projectile, Vector3 spawnPosition)
        {
            OnProjectileCreated?.Invoke(projectile, spawnPosition);
        }

        // Game state event publishers
        public void PublishClientConnected(ulong clientId)
        {
            OnClientConnected?.Invoke(clientId);
        }

        public void PublishClientDisconnected(ulong clientId)
        {
            OnClientDisconnected?.Invoke(clientId);
        }

        public void PublishGameStarted()
        {
            OnGameStarted?.Invoke();
        }

        public void PublishGameEnded()
        {
            OnGameEnded?.Invoke();
        }

        // Network event publishers
        public void PublishNetworkLatencyUpdate(float latency)
        {
            OnNetworkLatencyUpdate?.Invoke(latency);
        }

        public void PublishNetworkMessageSent(int messageCount)
        {
            OnNetworkMessageSent?.Invoke(messageCount);
        }

        public void PublishNetworkMessageReceived(int messageCount)
        {
            OnNetworkMessageReceived?.Invoke(messageCount);
        }

        // Subscription helpers
        public void SubscribeToPlayerEvents(IPlayerEventObserver observer)
        {
            OnPlayerHealthChanged += observer.OnPlayerHealthChanged;
            OnPlayerPositionChanged += observer.OnPlayerPositionChanged;
            OnPlayerCoinsChanged += observer.OnPlayerCoinsChanged;
            OnPlayerDeath += observer.OnPlayerDeath;
            OnPlayerRespawn += observer.OnPlayerRespawn;
        }

        public void UnsubscribeFromPlayerEvents(IPlayerEventObserver observer)
        {
            OnPlayerHealthChanged -= observer.OnPlayerHealthChanged;
            OnPlayerPositionChanged -= observer.OnPlayerPositionChanged;
            OnPlayerCoinsChanged -= observer.OnPlayerCoinsChanged;
            OnPlayerDeath -= observer.OnPlayerDeath;
            OnPlayerRespawn -= observer.OnPlayerRespawn;
        }

        public void SubscribeToAbilityEvents(IAbilityEventObserver observer)
        {
            OnAbilityCast += observer.OnAbilityCast;
            OnAbilityCooldownReady += observer.OnAbilityCooldownReady;
            OnAbilityRateLimitExceeded += observer.OnAbilityRateLimitExceeded;
        }

        public void UnsubscribeFromAbilityEvents(IAbilityEventObserver observer)
        {
            OnAbilityCast -= observer.OnAbilityCast;
            OnAbilityCooldownReady -= observer.OnAbilityCooldownReady;
            OnAbilityRateLimitExceeded -= observer.OnAbilityRateLimitExceeded;
        }

        public void SubscribeToProjectileEvents(IProjectileEventObserver observer)
        {
            OnProjectileHit += observer.OnProjectileHit;
            OnProjectileDestroyed += observer.OnProjectileDestroyed;
            OnProjectileCreated += observer.OnProjectileCreated;
        }

        public void UnsubscribeFromProjectileEvents(IProjectileEventObserver observer)
        {
            OnProjectileHit -= observer.OnProjectileHit;
            OnProjectileDestroyed -= observer.OnProjectileDestroyed;
            OnProjectileCreated -= observer.OnProjectileCreated;
        }
    }

    // Observer interfaces
    public interface IPlayerEventObserver
    {
        void OnPlayerHealthChanged(NetworkPlayerController player, float health);
        void OnPlayerPositionChanged(NetworkPlayerController player, Vector3 position);
        void OnPlayerCoinsChanged(NetworkPlayerController player, int coins);
        void OnPlayerDeath(NetworkPlayerController player);
        void OnPlayerRespawn(NetworkPlayerController player);
    }

    public interface IAbilityEventObserver
    {
        void OnAbilityCast(NetworkPlayerController player, AbilityType ability, Vector3 target);
        void OnAbilityCooldownReady(NetworkPlayerController player, AbilityType ability);
        void OnAbilityRateLimitExceeded(ulong clientId, AbilityType ability);
    }

    public interface IProjectileEventObserver
    {
        void OnProjectileHit(NetworkProjectile projectile, Vector3 hitPoint);
        void OnProjectileDestroyed(NetworkProjectile projectile);
        void OnProjectileCreated(NetworkProjectile projectile, Vector3 spawnPosition);
    }

    public interface INetworkEventObserver
    {
        void OnClientConnected(ulong clientId);
        void OnClientDisconnected(ulong clientId);
        void OnGameStarted();
        void OnGameEnded();
        void OnNetworkLatencyUpdate(float latency);
    }
}