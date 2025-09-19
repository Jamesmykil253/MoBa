using UnityEngine;
using Unity.Netcode;
using MOBA.Debugging;

namespace MOBA
{
    /// <summary>
    /// Pokemon Unite style crypto coin system with level-based calculations
    /// NPCs drop coins based on their level and type when defeated
    /// Coins are collected by walking over them and used for scoring and goal breaking
    /// </summary>
    public class PokemonUniteCoinSystem : MonoBehaviour
    {
        [Header("Pokemon Unite Coin Formula")]
        [SerializeField] private float baseCoinValue = 5f;          // Base coins per level
        [SerializeField] private float levelMultiplier = 1.5f;      // Coins scale with level
        [SerializeField] private float wildPokemonBonus = 2f;       // Extra coins for wild Pokemon
        [SerializeField] private float bossMultiplier = 5f;         // Boss Pokemon give 5x coins
        
        [Header("Coin Drop Physics")]
        [SerializeField] private int minCoinsDropped = 1;
        [SerializeField] private int maxCoinsDropped = 8;           // Pokemon Unite max coin drop
        [SerializeField] private float scatterRadius = 2f;
        [SerializeField] private float launchForce = 3f;
        [SerializeField] private GameObject coinPrefab;

        /// <summary>
        /// Calculate coins dropped based on Pokemon Unite formula using instance configuration
        /// Formula: Base * (1 + Level * Multiplier) * TypeBonus
        /// </summary>
        public int CalculateCoinDrop(int defeatedLevel, NPCType npcType)
        {
            float levelBonus = defeatedLevel * levelMultiplier;
            
            float typeMultiplier = npcType switch
            {
                NPCType.WildPokemon => wildPokemonBonus,     // Wild Pokemon bonus
                NPCType.ElitePokemon => wildPokemonBonus * 1.5f,    // Elite wild Pokemon 
                NPCType.BossPokemon => bossMultiplier,       // Boss Pokemon multiplier
                NPCType.PlayerPokemon => wildPokemonBonus * 2f,   // Opponent player Pokemon
                _ => 1.0f
            };

            int totalCoins = Mathf.RoundToInt(baseCoinValue + levelBonus * typeMultiplier);
            return Mathf.Clamp(totalCoins, 1, 50); // Pokemon Unite caps at ~50 coins per kill
        }

        /// <summary>
        /// Enhanced coin drop system for NPCs when defeated
        /// </summary>
        public void DropCoins(Vector3 position, int defeatedLevel, NPCType npcType)
        {
            if (coinPrefab == null)
            {
                GameDebug.LogWarning(new GameDebugContext(
                    GameDebugCategory.GameLifecycle,
                    GameDebugSystemTag.UI,
                    GameDebugMechanicTag.Score,
                    subsystem: nameof(PokemonUniteCoinSystem)),
                    "Cannot drop coins: coinPrefab is null");
                return;
            }

            int totalCoinValue = CalculateCoinDrop(defeatedLevel, npcType);
            
            // Determine number of individual coin drops using configured range
            int coinCount = Random.Range(minCoinsDropped, maxCoinsDropped + 1);
            coinCount = Mathf.Min(coinCount, totalCoinValue); // Don't drop more coins than value
            
            int coinsPerDrop = Mathf.Max(1, totalCoinValue / coinCount);
            int remainderCoins = totalCoinValue % coinCount;

            for (int i = 0; i < coinCount; i++)
            {
                int thisCoinValue = coinsPerDrop + (i < remainderCoins ? 1 : 0);
                DropSingleCoin(position, thisCoinValue);
            }

            GameDebug.Log(new GameDebugContext(
                GameDebugCategory.GameLifecycle,
                GameDebugSystemTag.UI,
                GameDebugMechanicTag.Score,
                subsystem: nameof(PokemonUniteCoinSystem)),
                "Pokemon Unite coins dropped.",
                ("TotalValue", totalCoinValue),
                ("CoinCount", coinCount),
                ("NPCLevel", defeatedLevel),
                ("NPCType", npcType));
        }

