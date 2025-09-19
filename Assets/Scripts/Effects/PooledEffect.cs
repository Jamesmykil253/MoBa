using System;
using System.Collections;
using UnityEngine;

namespace MOBA.Effects
{
    /// <summary>
    /// Component that returns itself to a pool after a timed lifetime while applying colour changes via material property blocks.
    /// </summary>
    public class PooledEffect : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private MaterialPropertyBlock propertyBlock;
        private Coroutine lifetimeRoutine;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        public void Play(Action releaseAction, float lifetimeSeconds, Color tint, Vector3 worldScale)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", tint);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            transform.localScale = worldScale;

            if (lifetimeRoutine != null)
            {
                StopCoroutine(lifetimeRoutine);
            }

            lifetimeRoutine = StartCoroutine(ReturnToPool(releaseAction, lifetimeSeconds));
        }

        private IEnumerator ReturnToPool(Action releaseAction, float lifetimeSeconds)
        {
            if (lifetimeSeconds > 0f)
            {
                yield return new WaitForSeconds(lifetimeSeconds);
            }

            releaseAction?.Invoke();
            lifetimeRoutine = null;
        }
    }
}
