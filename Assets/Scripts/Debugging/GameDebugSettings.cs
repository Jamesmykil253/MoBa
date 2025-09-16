using UnityEngine;

namespace MOBA.Debugging
{
    /// <summary>
    /// ScriptableObject that controls runtime debug logging behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "MOBA/Debug Settings", order = 0)]
    public class GameDebugSettings : ScriptableObject
    {
        [Header("Global")]
        public bool logEverything = true;
        public bool suppressWarnings = false;

        [Header("Categories")]
        public bool logGameLifecycle = true;
        public bool logPlayer = true;
        public bool logCamera = true;
        public bool logAbility = true;
        public bool logEnemy = true;
        public bool logNetworking = true;
        public bool logInput = true;
        public bool logPooling = true;
        public bool logUI = true;
        public bool logPerformance = true;
        public bool logErrorHandling = true;

        /// <summary>
        /// Determines whether the specified category should be logged.
        /// </summary>
        public bool IsEnabled(GameDebugCategory category)
        {
            if (logEverything)
                return true;

            return category switch
            {
                GameDebugCategory.GameLifecycle => logGameLifecycle,
                GameDebugCategory.Player => logPlayer,
                GameDebugCategory.Camera => logCamera,
                GameDebugCategory.Ability => logAbility,
                GameDebugCategory.Enemy => logEnemy,
                GameDebugCategory.Networking => logNetworking,
                GameDebugCategory.Input => logInput,
                GameDebugCategory.Pooling => logPooling,
                GameDebugCategory.UI => logUI,
                GameDebugCategory.Performance => logPerformance,
                GameDebugCategory.ErrorHandling => logErrorHandling,
                _ => logEverything
            };
        }
    }
}
