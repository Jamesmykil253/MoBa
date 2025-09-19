using UnityEngine;

namespace MOBA.Abilities
{
    /// <summary>
    /// Interface for ability system components that can be initialized and managed
    /// </summary>
    public interface IAbilityManagerComponent
    {
        /// <summary>
        /// Initialize the component with reference to the main ability system
        /// </summary>
        /// <param name="abilitySystem">Main enhanced ability system</param>
        void Initialize(EnhancedAbilitySystem abilitySystem);
        
        /// <summary>
        /// Shutdown the component and clean up resources
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Update the component logic (called from main system's Update)
        /// </summary>
        void UpdateComponent();
    }
    
    /// <summary>
    /// Base class for all ability system components providing common functionality
    /// </summary>
    public abstract class AbilityManagerComponent : MonoBehaviour, IAbilityManagerComponent
    {
        /// <summary>
        /// Reference to the main enhanced ability system
        /// </summary>
        protected EnhancedAbilitySystem enhancedAbilitySystem;
        
        /// <summary>
        /// Whether this component has been initialized
        /// </summary>
        protected bool isInitialized = false;
        
        /// <summary>
        /// Initialize the component with reference to the main ability system
        /// </summary>
        /// <param name="abilitySystem">Main enhanced ability system</param>
        public virtual void Initialize(EnhancedAbilitySystem abilitySystem)
        {
            enhancedAbilitySystem = abilitySystem;
            isInitialized = true;
        }
        
        /// <summary>
        /// Shutdown the component and clean up resources
        /// </summary>
        public virtual void Shutdown()
        {
            isInitialized = false;
            enhancedAbilitySystem = null;
        }
        
        /// <summary>
        /// Update the component logic (called from main system's Update)
        /// </summary>
        public virtual void UpdateComponent()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Check if the component is properly initialized
        /// </summary>
        /// <returns>True if initialized and ready to use</returns>
        protected bool IsInitialized()
        {
            return isInitialized && enhancedAbilitySystem != null;
        }
    }
}