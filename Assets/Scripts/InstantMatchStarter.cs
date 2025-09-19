using System.Collections;
using Unity.Netcode;
using UnityEngine;
using MOBA.Debugging;
using MOBA.Networking;

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
        [SerializeField, Tooltip("Seconds to wait for the networking stack to become authoritative before forcing a start.")]
        private float networkReadinessTimeout = 5f;

        private Coroutine startRoutine;

        private void Awake()
        {
            GameDebug.Log(DebugContext, "Ensuring game manager on Awake.");
            EnsureGameManager();
        }

        private void Start()
        {
            if (!startOnAwake)
            {
                return;
            }

            ScheduleStartIfNeeded();
        }

        /// <summary>
        /// Starts the match immediately, instantiating a manager if needed.
        /// </summary>
        public void StartMatch()
        {
            EnsureGameManager();

            if (AttemptStartMatch())
            {
                CancelScheduledStart();
                return;
            }

            ScheduleStartIfNeeded();
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

        private void ScheduleStartIfNeeded()
        {
            if (startRoutine != null)
            {
                return;
            }

            startRoutine = StartCoroutine(StartWhenNetworkReady());
        }

        private void CancelScheduledStart()
        {
            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }
        }

        private IEnumerator StartWhenNetworkReady()
        {
            float timeout = Time.realtimeSinceStartup + Mathf.Max(0.5f, networkReadinessTimeout);

            while (Time.realtimeSinceStartup < timeout)
            {
                if (AttemptStartMatch())
                {
                    startRoutine = null;
                    yield break;
                }

                yield return null;
            }

            GameDebug.LogWarning(DebugContext,
                "Network readiness timeout reached; forcing match start.",
                ("TimeoutSeconds", networkReadinessTimeout));

            AttemptStartMatch();
            startRoutine = null;
        }

        private bool AttemptStartMatch()
        {
            if (existingGameManager == null)
            {
                GameDebug.LogError(DebugContext, "No SimpleGameManager available to start a match.");
                return false;
            }

            if (!IsNetworkAuthoritative())
            {
                return false;
            }

            existingGameManager.StartMatch();
            
            if (existingGameManager.IsGameActive())
            {
                return true;
            }

            GameDebug.LogWarning(DebugContext, "SimpleGameManager declined to start the match.");
            return false;
        }

        private bool IsNetworkAuthoritative()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return true;
            }

            if (!networkManager.IsListening)
            {
                return false;
            }

            if (networkManager.IsServer || networkManager.IsHost)
            {
                return true;
            }

            var productionManager = ProductionNetworkManager.Instance;
            if (productionManager != null && (productionManager.IsServer() || productionManager.IsHost()))
            {
                return true;
            }

            return false;
        }
    }
}
