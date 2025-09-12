namespace MOBA
{
    /// <summary>
    /// Interface for objects that can take damage
    /// Following game programming patterns for component-based damage system
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this object
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        void TakeDamage(float damage);
        
        /// <summary>
        /// Get current health of this object
        /// </summary>
        /// <returns>Current health value</returns>
        float GetHealth();
        
        /// <summary>
        /// Check if this object is dead/destroyed
        /// </summary>
        /// <returns>True if dead, false if alive</returns>
        bool IsDead();
    }
}
