using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Generic State Machine implementation following the State Pattern
    /// Manages state transitions and provides event notifications
    /// Based on Game Programming Patterns by Robert Nystrom
    /// Thread-safe implementation following Clean Code principles
    /// </summary>
    /// <typeparam name="TContext">The type of object this state machine controls</typeparam>
    public class StateMachine<TContext>
    {
        protected TContext context;
        private volatile IState<TContext> currentState;
        private volatile IState<TContext> previousState;
        private Dictionary<Type, IState<TContext>> states;
        private readonly object stateLock = new object(); // Thread safety following Pragmatic Programmer principles

        // Events for state changes (Observer Pattern)
        public event Action<IState<TContext>, IState<TContext>> OnStateChanged;
        public event Action<IState<TContext>> OnStateEntered;
        public event Action<IState<TContext>> OnStateExited;

        public StateMachine(TContext context)
        {
            this.context = context;
            states = new Dictionary<Type, IState<TContext>>();
        }

        /// <summary>
        /// Register a state with the state machine
        /// </summary>
        public void RegisterState<TState>(TState state) where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (!states.ContainsKey(stateType))
            {
                state.SetContext(context);
                states[stateType] = state;
            }
        }

        /// <summary>
        /// Get a registered state by type
        /// </summary>
        public TState GetState<TState>() where TState : IState<TContext>
        {
            var stateType = typeof(TState);
            if (states.TryGetValue(stateType, out var state))
            {
                return (TState)state;
            }
            return default;
        }

        /// <summary>
        /// Change to a new state
        /// Thread-safe implementation following Clean Code defensive programming
        /// </summary>
        public void ChangeState<TState>() where TState : IState<TContext>
        {
            lock (stateLock) // Atomic state transition - Pragmatic Programmer principle
            {
                var newStateType = typeof(TState);

                if (!states.TryGetValue(newStateType, out var newState))
                {
                    Debug.LogError($"State {newStateType.Name} is not registered!");
                    return;
                }

                // Store references before transition to prevent race conditions
                var oldState = currentState;
                
                // Atomic state change - Critical section
                previousState = currentState;
                currentState = newState;

                // Execute transition logic outside of critical operations when possible
                try
                {
                    // Exit old state if exists
                    if (oldState != null)
                    {
                        oldState.Exit();
                        OnStateExited?.Invoke(oldState);
                    }

                    // Enter new state
                    currentState.Enter();
                    OnStateEntered?.Invoke(currentState);

                    // Notify listeners of complete transition
                    OnStateChanged?.Invoke(oldState, currentState);

                    Debug.Log($"State changed: {(oldState?.GetStateName() ?? "None")} -> {currentState.GetStateName()}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"State transition error: {ex.Message}");
                    // Restore previous state on error - defensive programming
                    if (oldState != null)
                    {
                        currentState = oldState;
                        previousState = null;
                    }
                }
            }
        }

        /// <summary>
        /// Update the current state
        /// Thread-safe read access following Clean Code principles
        /// </summary>
        public void Update()
        {
            IState<TContext> stateToUpdate;
            lock (stateLock)
            {
                stateToUpdate = currentState; // Get reference inside lock
            }
            
            // Execute update outside lock to prevent deadlocks
            stateToUpdate?.Update();
        }

        /// <summary>
        /// Get the current state
        /// Thread-safe property access
        /// </summary>
        public IState<TContext> CurrentState 
        { 
            get 
            { 
                lock (stateLock) 
                { 
                    return currentState; 
                } 
            } 
        }

        /// <summary>
        /// Get the previous state
        /// Thread-safe property access
        /// </summary>
        public IState<TContext> PreviousState 
        { 
            get 
            { 
                lock (stateLock) 
                { 
                    return previousState; 
                } 
            } 
        }

        /// <summary>
        /// Check if currently in a specific state
        /// Thread-safe comparison following defensive programming
        /// </summary>
        public bool IsInState<TState>() where TState : IState<TContext>
        {
            lock (stateLock)
            {
                return currentState?.GetType() == typeof(TState);
            }
        }

        /// <summary>
        /// Force exit current state without entering a new one
        /// Thread-safe operation with error handling
        /// </summary>
        public void ExitCurrentState()
        {
            lock (stateLock)
            {
                if (currentState != null)
                {
                    try
                    {
                        currentState.Exit();
                        OnStateExited?.Invoke(currentState);
                        previousState = currentState;
                        currentState = null;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error exiting state: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Revert to the previous state
        /// </summary>
        public void RevertToPreviousState()
        {
            if (previousState != null)
            {
                // Find the state instance and change to it
                var stateType = previousState.GetType();
                if (states.TryGetValue(stateType, out var stateInstance))
                {
                    // Exit current state
                    if (currentState != null)
                    {
                        currentState.Exit();
                        OnStateExited?.Invoke(currentState);
                    }

                    // Store previous state
                    var tempPrevious = currentState;

                    // Enter new state
                    currentState = stateInstance;
                    currentState.Enter();
                    OnStateEntered?.Invoke(currentState);

                    // Notify listeners
                    OnStateChanged?.Invoke(tempPrevious, currentState);

                    Debug.Log($"State reverted: {(tempPrevious?.GetStateName() ?? "None")} -> {currentState.GetStateName()}");
                }
            }
        }

        /// <summary>
        /// Get all registered state types
        /// </summary>
        public IEnumerable<Type> GetRegisteredStateTypes()
        {
            return states.Keys;
        }

        /// <summary>
        /// Clear all registered states
        /// </summary>
        public void ClearStates()
        {
            if (currentState != null)
            {
                currentState.Exit();
                OnStateExited?.Invoke(currentState);
            }

            states.Clear();
            currentState = null;
            previousState = null;
        }
    }

    /// <summary>
    /// Specialized state machine for character controllers
    /// Includes additional MOBA-specific functionality
    /// </summary>
    public class CharacterStateMachine : StateMachine<MOBACharacterController>
    {
        public CharacterStateMachine(MOBACharacterController controller) : base(controller)
        {
            // Register common MOBA character states
            RegisterState(new IdleState(controller));
            RegisterState(new MovingState(controller));
            RegisterState(new JumpingState(controller));
            RegisterState(new FallingState(controller));
            RegisterState(new AttackingState(controller));
            RegisterState(new AbilityCastingState(controller));
            RegisterState(new StunnedState(controller));
            RegisterState(new DeadState(controller));

            // Start in idle state
            ChangeState<IdleState>();
        }

        /// <summary>
        /// Get the context (controller) for this state machine
        /// </summary>
        public MOBACharacterController Context => context;

        /// <summary>
        /// Handle input-based state transitions
        /// </summary>
        public void HandleInput(Vector3 movementInput, bool jumpPressed, bool attackPressed, bool abilityPressed)
        {
            // Movement input
            if (movementInput != Vector3.zero && IsInState<IdleState>())
            {
                ChangeState<MovingState>();
            }
            else if (movementInput == Vector3.zero && IsInState<MovingState>())
            {
                ChangeState<IdleState>();
            }

            // Jump input
            if (jumpPressed && (IsInState<IdleState>() || IsInState<MovingState>()))
            {
                ChangeState<JumpingState>();
            }

            // Attack input
            if (attackPressed && !IsInState<AttackingState>() && !IsInState<AbilityCastingState>())
            {
                ChangeState<AttackingState>();
            }

            // Ability input
            if (abilityPressed && !IsInState<AttackingState>() && !IsInState<AbilityCastingState>())
            {
                ChangeState<AbilityCastingState>();
            }
        }

        /// <summary>
        /// Handle physics-based state transitions
        /// </summary>
        public void HandlePhysics(bool isGrounded, Vector3 velocity)
        {
            // Handle falling
            if (!isGrounded && velocity.y < -0.1f && !IsInState<FallingState>() && !IsInState<JumpingState>())
            {
                ChangeState<FallingState>();
            }

            // Handle landing
            if (isGrounded && (IsInState<FallingState>() || IsInState<JumpingState>()))
            {
                if (Context.GetComponent<MOBACharacterController>().MovementInput != Vector3.zero)
                {
                    ChangeState<MovingState>();
                }
                else
                {
                    ChangeState<IdleState>();
                }
            }
        }

        /// <summary>
        /// Handle damage-based state transitions
        /// </summary>
        public void HandleDamage(float damage)
        {
            if (damage > 0 && !IsInState<DeadState>())
            {
                ChangeState<StunnedState>();
            }
        }

        /// <summary>
        /// Handle death state transition
        /// </summary>
        public void HandleDeath()
        {
            if (!IsInState<DeadState>())
            {
                ChangeState<DeadState>();
            }
        }
    }
}