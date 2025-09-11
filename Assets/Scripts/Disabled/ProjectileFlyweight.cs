using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Flyweight pattern implementation for projectiles
    /// Shares immutable data across multiple projectile instances
    /// Based on Game Programming Patterns by Robert Nystrom
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileFlyweight", menuName = "MOBA/Projectile Flyweight")]
    public class ProjectileFlyweight : ScriptableObject
    {
        [Header("Visual Properties")]
        public Sprite sprite;
        public Material material;
        public Color color = Color.white;

        [Header("Physical Properties")]
        public float speed = 10f;
        public float damage = 50f;
        public float lifetime = 3f;
        public float size = 1f;

        [Header("Effects")]
        public ParticleSystem trailEffect;
        public AudioClip spawnSound;
        public AudioClip hitSound;

        [Header("Behavior")]
        public bool homing = false;
        public float turnSpeed = 180f;
        public LayerMask hitLayers;

        [Header("Critical Hit")]
        public float critChance = 0.1f;      // Base critical hit chance (0-1)
        public float critMultiplier = 1.5f;  // Critical hit damage multiplier

        /// <summary>
        /// Creates a copy of this flyweight for modification
        /// </summary>
        public ProjectileFlyweight Clone()
        {
            var clone = CreateInstance<ProjectileFlyweight>();
            clone.sprite = sprite;
            clone.material = material;
            clone.color = color;
            clone.speed = speed;
            clone.damage = damage;
            clone.lifetime = lifetime;
            clone.size = size;
            clone.trailEffect = trailEffect;
            clone.spawnSound = spawnSound;
            clone.hitSound = hitSound;
            clone.homing = homing;
            clone.turnSpeed = turnSpeed;
            clone.hitLayers = hitLayers;
            clone.critChance = critChance;
            clone.critMultiplier = critMultiplier;
            return clone;
        }
    }
}