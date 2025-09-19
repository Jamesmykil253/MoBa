using UnityEngine;

namespace MOBA.Debugging
{
    /// <summary>
    /// Identifies high-level systems for fine-grained debug filtering.
    /// </summary>
    public enum GameDebugSystemTag
    {
        Core,
        GameLifecycle,
        Player,
        Enemy,
        AI,
        Movement,
        Combat,
        Ability,
        Health,
        Resource,
        Projectile,
        Pooling,
        Networking,
        Input,
        UI,
        Camera,
        Configuration,
        Initialization,
        Scene,
        Performance,
        ErrorHandling,
        Audio
    }

    /// <summary>
    /// Identifies gameplay mechanics or concerns for debug filtering.
    /// </summary>
    public enum GameDebugMechanicTag
    {
        General,
        Lifecycle,
        Spawning,
        StateChange,
        Damage,
        Healing,
        Cooldown,
        Resource,
        Input,
        Camera,
        Movement,
        Targeting,
        Validation,
        Pooling,
        Networking,
        AI,
        Metrics,
        Recovery,
        Initialization,
        Configuration,
        Combat,
        AbilityUse,
        Score,
        Jump,
        AntiCheat,
        UI,
        Events,
        MatchLifecycle
    }
}
