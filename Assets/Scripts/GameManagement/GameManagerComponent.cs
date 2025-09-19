using UnityEngine;
using MOBA.Debugging;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Base interface for game manager components that handle specific aspects of game management.
    /// Ensures consistent lifecycle management and debugging context across all game manager components.
    /// </summary>
    public interface IGameManagerComponent
    {
        /// <summary>
        /// Initialize the component with reference to the main game manager
        /// </summary>
        /// <param name="gameManager">Main SimpleGameManager instance</param>
        void Initialize(SimpleGameManager gameManager);
        
        /// <summary>
        /// Shutdown the component and cleanup resources
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Update component logic (called from main manager's Update)
        /// </summary>
        void UpdateComponent();
    }
    
    /// <summary>
    /// Abstract base class for game manager components with common functionality.
    /// Provides shared debugging context and SimpleGameManager reference.
    /// </summary>
    public abstract class GameManagerComponent : MonoBehaviour, IGameManagerComponent
    {
        #region Protected Fields
        
        /// <summary>
        /// Reference to the main game manager
        /// </summary>
        protected SimpleGameManager simpleGameManager;
        
        /// <summary>
        /// Whether the component has been initialized
        /// </summary>
        protected bool isInitialized = false;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the component with the main game manager
        /// </summary>
        /// <param name="gameManager">Main SimpleGameManager instance</param>
        public virtual void Initialize(SimpleGameManager gameManager)
        {
            simpleGameManager = gameManager;
            isInitialized = true;
        }
        
        /// <summary>
        /// Shutdown the component and cleanup resources
        /// </summary>
        public virtual void Shutdown()
        {
            simpleGameManager = null;
            isInitialized = false;
        }
        
        /// <summary>
        /// Update component logic - override in derived classes
        /// </summary>
        public virtual void UpdateComponent()
        {
            // Override in derived classes
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for consistent logging across components
        /// </summary>
        /// <param name="tag">Debug mechanic tag</param>
        /// <returns>Debug context</returns>
        protected GameDebugContext BuildContext(GameDebugMechanicTag tag = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.GameLifecycle,
                GameDebugSystemTag.GameLifecycle,
                tag,
                subsystem: GetType().Name,
                actor: gameObject?.name);
        }
        
        #endregion
    }
}