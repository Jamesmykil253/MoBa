using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using MOBA.Debugging;

namespace MOBA.UI
{
    /// <summary>
    /// Chat ping wheel system for quick team communication.
    /// Provides predefined tactical callouts similar to other MOBAs.
    /// 🚧 IMPLEMENTATION NEEDED: UI system and network synchronization pending
    /// </summary>
    public class ChatPingSystem : MonoBehaviour
    {
        #region Configuration
        
        [Header("Input Configuration")]
        [SerializeField, Tooltip("Input action for opening chat ping wheel")]
        private InputActionReference chatActionReference;
        
        [SerializeField, Tooltip("Fallback action name if reference is null")]
        private string fallbackChatAction = "Chat";
        
        [Header("Ping Settings")]
        [SerializeField, Tooltip("Available ping options")]
        private PingOption[] pingOptions = new PingOption[]
        {
            new PingOption { Id = "help", DisplayText = "Help!", Color = Color.red },
            new PingOption { Id = "retreat", DisplayText = "Retreat!", Color = Color.yellow },
            new PingOption { Id = "push", DisplayText = "Push!", Color = Color.green },
            new PingOption { Id = "missing", DisplayText = "Missing Enemy!", Color = new Color(1f, 0.5f, 0f) }, // Orange color
            new PingOption { Id = "onway", DisplayText = "On My Way!", Color = Color.blue },
            new PingOption { Id = "defend", DisplayText = "Defend!", Color = Color.cyan },
            new PingOption { Id = "attack", DisplayText = "Attack!", Color = Color.red },
            new PingOption { Id = "caution", DisplayText = "Be Careful!", Color = Color.magenta }
        };
        
        [SerializeField, Tooltip("Duration to show ping messages")]
        private float pingDisplayDuration = 3f;
        
        [SerializeField, Tooltip("Enable debug logging")]
        private bool enableDebugLogging = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Chat input action reference
        /// </summary>
        private InputAction chatAction;
        
        /// <summary>
        /// Player input component reference
        /// </summary>
        private PlayerInput playerInput;
        
        /// <summary>
        /// Whether the ping wheel is currently open
        /// </summary>
        private bool isPingWheelOpen = false;
        
        /// <summary>
        /// Currently selected ping option
        /// </summary>
        private int selectedPingIndex = 0;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when a ping is sent. Parameters: pingId, senderName, worldPosition
        /// </summary>
        public System.Action<string, string, Vector3> OnPingSent;
        
        /// <summary>
        /// Raised when ping wheel is opened/closed
        /// </summary>
        public System.Action<bool> OnPingWheelToggled;
        
        #endregion
        
        #region Unity Lifecycle
        
        void Awake()
        {
            // Get player input component
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = FindFirstObjectByType<PlayerInput>();
            }
            
            // Resolve chat input action
            ResolveChatInputAction();
        }
        
        void OnEnable()
        {
            if (chatAction != null)
            {
                chatAction.performed += OnChatInputPerformed;
                chatAction.canceled += OnChatInputCanceled;
                chatAction.Enable();
            }
        }
        
        void OnDisable()
        {
            if (chatAction != null)
            {
                chatAction.performed -= OnChatInputPerformed;
                chatAction.canceled -= OnChatInputCanceled;
                chatAction.Disable();
            }
        }
        
        void Update()
        {
            if (isPingWheelOpen)
            {
                UpdatePingWheelSelection();
            }
        }
        
        #endregion
        
        #region Input Handling
        
        /// <summary>
        /// Resolve the chat input action from references or fallback
        /// </summary>
        private void ResolveChatInputAction()
        {
            if (chatActionReference != null)
            {
                chatAction = chatActionReference.action;
            }
            else if (playerInput?.actions != null)
            {
                chatAction = playerInput.actions.FindAction(fallbackChatAction, throwIfNotFound: false);
            }
            
            if (chatAction == null && enableDebugLogging)
            {
                GameDebug.LogWarning(
                    BuildDebugContext(),
                    "Chat input action not found. Chat system will be non-functional.",
                    ("ActionReference", chatActionReference != null),
                    ("FallbackAction", fallbackChatAction));
            }
        }
        
        /// <summary>
        /// Handle chat input press - opens ping wheel
        /// </summary>
        private void OnChatInputPerformed(InputAction.CallbackContext context)
        {
            if (!isPingWheelOpen)
            {
                OpenPingWheel();
            }
        }
        
        /// <summary>
        /// Handle chat input release - closes ping wheel and sends selected ping
        /// </summary>
        private void OnChatInputCanceled(InputAction.CallbackContext context)
        {
            if (isPingWheelOpen)
            {
                SendSelectedPing();
                ClosePingWheel();
            }
        }
        
        #endregion
        
        #region Ping Wheel Logic
        
        /// <summary>
        /// Open the ping wheel interface
        /// 🚧 TODO: Implement UI system for radial ping wheel
        /// </summary>
        private void OpenPingWheel()
        {
            isPingWheelOpen = true;
            selectedPingIndex = 0;
            
            // 🚧 TODO: Show ping wheel UI
            // - Create radial menu with ping options
            // - Position at mouse cursor or screen center
            // - Highlight default option
            
            OnPingWheelToggled?.Invoke(true);
            
            if (enableDebugLogging)
            {
                GameDebug.Log(
                    BuildDebugContext(),
                    "Ping wheel opened.",
                    ("AvailableOptions", pingOptions.Length));
            }
        }
        
