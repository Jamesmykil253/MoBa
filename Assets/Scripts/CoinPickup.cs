using UnityEngine;
using Unity.Netcode;
using MOBA.Debugging;

namespace MOBA
{
    /// <summary>
    /// Collectible coin that awards score when the player touches it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CoinPickup : MonoBehaviour
    {
        [Header("Coin Settings")]
        [SerializeField] private int scoreValue = 1;
        [SerializeField] private float lifetimeSeconds = 30f;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 spinSpeed = new Vector3(0f, 90f, 0f);

        [Header("Feedback")]
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private float soundVolume = 0.65f;

        private bool collected;
        private Collider triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            if (lifetimeSeconds > 0f)
            {
                Destroy(gameObject, lifetimeSeconds);
            }
        }

        private void Update()
        {
            if (visualRoot != null && spinSpeed != Vector3.zero)
            {
                visualRoot.Rotate(spinSpeed * Time.deltaTime, Space.Self);
            }
        }

        public void Initialize(int value)
        {
            scoreValue = Mathf.Max(1, value);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
            {
                return;
            }

            var player = other.GetComponentInParent<SimplePlayerController>();
            if (player == null)
            {
                return;
            }

            Collect(player);
        }

        private void Collect(SimplePlayerController collector)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }
            collected = true;
            triggerCollider.enabled = false;

            AwardScore(collector);
            PlayFeedback();

            GameDebug.Log(new GameDebugContext(
                    GameDebugCategory.GameLifecycle,
                    GameDebugSystemTag.Player,
                    GameDebugMechanicTag.Score,
                    subsystem: nameof(CoinPickup)),
                "Coin collected.",
                ("Score", scoreValue));

            Destroy(gameObject);
        }

        private void AwardScore(SimplePlayerController collector)
        {
            var gameManager = SimpleGameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<SimpleGameManager>();
            }

            if (gameManager != null)
            {
                int teamToCredit = collector != null ? collector.TeamIndex : 0;
                if (!gameManager.AddScore(teamToCredit, scoreValue))
                {
                    GameDebug.LogWarning(new GameDebugContext(
                            GameDebugCategory.GameLifecycle,
                            GameDebugSystemTag.GameLifecycle,
                            GameDebugMechanicTag.Score,
                            subsystem: nameof(CoinPickup)),
                        "Attempted to credit score to invalid team; falling back to team 0.",
                        ("RequestedTeam", teamToCredit));
                    gameManager.AddScore(0, scoreValue);
                }
            }
            else
            {
                GameDebug.LogWarning(new GameDebugContext(
                        GameDebugCategory.GameLifecycle,
                        GameDebugSystemTag.GameLifecycle,
                        GameDebugMechanicTag.Score,
                        subsystem: nameof(CoinPickup)),
                    "Coin collected but no SimpleGameManager was found to receive the score.");
            }
        }

        private void PlayFeedback()
        {
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
            }
        }
    }
}
