#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MOBA.EditorTools
{
    internal static class PooledPrefabValidator
    {
        [MenuItem("Tools/MOBA/Validate Pooled Prefabs")]
        private static void ValidatePooledPrefabs()
        {
            ValidateProjectilePools();
        }

        private static void ValidateProjectilePools()
        {
            var pools = Resources.FindObjectsOfTypeAll<ProjectilePool>();
            foreach (var pool in pools)
            {
                if (pool == null)
                {
                    continue;
                }

                if (pool.projectilePrefab == null)
                {
                    Debug.LogWarning(
                        $"[PoolingValidator] {pool.name} has no projectile prefab assigned.",
                        pool);
                    continue;
                }

                var prefabPath = AssetDatabase.GetAssetPath(pool.projectilePrefab);
                var projectileComponent = pool.projectilePrefab.GetComponent<Projectile>();
                var rigidbody = pool.projectilePrefab.GetComponent<Rigidbody>();
                var collider = pool.projectilePrefab.GetComponent<Collider>();

                if (projectileComponent == null)
                {
                    Debug.LogWarning(
                        $"[PoolingValidator] Projectile prefab '{prefabPath}' is missing a Projectile component.",
                        pool.projectilePrefab);
                }

                if (rigidbody == null)
                {
                    Debug.LogWarning(
                        $"[PoolingValidator] Projectile prefab '{prefabPath}' is missing a Rigidbody component.",
                        pool.projectilePrefab);
                }

                if (collider == null)
                {
                    Debug.LogWarning(
                        $"[PoolingValidator] Projectile prefab '{prefabPath}' is missing a Collider component.",
                        pool.projectilePrefab);
                }
                else if (!collider.isTrigger)
                {
                    Debug.LogWarning(
                        $"[PoolingValidator] Projectile prefab '{prefabPath}' uses a non-trigger collider; pooling expects triggers.",
                        pool.projectilePrefab);
                }
            }

            if (pools.Length == 0)
            {
                Debug.Log("[PoolingValidator] No ProjectilePool instances found in the open scenes or resources.");
            }
            else
            {
                Debug.Log("[PoolingValidator] Projectile pool validation completed.");
            }
        }
    }
}
#endif
