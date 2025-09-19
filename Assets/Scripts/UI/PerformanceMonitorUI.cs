using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using MOBA.Performance;
using MOBA.Debugging;

namespace MOBA.UI
{
    /// <summary>
    /// Performance monitoring UI overlay that displays real-time performance metrics.
    /// Shows memory usage, frame times, network bandwidth, and object pool efficiency.
    /// Provides visual warnings and optimization recommendations.
    /// </summary>
    public class PerformanceMonitorUI : MonoBehaviour
    {
        #region Configuration
        
        [Header("UI References")]
        [SerializeField] private Canvas performanceCanvas;
        [SerializeField] private Text performanceText;
        [SerializeField] private Text warningsText;
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject performancePanel;
        
        [Header("Display Settings")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private int maxWarningsDisplayed = 5;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color errorColor = Color.red;
        
        [Header("Metrics Display")]
        [SerializeField] private bool showMemoryMetrics = true;
        [SerializeField] private bool showFrameTimeMetrics = true;
        [SerializeField] private bool showNetworkMetrics = true;
        [SerializeField] private bool showPoolMetrics = true;
        [SerializeField] private bool showRecommendations = true;
        
        #endregion
        
        #region State
        
        private float lastUpdateTime = 0f;
        private bool isVisible = false;
        private readonly StringBuilder displayText = new StringBuilder(1024);
        private readonly List<PerformanceWarning> recentWarnings = new List<PerformanceWarning>();
        
        #endregion
        
        #region Initialization
        
        private void Start()
        {
            InitializeUI();
            SetVisible(showOnStart);
            
            // Subscribe to performance warnings
            if (EnhancedPerformanceProfiler.Instance != null)
            {
                EnhancedPerformanceProfiler.Instance.OnPerformanceWarning += HandlePerformanceWarning;
            }
        }
        
        private void InitializeUI()
        {
            // Create UI elements if not assigned
            if (performanceCanvas == null)
            {
                CreatePerformanceUI();
            }
            
            // Setup toggle button
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleVisibility);
            }
            
            // Position canvas appropriately
            if (performanceCanvas != null)
            {
                performanceCanvas.sortingOrder = 100; // Render on top
            }
        }
        
        private void CreatePerformanceUI()
        {
            // Create canvas
            var canvasGO = new GameObject("PerformanceMonitorCanvas");
            canvasGO.transform.SetParent(transform);
            
            performanceCanvas = canvasGO.AddComponent<Canvas>();
            performanceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            performanceCanvas.sortingOrder = 100;
            
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create performance panel
            var panelGO = new GameObject("PerformancePanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            
            performancePanel = panelGO;
            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0.4f, 1f);
            panelRect.offsetMin = new Vector2(10, 10);
            panelRect.offsetMax = new Vector2(-10, -10);
            
            // Create performance text
            var textGO = new GameObject("PerformanceText");
            textGO.transform.SetParent(panelGO.transform, false);
            
            performanceText = textGO.AddComponent<Text>();
            performanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            performanceText.fontSize = 12;
            performanceText.color = normalColor;
            performanceText.alignment = TextAnchor.UpperLeft;
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            // Create warnings text
            var warningsGO = new GameObject("WarningsText");
            warningsGO.transform.SetParent(canvasGO.transform, false);
            
            warningsText = warningsGO.AddComponent<Text>();
            warningsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningsText.fontSize = 14;
            warningsText.color = warningColor;
            warningsText.alignment = TextAnchor.UpperLeft;
            
            var warningsRect = warningsGO.GetComponent<RectTransform>();
            warningsRect.anchorMin = new Vector2(0.6f, 0.7f);
            warningsRect.anchorMax = new Vector2(1f, 1f);
            warningsRect.offsetMin = new Vector2(10, 10);
            warningsRect.offsetMax = new Vector2(-10, -10);
            
            // Create toggle button
            var buttonGO = new GameObject("ToggleButton");
            buttonGO.transform.SetParent(canvasGO.transform, false);
            
            toggleButton = buttonGO.AddComponent<Button>();
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-50, -50);
            buttonRect.sizeDelta = new Vector2(80, 30);
            
