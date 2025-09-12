using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace MOBA
{
    /// <summary>
    /// Crypto coin economy system implementing the mechanics specified in GAMEPLAY.md
    /// Handles coin generation, collection, scoring, and team synergy bonuses
    /// Based on Clean Code principles and Game Programming Patterns (Observer Pattern)
    /// </summary>
    public class CryptoCoinSystem : MonoBehaviour
    {
        [Header("Coin Configuration")]
        [SerializeField] private float pickupRange = 2f; // 2 unit radius per spec
        [SerializeField] private float baseScoreTime = 0.5f; // Base scoring time per spec
        [SerializeField] private float coinScoreMultiplier = 0.05f; // 0.05 seconds per coin per spec
        [SerializeField] private float teamSynergyReduction = 0.15f; // 15% reduction per ally per spec
        [SerializeField] private float maxTeamSynergyReduction = 0.75f; // 75% max reduction
        
        [Header("Coin Generation Settings")]
        [SerializeField] private int npcKillCoins = 7; // 5-10 coins per spec (using middle value)
        [SerializeField] private int playerKillBaseCoins = 37; // 25-50 coins per spec (using middle value)
        [SerializeField] private int assistBonusCoins = 15; // 15 coins per spec
        [SerializeField] private float assistTimeWindow = 3f; // 3 seconds per spec
        [SerializeField] private float deathCoinDropPercentage = 0.5f; // 50% per spec
        
        [Header("UI Elements")]
        [SerializeField] private Slider scoringProgressBar;
        [SerializeField] private Text coinCountText;
        [SerializeField] private Text teamScoreText;
        
        [Header("Audio")]
        [SerializeField] private AudioClip coinPickupSound;
        [SerializeField] private AudioClip scoringCompleteSound;
        [SerializeField] private AudioSource audioSource;

        // Player state tracking
        private int carriedCoins = 0;
        private bool isScoring = false;
        private float scoringStartTime;
        private float currentScoringDuration;
        private List<Transform> alliesInScoringRange = new List<Transform>();
        
        // Assist tracking
        private float lastDamageTime = 0f;
        
        // Team state
        private static int teamScore = 0;
        private static Dictionary<int, int> playerScores = new Dictionary<int, int>();
        
        // Scoring zone detection
        private List<Collider> nearbyScoreZones = new List<Collider>();
        
        // Events for Observer Pattern implementation
        public static event System.Action<int> OnCoinPickup; // coins picked up
        public static event System.Action<int, int> OnCoinsScored; // player ID, coins scored
        public static event System.Action<int> OnPlayerKill; // coins earned from kill
        public static event System.Action<Transform, int> OnCoinDrop; // location, coin count

        private void Awake()
        {
            // Initialize audio source if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Initialize player score tracking
            int playerId = GetInstanceID();
            if (!playerScores.ContainsKey(playerId))
            {
                playerScores[playerId] = 0;
            }
        }

        private void Update()
        {
            // Update UI
            UpdateUI();
            
            // Handle coin collection
            CheckForNearbyCoins();
            
            // Handle scoring zone detection
            CheckForScoringZones();
            
            // Update scoring progress
            if (isScoring)
            {
                UpdateScoringProgress();
            }
        }

        /// <summary>
        /// Start scoring coins using Left Alt interaction
        /// Implements the scoring formula from GAMEPLAY.md
        /// </summary>
        public void StartScoring()
        {
            if (carriedCoins <= 0)
            {
                Debug.LogWarning("[CryptoCoinSystem] No coins to score");
                return;
            }
            
            if (nearbyScoreZones.Count == 0)
            {
                Debug.LogWarning("[CryptoCoinSystem] Not in scoring zone");
                return;
            }
            
            if (isScoring)
            {
                Debug.LogWarning("[CryptoCoinSystem] Already scoring");
                return;
            }

            // Calculate scoring time with team synergy bonus
            CalculateScoringDuration();
            
            isScoring = true;
            scoringStartTime = Time.time;
            
            // Show progress bar
            if (scoringProgressBar != null)
            {
                scoringProgressBar.gameObject.SetActive(true);
                scoringProgressBar.value = 0f;
            }
            
            Debug.Log($"[CryptoCoinSystem] Started scoring {carriedCoins} coins (Duration: {currentScoringDuration:F2}s)");
        }

        /// <summary>
        /// Stop scoring (if interrupted or canceled)
        /// </summary>
        public void StopScoring()
        {
            if (!isScoring) return;
            
            isScoring = false;
            
            // Hide progress bar
            if (scoringProgressBar != null)
            {
                scoringProgressBar.gameObject.SetActive(false);
            }
            
            Debug.Log("[CryptoCoinSystem] Scoring interrupted");
        }

        /// <summary>
        /// Handle coin pickup from environment
        /// Implements the pickup mechanics with audio feedback
        /// </summary>
        public void PickupCoins(int coinCount)
        {
            carriedCoins += coinCount;
            
            // Play pickup sound with satisfying audio cue per spec
            if (audioSource != null && coinPickupSound != null)
            {
                audioSource.PlayOneShot(coinPickupSound);
            }
            
            // Fire pickup event
            OnCoinPickup?.Invoke(coinCount);
            
            Debug.Log($"[CryptoCoinSystem] Picked up {coinCount} coins. Total carried: {carriedCoins}");
        }

        /// <summary>
        /// Handle player death - drop percentage of carried coins
        /// </summary>
        public void HandleDeath()
        {
            if (carriedCoins <= 0) return;
            
            int coinsDropped = Mathf.FloorToInt(carriedCoins * deathCoinDropPercentage);
            carriedCoins -= coinsDropped;
            
            // Stop any active scoring
            if (isScoring)
            {
                StopScoring();
            }
            
            // Fire coin drop event
            OnCoinDrop?.Invoke(transform, coinsDropped);
            
            Debug.Log($"[CryptoCoinSystem] Dropped {coinsDropped} coins on death. Remaining: {carriedCoins}");
        }

        /// <summary>
        /// Handle NPC kill - award coins
        /// </summary>
        public void HandleNPCKill()
        {
            PickupCoins(npcKillCoins);
            Debug.Log($"[CryptoCoinSystem] Earned {npcKillCoins} coins from NPC kill");
        }

        /// <summary>
        /// Handle player kill - award coins based on target's carried coins
        /// </summary>
        public void HandlePlayerKill(CryptoCoinSystem targetCoinSystem)
        {
            int baseCoins = playerKillBaseCoins;
            int bonusCoins = targetCoinSystem != null ? Mathf.FloorToInt(targetCoinSystem.carriedCoins * 0.5f) : 0;
            int totalCoins = baseCoins + bonusCoins;
            
            PickupCoins(totalCoins);
            OnPlayerKill?.Invoke(totalCoins);
            
            Debug.Log($"[CryptoCoinSystem] Earned {totalCoins} coins from player kill ({baseCoins} base + {bonusCoins} bonus)");
        }

        /// <summary>
        /// Track damage dealt for assist calculations
        /// </summary>
        public void TrackDamageDealt()
        {
            lastDamageTime = Time.time;
        }

        /// <summary>
        /// Handle assist - award coins for helping with kill
        /// Only awards if damage was dealt within the assist time window
        /// </summary>
        public void HandleAssist()
        {
            // Check if this player is eligible for assist (dealt damage within time window)
            if (Time.time - lastDamageTime <= assistTimeWindow)
            {
                PickupCoins(assistBonusCoins);
                Debug.Log($"[CryptoCoinSystem] Earned {assistBonusCoins} coins from assist (damage dealt {Time.time - lastDamageTime:F1}s ago)");
            }
            else
            {
                Debug.Log($"[CryptoCoinSystem] No assist awarded - damage too old ({Time.time - lastDamageTime:F1}s ago, window: {assistTimeWindow}s)");
            }
        }

        /// <summary>
        /// Calculate scoring duration with team synergy bonus
        /// Implements the formula from GAMEPLAY.md
        /// </summary>
        private void CalculateScoringDuration()
        {
            // Base formula: baseScoreTime = 0.5 + (carriedCoins × 0.05) seconds
            float baseTime = baseScoreTime + (carriedCoins * coinScoreMultiplier);
            
            // Team synergy: teamSynergyMultiplier = 1.0 - (alliesInRange × 0.15)
            int alliesInRange = CountAlliesInScoringRange();
            float synergyReduction = Mathf.Min(alliesInRange * teamSynergyReduction, maxTeamSynergyReduction);
            float synergyMultiplier = 1f - synergyReduction;
            
            // Final formula: finalScoreTime = baseScoreTime × teamSynergyMultiplier
            currentScoringDuration = baseTime * synergyMultiplier;
            
            Debug.Log($"[CryptoCoinSystem] Scoring calculation: Base={baseTime:F2}s, Allies={alliesInRange}, Synergy={synergyMultiplier:F2}, Final={currentScoringDuration:F2}s");
        }

        /// <summary>
        /// Count allies within scoring range for team synergy bonus
        /// </summary>
        private int CountAlliesInScoringRange()
        {
            alliesInScoringRange.Clear();
            
            // Find all player controllers in range (excluding self)
            var allPlayers = FindObjectsByType<UnifiedPlayerController>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                if (player.transform == transform) continue; // Skip self
                
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= pickupRange * 2f) // Double pickup range for team synergy
                {
                    alliesInScoringRange.Add(player.transform);
                }
            }
            
            return alliesInScoringRange.Count;
        }

        /// <summary>
        /// Update scoring progress and complete when finished
        /// </summary>
        private void UpdateScoringProgress()
        {
            float elapsedTime = Time.time - scoringStartTime;
            float progress = elapsedTime / currentScoringDuration;
            
            // Update progress bar
            if (scoringProgressBar != null)
            {
                scoringProgressBar.value = progress;
            }
            
            // Check for completion
            if (progress >= 1f)
            {
                CompleteCoinScoring();
            }
        }

        /// <summary>
        /// Complete coin scoring and transfer to team score
        /// </summary>
        private void CompleteCoinScoring()
        {
            int coinsToScore = carriedCoins;
            carriedCoins = 0;
            
            // Add to team score
            teamScore += coinsToScore;
            
            // Add to player score
            int playerId = GetInstanceID();
            if (playerScores.ContainsKey(playerId))
            {
                playerScores[playerId] += coinsToScore;
            }
            
            isScoring = false;
            
            // Hide progress bar
            if (scoringProgressBar != null)
            {
                scoringProgressBar.gameObject.SetActive(false);
            }
            
            // Play completion sound
            if (audioSource != null && scoringCompleteSound != null)
            {
                audioSource.PlayOneShot(scoringCompleteSound);
            }
            
            // Fire scoring event
            OnCoinsScored?.Invoke(playerId, coinsToScore);
            
            Debug.Log($"[CryptoCoinSystem] Scored {coinsToScore} coins! Team total: {teamScore}");
        }

        /// <summary>
        /// Check for nearby coins to collect automatically
        /// </summary>
        private void CheckForNearbyCoins()
        {
            // This would typically check for coin GameObjects in range
            // For now, this is a placeholder for when coin pickups are spawned
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, pickupRange, LayerMask.GetMask("Coin"));
            
            foreach (var collider in nearbyColliders)
            {
                var coinPickup = collider.GetComponent<CoinPickup>();
                if (coinPickup != null)
                {
                    PickupCoins(coinPickup.CoinValue);
                    Destroy(collider.gameObject);
                }
            }
        }

        /// <summary>
        /// Check for nearby scoring zones
        /// </summary>
        private void CheckForScoringZones()
        {
            nearbyScoreZones.Clear();
            Collider[] scoreZones = Physics.OverlapSphere(transform.position, pickupRange, LayerMask.GetMask("ScoreZone"));
            
            foreach (var zone in scoreZones)
            {
                nearbyScoreZones.Add(zone);
            }
        }

        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (coinCountText != null)
            {
                coinCountText.text = $"Coins: {carriedCoins}";
            }
            
            if (teamScoreText != null)
            {
                teamScoreText.text = $"Team Score: {teamScore}";
            }
        }

        /// <summary>
        /// Get current carried coins (for external systems)
        /// </summary>
        public int GetCarriedCoins() => carriedCoins;

        /// <summary>
        /// Get team score (static access)
        /// </summary>
        public static int GetTeamScore() => teamScore;

        /// <summary>
        /// Get player score by ID
        /// </summary>
        public static int GetPlayerScore(int playerId)
        {
            return playerScores.TryGetValue(playerId, out int score) ? score : 0;
        }

        /// <summary>
        /// Check if player is currently in a scoring zone
        /// </summary>
        public bool IsInScoringZone() => nearbyScoreZones.Count > 0;

        /// <summary>
        /// Check if player is currently scoring
        /// </summary>
        public bool IsCurrentlyScoring() => isScoring;
    }

    /// <summary>
    /// Simple coin pickup component for environmental coins
    /// </summary>
    public class CoinPickup : MonoBehaviour
    {
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float spinSpeed = 90f;
        
        public int CoinValue => coinValue;
        
        private void Update()
        {
            // Spin animation for visual feedback as specified
            transform.Rotate(0, spinSpeed * Time.deltaTime, 0);
        }
    }
}
