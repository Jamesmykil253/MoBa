using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Ensures a <see cref="SimpleGameManager"/> is present and starts a match immediately.
    /// Place this in a bootstrap scene to skip manual setup.
    /// </summary>
    public class InstantMatchStarter : MonoBehaviour
    {
        [Header("Game Manager Source")]
        [SerializeField] private SimpleGameManager existingGameManager;
        [SerializeField] private SimpleGameManager gameManagerPrefab;
        [SerializeField] private bool instantiateIfMissing = true;

        [Header("Behaviour")]
        [SerializeField] private bool startOnAwake = true;

        private void Awake()
        {
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
                Debug.LogError("[InstantMatchStarter] No SimpleGameManager available to start a match.");
                return;
            }

            if (!existingGameManager.StartMatch())
            {
                Debug.LogWarning("[InstantMatchStarter] SimpleGameManager reported an active match; no additional match was started.");
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
            }
        }
    }
}
