using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using MOBA.Abilities;
using MOBA.Debugging;

namespace MOBA.Networking
{
    public enum AbilityFailureCode : byte
    {
        None = 0,
        InvalidAbility = 1,
        AbilityLocked = 2,
        OnCooldown = 3,
        GlobalCooldown = 4,
        InsufficientMana = 5,
        NotOwner = 6,
        Unknown = 255
    }

    [Serializable]
    public struct AbilityCastRequest : INetworkSerializable
    {
        public uint RequestId;
        public ushort AbilityIndex;
        public Vector3 TargetPosition;
        public Vector3 TargetDirection;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RequestId);
            serializer.SerializeValue(ref AbilityIndex);
            serializer.SerializeValue(ref TargetPosition);
            serializer.SerializeValue(ref TargetDirection);
        }
    }

    [Serializable]
    public struct AbilityHitResult : INetworkSerializable
    {
        public ulong TargetNetworkId;
        public float AppliedDamage;
        public float RemainingHealth;
        public bool IsDead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TargetNetworkId);
            serializer.SerializeValue(ref AppliedDamage);
            serializer.SerializeValue(ref RemainingHealth);
            serializer.SerializeValue(ref IsDead);
        }
    }

    [Serializable]
    public struct AbilityCastResult : INetworkSerializable
    {
        public uint RequestId;
        public ushort AbilityIndex;
        public bool Approved;
        public AbilityFailureCode FailureCode;
        public float CurrentMana;
        public float CooldownSeconds;
        public float GlobalCooldownSeconds;
        public FixedList128Bytes<AbilityHitResult> Hits;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RequestId);
            serializer.SerializeValue(ref AbilityIndex);
            serializer.SerializeValue(ref Approved);
            serializer.SerializeValue(ref FailureCode);
            serializer.SerializeValue(ref CurrentMana);
            serializer.SerializeValue(ref CooldownSeconds);
            serializer.SerializeValue(ref GlobalCooldownSeconds);

            int count = Hits.Length;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader)
            {
                Hits = new FixedList128Bytes<AbilityHitResult>();
            }

            if (serializer.IsWriter)
            {
                for (int i = 0; i < count && i < Hits.Length; i++)
                {
                    var hit = Hits[i];
                    serializer.SerializeValue(ref hit);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    AbilityHitResult hit = default;
                    serializer.SerializeValue(ref hit);
                    Hits.Add(hit);
                }
            }
        }

        public static AbilityCastResult CreateFailure(uint requestId, int abilityIndex, AbilityFailureCode code, float currentMana)
        {
            return new AbilityCastResult
            {
                RequestId = requestId,
                AbilityIndex = (ushort)Mathf.Max(0, abilityIndex),
                Approved = false,
                FailureCode = code,
                CurrentMana = currentMana,
                CooldownSeconds = 0f,
                GlobalCooldownSeconds = 0f,
                Hits = new FixedList128Bytes<AbilityHitResult>()
            };
        }

        public static AbilityCastResult CreateSuccess(uint requestId, int abilityIndex, float currentMana, float cooldown, float globalCooldown, IList<AbilityHitResult> hits)
        {
            var result = new AbilityCastResult
            {
                RequestId = requestId,
                AbilityIndex = (ushort)Mathf.Max(0, abilityIndex),
                Approved = true,
                FailureCode = AbilityFailureCode.None,
                CurrentMana = currentMana,
                CooldownSeconds = cooldown,
                GlobalCooldownSeconds = globalCooldown,
                Hits = new FixedList128Bytes<AbilityHitResult>()
            };

            if (hits != null)
            {
                for (int i = 0; i < hits.Count; i++)
                {
                    if (result.Hits.Length < result.Hits.Capacity)
                    {
                        result.Hits.Add(hits[i]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Bridges EnhancedAbilitySystem with Netcode for GameObjects, ensuring all casts are processed server-side.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class AbilityNetworkController : NetworkBehaviour
    {
        [SerializeField] private EnhancedAbilitySystem abilitySystem;

        private readonly Dictionary<uint, ushort> pendingRequests = new();
        private uint nextRequestId = 1;

        public bool HasNetworkAuthority => !IsSpawned || IsServer;

        private void Awake()
        {
            if (abilitySystem == null)
            {
                abilitySystem = GetComponent<EnhancedAbilitySystem>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (abilitySystem != null)
            {
                abilitySystem.AttachNetworkBridge(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (abilitySystem != null)
            {
                abilitySystem.DetachNetworkBridge(this);
            }
        }

        public bool RequestAbilityCast(int abilityIndex, Vector3 targetPosition, Vector3 targetDirection)
        {
            if (abilitySystem == null)
            {
                return false;
            }

            uint requestId = GenerateRequestId();
            var request = new AbilityCastRequest
            {
                RequestId = requestId,
                AbilityIndex = (ushort)Mathf.Max(0, abilityIndex),
                TargetPosition = targetPosition,
                TargetDirection = targetDirection
            };

            if (HasNetworkAuthority)
            {
                abilitySystem.HandleAbilityCastRequest(request, OwnerClientId);
                return true;
            }

            if (!IsOwner)
            {
                GameDebug.LogWarning(new GameDebugContext(GameDebugCategory.Networking, GameDebugSystemTag.Networking, GameDebugMechanicTag.Validation, subsystem: nameof(AbilityNetworkController)),
                    "Non-owner attempted to send ability cast request.");
                return false;
            }

            if (!pendingRequests.ContainsKey(request.RequestId))
            {
                pendingRequests.Add(request.RequestId, request.AbilityIndex);
            }

            SubmitAbilityCastServerRpc(request);
            return true;
        }

        internal void NotifyAbilityCastResult(AbilityCastResult result)
        {
            if (IsServer)
            {
                if (pendingRequests.ContainsKey(result.RequestId))
                {
                    pendingRequests.Remove(result.RequestId);
                }
                abilitySystem?.ApplyAbilityResult(result, true);
                SendAbilityCastResultClientRpc(result);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitAbilityCastServerRpc(AbilityCastRequest request, ServerRpcParams serverRpcParams = default)
        {
            if (!IsServer || abilitySystem == null)
            {
                return;
            }

            ulong senderClientId = serverRpcParams.Receive.SenderClientId;
            var networkManager = NetworkManager.Singleton;
            ulong serverClientId = networkManager != null ? NetworkManager.ServerClientId : senderClientId;

            if (networkManager != null && NetworkObject != null && NetworkObject.OwnerClientId != senderClientId && senderClientId != serverClientId)
            {
                GameDebug.LogWarning(
                    new GameDebugContext(GameDebugCategory.Networking, GameDebugSystemTag.Networking, GameDebugMechanicTag.Validation, subsystem: nameof(AbilityNetworkController)),
                    "Rejected ability cast from non-owner client.",
                    ("OwnerClientId", NetworkObject.OwnerClientId),
                    ("SenderClientId", senderClientId),
                    ("AbilityIndex", request.AbilityIndex));
                return;
            }

            abilitySystem.HandleAbilityCastRequest(request, senderClientId);
        }

        [ClientRpc]
        private void SendAbilityCastResultClientRpc(AbilityCastResult result, ClientRpcParams clientRpcParams = default)
        {
            bool isAuthorityInstance = HasNetworkAuthority;
            if (!isAuthorityInstance)
            {
                abilitySystem?.ApplyAbilityResult(result, false);
            }

            if (pendingRequests.ContainsKey(result.RequestId))
            {
                pendingRequests.Remove(result.RequestId);
            }

            if (NetworkManager.Singleton == null)
            {
                return;
            }

            var spawnManager = NetworkManager.Singleton.SpawnManager;
            foreach (var hit in result.Hits)
            {
                if (spawnManager.SpawnedObjects.TryGetValue(hit.TargetNetworkId, out var netObj))
                {
                    foreach (var snapshotReceiver in netObj.GetComponents<IDamageSnapshotReceiver>())
                    {
                        snapshotReceiver.ApplyServerHealthSnapshot(hit.RemainingHealth, hit.IsDead);
                    }
                }
            }
        }

        internal uint GenerateRequestId()
        {
            if (nextRequestId == 0)
            {
                nextRequestId = 1;
            }
            return nextRequestId++;
        }
    }
}
