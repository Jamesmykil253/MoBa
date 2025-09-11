using Unity.Netcode;
using UnityEngine;

namespace MOBA.Networking
{
    /// <summary>
    /// Network projectile with server authority and client interpolation
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkProjectile : NetworkBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private int damage = 100;
        [SerializeField] private float interpolationSpeed = 10f;

        [Header("Visual Settings")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private TrailRenderer trailRenderer;

        // Network state
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> networkActive = new NetworkVariable<bool>(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Client interpolation
        private Vector3 targetPosition;
        private Vector3 targetVelocity;
        private float lastNetworkUpdate;

        // Components
        private Rigidbody rb;
        private Collider projectileCollider;

        // State
        private float spawnTime;
        private bool hasHit = false;
        private ulong ownerClientId;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            projectileCollider = GetComponent<Collider>();

            // Disable gravity for projectiles
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        public override void OnNetworkSpawn()
        {
            spawnTime = Time.time;
            hasHit = false;

            if (IsServer)
            {
                // Server authoritative initialization
                networkPosition.Value = transform.position;
                networkVelocity.Value = rb.linearVelocity;
                networkActive.Value = true;

                // Get owner from spawn parameters
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    NetworkObjectId, out var networkObject))
                {
                    ownerClientId = networkObject.OwnerClientId;
                }
            }
            else
            {
                // Client initialization
                targetPosition = transform.position;
                targetVelocity = rb.linearVelocity;
            }

            // Subscribe to network variable changes
            networkPosition.OnValueChanged += OnPositionChanged;
            networkVelocity.OnValueChanged += OnVelocityChanged;
            networkActive.OnValueChanged += OnActiveChanged;
        }

        public override void OnNetworkDespawn()
        {
            networkPosition.OnValueChanged -= OnPositionChanged;
            networkVelocity.OnValueChanged -= OnVelocityChanged;
            networkActive.OnValueChanged -= OnActiveChanged;
        }

        private void Update()
        {
            if (IsServer)
            {
                ServerUpdate();
            }
            else
            {
                ClientUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                ServerFixedUpdate();
            }
            else
            {
                ClientFixedUpdate();
            }
        }

        private void ServerUpdate()
        {
            // Lifetime check
            if (Time.time - spawnTime > lifetime && networkActive.Value)
            {
                networkActive.Value = false;
                NetworkObject.Despawn();
            }
        }

        private void ServerFixedUpdate()
        {
            if (!networkActive.Value) return;

            // Update network variables
            networkPosition.Value = transform.position;
            networkVelocity.Value = rb.linearVelocity;
        }

        private void ClientUpdate()
        {
            // Client-side lifetime check
            if (Time.time - spawnTime > lifetime && networkActive.Value)
            {
                networkActive.Value = false;
            }
        }

        private void ClientFixedUpdate()
        {
            if (!networkActive.Value) return;

            // Client interpolation
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * interpolationSpeed);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit || !networkActive.Value) return;

            // Notify server of collision
            OnCollisionServerRpc(collision.GetContact(0).point, collision.gameObject.GetComponent<NetworkObject>()?.NetworkObjectId ?? 0);
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnCollisionServerRpc(Vector3 hitPoint, ulong hitObjectId, ServerRpcParams rpcParams = default)
        {
            if (hasHit || !networkActive.Value) return;

            hasHit = true;
            networkActive.Value = false;

            // Handle collision on server
            HandleServerCollision(hitPoint, hitObjectId);

            // Notify clients
            CollisionClientRpc(hitPoint);

            // Destroy projectile
            NetworkObject.Despawn();
        }

        [ClientRpc]
        private void CollisionClientRpc(Vector3 hitPoint)
        {
            // Client collision effects
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            }

            // Disable trail
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }
        }

        private void HandleServerCollision(Vector3 hitPoint, ulong hitObjectId)
        {
            // Find the hit object
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(hitObjectId, out var hitNetworkObject))
            {
                var hitGameObject = hitNetworkObject.gameObject;

                // Check if it's a player
                var playerController = hitGameObject.GetComponent<NetworkPlayerController>();
                if (playerController != null)
                {
                    // Don't damage owner
                    if (hitNetworkObject.OwnerClientId != ownerClientId)
                    {
                        playerController.TakeDamage(damage);
                        Debug.Log($"[NetworkProjectile] Hit player {hitNetworkObject.OwnerClientId} for {damage} damage");
                    }
                }
                else
                {
                    // Hit environment or other object
                    Debug.Log($"[NetworkProjectile] Hit object {hitObjectId} at {hitPoint}");
                }
            }

            // Create hit effect
            if (hitEffectPrefab != null)
            {
                var effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
                Object.Destroy(effect, 2f);
            }
        }

        // Network variable change handlers
        private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (!IsServer)
            {
                targetPosition = newValue;
                lastNetworkUpdate = Time.time;
            }
        }

        private void OnVelocityChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (!IsServer)
            {
                targetVelocity = newValue;
            }
        }

        private void OnActiveChanged(bool previousValue, bool newValue)
        {
            if (!newValue)
            {
                // Deactivate projectile
                if (projectileCollider != null)
                {
                    projectileCollider.enabled = false;
                }

                if (trailRenderer != null)
                {
                    trailRenderer.enabled = false;
                }

                // Start fade out
                StartCoroutine(FadeOut());
            }
        }

        private System.Collections.IEnumerator FadeOut()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                float duration = 0.5f;
                float elapsed = 0f;
                Color startColor = renderer.material.color;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    renderer.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }
            }

            // Self-destruct on client
            if (!IsServer)
            {
                Object.Destroy(gameObject);
            }
        }

        // Public API for spawning
        public static NetworkProjectile SpawnProjectile(
            Vector3 position,
            Vector3 direction,
            int damage,
            float lifetime,
            ulong ownerClientId)
        {
            if (!NetworkManager.Singleton.IsServer) return null;

            // Removed automatic Resources.Load to prevent automatic loading
            // Use SpawnProjectileWithPrefab() method with manually provided prefab instead
            Debug.LogError("[NetworkProjectile] Automatic prefab loading disabled - use SpawnProjectileWithPrefab() instead");
            return null;
        }
    }
}