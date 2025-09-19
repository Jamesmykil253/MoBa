using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MOBA.Tests.PlayMode
{
    public class TestMatchLifecycle
    {
        private const string TestSceneName = "MOBASceneSetup";

        [UnitySetUp]
        public System.Collections.IEnumerator SetUp()
        {
            if (!Application.isPlaying)
            {
                yield return null;
            }

            var loadOp = SceneManager.LoadSceneAsync(TestSceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        [UnityTest]
        public System.Collections.IEnumerator Host_StartMatch_EndMatch()
        {
            var productionManager = Object.FindFirstObjectByType<ProductionNetworkManager>();
            Assert.IsNotNull(productionManager, "ProductionNetworkManager instance not found in scene.");

            productionManager.StartHost();
            yield return new WaitForSeconds(0.5f);

            var gameManager = Object.FindFirstObjectByType<SimpleGameManager>();
            Assert.IsNotNull(gameManager, "SimpleGameManager instance not found in scene.");

            Assert.IsTrue(NetworkManager.Singleton.IsHost, "NetworkManager failed to start host mode.");

            bool started = gameManager.StartMatch();
            Assert.IsTrue(started, "StartMatch should succeed on server.");
            Assert.IsTrue(gameManager.IsGameActiveServer, "Game should be active after StartMatch.");

            gameManager.AddScore(0, gameManager.scoreToWin);
            yield return null;
            Assert.IsFalse(gameManager.IsGameActiveServer, "Game should become inactive after reaching score to win.");

            productionManager.Disconnect();
            yield return null;
        }
    }
}
