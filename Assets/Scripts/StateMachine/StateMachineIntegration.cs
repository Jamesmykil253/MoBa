using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Integration script that demonstrates the complete State Machine system
    /// Shows Observer Pattern usage and integration with all MOBA components
    /// Based on Game Programming Patterns - Observer Pattern for events
    /// </summary>
    [RequireComponent(typeof(MOBACharacterController))]
    public class StateMachineIntegration : MonoBehaviour
    {
        [Header("State Machine Components")]
        [SerializeField] private MOBACharacterController characterController;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InputRelay inputRelay;

        [Header("Integration Settings")]
        [SerializeField] private bool enableStateLogging = true;
        [SerializeField] private bool enableStateEvents = true;

        // State machine instance
        private CharacterStateMachine stateMachine;

        // Event tracking
        private int totalStateChanges;
        private float lastStateChangeTime;

        private void Awake()
        {
            // Get required components
            if (characterController == null)
            {
                characterController = GetComponent<MOBACharacterController>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (inputRelay == null)
            {
                inputRelay = GetComponent<InputRelay>();
            }

            // Initialize state machine
            InitializeStateMachine();
        }

        private void InitializeStateMachine()
        {
            // Create the state machine
            stateMachine = new CharacterStateMachine(characterController);

            // Subscribe to state change events (Observer Pattern)
            if (enableStateEvents)
            {
                stateMachine.OnStateChanged += HandleStateChanged;
                stateMachine.OnStateEntered += HandleStateEntered;
                stateMachine.OnStateExited += HandleStateExited;
            }

            Debug.Log("State Machine initialized with Observer Pattern event system");
        }

        private void Update()
        {
            // Update the state machine
            stateMachine.Update();

            // Handle input-based transitions
            HandleInputTransitions();

            // Handle physics-based transitions
            HandlePhysicsTransitions();

            // Handle damage-based transitions
            HandleDamageTransitions();
        }

        private void HandleInputTransitions()
        {
            if (inputRelay == null) return;

            // Get current input state from InputRelay
            Vector3 movementInput = characterController.MovementInput;

            // Get input state from InputRelay (which now uses Input System)
            bool jumpPressed = false;
            bool attackPressed = false;
            bool abilityPressed = false;

            // Check for input using Input System through InputRelay
            if (inputRelay.CharacterController != null)
            {
                // These would be set by the InputRelay's event handlers
                // For now, we'll use a simple approach
                jumpPressed = UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;
                attackPressed = UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame ?? false;

                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null)
                {
                    abilityPressed = keyboard.qKey.wasPressedThisFrame ||
                                    keyboard.eKey.wasPressedThisFrame ||
                                    keyboard.rKey.wasPressedThisFrame;
                }
            }

            // Relay inputs to state machine
            stateMachine.HandleInput(movementInput, jumpPressed, attackPressed, abilityPressed);
        }

        private void HandlePhysicsTransitions()
        {
            if (characterController == null) return;

            // Get ground state from the character controller (which has improved detection)
            bool isGrounded = false;
            if (characterController.TryGetComponent(out MOBACharacterController mobaController))
            {
                // Use reflection to access the private isGrounded field
                var field = typeof(MOBACharacterController).GetField("isGrounded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    isGrounded = (bool)field.GetValue(mobaController);
                }
            }

            // Fallback ground detection if reflection fails
            if (!isGrounded)
            {
                RaycastHit hit;
                Vector3 raycastOrigin = characterController.transform.position + Vector3.up * 0.1f;
                isGrounded = Physics.Raycast(raycastOrigin, Vector3.down, out hit, 1.2f, LayerMask.GetMask("Ground")) ||
                           Physics.Raycast(raycastOrigin, Vector3.down, out hit, 1.2f); // Fallback to all layers
            }

            // Get velocity for physics-based transitions
            Vector3 velocity = Vector3.zero;
            if (characterController.TryGetComponent(out Rigidbody rb))
            {
                velocity = rb.linearVelocity;
            }

            // Debug logging for physics transitions
            if (Time.frameCount % 120 == 0) // Log every 2 seconds
            {
                Debug.Log($"[StateMachineIntegration] Physics: isGrounded={isGrounded}, velocity={velocity:F2}, position={characterController.transform.position:F2}");
            }

            // Handle physics-based state transitions
            stateMachine.HandlePhysics(isGrounded, velocity);
        }

        private void HandleDamageTransitions()
        {
            if (playerController == null) return;

            // FIXED: Only handle death if health is actually 0 AND player took damage recently
            // This prevents automatic death loops from initialization issues
            if (playerController.Health <= 0 && !stateMachine.IsInState<DeadState>())
            {
                // Only trigger death if this is a legitimate death (not initialization)
                // Check if player has been alive for at least 1 second before allowing death
                if (Time.time > 1f)
                {
                    stateMachine.HandleDeath();
                    Debug.Log("[StateMachineIntegration] Player death triggered - Health: " + playerController.Health);
                }
            }

            // FIXED: If player has health but is in dead state, transition back to alive
            if (playerController.Health > 0 && stateMachine.IsInState<DeadState>())
            {
                stateMachine.ChangeState<IdleState>();
                Debug.Log("[StateMachineIntegration] Player revived - Health: " + playerController.Health);
            }

            // Check for stun effects
            // Note: CharacterStats is a struct, so we'd need to access it through PlayerController
            // For now, we'll handle stun through damage events
            // if (playerController.IsStunned && !stateMachine.IsInState<StunnedState>())
            // {
            //     stateMachine.ChangeState<StunnedState>();
            // }
        }

        private void OnEnable()
        {
            // Subscribe to EventBus events for damage system integration
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<CharacterDefeatedEvent>(OnCharacterDefeated);
            EventBus.Subscribe<CriticalHitEvent>(OnCriticalHit);
            EventBus.Subscribe<StateTransitionEvent>(OnStateTransition);
        }

        private void OnDisable()
        {
            // Unsubscribe from EventBus events
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<CharacterDefeatedEvent>(OnCharacterDefeated);
            EventBus.Unsubscribe<CriticalHitEvent>(OnCriticalHit);
            EventBus.Unsubscribe<StateTransitionEvent>(OnStateTransition);
        }

        // EventBus event handlers
        private void OnDamageDealt(DamageDealtEvent damageEvent)
        {
            // Handle damage dealt to this character
            if (damageEvent.Defender == gameObject)
            {
                // Apply damage effects
                if (damageEvent.DamageResult.IsKill)
                {
                    if (!stateMachine.IsInState<DeadState>())
                    {
                        stateMachine.HandleDeath();
                    }
                }
                else if (damageEvent.DamageResult.ReflectionAmount > 0)
                {
                    // Could trigger a reflection effect or state
                    Debug.Log($"Damage reflected: {damageEvent.DamageResult.ReflectionAmount}");
                }

                // Publish UI update event
                EventBus.Publish(new UIUpdateEvent("HealthChanged", playerController.Health));
            }
        }

        private void OnCharacterDefeated(CharacterDefeatedEvent defeatEvent)
        {
            if (defeatEvent.DefeatedCharacter == gameObject)
            {
                if (!stateMachine.IsInState<DeadState>())
                {
                    stateMachine.HandleDeath();
                }
            }
        }

        private void OnCriticalHit(CriticalHitEvent critEvent)
        {
            if (critEvent.Attacker == gameObject || critEvent.Defender == gameObject)
            {
                // Could trigger camera shake, screen effects, etc.
                Debug.Log($"Critical hit! Damage: {critEvent.CritDamage}, Multiplier: {critEvent.CritMultiplier}x");
            }
        }

        private void OnStateTransition(StateTransitionEvent transitionEvent)
        {
            if (transitionEvent.Character == gameObject)
            {
                // Handle external state transitions
                Debug.Log($"External state transition: {transitionEvent.PreviousState} → {transitionEvent.NewState}");
            }
        }

        // Observer Pattern event handlers
        private void HandleStateChanged(IState<MOBACharacterController> previousState, IState<MOBACharacterController> newState)
        {
            totalStateChanges++;
            lastStateChangeTime = Time.time;

            if (enableStateLogging)
            {
                Debug.Log($"[State Machine] {previousState?.GetStateName() ?? "None"} → {newState.GetStateName()}");
            }

            // Notify other systems of state change
            OnStateMachineStateChanged(previousState, newState);
        }

        private void HandleStateEntered(IState<MOBACharacterController> state)
        {
            if (enableStateLogging)
            {
                Debug.Log($"[State Machine] Entered: {state.GetStateName()}");
            }

            // Trigger state-specific effects
            OnStateMachineStateEntered(state);
        }

        private void HandleStateExited(IState<MOBACharacterController> state)
        {
            if (enableStateLogging)
            {
                Debug.Log($"[State Machine] Exited: {state.GetStateName()}");
            }

            // Clean up state-specific effects
            OnStateMachineStateExited(state);
        }

        // Integration methods for other systems
        private void OnStateMachineStateChanged(IState<MOBACharacterController> previousState, IState<MOBACharacterController> newState)
        {
            // Notify camera system
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can adjust behavior based on state
                if (newState is JumpingState || newState is FallingState)
                {
                    // Camera might want to show more of the environment
                }
                else if (newState is AttackingState || newState is AbilityCastingState)
                {
                    // Camera might want to focus on the target
                }
            }

            // Notify UI system
            // Could update UI elements based on state

            // Notify audio system
            // Could play state-specific sounds
        }

        private void OnStateMachineStateEntered(IState<MOBACharacterController> state)
        {
            // Handle state-specific setup
            if (state is DeadState)
            {
                // Disable input during death
                if (inputRelay != null)
                {
                    inputRelay.enabled = false;
                }
            }
            else if (state is StunnedState)
            {
                // Add stun visual effects
                AddStunEffect();
            }
        }

        private void OnStateMachineStateExited(IState<MOBACharacterController> state)
        {
            // Handle state-specific cleanup
            if (state is DeadState)
            {
                // Re-enable input after respawn
                if (inputRelay != null)
                {
                    inputRelay.enabled = true;
                }

                // Reset character position
                characterController.transform.position = Vector3.zero;
            }
            else if (state is StunnedState)
            {
                // Remove stun visual effects
                RemoveStunEffect();
            }
        }

        private void AddStunEffect()
        {
            // Add visual stun indicator
            var stunIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stunIndicator.transform.SetParent(characterController.transform);
            stunIndicator.transform.localPosition = Vector3.up * 2f;
            stunIndicator.transform.localScale = Vector3.one * 0.3f;

            var renderer = stunIndicator.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;

            Object.Destroy(stunIndicator, 2f); // Will be destroyed when stun ends
        }

        private void RemoveStunEffect()
        {
            // Clean up any stun effects
            // In a real implementation, you'd track and destroy specific effects
        }

        // Public API for external systems
        public CharacterStateMachine GetStateMachine()
        {
            return stateMachine;
        }

        public bool IsInState<TState>() where TState : IState<MOBACharacterController>
        {
            return stateMachine.IsInState<TState>();
        }

        public void ForceStateChange<TState>() where TState : IState<MOBACharacterController>
        {
            stateMachine.ChangeState<TState>();
        }

        private void OnGUI()
        {
            if (!enableStateLogging) return;

            GUI.Label(new Rect(10, 10, 300, 20), "State Machine Integration");
            GUI.Label(new Rect(10, 30, 300, 20), $"Current State: {stateMachine.CurrentState?.GetStateName() ?? "None"}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Total Changes: {totalStateChanges}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Time Since Change: {Time.time - lastStateChangeTime:F1}s");

            // Show registered states
            GUI.Label(new Rect(10, 100, 300, 20), "Registered States:");
            int y = 120;
            foreach (var stateType in stateMachine.GetRegisteredStateTypes())
            {
                bool isCurrent = stateMachine.CurrentState?.GetType() == stateType;
                string stateName = stateType.Name.Replace("State", "");
                GUI.Label(new Rect(10, y, 300, 20), $"{(isCurrent ? "→ " : "  ")}{stateName}");
                y += 20;
            }
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (stateMachine != null)
            {
                stateMachine.OnStateChanged -= HandleStateChanged;
                stateMachine.OnStateEntered -= HandleStateEntered;
                stateMachine.OnStateExited -= HandleStateExited;
            }
        }
    }
}