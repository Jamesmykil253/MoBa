using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace MOBA.UI
{
    /// <summary>
    /// Advanced UI system providing comprehensive UI feedback and management
    /// Implements Observer pattern for responsive UI updates and Clean Code principles
    /// Updated to use animation tracking properly
    /// </summary>
    public class AdvancedUISystem : MonoBehaviour
    {
        [Header("Main UI Panels")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject gameplayUI;
        [SerializeField] private GameObject performanceOverlay;
        [SerializeField] private GameObject combatFeedback;
        [SerializeField] private GameObject scoringInterface;
        
        [Header("Ability System UI")]
        [SerializeField] private Button ability1Button;
        [SerializeField] private Button ability2Button;
        [SerializeField] private Button ultimateButton;
        [SerializeField] private Image ability1Cooldown;
        [SerializeField] private Image ability2Cooldown;
        [SerializeField] private Image ultimateCooldown;
        [SerializeField] private GameObject holdToAimReticle;
        [SerializeField] private Slider aimAccuracyMeter;
        
        [Header("Crypto Coin System UI")]
        [SerializeField] private TextMeshProUGUI coinCountText;
        [SerializeField] private TextMeshProUGUI teamScoreText;
        [SerializeField] private Slider scoringProgressBar;
        [SerializeField] private GameObject scoringZoneIndicator;
        [SerializeField] private TextMeshProUGUI scoringTimeText;
        
        [Header("Combat Feedback UI")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Transform damageNumberParent;
        [SerializeField] private TextMeshProUGUI combatLogText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI rsbMultiplierText;
        
        [Header("Performance Monitoring UI")]
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI networkStatsText;
        [SerializeField] private TextMeshProUGUI memoryUsageText;
        [SerializeField] private GameObject performanceWarning;
        
        [Header("Audio Feedback")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip abilityReadySound;
        [SerializeField] private AudioClip scoringCompleteSound;
        [SerializeField] private AudioClip damageSound;
        
        // System references
        private AbilitySystem abilitySystem;
        private CryptoCoinSystem cryptoCoinSystem;
        private RSBCombatSystem rsbCombatSystem;
        private HoldToAimSystem holdToAimSystem;
        private MOBA.Networking.NetworkProfiler networkProfiler;
        
        // UI state tracking
        private List<GameObject> activeDamageNumbers = new List<GameObject>();
        private Queue<string> combatLogEntries = new Queue<string>();
        private const int maxCombatLogEntries = 10;
        
        // Performance tracking
        private float fpsUpdateTimer = 0f;
        private const float fpsUpdateInterval = 0.5f;
        private List<float> fpsHistory = new List<float>();
        
        // Animation state
        private bool isAnimatingCooldowns = false;
        private Dictionary<string, float> cooldownAnimationProgress = new Dictionary<string, float>();

        private void Awake()
        {
            InitializeUIComponents();
            CacheSystemReferences();
        }

        private void Start()
        {
            SetupUIEventHandlers();
            InitializeUIState();
        }

        private void Update()
        {
            UpdateAbilityUI();
            UpdateCryptoCoinUI();
            UpdateCombatFeedbackUI();
            UpdatePerformanceUI();
            UpdateHoldToAimUI();
        }

        /// <summary>
        /// Initialize all UI components with proper error handling
        /// </summary>
        private void InitializeUIComponents()
        {
            // Ensure main canvas is set up
            if (mainCanvas == null)
            {
                mainCanvas = FindAnyObjectByType<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError("[AdvancedUISystem] No Canvas found in scene!");
                    return;
                }
            }
            
            // Initialize audio source
            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
                if (uiAudioSource == null)
                {
                    uiAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Initialize damage number parent
            if (damageNumberParent == null && mainCanvas != null)
            {
                GameObject damageParent = new GameObject("DamageNumbers");
                damageParent.transform.SetParent(mainCanvas.transform);
                damageNumberParent = damageParent.transform;
            }
        }

        /// <summary>
        /// Cache references to all systems for performance
        /// </summary>
        private void CacheSystemReferences()
        {
            abilitySystem = FindAnyObjectByType<AbilitySystem>();
            cryptoCoinSystem = FindAnyObjectByType<CryptoCoinSystem>();
            rsbCombatSystem = FindAnyObjectByType<RSBCombatSystem>();
            holdToAimSystem = FindAnyObjectByType<HoldToAimSystem>();
            networkProfiler = FindAnyObjectByType<MOBA.Networking.NetworkProfiler>();
            
            if (abilitySystem == null) Debug.LogWarning("[AdvancedUISystem] AbilitySystem not found");
            if (cryptoCoinSystem == null) Debug.LogWarning("[AdvancedUISystem] CryptoCoinSystem not found");
            if (rsbCombatSystem == null) Debug.LogWarning("[AdvancedUISystem] RSBCombatSystem not found");
            if (holdToAimSystem == null) Debug.LogWarning("[AdvancedUISystem] HoldToAimSystem not found");
        }

        /// <summary>
        /// Set up event handlers for UI interactions
        /// </summary>
        private void SetupUIEventHandlers()
        {
            // Ability button handlers
            if (ability1Button != null)
            {
                ability1Button.onClick.AddListener(() => OnAbilityButtonClick("Ability1"));
            }
            
            if (ability2Button != null)
            {
                ability2Button.onClick.AddListener(() => OnAbilityButtonClick("Ability2"));
            }
            
            if (ultimateButton != null)
            {
                ultimateButton.onClick.AddListener(() => OnAbilityButtonClick("Ultimate"));
            }
            
            // Subscribe to system events
            if (cryptoCoinSystem != null)
            {
                CryptoCoinSystem.OnCoinPickup += OnCoinPickup;
                CryptoCoinSystem.OnCoinsScored += OnCoinsScored;
                CryptoCoinSystem.OnPlayerKill += OnPlayerKill;
            }
            
            if (rsbCombatSystem != null)
            {
                // Subscribe to combat events if available
                // This would need to be implemented in RSBCombatSystem as events
            }
        }

        /// <summary>
        /// Initialize UI state on startup
        /// </summary>
        private void InitializeUIState()
        {
            // Hide performance overlay by default
            if (performanceOverlay != null)
            {
                performanceOverlay.SetActive(false);
            }
            
            // Hide scoring progress bar initially
            if (scoringProgressBar != null)
            {
                scoringProgressBar.gameObject.SetActive(false);
            }
            
            // Hide hold-to-aim reticle initially
            if (holdToAimReticle != null)
            {
                holdToAimReticle.SetActive(false);
            }
            
            // Initialize cooldown images
            ResetCooldownImages();
        }

        /// <summary>
        /// Update ability system UI elements
        /// </summary>
        private void UpdateAbilityUI()
        {
            if (abilitySystem == null) return;
            
            // Update cooldown displays
            UpdateCooldownDisplay("Ability1", ability1Cooldown);
            UpdateCooldownDisplay("Ability2", ability2Cooldown);
            UpdateCooldownDisplay("Ultimate", ultimateCooldown);
            
            // Update button interactability
            if (ability1Button != null)
            {
                bool canCast = abilitySystem.GetRemainingCooldown("Ability1") <= 0;
                ability1Button.interactable = canCast;
                if (canCast && !cooldownAnimationProgress.ContainsKey("Ability1"))
                {
                    PlayAbilityReadySound();
                }
            }
        }

        /// <summary>
        /// Update crypto coin system UI elements
        /// </summary>
        private void UpdateCryptoCoinUI()
        {
            if (cryptoCoinSystem == null) return;
            
            // Update coin count
            if (coinCountText != null)
            {
                coinCountText.text = $"Coins: {cryptoCoinSystem.GetCarriedCoins()}";
            }
            
            // Update team score
            if (teamScoreText != null)
            {
                teamScoreText.text = $"Team Score: {CryptoCoinSystem.GetTeamScore()}";
            }
            
            // Update scoring progress
            if (cryptoCoinSystem.IsCurrentlyScoring())
            {
                if (scoringProgressBar != null && !scoringProgressBar.gameObject.activeSelf)
                {
                    scoringProgressBar.gameObject.SetActive(true);
                }
            }
            else
            {
                if (scoringProgressBar != null && scoringProgressBar.gameObject.activeSelf)
                {
                    scoringProgressBar.gameObject.SetActive(false);
                }
            }
            
            // Update scoring zone indicator
            if (scoringZoneIndicator != null)
            {
                scoringZoneIndicator.SetActive(cryptoCoinSystem.IsInScoringZone());
            }
        }

        /// <summary>
        /// Update combat feedback UI elements
        /// </summary>
        private void UpdateCombatFeedbackUI()
        {
            // Update RSB multiplier display
            if (rsbCombatSystem != null && rsbMultiplierText != null)
            {
                var analytics = rsbCombatSystem.GetCombatAnalytics();
                rsbMultiplierText.text = $"Avg Damage: {analytics.averageDamage:F1}";
            }
            
            // Update combat log
            UpdateCombatLog();
            
            // Cleanup old damage numbers
            CleanupDamageNumbers();
        }

        /// <summary>
        /// Update performance monitoring UI
        /// </summary>
        private void UpdatePerformanceUI()
        {
            fpsUpdateTimer += Time.deltaTime;
            if (fpsUpdateTimer >= fpsUpdateInterval)
            {
                UpdateFPSDisplay();
                UpdateNetworkStatsDisplay();
                UpdateMemoryUsageDisplay();
                fpsUpdateTimer = 0f;
            }
        }

        /// <summary>
        /// Update hold-to-aim UI elements
        /// </summary>
        private void UpdateHoldToAimUI()
        {
            if (holdToAimSystem == null) return;
            
            var aimInfo = holdToAimSystem.GetAimInfo();
            
            // Show/hide reticle
            if (holdToAimReticle != null)
            {
                holdToAimReticle.SetActive(aimInfo.isAiming);
            }
            
            // Update aim accuracy meter
            if (aimAccuracyMeter != null && aimInfo.isAiming)
            {
                aimAccuracyMeter.value = aimInfo.accuracy;
            }
        }

        /// <summary>
        /// Update individual cooldown display
        /// </summary>
        private void UpdateCooldownDisplay(string abilityName, Image cooldownImage)
        {
            if (cooldownImage == null || abilitySystem == null) return;
            
            float remainingCooldown = abilitySystem.GetRemainingCooldown(abilityName);
            float maxCooldown = abilitySystem.GetAbilityCooldown(abilityName);
            
            if (remainingCooldown > 0)
            {
                float fillAmount = remainingCooldown / maxCooldown;
                cooldownImage.fillAmount = fillAmount;
                cooldownImage.gameObject.SetActive(true);
                cooldownAnimationProgress[abilityName] = fillAmount;
                isAnimatingCooldowns = true;
            }
            else
            {
                cooldownImage.gameObject.SetActive(false);
                if (cooldownAnimationProgress.ContainsKey(abilityName))
                {
                    cooldownAnimationProgress.Remove(abilityName);
                }
                
                // Check if any cooldowns are still animating
                isAnimatingCooldowns = cooldownAnimationProgress.Count > 0;
            }
        }

        /// <summary>
        /// Show damage number at world position
        /// </summary>
        public void ShowDamageNumber(Vector3 worldPosition, float damage, bool isCritical = false)
        {
            if (damageNumberPrefab == null || damageNumberParent == null) return;
            
            GameObject damageNumber = Instantiate(damageNumberPrefab, damageNumberParent);
            var damageText = damageNumber.GetComponent<TextMeshProUGUI>();
            
            if (damageText != null)
            {
                damageText.text = damage.ToString("F0");
                damageText.color = isCritical ? Color.red : Color.white;
                damageText.fontSize = isCritical ? 36f : 24f;
            }
            
            // Convert world position to screen position
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            damageNumber.transform.position = screenPos;
            
            // Add to active list for cleanup
            activeDamageNumbers.Add(damageNumber);
            
            // Animate damage number
            StartCoroutine(AnimateDamageNumber(damageNumber));
            
            // Play damage sound
            if (uiAudioSource != null && damageSound != null)
            {
                uiAudioSource.PlayOneShot(damageSound);
            }
        }

        /// <summary>
        /// Animate damage number with fade and movement
        /// </summary>
        private System.Collections.IEnumerator AnimateDamageNumber(GameObject damageNumber)
        {
            float duration = 2f;
            float elapsed = 0f;
            Vector3 startPos = damageNumber.transform.position;
            Vector3 endPos = startPos + Vector3.up * 100f; // Move up 100 pixels
            
            var canvasGroup = damageNumber.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = damageNumber.AddComponent<CanvasGroup>();
            }
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Move up
                damageNumber.transform.position = Vector3.Lerp(startPos, endPos, progress);
                
                // Fade out
                canvasGroup.alpha = 1f - progress;
                
                yield return null;
            }
            
            // Remove from active list and destroy
            activeDamageNumbers.Remove(damageNumber);
            Destroy(damageNumber);
        }

        /// <summary>
        /// Add entry to combat log
        /// </summary>
        public void AddCombatLogEntry(string message)
        {
            combatLogEntries.Enqueue(message);
            
            if (combatLogEntries.Count > maxCombatLogEntries)
            {
                combatLogEntries.Dequeue();
            }
        }

        /// <summary>
        /// Update combat log display
        /// </summary>
        private void UpdateCombatLog()
        {
            if (combatLogText == null) return;
            
            combatLogText.text = string.Join("\n", combatLogEntries);
        }

        /// <summary>
        /// Update FPS display
        /// </summary>
        private void UpdateFPSDisplay()
        {
            if (fpsText == null) return;
            
            float currentFps = 1f / Time.deltaTime;
            fpsHistory.Add(currentFps);
            
            if (fpsHistory.Count > 10)
            {
                fpsHistory.RemoveAt(0);
            }
            
            float avgFps = 0f;
            foreach (float fps in fpsHistory)
            {
                avgFps += fps;
            }
            avgFps /= fpsHistory.Count;
            
            fpsText.text = $"FPS: {avgFps:F1}";
            fpsText.color = avgFps < 30f ? Color.red : (avgFps < 60f ? Color.yellow : Color.green);
        }

        /// <summary>
        /// Update network statistics display
        /// </summary>
        private void UpdateNetworkStatsDisplay()
        {
            if (networkStatsText == null || networkProfiler == null) return;
            
            var stats = networkProfiler.GetCurrentStats();
            networkStatsText.text = $"Clients: {stats.connectedClients}\n" +
                                   $"Latency: {stats.averageLatency:F1}ms\n" +
                                   $"Bytes/s: {stats.bytesSentPerSecond}";
        }

        /// <summary>
        /// Update memory usage display
        /// </summary>
        private void UpdateMemoryUsageDisplay()
        {
            if (memoryUsageText == null) return;
            
            float memoryMB = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);
            memoryUsageText.text = $"Memory: {memoryMB:F1} MB";
            memoryUsageText.color = memoryMB > 400f ? Color.red : (memoryMB > 200f ? Color.yellow : Color.green);
        }

        /// <summary>
        /// Event handlers
        /// </summary>
        private void OnAbilityButtonClick(string abilityName)
        {
            PlayButtonClickSound();
            AddCombatLogEntry($"{abilityName} activated");
        }

        private void OnCoinPickup(int coins)
        {
            AddCombatLogEntry($"Picked up {coins} coins");
        }

        private void OnCoinsScored(int playerId, int coins)
        {
            AddCombatLogEntry($"Scored {coins} coins!");
            if (uiAudioSource != null && scoringCompleteSound != null)
            {
                uiAudioSource.PlayOneShot(scoringCompleteSound);
            }
        }

        private void OnPlayerKill(int coins)
        {
            AddCombatLogEntry($"Player eliminated! +{coins} coins");
        }

        /// <summary>
        /// Audio feedback methods
        /// </summary>
        private void PlayButtonClickSound()
        {
            if (uiAudioSource != null && buttonClickSound != null)
            {
                uiAudioSource.PlayOneShot(buttonClickSound);
            }
        }

        private void PlayAbilityReadySound()
        {
            if (uiAudioSource != null && abilityReadySound != null)
            {
                uiAudioSource.PlayOneShot(abilityReadySound);
            }
        }

        /// <summary>
        /// Utility methods
        /// </summary>
        private void ResetCooldownImages()
        {
            if (ability1Cooldown != null) ability1Cooldown.fillAmount = 0f;
            if (ability2Cooldown != null) ability2Cooldown.fillAmount = 0f;
            if (ultimateCooldown != null) ultimateCooldown.fillAmount = 0f;
        }

        private void CleanupDamageNumbers()
        {
            activeDamageNumbers.RemoveAll(item => item == null);
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public void TogglePerformanceOverlay()
        {
            if (performanceOverlay != null)
            {
                performanceOverlay.SetActive(!performanceOverlay.activeSelf);
            }
        }

        public void ShowPerformanceWarning(string message)
        {
            if (performanceWarning != null)
            {
                performanceWarning.SetActive(true);
                // Set warning message if text component exists
                var warningText = performanceWarning.GetComponentInChildren<TextMeshProUGUI>();
                if (warningText != null)
                {
                    warningText.text = message;
                }
            }
        }

        public void HidePerformanceWarning()
        {
            if (performanceWarning != null)
            {
                performanceWarning.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (cryptoCoinSystem != null)
            {
                CryptoCoinSystem.OnCoinPickup -= OnCoinPickup;
                CryptoCoinSystem.OnCoinsScored -= OnCoinsScored;
                CryptoCoinSystem.OnPlayerKill -= OnPlayerKill;
            }
        }
    }
}