            var buttonTextGO = new GameObject("ButtonText");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            
            var buttonText = buttonTextGO.AddComponent<Text>();
            buttonText.text = "Perf";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
        }
        
        #endregion
        
        #region Update Loop
        
        private void Update()
        {
            if (!isVisible || EnhancedPerformanceProfiler.Instance == null)
                return;
                
            float currentTime = Time.unscaledTime;
            if (currentTime - lastUpdateTime >= updateInterval)
            {
                UpdatePerformanceDisplay();
                lastUpdateTime = currentTime;
            }
        }
        
        private void UpdatePerformanceDisplay()
        {
            if (performanceText == null) return;
            
            displayText.Clear();
            
            var snapshot = EnhancedPerformanceProfiler.Instance.GetPerformanceSnapshot();
            
            // Display header
            displayText.AppendLine("=== PERFORMANCE MONITOR ===");
            displayText.AppendLine($"Time: {Time.unscaledTime:F1}s");
            displayText.AppendLine();
            
            // Memory metrics
            if (showMemoryMetrics)
            {
                displayText.AppendLine("--- MEMORY ---");
                displayText.AppendLine($"Total: {snapshot.TotalMemoryUsage / 1024 / 1024:F1} MB");
                
                foreach (var kvp in snapshot.MemoryMetrics)
                {
                    var metrics = kvp.Value;
                    displayText.AppendLine($"{kvp.Key}: {metrics.LatestMemoryUsage / 1024 / 1024:F1} MB (Peak: {metrics.PeakMemoryUsage / 1024 / 1024:F1} MB)");
                }
                displayText.AppendLine();
            }
            
            // Frame time metrics
            if (showFrameTimeMetrics)
            {
                displayText.AppendLine("--- FRAME TIME ---");
                displayText.AppendLine($"FPS: {snapshot.CurrentFPS:F1}");
                displayText.AppendLine($"Frame Time: {snapshot.AverageFrameTime:F2} ms");
                
                foreach (var kvp in snapshot.FrameTimeMetrics)
                {
                    var metrics = kvp.Value;
                    displayText.AppendLine($"{kvp.Key}: {metrics.LatestFrameTime:F2} ms (Avg: {metrics.AverageFrameTime:F2} ms)");
                }
                displayText.AppendLine();
            }
            
            // Network metrics
            if (showNetworkMetrics && snapshot.NetworkMetrics.Count > 0)
            {
                displayText.AppendLine("--- NETWORK ---");
                
                foreach (var kvp in snapshot.NetworkMetrics)
                {
                    var metrics = kvp.Value;
                    displayText.AppendLine($"{kvp.Key}: {metrics.LatestBandwidth:F1} B/s (Avg: {metrics.AverageBandwidth:F1} B/s)");
                }
                displayText.AppendLine();
            }
            
            // Object pool metrics
            if (showPoolMetrics && snapshot.PoolMetrics.Count > 0)
            {
                displayText.AppendLine("--- OBJECT POOLS ---");
                
                foreach (var kvp in snapshot.PoolMetrics)
                {
                    var metrics = kvp.Value;
                    displayText.AppendLine($"{kvp.Key}: {metrics.LatestEfficiency:P1} efficiency ({metrics.LatestActiveCount}/{metrics.LatestTotalCount})");
                }
                displayText.AppendLine();
            }
            
            // Recommendations
            if (showRecommendations)
            {
                var recommendations = EnhancedPerformanceProfiler.Instance.GetPerformanceRecommendations();
                if (recommendations.Count > 0)
                {
                    displayText.AppendLine("--- RECOMMENDATIONS ---");
                    foreach (var recommendation in recommendations)
                    {
                        displayText.AppendLine($"• {recommendation}");
                    }
                }
            }
            
            performanceText.text = displayText.ToString();
            
            // Update text color based on performance
            UpdateTextColor(snapshot);
        }
        
        private void UpdateTextColor(PerformanceSnapshot snapshot)
        {
            Color textColor = normalColor;
            
            // Check for performance issues
            if (snapshot.CurrentFPS < 30f || snapshot.TotalMemoryUsage > 500_000_000)
            {
                textColor = errorColor;
            }
            else if (snapshot.CurrentFPS < 45f || snapshot.TotalMemoryUsage > 250_000_000)
            {
                textColor = warningColor;
            }
            
            performanceText.color = textColor;
        }
        
        #endregion
        
        #region Warning Management
        
        private void HandlePerformanceWarning(PerformanceWarning warning)
        {
            recentWarnings.Add(warning);
            
            // Keep only recent warnings
            while (recentWarnings.Count > maxWarningsDisplayed)
            {
                recentWarnings.RemoveAt(0);
            }
            
            UpdateWarningsDisplay();
        }
        
        private void UpdateWarningsDisplay()
        {
            if (warningsText == null) return;
            
            displayText.Clear();
            displayText.AppendLine("=== WARNINGS ===");
            
            for (int i = recentWarnings.Count - 1; i >= 0; i--)
            {
                var warning = recentWarnings[i];
                float age = Time.unscaledTime - warning.Timestamp;
                displayText.AppendLine($"[{age:F1}s] {warning.Message}");
            }
            
            warningsText.text = displayText.ToString();
        }
        
        #endregion
        
        #region Visibility Control
        
        public void ToggleVisibility()
        {
            SetVisible(!isVisible);
        }
        
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            
            if (performancePanel != null)
            {
                performancePanel.SetActive(visible);
            }
            
            if (warningsText != null)
            {
                warningsText.gameObject.SetActive(visible);
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void OnGUI()
        {
            // Handle keyboard shortcut for toggling
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.F3)
                {
                    ToggleVisibility();
                }
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (EnhancedPerformanceProfiler.Instance != null)
            {
                EnhancedPerformanceProfiler.Instance.OnPerformanceWarning -= HandlePerformanceWarning;
            }
            
            // Cleanup button listener
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveAllListeners();
            }
        }
        
        #endregion
    }
}