        private void DropSingleCoin(Vector3 basePosition, int coinValue)
        {
            // Use configured scatter radius
            Vector3 randomOffset = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPosition = basePosition + new Vector3(randomOffset.x, 0.5f, randomOffset.y);
            
            var coinInstance = Instantiate(coinPrefab, spawnPosition, Quaternion.identity, transform);

            // Add physics launch using configured launch force
            if (coinInstance.TryGetComponent<Rigidbody>(out var rb))
            {
                Vector3 launchDirection = Vector3.up + new Vector3(randomOffset.x, 0f, randomOffset.y).normalized * 0.5f;
                rb.AddForce(launchDirection * launchForce, ForceMode.Impulse);
            }

            // Initialize coin with calculated value
            if (coinInstance.TryGetComponent<PokemonUniteCoinPickup>(out var coinPickup))
            {
                coinPickup.Initialize(coinValue);
            }
            else if (coinInstance.TryGetComponent<CoinPickup>(out var legacyCoin))
            {
                legacyCoin.Initialize(coinValue);
            }
        }
    }

    /// <summary>
    /// Pokemon Unite NPC types for coin calculation
    /// </summary>
    public enum NPCType
    {
        WildPokemon,    // Standard wild Pokemon (Aipom, Corphish, etc.)
        ElitePokemon,   // Elite wild Pokemon (Audino, Bouffalant, etc.)
        BossPokemon,    // Boss Pokemon (Drednaw, Zapdos, Rayquaza)
        PlayerPokemon   // Opponent player Pokemon
    }

    /// <summary>
    /// Enhanced coin pickup component with Pokemon Unite mechanics
    /// Supports larger coin values and goal zone interactions
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PokemonUniteCoinPickup : MonoBehaviour
    {
        [Header("Pokemon Unite Coin Settings")]
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float lifetimeSeconds = 15f;      // Pokemon Unite coins disappear after 15s
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 spinSpeed = new Vector3(0f, 180f, 0f); // Faster spin

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private float soundVolume = 0.7f;
        [SerializeField] private Color coinColor = Color.yellow;

        [Header("Pokemon Unite Features")]
        [SerializeField] private bool canBreakGoals = true;        // Coins can break goal zones
        [SerializeField] private float magnetRange = 1.5f;        // Auto-collect range
        [SerializeField] private float magnetSpeed = 8f;          // Speed when magnetizing to player
        [SerializeField] private bool enableDebugLogging = true;  // Enable debug output

        private bool collected;
        private Collider triggerCollider;
        private Renderer coinRenderer;
        private Transform targetPlayer;
        private bool magnetizing;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            coinRenderer = GetComponent<Renderer>();
            
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            // Set coin color
            if (coinRenderer != null && coinRenderer.material != null)
            {
                coinRenderer.material.color = coinColor;
            }

            if (lifetimeSeconds > 0f)
            {
                Destroy(gameObject, lifetimeSeconds);
            }
        }

        private void Update()
        {
            if (collected) return;

            // Pokemon Unite style visual spinning
            if (visualRoot != null && spinSpeed != Vector3.zero)
            {
                visualRoot.Rotate(spinSpeed * Time.deltaTime, Space.Self);
            }

            // Pokemon Unite magnetization to nearby players
            if (!magnetizing && targetPlayer == null)
            {
                FindNearbyPlayer();
            }

            if (magnetizing && targetPlayer != null)
            {
                MagnetizeToPlayer();
            }
        }

