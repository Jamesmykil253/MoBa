using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace MOBA.UI
{
    /// <summary>
    /// Simplified Ability evolution UI for Pokemon Unite-style ability upgrades
    /// Provides dynamic ability point allocation without external animation dependencies
    /// Reference: Pokemon Unite ability system design patterns
    /// </summary>
    public class AbilityEvolutionUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private GameObject menuContainer;
        [SerializeField] private RectTransform mainContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text abilityPointsText;
        [SerializeField] private Text selectedAbilityText;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject abilitySlotPrefab;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip menuOpenSound;
        [SerializeField] private AudioClip menuCloseSound;
        [SerializeField] private AudioClip slotSelectSound;
        [SerializeField] private AudioClip abilityUnlockSound;
        [SerializeField] private AudioClip invalidSelectionSound;
        
        [Header("Animation Settings")]
        [SerializeField] private float slotAnimationDuration = 0.3f;
        #pragma warning disable 0414 // Field assigned but never used - reserved for future line animation
        [SerializeField] private float lineDrawSpeed = 1f;
        #pragma warning restore 0414
        
        [Header("Evolution Settings")]
        [SerializeField] private int maxAbilityLevel = 15;
        [SerializeField] private int[] abilityUnlockLevels = { 1, 3, 5, 7, 9, 11, 13, 15 };
        
        #endregion
        
        #region Private Fields
        
        // UI Management
        private List<AbilitySlot> abilitySlots = new List<AbilitySlot>();
        private Dictionary<string, AbilityData> availableAbilities = new Dictionary<string, AbilityData>();
        private AbilitySlot selectedSlot;
        
        // State Management
        private bool isInitialized = false;
        private int currentPlayerLevel = 1;
        private int availableAbilityPoints = 0;
        private int selectedSlotIndex = -1;
        private bool isMenuOpen = false;
        
        // Animation state
        private Coroutine openAnimationCoroutine;
        private List<GameObject> evolutionLines = new List<GameObject>();
        
        #endregion
        
        #region Events
        
        public System.Action<string> OnAbilitySelected;
        public System.Action<string, int> OnAbilityEvolved;
        public System.Action OnMenuOpened;
        public System.Action OnMenuClosed;
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public class AbilityData
        {
            public string abilityId;
            public string displayName;
            public string description;
            public Sprite icon;
            public AbilityType type;
            public int baseCost;
            public int maxLevel;
            public int currentLevel;
            public bool isUnlocked;
            public Vector2 gridPosition;
            public List<string> prerequisiteIds;
            public List<string> nextTierIds;
            
            public bool CanUpgrade(int availablePoints)
            {
                return isUnlocked && currentLevel < maxLevel && availablePoints >= baseCost;
            }
            
            public bool CanUnlock(Dictionary<string, AbilityData> allAbilities)
            {
                if (isUnlocked) return false;
                
                foreach (var prereqId in prerequisiteIds)
                {
                    if (!allAbilities.ContainsKey(prereqId) || !allAbilities[prereqId].isUnlocked)
                        return false;
                }
                
                return true;
            }
        }
        
        public enum AbilityType
        {
            Basic,
            Special,
            Ultimate,
            Passive
        }
        
        public class AbilitySlot : MonoBehaviour
        {
            [Header("UI Components")]
            public Image iconImage;
            public Image borderImage;
            public Image backgroundImage;
            public Image glowImage;
            public Text levelText;
            public Button button;
            
            [Header("Visual Settings")]
            public Color normalColor = Color.white;
            public Color highlightColor = Color.yellow;
            public Color selectedColor = Color.green;
            public Color disabledColor = Color.gray;
            public Color unlockedColor = Color.cyan;
            
            // Data
            public AbilityData abilityData;
            public bool isSelected;
            public bool isInteractable = true;
            
            private Vector3 originalScale;
            private Color originalBorderColor;
            
            private void Awake()
            {
                originalScale = transform.localScale;
                if (borderImage != null)
                    originalBorderColor = borderImage.color;
            }
            
            public void Initialize(AbilityData data)
            {
                abilityData = data;
                UpdateVisuals();
                
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(OnSlotClicked);
                }
            }
            
            public void UpdateVisuals()
            {
                if (abilityData == null) return;
                
                // Update icon
                if (iconImage != null && abilityData.icon != null)
                    iconImage.sprite = abilityData.icon;
                
                // Update level text
                if (levelText != null)
                    levelText.text = abilityData.currentLevel.ToString();
                
                // Update colors based on state
                Color targetColor = normalColor;
                
                if (!abilityData.isUnlocked)
                    targetColor = disabledColor;
                else if (isSelected)
                    targetColor = selectedColor;
                else if (abilityData.currentLevel > 0)
                    targetColor = unlockedColor;
                
                if (iconImage != null)
                    iconImage.color = targetColor;
                
                // Update border
                if (borderImage != null)
                {
                    borderImage.color = isSelected ? highlightColor : originalBorderColor;
                }
                
                // Update glow effect
                if (glowImage != null)
                {
                    glowImage.gameObject.SetActive(abilityData.isUnlocked && abilityData.currentLevel > 0);
                }
            }
            
            public void SetSelected(bool selected)
            {
                isSelected = selected;
                UpdateVisuals();
                
                if (selected)
                {
                    // Simple scale animation
                    StartCoroutine(AnimateScale(originalScale * 1.1f, 0.2f));
                }
                else
                {
                    StartCoroutine(AnimateScale(originalScale, 0.2f));
                }
            }
            
            public void ShowUpgradeEffect()
            {
                // Simple punch effect
                StartCoroutine(AnimatePunch());
            }
            
            public void ShowInvalidEffect()
            {
                // Simple shake effect
                StartCoroutine(AnimateShake());
            }
            
            private void OnSlotClicked()
            {
                var evolutionUI = FindFirstObjectByType<AbilityEvolutionUI>();
                evolutionUI?.SelectSlot(this);
            }
            
            private System.Collections.IEnumerator AnimateScale(Vector3 targetScale, float duration)
            {
                Vector3 startScale = transform.localScale;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                    yield return null;
                }
                
                transform.localScale = targetScale;
            }
            
            private System.Collections.IEnumerator AnimatePunch()
            {
                Vector3 originalScale = transform.localScale;
                Vector3 punchScale = originalScale * 1.2f;
                
                // Scale up
                yield return StartCoroutine(AnimateScale(punchScale, 0.1f));
                // Scale back
                yield return StartCoroutine(AnimateScale(originalScale, 0.2f));
            }
            
            private System.Collections.IEnumerator AnimateShake()
            {
                Vector3 originalPosition = transform.localPosition;
                float duration = 0.3f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    float x = Random.Range(-5f, 5f);
                    float y = Random.Range(-5f, 5f);
                    transform.localPosition = originalPosition + new Vector3(x, y, 0);
                    
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                transform.localPosition = originalPosition;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeAbilitySystem();
        }
        
        private void OnEnable()
        {
            SubscribeToInputEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromInputEvents();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Validate required components
            if (menuContainer == null)
            {
                Debug.LogError("[AbilityEvolutionUI] Menu container is not assigned!");
                return;
            }
            
            if (mainContainer == null)
                mainContainer = menuContainer.GetComponent<RectTransform>();
            
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseMenu);
            }
            
            // Initially hide the menu
            menuContainer.SetActive(false);
        }
        
        private void InitializeAbilitySystem()
        {
            LoadAbilityData();
            CreateAbilitySlots();
            UpdateAbilityPoints();
            
            isInitialized = true;
            Debug.Log("[AbilityEvolutionUI] Ability system initialized successfully");
        }
        
        private void LoadAbilityData()
        {
            // Example ability data - in a real game this would come from ScriptableObjects or JSON
            var basicAttack = new AbilityData
            {
                abilityId = "basic_attack",
                displayName = "Basic Attack",
                description = "Enhanced basic attacks",
                type = AbilityType.Basic,
                baseCost = 1,
                maxLevel = 3,
                currentLevel = 0,
                isUnlocked = true,
                gridPosition = new Vector2(0, 0),
                prerequisiteIds = new List<string>(),
                nextTierIds = new List<string> { "enhanced_attack" }
            };
            
            var specialAbility = new AbilityData
            {
                abilityId = "special_ability",
                displayName = "Special Ability",
                description = "Powerful special attack",
                type = AbilityType.Special,
                baseCost = 2,
                maxLevel = 5,
                currentLevel = 0,
                isUnlocked = false,
                gridPosition = new Vector2(1, 0),
                prerequisiteIds = new List<string> { "basic_attack" },
                nextTierIds = new List<string> { "ultimate_ability" }
            };
            
            var ultimateAbility = new AbilityData
            {
                abilityId = "ultimate_ability",
                displayName = "Ultimate",
                description = "Devastating ultimate ability",
                type = AbilityType.Ultimate,
                baseCost = 3,
                maxLevel = 3,
                currentLevel = 0,
                isUnlocked = false,
                gridPosition = new Vector2(2, 0),
                prerequisiteIds = new List<string> { "special_ability" },
                nextTierIds = new List<string>()
            };
            
            availableAbilities[basicAttack.abilityId] = basicAttack;
            availableAbilities[specialAbility.abilityId] = specialAbility;
            availableAbilities[ultimateAbility.abilityId] = ultimateAbility;
        }
        
        private void CreateAbilitySlots()
        {
            if (slotContainer == null || abilitySlotPrefab == null)
            {
                Debug.LogError("[AbilityEvolutionUI] Slot container or prefab not assigned!");
                return;
            }
            
            // Clear existing slots
            foreach (var slot in abilitySlots)
            {
                if (slot != null)
                    DestroyImmediate(slot.gameObject);
            }
            abilitySlots.Clear();
            
            // Create new slots
            foreach (var abilityData in availableAbilities.Values)
            {
                GameObject slotObject = Instantiate(abilitySlotPrefab, slotContainer);
                var slot = slotObject.GetComponent<AbilitySlot>();
                
                if (slot == null)
                    slot = slotObject.AddComponent<AbilitySlot>();
                
                slot.Initialize(abilityData);
                abilitySlots.Add(slot);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public void OpenMenu()
        {
            if (!isInitialized || isMenuOpen) return;
            
            isMenuOpen = true;
            menuContainer.SetActive(true);
            
            // Play sound
            PlaySound(menuOpenSound);
            
            // Animate opening
            AnimateMenuOpen();
            
            // Update UI
            RefreshAbilitySlots();
            UpdateAbilityPoints();
            
            OnMenuOpened?.Invoke();
        }
        
        public void CloseMenu()
        {
            if (!isMenuOpen) return;
            
            isMenuOpen = false;
            
            // Play sound
            PlaySound(menuCloseSound);
            
            // Animate closing
            AnimateMenuClose();
            
            OnMenuClosed?.Invoke();
        }
        
        public void SetPlayerLevel(int level)
        {
            currentPlayerLevel = level;
            CalculateAbilityPoints();
            RefreshAbilitySlots();
        }
        
        public void AddAbilityPoints(int points)
        {
            availableAbilityPoints += points;
            UpdateAbilityPoints();
        }
        
        public bool TryUpgradeAbility(string abilityId)
        {
            if (!availableAbilities.ContainsKey(abilityId))
                return false;
            
            var ability = availableAbilities[abilityId];
            
            if (!ability.CanUpgrade(availableAbilityPoints))
                return false;
            
            // Upgrade ability
            ability.currentLevel++;
            availableAbilityPoints -= ability.baseCost;
            
            // Unlock next tier abilities
            UnlockNextTierAbilities(ability);
            
            // Update UI
            RefreshAbilitySlots();
            UpdateAbilityPoints();
            
            // Play sound and effects
            PlaySound(abilityUnlockSound);
            
            // Fire event
            OnAbilityEvolved?.Invoke(abilityId, ability.currentLevel);
            
            return true;
        }
        
        #endregion
        
        #region Private Methods
        
        private void SubscribeToInputEvents()
        {
            // Input system events would go here
        }
        
        private void UnsubscribeFromInputEvents()
        {
            // Input system cleanup would go here
        }
        
        private void CalculateAbilityPoints()
        {
            // Calculate available points based on level
            availableAbilityPoints = Mathf.Max(0, currentPlayerLevel - 1);
        }
        
        private void UpdateAbilityPoints()
        {
            if (abilityPointsText != null)
                abilityPointsText.text = $"Ability Points: {availableAbilityPoints}";
        }
        
        private void RefreshAbilitySlots()
        {
            foreach (var slot in abilitySlots)
            {
                slot.UpdateVisuals();
            }
        }
        
        private void UnlockNextTierAbilities(AbilityData upgradedAbility)
        {
            foreach (var nextTierId in upgradedAbility.nextTierIds)
            {
                if (availableAbilities.ContainsKey(nextTierId))
                {
                    var nextAbility = availableAbilities[nextTierId];
                    if (nextAbility.CanUnlock(availableAbilities))
                    {
                        nextAbility.isUnlocked = true;
                    }
                }
            }
        }
        
        public void SelectSlot(AbilitySlot slot)
        {
            if (selectedSlot != null)
                selectedSlot.SetSelected(false);
            
            selectedSlot = slot;
            selectedSlotIndex = abilitySlots.IndexOf(slot);
            
            if (slot != null)
            {
                slot.SetSelected(true);
                PlaySound(slotSelectSound);
                
                // Update description
                if (selectedAbilityText != null && slot.abilityData != null)
                    selectedAbilityText.text = slot.abilityData.description;
                
                OnAbilitySelected?.Invoke(slot.abilityData.abilityId);
            }
        }
        
        public void ConfirmUpgrade()
        {
            if (selectedSlot == null || selectedSlot.abilityData == null)
                return;
            
            string abilityId = selectedSlot.abilityData.abilityId;
            
            if (TryUpgradeAbility(abilityId))
            {
                selectedSlot.ShowUpgradeEffect();
            }
            else
            {
                selectedSlot.ShowInvalidEffect();
                PlaySound(invalidSelectionSound);
            }
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                var audioManager = MOBA.Audio.AudioManager.Instance;
                audioManager?.PlaySFX(clip.name);
            }
        }
        
        private void AnimateMenuOpen()
        {
            if (openAnimationCoroutine != null)
                StopCoroutine(openAnimationCoroutine);
            
            openAnimationCoroutine = StartCoroutine(AnimateMenuOpenCoroutine());
        }
        
        private System.Collections.IEnumerator AnimateMenuOpenCoroutine()
        {
            mainContainer.localScale = Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < slotAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slotAnimationDuration;
                t = t * t * (3f * t - 2f); // Ease out
                mainContainer.localScale = Vector3.one * t;
                yield return null;
            }
            
            mainContainer.localScale = Vector3.one;
        }
        
        private void AnimateMenuClose()
        {
            if (openAnimationCoroutine != null)
                StopCoroutine(openAnimationCoroutine);
            
            openAnimationCoroutine = StartCoroutine(AnimateMenuCloseCoroutine());
        }
        
        private System.Collections.IEnumerator AnimateMenuCloseCoroutine()
        {
            float elapsed = 0f;
            Vector3 startScale = mainContainer.localScale;
            
            while (elapsed < slotAnimationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (slotAnimationDuration * 0.5f);
                mainContainer.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            
            mainContainer.localScale = Vector3.zero;
            menuContainer.SetActive(false);
        }
        
        #endregion
        
        #region Debug and Utilities
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            if (maxAbilityLevel <= 0)
                maxAbilityLevel = 15;
            
            if (abilityUnlockLevels == null || abilityUnlockLevels.Length == 0)
                abilityUnlockLevels = new int[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        }
        
        #endregion
    }
}