using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Basic controller for Sphere prefab
    /// Resolves missing script error in Sphere.prefab
    /// </summary>
    public class SphereController : MonoBehaviour
    {
        [Header("Sphere Settings")]
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private bool autoRotate = true;

        private void Update()
        {
            if (autoRotate)
            {
                // Simple rotation animation
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Set the sphere's color
        /// </summary>
        public void SetColor(Color color)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        /// <summary>
        /// Set the sphere's size
        /// </summary>
        public void SetSize(float size)
        {
            transform.localScale = Vector3.one * size;
        }
    }
}