        public void Initialize(int value)
        {
            coinValue = Mathf.Max(1, value);
            
            // Larger coins for higher values (Pokemon Unite visual feedback)
            if (value >= 10)
            {
                transform.localScale *= 1.5f;
                coinColor = Color.cyan; // Blue for high-value coins
            }
            else if (value >= 5)
            {
                transform.localScale *= 1.2f;
                coinColor = new Color(1f, 0.8f, 0f); // Gold for medium coins
            }

            if (coinRenderer != null && coinRenderer.material != null)
            {
                coinRenderer.material.color = coinColor;
            }
        }

        private void FindNearbyPlayer()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, magnetRange);
            
            foreach (var collider in nearbyColliders)
            {
                var player = collider.GetComponentInParent<SimplePlayerController>();
                if (player != null)
                {
                    targetPlayer = player.transform;
                    magnetizing = true;
                    break;
                }
            }
        }

        private void MagnetizeToPlayer()
        {
            if (targetPlayer == null)
            {
                magnetizing = false;
                return;
            }

            Vector3 direction = (targetPlayer.position - transform.position).normalized;
            transform.position += direction * magnetSpeed * Time.deltaTime;

            // Check if close enough to collect
            if (Vector3.Distance(transform.position, targetPlayer.position) < 0.5f)
            {
                var player = targetPlayer.GetComponent<SimplePlayerController>();
                if (player != null)
                {
                    Collect(player);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            var player = other.GetComponentInParent<SimplePlayerController>();
            if (player != null)
            {
                Collect(player);
            }
        }

        private void Collect(SimplePlayerController collector)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            collected = true;
            triggerCollider.enabled = false;

            // Pokemon Unite style collection effects
            PlayEnhancedFeedback();
            AwardScore(collector);
            AddToPlayerInventory(collector);

            GameDebug.Log(new GameDebugContext(
                GameDebugCategory.GameLifecycle,
                GameDebugSystemTag.Player,
                GameDebugMechanicTag.Score,
                subsystem: nameof(PokemonUniteCoinPickup)),
                "Pokemon Unite coin collected.",
                ("CoinValue", coinValue),
                ("PlayerTeam", collector.TeamIndex));

            Destroy(gameObject);
        }

        private void AwardScore(SimplePlayerController collector)
        {
            var gameManager = SimpleGameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<SimpleGameManager>();
            }

            if (gameManager != null)
            {
                int teamToCredit = collector != null ? collector.TeamIndex : 0;
                if (!gameManager.AddScore(teamToCredit, coinValue))
                {
                    GameDebug.LogWarning(new GameDebugContext(
                        GameDebugCategory.GameLifecycle,
                        GameDebugSystemTag.GameLifecycle,
                        GameDebugMechanicTag.Score,
                        subsystem: nameof(PokemonUniteCoinPickup)),
                        "Failed to credit Pokemon Unite coin score.",
                        ("RequestedTeam", teamToCredit),
                        ("CoinValue", coinValue));
                }
            }
        }

        private void AddToPlayerInventory(SimplePlayerController collector)
        {
            // In Pokemon Unite, coins are stored in player inventory for goal scoring
            var inventory = collector.GetComponent<PlayerCoinInventory>();
            if (inventory != null)
            {
                inventory.AddCoins(coinValue);
                
                // Check if these coins can break goals (high-value coins from bosses)
                if (canBreakGoals && coinValue >= 10)
                {
                    inventory.SetCanBreakGoals(true);
                    
                    if (enableDebugLogging)
                    {
                        GameDebug.Log(new GameDebugContext(
                            GameDebugCategory.GameLifecycle,
                            GameDebugSystemTag.UI,
                            GameDebugMechanicTag.Score,
                            subsystem: nameof(PokemonUniteCoinPickup)),
                            "High-value coin enables goal breaking.",
                            ("CoinValue", coinValue),
                            ("PlayerName", collector.name));
                    }
                }
            }
        }

        private void PlayEnhancedFeedback()
        {
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                var main = effect.main;
                main.startColor = coinColor;
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw magnetization range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, magnetRange);
        }
    }

    /// <summary>
    /// Player inventory component for Pokemon Unite style coin storage
    /// Stores coins until deposited at goal zones
    /// </summary>
    public class PlayerCoinInventory : MonoBehaviour
    {
        [Header("Pokemon Unite Inventory")]
        [SerializeField] private int currentCoins;
        [SerializeField] private int maxCoins = 50;               // Pokemon Unite max carry capacity
        [SerializeField] private float coinDecayRate = 0.1f;      // Lose 10% coins per minute
        [SerializeField] private float decayInterval = 60f;       // Every minute
        [SerializeField] private bool canBreakGoals = false;       // Can break enemy goal zones

        private float lastDecayTime;

        public int CurrentCoins => currentCoins;
        public int MaxCoins => maxCoins;
        public float CoinPercentage => (float)currentCoins / maxCoins;
        public bool CanBreakGoals => canBreakGoals;

        private void Start()
        {
            lastDecayTime = Time.time;
        }

        private void Update()
        {
            // Pokemon Unite coin decay over time (encourages active scoring)
            if (Time.time >= lastDecayTime + decayInterval && currentCoins > 0)
            {
                int coinsToLose = Mathf.RoundToInt(currentCoins * coinDecayRate);
                coinsToLose = Mathf.Max(1, coinsToLose); // Always lose at least 1 coin
                
                RemoveCoins(coinsToLose);
                lastDecayTime = Time.time;

                GameDebug.Log(new GameDebugContext(
                    GameDebugCategory.GameLifecycle,
                    GameDebugSystemTag.Player,
                    GameDebugMechanicTag.Score,
                    subsystem: nameof(PlayerCoinInventory)),
                    "Coins decayed over time.",
                    ("CoinsLost", coinsToLose),
                    ("RemainingCoins", currentCoins));
            }
        }

        public bool AddCoins(int amount)
        {
            if (amount <= 0) return false;

            int coinsToAdd = Mathf.Min(amount, maxCoins - currentCoins);
            currentCoins += coinsToAdd;

            if (coinsToAdd < amount)
            {
                GameDebug.LogWarning(new GameDebugContext(
                    GameDebugCategory.GameLifecycle,
                    GameDebugSystemTag.Player,
                    GameDebugMechanicTag.Score,
                    subsystem: nameof(PlayerCoinInventory)),
                    "Coin inventory at capacity.",
                    ("RequestedCoins", amount),
                    ("CoinsAdded", coinsToAdd),
                    ("CurrentCoins", currentCoins));
            }

            return coinsToAdd > 0;
        }

        public bool RemoveCoins(int amount)
        {
            if (amount <= 0 || currentCoins < amount) return false;

            currentCoins -= amount;
            return true;
        }

        public int SpendAllCoins()
        {
            int coinsToSpend = currentCoins;
            currentCoins = 0;
            canBreakGoals = false; // Reset goal breaking ability when spending coins
            return coinsToSpend;
        }

        /// <summary>
        /// Set whether this player can break enemy goal zones
        /// </summary>
        public void SetCanBreakGoals(bool canBreak)
        {
            canBreakGoals = canBreak;
        }

        /// <summary>
        /// Called when player is defeated - lose percentage of coins
        /// </summary>
        public void OnPlayerDefeated()
        {
            if (currentCoins > 0)
            {
                int coinsLost = Mathf.RoundToInt(currentCoins * 0.5f); // Lose 50% on defeat
                coinsLost = Mathf.Max(1, coinsLost);
                
                RemoveCoins(coinsLost);

                GameDebug.Log(new GameDebugContext(
                    GameDebugCategory.GameLifecycle,
                    GameDebugSystemTag.Player,
                    GameDebugMechanicTag.Score,
                    subsystem: nameof(PlayerCoinInventory)),
                    "Lost coins on defeat.",
                    ("CoinsLost", coinsLost),
                    ("RemainingCoins", currentCoins));
            }
        }
    }
}