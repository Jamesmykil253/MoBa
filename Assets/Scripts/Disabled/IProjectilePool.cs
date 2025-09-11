namespace MOBA
{
    /// <summary>
    /// Interface for projectile pool implementations
    /// Allows Projectile class to work with both ProjectilePool and EnhancedProjectilePool
    /// </summary>
    public interface IProjectilePool
    {
        /// <summary>
        /// Returns a projectile to the pool
        /// </summary>
        /// <param name="projectile">Projectile to return</param>
        void ReturnProjectile(Projectile projectile);
    }
}
