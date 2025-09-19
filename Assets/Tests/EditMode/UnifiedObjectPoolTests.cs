using NUnit.Framework;
using UnityEngine;

namespace MOBA.Tests.EditMode
{
    public class UnifiedObjectPoolTests
    {
        private GameObject componentPrefab;
        private TestPoolComponent componentInstance;

        [SetUp]
        public void SetUp()
        {
            UnifiedObjectPool.ClearAllPools();
            componentPrefab = new GameObject("PoolComponentPrefab");
            componentInstance = componentPrefab.AddComponent<TestPoolComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            UnifiedObjectPool.ClearAllPools();
            if (componentPrefab != null)
            {
                Object.DestroyImmediate(componentPrefab);
            }
        }

        [Test]
        public void ComponentPool_ReusesReturnedInstance()
        {
            var pool = UnifiedObjectPool.GetComponentPool("TestComponentPool", componentInstance, initialSize: 1);
            var first = pool.Get();
            Assert.IsNotNull(first);

            pool.Return(first);
            var second = pool.Get();
            Assert.AreSame(first, second);
        }

        [Test]
        public void GameObjectPool_ClearsSuccessfully()
        {
            var prefab = new GameObject("GameObjectPoolPrefab");
            var pool = UnifiedObjectPool.GetGameObjectPool("TestGameObjectPool", prefab, initialSize: 1);
            var instance = pool.Get();
            Assert.IsNotNull(instance);
            pool.Return(instance);

            UnifiedObjectPool.ClearPool("TestGameObjectPool");
            var stats = UnifiedObjectPool.GetAllPoolStats();
            Assert.IsFalse(stats.ContainsKey("TestGameObjectPool"));

            Object.DestroyImmediate(prefab);
        }

        private class TestPoolComponent : MonoBehaviour
        {
        }
    }
}
