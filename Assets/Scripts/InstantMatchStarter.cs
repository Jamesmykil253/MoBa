using UnityEngine;
using MOBA.Debugging;

namespace MOBA
{
    /// <summary>
    /// Ensures a <see cref="SimpleGameManager"/> is present and starts a match immediately.
    /// Place this in a bootstrap scene to skip manual setup.
    /// </summary>
    public class InstantMatchStarter : MonoBehaviour
    {
        private static readonly GameDebugContext DebugContext = new GameDebugContext(
            GameDebugCategory.GameLifecycle,
            GameDebugSystemTag.GameLifecycle,
            GameDebugMechanicTag.Initialization,
            subsystem: nameof(InstantMatchStarter));

        [Header("Game Manager Source")]
        [SerializeField] private SimpleGameManager existingGameManager;
        [SerializeField] private SimpleGameManager gameManagerPrefab;
        [SerializeField] private bool instantiateIfMissing = true;

        [Header("Behaviour")]
        [SerializeField] private bool startOnAwake = true;

        private void Awake()
        {
            GameDebug.Log(DebugContext, "Ensuring game manager on Awake.");
            EnsureGameManager();

            if (startOnAwake)
            {
                StartMatch();
            }
        }

        /// <summary>
        /// Starts the match immediately, instantiating a manager if needed.
        /// </summary>
        public void StartMatch()
        {
            EnsureGameManager();

            if (existingGameManager == null)
            {
                GameDebug.LogError(DebugContext, "No SimpleGameManager available to start a match.");
                return;
            }

            if (!existingGameManager.StartMatch())
            {
                GameDebug.LogWarning(DebugContext, "SimpleGameManager reported an active match; skipping duplicate start.");
            }
        }

        private void EnsureGameManager()
        {
            if (existingGameManager != null)
            {
                return;
            }

            existingGameManager = FindFirstObjectByType<SimpleGameManager>();

            if (existingGameManager == null && instantiateIfMissing && gameManagerPrefab != null)
            {
                existingGameManager = Instantiate(gameManagerPrefab);
                GameDebug.Log(DebugContext, "Instantiated fallback SimpleGameManager instance.");
            }
        }
    }
}
