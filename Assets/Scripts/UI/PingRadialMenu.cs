using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace MOBA.UI
{
    /// <summary>
    /// Simplified radial ping menu for quick communication in MOBA games
    /// Provides context-sensitive ping options without external animation dependencies
    /// Reference: League of Legends ping system design patterns
    /// </summary>
    public class PingRadialMenu : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private GameObject menuContainer;
        [SerializeField] private RectTransform menuBackground;
        [SerializeField] private Transform optionContainer;
        [SerializeField] private GameObject pingOptionPrefab;
        [SerializeField] private Text instructionText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color disabledColor = Color.gray;
        [SerializeField] private Color backgroundColorActive = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Color backgroundColorInactive = Color.clear;
        
        [Header("Layout Settings")]
        [SerializeField] private float menuRadius = 100f;
        [SerializeField] private float optionSize = 60f;
        [SerializeField] private float animationDuration = 0.3f;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip menuOpenSound;
        [SerializeField] private AudioClip menuCloseSound;
        [SerializeField] private AudioClip optionSelectSound;
        [SerializeField] private AudioClip pingConfirmSound;
        
        #endregion
        
        #region Private Fields
        
        // State Management
        private bool isMenuOpen = false;
        private bool isInitialized = false;
        private Vector3 menuWorldPosition;
        private Vector2 menuScreenPosition;
        private int selectedOptionIndex = -1;
        
        // UI Management
        private List<PingOption> availablePings = new List<PingOption>();
        private List<PingOptionUI> optionUIElements = new List<PingOptionUI>();
        private Camera playerCamera;
        
        // Input
        private Vector2 currentPointerPosition;
        #pragma warning disable 0414 // Field assigned but never used - reserved for future drag functionality
        private bool isDragging = false;
        #pragma warning restore 0414
        
        // Animation
        private Coroutine menuAnimationCoroutine;
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public class PingOption
        {
            public PingType type;
            public string displayName;
            public Sprite icon;
            public Color color;
            public string audioClipId;
            
            public PingOption(PingType type, string displayName, Sprite icon, Color color, string audioClipId)
            {
                this.type = type;
                this.displayName = displayName;
                this.icon = icon;
                this.color = color;
                this.audioClipId = audioClipId;
            }
        }
        
        public enum PingType
        {
            Attack,
            Retreat,
            Defend,
            Help,
            Enemy,
            Missing,
            OnMyWay,
            Careful
        }
        
        public class PingOptionUI : MonoBehaviour
        {
            [Header("UI Components")]
            public Image backgroundImage;
            public Image iconImage;
            public Text labelText;
            public Button button;
            
            // Data
            public PingOption pingData;
            public bool isSelected;
            public bool isHighlighted;
            
            private Vector3 originalScale;
            private Color originalColor;
            
            private void Awake()
            {
                originalScale = transform.localScale;
                if (iconImage != null)
                    originalColor = iconImage.color;
            }
            
            public void Initialize(PingOption data)
            {
                pingData = data;
                UpdateVisuals();
                
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(OnOptionClicked);
                }
            }
            
            public void UpdateVisuals()
            {
                if (pingData == null) return;
                
                // Update icon
                if (iconImage != null && pingData.icon != null)
                    iconImage.sprite = pingData.icon;
                
                // Update label
                if (labelText != null)
                    labelText.text = pingData.displayName;
                
                // Update colors
                Color targetColor = originalColor;
                
                if (isSelected)
                    targetColor = Color.green;
                else if (isHighlighted)
                    targetColor = Color.yellow;
                
                if (iconImage != null)
                    iconImage.color = targetColor;
                
                if (backgroundImage != null)
                    backgroundImage.color = pingData.color;
            }
            
            public void SetHighlighted(bool highlighted)
            {
                isHighlighted = highlighted;
                UpdateVisuals();
                
                if (highlighted)
                {
                    StartCoroutine(AnimateScale(originalScale * 1.2f, 0.1f));
                }
                else
                {
                    StartCoroutine(AnimateScale(originalScale, 0.1f));
                }
            }
            
            public void SetSelected(bool selected)
            {
                isSelected = selected;
                UpdateVisuals();
                
                if (selected)
                {
                    StartCoroutine(AnimatePunch());
                }
            }
            
            private void OnOptionClicked()
            {
                var pingMenu = FindFirstObjectByType<PingRadialMenu>();
                pingMenu?.SelectOption(this);
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
                Vector3 punchScale = originalScale * 1.1f;
                
                yield return StartCoroutine(AnimateScale(punchScale, 0.1f));
                yield return StartCoroutine(AnimateScale(originalScale, 0.1f));
            }
        }
        
        #endregion
        
        #region Events
        
        public System.Action<PingType, Vector3> OnPingSelected;
        public System.Action OnMenuOpened;
        public System.Action OnMenuClosed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializePingSystem();
        }
        
        private void OnEnable()
        {
            SubscribeToInputEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromInputEvents();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Get player camera
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<Camera>();
            
            // Validate required components
            if (menuContainer == null)
            {
                Debug.LogError("[PingRadialMenu] Menu container is not assigned!");
                return;
            }
            
            if (menuBackground == null)
                menuBackground = menuContainer.GetComponent<RectTransform>();
            
            // Initially hide the menu
            menuContainer.SetActive(false);
        }
        
        private void InitializePingSystem()
        {
            LoadPingOptions();
            CreatePingOptionUI();
            
            isInitialized = true;
            Debug.Log("[PingRadialMenu] Ping system initialized successfully");
        }
        
        private void LoadPingOptions()
        {
            // Define available ping options
            availablePings.Clear();
            
            availablePings.Add(new PingOption(PingType.Attack, "Attack", null, Color.red, "ping_attack"));
            availablePings.Add(new PingOption(PingType.Retreat, "Retreat", null, new Color(1f, 0.5f, 0f), "ping_retreat"));
            availablePings.Add(new PingOption(PingType.Defend, "Defend", null, Color.blue, "ping_defend"));
            availablePings.Add(new PingOption(PingType.Help, "Help", null, Color.yellow, "ping_help"));
            availablePings.Add(new PingOption(PingType.Enemy, "Enemy", null, Color.magenta, "ping_enemy"));
            availablePings.Add(new PingOption(PingType.Missing, "Missing", null, Color.gray, "ping_missing"));
            availablePings.Add(new PingOption(PingType.OnMyWay, "On My Way", null, Color.green, "ping_onmyway"));
            availablePings.Add(new PingOption(PingType.Careful, "Careful", null, new Color(1f, 0.5f, 0f), "ping_careful"));
        }
        
        private void CreatePingOptionUI()
        {
            if (optionContainer == null || pingOptionPrefab == null)
            {
                Debug.LogError("[PingRadialMenu] Option container or prefab not assigned!");
                return;
            }
            
            // Clear existing options
            foreach (var option in optionUIElements)
            {
                if (option != null)
                    DestroyImmediate(option.gameObject);
            }
            optionUIElements.Clear();
            
            // Create new options
            float angleStep = 360f / availablePings.Count;
            
            for (int i = 0; i < availablePings.Count; i++)
            {
                GameObject optionObject = Instantiate(pingOptionPrefab, optionContainer);
                var optionUI = optionObject.GetComponent<PingOptionUI>();
                
                if (optionUI == null)
                    optionUI = optionObject.AddComponent<PingOptionUI>();
                
                optionUI.Initialize(availablePings[i]);
                
                // Position option in circle
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * menuRadius,
                    Mathf.Sin(angle) * menuRadius,
                    0
                );
                optionObject.transform.localPosition = position;
                
                optionUIElements.Add(optionUI);
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void SubscribeToInputEvents()
        {
            // Input system events would go here
        }
        
        private void UnsubscribeFromInputEvents()
        {
            // Input system cleanup would go here
        }
        
        private void HandleInput()
        {
            if (!isInitialized) return;
            
            // Handle mouse/touch input
            if (Input.GetMouseButtonDown(1)) // Right click
            {
                OpenMenu(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(1) && isMenuOpen)
            {
                ConfirmSelection();
            }
            
            if (isMenuOpen)
            {
                UpdateSelection(Input.mousePosition);
            }
        }
        
        private void UpdateSelection(Vector2 pointerPosition)
        {
            currentPointerPosition = pointerPosition;
            
            // Calculate which option is being highlighted
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(playerCamera, menuContainer.transform.position);
            Vector2 direction = (pointerPosition - screenCenter).normalized;
            
            if ((pointerPosition - screenCenter).magnitude > 30f) // Minimum distance threshold
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;
                
                int optionIndex = Mathf.RoundToInt(angle / (360f / availablePings.Count)) % availablePings.Count;
                
                if (optionIndex != selectedOptionIndex)
                {
                    // Clear previous selection
                    if (selectedOptionIndex >= 0 && selectedOptionIndex < optionUIElements.Count)
                    {
                        optionUIElements[selectedOptionIndex].SetHighlighted(false);
                    }
                    
                    // Set new selection
                    selectedOptionIndex = optionIndex;
                    if (selectedOptionIndex >= 0 && selectedOptionIndex < optionUIElements.Count)
                    {
                        optionUIElements[selectedOptionIndex].SetHighlighted(true);
                        PlaySound(optionSelectSound);
                    }
                }
            }
            else
            {
                // Clear selection if too close to center
                if (selectedOptionIndex >= 0 && selectedOptionIndex < optionUIElements.Count)
                {
                    optionUIElements[selectedOptionIndex].SetHighlighted(false);
                }
                selectedOptionIndex = -1;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public void OpenMenu(Vector2 screenPosition)
        {
            if (!isInitialized || isMenuOpen) return;
            
            isMenuOpen = true;
            menuScreenPosition = screenPosition;
            
            // Convert screen position to world position
            if (playerCamera != null)
            {
                Ray ray = playerCamera.ScreenPointToRay(screenPosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    menuWorldPosition = hit.point;
                }
                else
                {
                    menuWorldPosition = ray.GetPoint(10f); // Default distance
                }
            }
            
            // Position menu at screen position
            menuContainer.transform.position = screenPosition;
            menuContainer.SetActive(true);
            
            // Play sound
            PlaySound(menuOpenSound);
            
            // Animate opening
            AnimateMenuOpen();
            
            OnMenuOpened?.Invoke();
        }
        
        public void CloseMenu()
        {
            if (!isMenuOpen) return;
            
            isMenuOpen = false;
            selectedOptionIndex = -1;
            
            // Clear highlights
            foreach (var option in optionUIElements)
            {
                option.SetHighlighted(false);
            }
            
            // Play sound
            PlaySound(menuCloseSound);
            
            // Animate closing
            AnimateMenuClose();
            
            OnMenuClosed?.Invoke();
        }
        
        public void SelectOption(PingOptionUI optionUI)
        {
            if (!isMenuOpen) return;
            
            int index = optionUIElements.IndexOf(optionUI);
            if (index >= 0)
            {
                selectedOptionIndex = index;
                ConfirmSelection();
            }
        }
        
        public void ConfirmSelection()
        {
            if (!isMenuOpen || selectedOptionIndex < 0 || selectedOptionIndex >= availablePings.Count)
            {
                CloseMenu();
                return;
            }
            
            var selectedPing = availablePings[selectedOptionIndex];
            var selectedOptionUI = optionUIElements[selectedOptionIndex];
            
            // Show selection feedback
            selectedOptionUI.SetSelected(true);
            
            // Play confirmation sound
            PlaySound(pingConfirmSound);
            
            // Fire ping event
            OnPingSelected?.Invoke(selectedPing.type, menuWorldPosition);
            
            // Close menu after short delay
            StartCoroutine(CloseMenuAfterDelay(0.2f));
        }
        
        #endregion
        
        #region Private Methods
        
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
            if (menuAnimationCoroutine != null)
                StopCoroutine(menuAnimationCoroutine);
            
            menuAnimationCoroutine = StartCoroutine(AnimateMenuOpenCoroutine());
        }
        
        private System.Collections.IEnumerator AnimateMenuOpenCoroutine()
        {
            // Background fade in
            if (menuBackground != null)
            {
                var backgroundImage = menuBackground.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    StartCoroutine(AnimateImageColor(backgroundImage, backgroundColorInactive, backgroundColorActive, animationDuration));
                }
            }
            
            // Container scale up
            menuContainer.transform.localScale = Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f * t - 2f); // Ease out
                menuContainer.transform.localScale = Vector3.one * t;
                yield return null;
            }
            
            menuContainer.transform.localScale = Vector3.one;
            
            // Animate options appearing
            for (int i = 0; i < optionUIElements.Count; i++)
            {
                var option = optionUIElements[i];
                StartCoroutine(AnimateOptionAppear(option.transform, i * 0.05f));
            }
        }
        
        private System.Collections.IEnumerator AnimateOptionAppear(Transform option, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            option.localScale = Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < animationDuration * 0.8f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.8f);
                option.localScale = Vector3.one * t;
                yield return null;
            }
            
            option.localScale = Vector3.one;
        }
        
        private void AnimateMenuClose()
        {
            if (menuAnimationCoroutine != null)
                StopCoroutine(menuAnimationCoroutine);
            
            menuAnimationCoroutine = StartCoroutine(AnimateMenuCloseCoroutine());
        }
        
        private System.Collections.IEnumerator AnimateMenuCloseCoroutine()
        {
            // Background fade out
            if (menuBackground != null)
            {
                var backgroundImage = menuBackground.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    StartCoroutine(AnimateImageColor(backgroundImage, backgroundColorActive, backgroundColorInactive, animationDuration * 0.5f));
                }
            }
            
            // Container scale down
            float elapsed = 0f;
            Vector3 startScale = menuContainer.transform.localScale;
            
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                menuContainer.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            
            menuContainer.transform.localScale = Vector3.zero;
            menuContainer.SetActive(false);
        }
        
        private System.Collections.IEnumerator AnimateImageColor(Image image, Color fromColor, Color toColor, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                image.color = Color.Lerp(fromColor, toColor, t);
                yield return null;
            }
            
            image.color = toColor;
        }
        
        private System.Collections.IEnumerator CloseMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            CloseMenu();
        }
        
        #endregion
        
        #region Debug and Utilities
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            if (menuRadius <= 0f)
                menuRadius = 100f;
            
            if (optionSize <= 0f)
                optionSize = 60f;
            
            if (animationDuration <= 0f)
                animationDuration = 0.3f;
        }
        
        #endregion
    }
}