using System.Collections.Generic;
using UnityEngine;

namespace MOBA.Effects
{
    /// <summary>
    /// Centralised runtime VFX pooling utility to avoid per-effect allocations.
    /// </summary>
    public static class EffectPoolService
    {
        private const string SpherePoolKey = "EffectPoolService_Sphere";

        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, UnifiedObjectPool.GameObjectPool> pools = new Dictionary<string, UnifiedObjectPool.GameObjectPool>();

        public static GameObject SpawnSphereEffect(Vector3 position, Color tint, float scale, float lifetimeSeconds, Transform parent = null)
        {
            var pool = GetSpherePool();
            var effect = pool.Get();
            effect.transform.SetParent(parent, worldPositionStays: true);
            effect.transform.position = position;

            var pooledEffect = effect.GetComponent<PooledEffect>();
            pooledEffect.Play(() => pool.Return(effect), Mathf.Max(0.01f, lifetimeSeconds), tint, Vector3.one * scale);
            return effect;
        }

        public static void SpawnSphereBurst(Vector3 origin, Color tint, float radius, int count, float lifetimeSeconds, float scale, Transform parent = null)
        {
            for (int i = 0; i < count; i++)
            {
                var offset = Random.insideUnitSphere * radius;
                SpawnSphereEffect(origin + offset, tint, scale, lifetimeSeconds, parent);
            }
        }

        private static UnifiedObjectPool.GameObjectPool GetSpherePool()
        {
            if (!pools.TryGetValue(SpherePoolKey, out var pool))
            {
                var prefab = EnsureSpherePrefab();
                pool = UnifiedObjectPool.GetGameObjectPool(SpherePoolKey, prefab, 8, 64);
                pools[SpherePoolKey] = pool;
            }

            return pool;
        }

        private static GameObject EnsureSpherePrefab()
        {
            if (!prefabs.TryGetValue(SpherePoolKey, out var prefab) || prefab == null)
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prefab.name = "PooledSphereEffectPrefab";
                var collider = prefab.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                prefab.AddComponent<PooledEffect>();
                prefab.SetActive(false);
                prefab.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(prefab);

                prefabs[SpherePoolKey] = prefab;
            }

            return prefab;
        }
    }
}
