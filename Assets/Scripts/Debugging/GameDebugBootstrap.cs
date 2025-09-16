using UnityEngine;

namespace MOBA.Debugging
{
    /// <summary>
    /// Minimal helper so newcomers can press Play and immediately see console output.
    /// Attach to any GameObject in the starting scene.
    /// </summary>
    public class GameDebugBootstrap : MonoBehaviour
    {
        [Header("Startup Behaviour")]
        [Tooltip("Leave unchecked to keep logs simple and always visible. Enable to use the advanced filter asset.")]
        [SerializeField] private bool enableAdvancedFiltering = false;

        [Tooltip("Optional list of messages to print when Play Mode starts.")]
        [SerializeField] private string[] startupMessages =
        {
            "GameDebug bootstrap active.",
            "You can call GameDebug.Log from any script to print messages."
        };

        private void Awake()
        {
            GameDebug.UseAdvancedFiltering(enableAdvancedFiltering);
        }

        private void Start()
        {
            if (startupMessages == null || startupMessages.Length == 0)
            {
                GameDebug.Log("GameDebug bootstrap ready (no custom startup messages set).");
                return;
            }

            foreach (var message in startupMessages)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    GameDebug.Log(message);
                }
            }
        }
    }
}
