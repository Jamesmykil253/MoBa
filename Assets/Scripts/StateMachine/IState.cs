using System;
using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Interface for all character states in the MOBA game
    /// Based on the State Pattern from Game Programming Patterns
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when entering this state
        /// </summary>
        void Enter();

        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        void Update();

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void Exit();

        /// <summary>
        /// Get the name of this state for debugging
        /// </summary>
        string GetStateName();
    }

    /// <summary>
    /// Generic state interface with context parameter
    /// Allows states to work with different controller types
    /// </summary>
    /// <typeparam name="TContext">The type of controller this state works with</typeparam>
    public interface IState<TContext> : IState
    {
        /// <summary>
        /// Set the context (controller) for this state
        /// </summary>
        void SetContext(TContext context);
    }

    /// <summary>
    /// Base class for character states with common functionality
    /// </summary>
    public abstract class CharacterStateBase : IState<UnifiedPlayerController>
    {
        protected UnifiedPlayerController controller;
        protected float stateTimer;

        public void SetContext(UnifiedPlayerController context)
        {
            controller = context;
        }

        public virtual void Enter()
        {
            stateTimer = 0f;
            OnEnter();
        }

        public virtual void Update()
        {
            stateTimer += Time.deltaTime;
            OnUpdate();
        }

        public virtual void Exit()
        {
            OnExit();
        }

        /// <summary>
        /// Override this for state-specific enter logic
        /// </summary>
        protected virtual void OnEnter() { }

        /// <summary>
        /// Override this for state-specific update logic
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Override this for state-specific exit logic
        /// </summary>
        protected virtual void OnExit() { }

        public abstract string GetStateName();
    }
}