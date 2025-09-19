using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using MOBA.Networking;
using MOBA.Abilities;

namespace MOBA.Tests.PlayMode
{
    internal class NetworkSoakTests : NetcodeIntegrationTest
    {
        private ProductionNetworkManager serverNetworkManager;
        private readonly List<ProductionNetworkManager> clientNetworkManagers = new();
        private EnhancedAbility testAbility;
        protected override int NumberOfClients => 2;

        [SetUp]
        public void TestSetUp()
        {
            ProductionNetworkManager.AllowMultipleInstancesForTesting = true;
        }

        [TearDown]
        public void TestTearDown()
        {
            ProductionNetworkManager.AllowMultipleInstancesForTesting = false;
            if (testAbility != null)
            {
                ScriptableObject.DestroyImmediate(testAbility);
                testAbility = null;
            }
        }

        protected override void OnCreatePlayerPrefab()
        {
            base.OnCreatePlayerPrefab();
            var networkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();
            }

            var abilitySystem = m_PlayerPrefab.AddComponent<EnhancedAbilitySystem>();
            abilitySystem.SetInputEnabled(false);
            m_PlayerPrefab.AddComponent<AbilityNetworkController>();
            m_PlayerPrefab.AddComponent<TestAbilityConsumer>();
        }

        protected override void OnServerAndClientsCreated()
        {
            base.OnServerAndClientsCreated();

            ProductionNetworkManager.AllowMultipleInstancesForTesting = true;

            serverNetworkManager = m_ServerNetworkManager.gameObject.AddComponent<ProductionNetworkManager>();

            clientNetworkManagers.Clear();
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = client.gameObject.AddComponent<ProductionNetworkManager>();
                clientNetworkManagers.Add(manager);
            }
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            yield return base.OnServerAndClientsConnected();
            AttachPlayerAvatars();
            yield break;
        }

        [UnityTest]
        public IEnumerator AuthorityFloodTriggersAntiCheatKick()
        {
            yield return WaitForClientsConnectedOrTimeOut();
            AttachPlayerAvatars();

            var connectedPlayers = GetConnectedPlayers();
            Assert.GreaterOrEqual(connectedPlayers.Count, 2, "Expected at least two players to be connected.");

            foreach (var kvp in connectedPlayers.ToList())
            {
                if (kvp.Key == m_ServerNetworkManager.LocalClientId)
                {
                    continue;
                }

                var avatar = kvp.Value.Avatar;
                Assert.IsNotNull(avatar, "Player avatar not assigned for client {0}.", kvp.Key);

                float elapsed = 0f;
                while (serverNetworkManager != null && connectedPlayers.ContainsKey(kvp.Key) && elapsed < 5f)
                {
                    avatar.transform.position += Vector3.forward * 50f;
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            yield return WaitForConditionOrTimeOut(() => !GetConnectedPlayers().Any(pair => pair.Key != m_ServerNetworkManager.LocalClientId));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Anti-cheat did not kick offending client within timeout window.");
        }

        [UnityTest]
        public IEnumerator AbilityBurstLatencyRemainsUnderThreshold()
        {
            yield return WaitForClientsConnectedOrTimeOut();
            AttachPlayerAvatars();

            PrepareTestAbility();
            abilityLatencySamples.Clear();

            var clientManager = m_ClientNetworkManagers[0];
            var clientPlayer = clientManager.LocalClient.PlayerObject;
            var clientAbility = clientPlayer.GetComponent<EnhancedAbilitySystem>();
            var clientController = clientPlayer.GetComponent<AbilityNetworkController>();

            ConfigureAbilitySystem(clientAbility);
            var serverClient = m_ServerNetworkManager.ConnectedClients[clientManager.LocalClientId].PlayerObject.GetComponent<EnhancedAbilitySystem>();
            ConfigureAbilitySystem(serverClient);

            const int burstCount = 5;
            var latencies = new List<float>(burstCount);

            for (int i = 0; i < burstCount; i++)
            {
                float start = Time.time;
                bool completed = false;
                void Handler(int index)
                {
                    if (index == 0)
                    {
                        latencies.Add(Time.time - start);
                        completed = true;
                    }
                }

                clientAbility.OnAbilityCast += Handler;
                Assert.IsTrue(clientController.RequestAbilityCast(0, clientPlayer.transform.position, clientPlayer.transform.forward));
                yield return WaitForConditionOrTimeOut(() => completed);
                clientAbility.OnAbilityCast -= Handler;
                Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for ability approval.");
            }

            var averageLatency = latencies.Average();
            Assert.Less(averageLatency, 0.2f, $"Average latency too high: {averageLatency:F3}s");
        }

        [UnityTest]
        public IEnumerator RapidReconnectTriggersCooldown()
        {
            yield return WaitForClientsConnectedOrTimeOut();
            AttachPlayerAvatars();

            var testClient = m_ClientNetworkManagers[0];
            var productionManager = clientNetworkManagers[0];

            SetPrivateField(productionManager, "lastErrorCode", NetworkErrorCode.None);
            SetPrivateField(productionManager, "lastErrorMessage", string.Empty);

            NetcodeIntegrationTestHelpers.StopOneClient(testClient, destroy: false);
            yield return null;

            SetPrivateField(productionManager, "lastDisconnectTime", Time.time);

            NetcodeIntegrationTestHelpers.StartOneClient(testClient);
            yield return null;

            Assert.AreEqual(NetworkErrorCode.RapidReconnect, GetLastErrorCode(productionManager), "Rapid reconnect warning not raised.");
        }

        private void AttachPlayerAvatars()
        {
            var players = GetConnectedPlayers();
            var avatarMap = GetAvatarMap();
            foreach (var pair in players)
            {
                if (!m_ServerNetworkManager.ConnectedClients.TryGetValue(pair.Key, out var client))
                {
                    continue;
                }

                var playerObject = client.PlayerObject;
                if (playerObject == null)
                {
                    continue;
                }

                pair.Value.Avatar = playerObject;
                avatarMap[pair.Key] = playerObject;
            }
        }

        private void PrepareTestAbility()
        {
            testAbility = ScriptableObject.CreateInstance<EnhancedAbility>();
            testAbility.abilityName = "Test Bolt";
            testAbility.cooldown = 0.05f;
            testAbility.manaCost = 1f;
            testAbility.damage = 5f;
            testAbility.maxTargets = 1;
            testAbility.range = 3f;
            testAbility.castTime = 0f;
        }

        private void ConfigureAbilitySystem(EnhancedAbilitySystem system)
        {
            if (system == null)
            {
                return;
            }

            system.SetMaxMana(100f);
            system.RestoreMana(100f);
            system.SetAbility(0, testAbility);
        }

        private Dictionary<ulong, PlayerNetworkData> GetConnectedPlayers()
        {
            var field = typeof(ProductionNetworkManager).GetField("connectedPlayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Dictionary<ulong, PlayerNetworkData>)field.GetValue(serverNetworkManager);
        }

        private Dictionary<ulong, NetworkObject> GetAvatarMap()
        {
            var field = typeof(ProductionNetworkManager).GetField("playerAvatars", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Dictionary<ulong, NetworkObject>)field.GetValue(serverNetworkManager);
        }

        private NetworkErrorCode GetLastErrorCode(ProductionNetworkManager manager)
        {
            var field = typeof(ProductionNetworkManager).GetField("lastErrorCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (NetworkErrorCode)field.GetValue(manager);
        }

        private void SetPrivateField<T>(ProductionNetworkManager manager, string fieldName, T value)
        {
            var field = typeof(ProductionNetworkManager).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(manager, value);
        }
    }

    internal class TestAbilityConsumer : NetworkBehaviour
    {
    }
}
