using UnityEngine;
using System.Text;
using MOBA.Networking;

namespace MOBA.Debugging
{
    /// <summary>
    /// Lightweight performance overlay inspired by the instrumentation guidance in
    /// Real-Time Rendering and Game Programming Patterns. Attach to a persistent
    /// debug object to visualise frame and networking stats during play mode.
    /// </summary>
    [DisallowMultipleComponent]
    public class PerformanceOverlay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private bool startVisible = true;
        [SerializeField, Tooltip("World-space safe margin for the OnGUI overlay.")]
        private Vector2 onscreenMargin = new Vector2(12f, 10f);
        [SerializeField, Tooltip("GUI scale multiplier for high DPI displays.")]
        private float guiScale = 1f;

        [Header("Frame Timing")]
        [SerializeField, Tooltip("Smoothing factor for frame timing (0 = raw, 1 = frozen).")] 
        [Range(0f, 0.95f)] private float frameSmoothing = 0.1f;

        [Header("Network Stats")]
        [SerializeField] private bool showNetworkStats = true;
        [SerializeField] private bool showCustomTimers = false;

        [Header("Input")]
        [SerializeField, Tooltip("Toggles the overlay at runtime.")]
        private KeyCode toggleKey = KeyCode.F9;

        private bool isVisible;
        private float smoothedDeltaTime;
        private readonly StringBuilder builder = new StringBuilder(256);

        private const float BytesToMegabytes = 1f / (1024f * 1024f);

        private void Awake()
        {
            isVisible = startVisible;
            smoothedDeltaTime = Time.unscaledDeltaTime;
        }

        private void Update()
        {
            float weight = 1f - Mathf.Clamp01(frameSmoothing);
            smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, weight);

            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            var originalMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));

            builder.Length = 0;

            float fps = smoothedDeltaTime > Mathf.Epsilon ? 1f / smoothedDeltaTime : 0f;
            float frameMs = smoothedDeltaTime * 1000f;
            float allocMb = System.GC.GetTotalMemory(false) * BytesToMegabytes;

            builder.AppendLine("Performance Overlay")
                   .Append("FPS: ").Append(fps.ToString("F1"))
                   .Append(" ( ").Append(frameMs.ToString("F2")).Append(" ms )")
                   .AppendLine()
                   .Append("Alloc: ").Append(allocMb.ToString("F2")).Append(" MB");

            if (showNetworkStats)
            {
                AppendNetworkStats(builder);
            }

            if (showCustomTimers)
            {
                AppendCustomProfiling(builder);
            }

            var content = builder.ToString();
            var size = GUI.skin.label.CalcSize(new GUIContent(content));
            Rect rect = new Rect(onscreenMargin.x, onscreenMargin.y, size.x + 8f, size.y + 4f);

            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, content);

            GUI.matrix = originalMatrix;
        }

        private static void AppendNetworkStats(StringBuilder target)
        {
            var productionManager = ProductionNetworkManager.Instance;
            if (productionManager == null)
            {
                target.AppendLine()
                      .Append("Network: offline");
                return;
            }

            var stats = productionManager.GetNetworkStats();
            target.AppendLine()
                  .Append("Network: ").Append(stats.ConnectionState)
                  .Append(" | Players: ").Append(stats.ConnectedPlayers)
                  .Append(" | Ping: ").Append(stats.CurrentPing.ToString("F0"));
        }

        private static void AppendCustomProfiling(StringBuilder target)
        {
            var snapshot = PerformanceProfiler.GetSnapshot();
            foreach (var pair in snapshot)
            {
                var metrics = pair.Value;
                if (metrics.SampleCount == 0)
                {
                    continue;
                }

                target.AppendLine()
                      .Append(pair.Key)
                      .Append(": ")
                      .Append(metrics.AverageMilliseconds.ToString("F2"))
                      .Append(" ms avg (")
                      .Append(metrics.MinMilliseconds.ToString("F2"))
                      .Append(" - ")
                      .Append(metrics.MaxMilliseconds.ToString("F2"))
                      .Append(" | ")
                      .Append(metrics.SampleCount)
                      .Append(" samples)");
            }
        }
    }
}
