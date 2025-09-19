using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using MOBA.Debugging;

namespace MOBA.Abilities
{
    /// <summary>
    /// Manages ability input handling, key bindings, and input state management.
    /// Responsible for processing input events and triggering ability execution.
    /// </summary>
    public class AbilityInputManager : AbilityManagerComponent
    {
        #region Configuration
        
        [Header("Input Settings")]
        [SerializeField, Tooltip("Input action asset for ability inputs")]
        private InputActionAsset inputActions;
        
        [SerializeField, Tooltip("Default key bindings for abilities")]
        private KeyCode[] defaultAbilityKeys = { KeyCode.Q, KeyCode.E, KeyCode.G, KeyCode.R };
        
        [SerializeField, Tooltip("Enable input buffering for better responsiveness")]
        private bool enableInputBuffering = true;
        
        [SerializeField, Tooltip("Input buffer window in seconds")]
        private float inputBufferWindow = 0.1f;
        
        [SerializeField, Tooltip("Enable detailed input logging")]
        private bool logInputEvents = false;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Reference to execution manager for ability casting
        /// </summary>
        private AbilityExecutionManager executionManager;
        
        /// <summary>
        /// Reference to cooldown manager for ability readiness checks
        /// </summary>
        private AbilityCooldownManager cooldownManager;
        
        /// <summary>
        /// Input action references for each ability
        /// </summary>
        private InputAction[] abilityInputActions;
        
        /// <summary>
        /// Buffered input queue for responsive input handling
        /// </summary>
        private readonly Queue<BufferedInput> inputBuffer = new Queue<BufferedInput>();
        
        /// <summary>
        /// Current input state tracking
        /// </summary>
        private readonly Dictionary<int, AbilityInputState> inputStates = new Dictionary<int, AbilityInputState>();
        
        /// <summary>
        /// Key binding configuration
        /// </summary>
        private readonly Dictionary<KeyCode, int> keyToAbilityMap = new Dictionary<KeyCode, int>();
        
        /// <summary>
        /// Input action map for abilities
        /// </summary>
        private InputActionMap abilityActionMap;
        
        /// <summary>
        /// Whether input is currently enabled
        /// </summary>
        private bool inputEnabled = true;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when an ability input is detected. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnAbilityInputReceived; // ability index
        
        /// <summary>
        /// Raised when an ability input is buffered
        /// </summary>
        public System.Action<int> OnAbilityInputBuffered; // ability index
        