        /// <summary>
        /// Close the ping wheel interface
        /// 🚧 TODO: Implement UI system for hiding ping wheel
        /// </summary>
        private void ClosePingWheel()
        {
            isPingWheelOpen = false;
            
            // 🚧 TODO: Hide ping wheel UI
            
            OnPingWheelToggled?.Invoke(false);
            
            if (enableDebugLogging)
            {
                GameDebug.Log(
                    BuildDebugContext(),
                    "Ping wheel closed.");
            }
        }
        
        /// <summary>
        /// Update ping selection based on mouse position
        /// 🚧 TODO: Implement mouse-based radial selection
        /// </summary>
        private void UpdatePingWheelSelection()
        {
            // 🚧 TODO: Calculate selected ping based on mouse position relative to wheel center
            // - Get mouse world position
            // - Calculate angle from wheel center
            // - Map angle to ping option index
            // - Update visual highlighting
            
            // Placeholder: Cycle through options with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                selectedPingIndex = (selectedPingIndex + 1) % pingOptions.Length;
            }
            else if (scroll < 0f)
            {
                selectedPingIndex = (selectedPingIndex - 1 + pingOptions.Length) % pingOptions.Length;
            }
        }
        
        /// <summary>
        /// Send the currently selected ping
        /// 🚧 TODO: Implement network synchronization
        /// </summary>
        private void SendSelectedPing()
        {
            if (selectedPingIndex >= 0 && selectedPingIndex < pingOptions.Length)
            {
                var selectedPing = pingOptions[selectedPingIndex];
                
                // Get world position for ping (mouse position or player position)
                Vector3 pingPosition = GetPingWorldPosition();
                
                // 🚧 TODO: Send ping over network to other players
                // - Serialize ping data (id, sender, position, timestamp)
                // - Broadcast to team members
                // - Handle ping display on other clients
                
                OnPingSent?.Invoke(selectedPing.Id, GetPlayerName(), pingPosition);
                
                // Start ping display coroutine
                StartCoroutine(DisplayPingCoroutine(selectedPing, pingPosition));
                
                if (enableDebugLogging)
                {
                    GameDebug.Log(
                        BuildDebugContext(),
                        "Ping sent.",
                        ("PingId", selectedPing.Id),
                        ("DisplayText", selectedPing.DisplayText),
                        ("Position", pingPosition));
                }
            }
        }
        
        /// <summary>
        /// Get world position for ping placement
        /// 🚧 TODO: Implement proper world position calculation
        /// </summary>
        private Vector3 GetPingWorldPosition()
        {
            // 🚧 TODO: Raycast from camera through mouse position to world
            // For now, return player position as fallback
            return transform.position;
        }
        
        /// <summary>
        /// Get the current player's name
        /// 🚧 TODO: Integrate with player identity system
        /// </summary>
        private string GetPlayerName()
        {
            // 🚧 TODO: Get actual player name from player profile/identity system
            return gameObject.name ?? "Player";
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Programmatically send a specific ping
        /// </summary>
        /// <param name="pingId">ID of ping to send</param>
        /// <param name="worldPosition">World position for ping</param>
        public void SendPing(string pingId, Vector3 worldPosition)
        {
            var pingOption = System.Array.Find(pingOptions, p => p.Id == pingId);
            if (pingOption != null)
            {
                OnPingSent?.Invoke(pingId, GetPlayerName(), worldPosition);
                
                if (enableDebugLogging)
                {
                    GameDebug.Log(
                        BuildDebugContext(),
                        "Programmatic ping sent.",
                        ("PingId", pingId),
                        ("Position", worldPosition));
                }
            }
        }
        
        /// <summary>
        /// Check if ping wheel is currently open
        /// </summary>
        public bool IsPingWheelOpen => isPingWheelOpen;
        
        /// <summary>
        /// Get all available ping options
        /// </summary>
        public PingOption[] GetAvailablePings() => pingOptions;
        
        /// <summary>
        /// Enable or disable the chat ping system
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            if (!enabled && isPingWheelOpen)
            {
                ClosePingWheel();
            }
        }
        
        #endregion
        
        #region Debug Helpers
        
        /// <summary>
        /// Build debug context for logging
        /// </summary>
        private GameDebugContext BuildDebugContext()
        {
            return new GameDebugContext(
                GameDebugCategory.UI,
                GameDebugSystemTag.UI,
                GameDebugMechanicTag.Input,
                subsystem: nameof(ChatPingSystem),
                actor: gameObject?.name);
        }

        /// <summary>
        /// Coroutine to display ping for configured duration
        /// </summary>
        private System.Collections.IEnumerator DisplayPingCoroutine(PingOption ping, Vector3 worldPosition)
        {
            if (enableDebugLogging)
            {
                GameDebug.Log(
                    BuildDebugContext(),
                    "Ping display started.",
                    ("Duration", pingDisplayDuration),
                    ("PingText", ping.DisplayText));
            }

            // TODO: Implement actual ping visual display
            // This would show a ping indicator in the game world
            // For now, we just wait for the configured duration
            
            yield return new WaitForSeconds(pingDisplayDuration);
            
            if (enableDebugLogging)
            {
                GameDebug.Log(
                    BuildDebugContext(),
                    "Ping display ended.",
                    ("PingText", ping.DisplayText));
            }
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration for a ping option
    /// </summary>
    [System.Serializable]
    public class PingOption
    {
        [Tooltip("Unique identifier for this ping")]
        public string Id;
        
        [Tooltip("Text to display for this ping")]
        public string DisplayText;
        
        [Tooltip("Color for this ping option")]
        public Color Color = Color.white;
        
        [Tooltip("Icon sprite for this ping (optional)")]
        public Sprite Icon;
        
        [Tooltip("Audio clip to play when this ping is sent (optional)")]
        public AudioClip SoundEffect;
    }
    
    #endregion
}