        /// <summary>
        /// Raised when key bindings are changed
        /// </summary>
        public System.Action OnKeyBindingsChanged;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(EnhancedAbilitySystem abilitySystem)
        {
            base.Initialize(abilitySystem);
            
            InitializeInputActions();
            InitializeKeyBindings();
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Input manager initialized.",
                    ("InputBuffering", enableInputBuffering),
                    ("BufferWindow", inputBufferWindow));
            }
        }
        
        public override void Shutdown()
        {
            // Disable input actions
            DisableInput();
            
            // Clean up input actions
            if (abilityInputActions != null)
            {
                for (int i = 0; i < abilityInputActions.Length; i++)
                {
                    if (abilityInputActions[i] != null)
                    {
                        abilityInputActions[i].performed -= (ctx) => OnAbilityInput(i);
                        abilityInputActions[i].Disable();
                    }
                }
                abilityInputActions = null;
            }
            
            // Clear collections
            inputBuffer.Clear();
            inputStates.Clear();
            keyToAbilityMap.Clear();
            
            // Clear events
            OnAbilityInputReceived = null;
            OnAbilityInputBuffered = null;
            OnKeyBindingsChanged = null;
            
            // Clear references
            executionManager = null;
            cooldownManager = null;
            abilityActionMap = null;
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Input manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        /// <summary>
        /// Set references to other ability managers
        /// </summary>
        public void SetManagers(AbilityExecutionManager execMgr, AbilityCooldownManager cdMgr)
        {
            executionManager = execMgr;
            cooldownManager = cdMgr;
        }
        
        #endregion
        
        #region Input System Integration
        
        /// <summary>
        /// Initialize input actions for abilities
        /// </summary>
        private void InitializeInputActions()
        {
            if (inputActions != null)
            {
                // Find or create ability action map
                abilityActionMap = inputActions.FindActionMap("Abilities");
                if (abilityActionMap == null)
                {
                    abilityActionMap = inputActions.AddActionMap("Abilities");
                }
                
                // Initialize ability input actions
                int abilityCount = enhancedAbilitySystem.Abilities.Length;
                abilityInputActions = new InputAction[abilityCount];
                
                for (int i = 0; i < abilityCount; i++)
                {
                    // Create or find input action for this ability
                    string actionName = $"Ability{i}";
                    var action = abilityActionMap.FindAction(actionName);
                    
                    if (action == null)
                    {
                        action = abilityActionMap.AddAction(actionName, InputActionType.Button);
                        
                        // Set default binding if available
                        if (i < defaultAbilityKeys.Length)
                        {
                            action.AddBinding($"<Keyboard>/{defaultAbilityKeys[i].ToString().ToLower()}");
                        }
                    }
                    
                    abilityInputActions[i] = action;
                    
                    // Subscribe to input events
                    int abilityIndex = i; // Capture for closure
                    action.performed += (ctx) => OnAbilityInput(abilityIndex);
                    
                    // Initialize input state
                    inputStates[i] = new AbilityInputState();
                }
                
                // Enable the action map
                abilityActionMap.Enable();
            }
            else
            {
                // Fallback to legacy input system
                InitializeLegacyInput();
            }
        }
        
        /// <summary>
        /// Initialize legacy input system as fallback
        /// </summary>
        private void InitializeLegacyInput()
        {
            int abilityCount = enhancedAbilitySystem.Abilities.Length;
            
            for (int i = 0; i < abilityCount && i < defaultAbilityKeys.Length; i++)
            {
                keyToAbilityMap[defaultAbilityKeys[i]] = i;
                inputStates[i] = new AbilityInputState();
            }
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Using legacy input system.",
                    ("MappedKeys", keyToAbilityMap.Count));
            }
        }
        
        /// <summary>
        /// Initialize key binding configuration
        /// </summary>
        private void InitializeKeyBindings()
        {
            for (int i = 0; i < defaultAbilityKeys.Length; i++)
            {
                if (i < enhancedAbilitySystem.Abilities.Length)
                {
                    keyToAbilityMap[defaultAbilityKeys[i]] = i;
                }
            }
        }
        
        #endregion
        
        #region Input Processing
        
        public override void UpdateComponent()
        {
            if (!inputEnabled)
            {
                return;
            }
            
            // Process legacy input if not using new input system
            if (inputActions == null)
            {
                ProcessLegacyInput();
            }
            
            // Process input buffer
            if (enableInputBuffering)
            {
                ProcessInputBuffer();
            }
        }
        
        /// <summary>
        /// Process legacy input system
        /// </summary>
        private void ProcessLegacyInput()
        {
            foreach (var kvp in keyToAbilityMap)
            {
                if (Input.GetKeyDown(kvp.Key))
                {
                    OnAbilityInput(kvp.Value);
                }
            }
        }
        
        /// <summary>
        /// Process buffered input for responsive gameplay
        /// </summary>
        private void ProcessInputBuffer()
        {
            float currentTime = Time.time;
            
            // Process buffered inputs
            while (inputBuffer.Count > 0)
            {
                var bufferedInput = inputBuffer.Peek();
                
                // Check if input has expired
                if (currentTime - bufferedInput.Timestamp > inputBufferWindow)
                {
                    inputBuffer.Dequeue();
                    continue;
                }
                
                // Try to execute buffered input
                if (TryExecuteBufferedInput(bufferedInput))
                {
                    inputBuffer.Dequeue();
                }
                else
                {
                    // Can't execute yet, try again next frame
                    break;
                }
            }
        }
        
        /// <summary>
        /// Handles input for a specific ability
        /// </summary>
        /// <param name="abilityIndex">Index of the ability</param>
        private void OnAbilityInput(int abilityIndex)
        {
            if (!inputEnabled)
            {
                return;
            }
            
            // Update input state
            var inputState = inputStates[abilityIndex];
            inputState.LastInputTime = Time.time;
            inputState.InputCount++;
            
            // Trigger input event
            OnAbilityInputReceived?.Invoke(abilityIndex);
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Ability input received.",
                    ("AbilityIndex", abilityIndex),
                    ("InputCount", inputState.InputCount));
            }
            
            // Try immediate execution
            if (executionManager != null && executionManager.TryCastAbility(abilityIndex))
            {
                inputState.LastExecutionTime = Time.time;
                return;
            }
            
            // Buffer input if execution failed and buffering is enabled
            if (enableInputBuffering)
            {
                BufferInput(abilityIndex);
            }
        }
        
        /// <summary>
        /// Buffers an input for later execution
        /// </summary>
        /// <param name="abilityIndex">Ability index to buffer</param>
        private void BufferInput(int abilityIndex)
        {
            var bufferedInput = new BufferedInput
            {
                AbilityIndex = abilityIndex,
                Timestamp = Time.time
            };
            
            inputBuffer.Enqueue(bufferedInput);
            OnAbilityInputBuffered?.Invoke(abilityIndex);
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Input buffered.",
                    ("AbilityIndex", abilityIndex),
                    ("BufferSize", inputBuffer.Count));
            }
        }
        
        /// <summary>
        /// Attempts to execute a buffered input
        /// </summary>
        /// <param name="bufferedInput">Buffered input data</param>
        /// <returns>True if execution was successful</returns>
        private bool TryExecuteBufferedInput(BufferedInput bufferedInput)
        {
            if (executionManager == null)
            {
                return false;
            }
            
            bool success = executionManager.TryCastAbility(bufferedInput.AbilityIndex);
            
            if (success)
            {
                var inputState = inputStates[bufferedInput.AbilityIndex];
                inputState.LastExecutionTime = Time.time;
                inputState.BufferedExecutionCount++;
                
                if (logInputEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Input),
                        "Buffered input executed.",
                        ("AbilityIndex", bufferedInput.AbilityIndex),
                        ("Delay", Time.time - bufferedInput.Timestamp));
                }
            }
            
            return success;
        }
        
        #endregion
        
        #region Input State Management
        
        /// <summary>
        /// Enable input processing
        /// </summary>
        public void EnableInput()
        {
            inputEnabled = true;
            abilityActionMap?.Enable();
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Input enabled.");
            }
        }
        
        /// <summary>
        /// Disable input processing
        /// </summary>
        public void DisableInput()
        {
            inputEnabled = false;
            abilityActionMap?.Disable();
            
            // Clear input buffer when disabled
            inputBuffer.Clear();
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Input disabled.");
            }
        }
        
        /// <summary>
        /// Check if input is currently enabled
        /// </summary>
        /// <returns>True if input is enabled</returns>
        public bool IsInputEnabled()
        {
            return inputEnabled;
        }
        
        /// <summary>
        /// Clear all buffered inputs
        /// </summary>
        public void ClearInputBuffer()
        {
            int clearedCount = inputBuffer.Count;
            inputBuffer.Clear();
            
            if (logInputEvents && clearedCount > 0)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Input buffer cleared.",
                    ("ClearedInputs", clearedCount));
            }
        }
        
        /// <summary>
        /// Get input state for a specific ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <returns>Input state data</returns>
        public AbilityInputState GetInputState(int abilityIndex)
        {
            return inputStates.TryGetValue(abilityIndex, out var state) ? state : new AbilityInputState();
        }
        
        #endregion
        
        #region Key Binding Management
        
        /// <summary>
        /// Set key binding for an ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <param name="keyCode">Key to bind</param>
        public void SetKeyBinding(int abilityIndex, KeyCode keyCode)
        {
            // Remove old binding if it exists
            var oldKey = KeyCode.None;
            foreach (var kvp in keyToAbilityMap)
            {
                if (kvp.Value == abilityIndex)
                {
                    oldKey = kvp.Key;
                    break;
                }
            }
            
            if (oldKey != KeyCode.None)
            {
                keyToAbilityMap.Remove(oldKey);
            }
            
            // Add new binding
            keyToAbilityMap[keyCode] = abilityIndex;
            
            // Update input action binding if using new input system
            if (abilityInputActions != null && abilityIndex < abilityInputActions.Length)
            {
                var action = abilityInputActions[abilityIndex];
                if (action != null)
                {
                    // Remove old bindings
                    for (int i = action.bindings.Count - 1; i >= 0; i--)
                    {
                        action.RemoveBindingOverride(i);
                    }
                    
                    // Add new binding
                    action.ApplyBindingOverride($"<Keyboard>/{keyCode.ToString().ToLower()}");
                }
            }
            
            OnKeyBindingsChanged?.Invoke();
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Key binding updated.",
                    ("AbilityIndex", abilityIndex),
                    ("OldKey", oldKey),
                    ("NewKey", keyCode));
            }
        }
        
        /// <summary>
        /// Get key binding for an ability
        /// </summary>
        /// <param name="abilityIndex">Ability index</param>
        /// <returns>Bound key code</returns>
        public KeyCode GetKeyBinding(int abilityIndex)
        {
            foreach (var kvp in keyToAbilityMap)
            {
                if (kvp.Value == abilityIndex)
                {
                    return kvp.Key;
                }
            }
            
            return KeyCode.None;
        }
        
        /// <summary>
        /// Reset key bindings to defaults
        /// </summary>
        public void ResetKeyBindings()
        {
            keyToAbilityMap.Clear();
            
            for (int i = 0; i < defaultAbilityKeys.Length && i < enhancedAbilitySystem.Abilities.Length; i++)
            {
                SetKeyBinding(i, defaultAbilityKeys[i]);
            }
            
            if (logInputEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Input),
                    "Key bindings reset to defaults.");
            }
        }
        
        #endregion
        
        #region Debug and Statistics
        
        /// <summary>
        /// Get input statistics for debugging
        /// </summary>
        /// <returns>Input statistics data</returns>
        public InputStatistics GetInputStatistics()
        {
            var stats = new InputStatistics
            {
                InputEnabled = inputEnabled,
                BufferedInputCount = inputBuffer.Count,
                BufferWindowSize = inputBufferWindow,
                TotalAbilities = enhancedAbilitySystem.Abilities.Length,
                BoundAbilities = keyToAbilityMap.Count
            };
            
            foreach (var inputState in inputStates.Values)
            {
                stats.TotalInputCount += inputState.InputCount;
                stats.TotalBufferedExecutions += inputState.BufferedExecutionCount;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Log current input state for debugging
        /// </summary>
        public void LogInputState()
        {
            var stats = GetInputStatistics();
            
            GameDebug.Log(
                BuildContext(GameDebugMechanicTag.Input),
                "Input state summary.",
                ("InputEnabled", stats.InputEnabled),
                ("BufferedInputs", stats.BufferedInputCount),
                ("TotalInputs", stats.TotalInputCount),
                ("BufferedExecutions", stats.TotalBufferedExecutions),
                ("BoundAbilities", stats.BoundAbilities));
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for input logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag)
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                tag,
                subsystem: nameof(AbilityInputManager),
                actor: gameObject?.name);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Input state tracking for abilities
    /// </summary>
    [System.Serializable]
    public class AbilityInputState
    {
        public float LastInputTime;
        public float LastExecutionTime;
        public int InputCount;
        public int BufferedExecutionCount;
        
        public float TimeSinceLastInput => Time.time - LastInputTime;
        public float TimeSinceLastExecution => Time.time - LastExecutionTime;
    }
    
    /// <summary>
    /// Buffered input data
    /// </summary>
    [System.Serializable]
    public struct BufferedInput
    {
        public int AbilityIndex;
        public float Timestamp;
    }
    
    /// <summary>
    /// Input system statistics
    /// </summary>
    [System.Serializable]
    public struct InputStatistics
    {
        public bool InputEnabled;
        public int BufferedInputCount;
        public float BufferWindowSize;
        public int TotalAbilities;
        public int BoundAbilities;
        public int TotalInputCount;
        public int TotalBufferedExecutions;
    }
    
    #endregion
}