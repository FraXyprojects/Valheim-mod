using UnityEngine;
using UnityEngine.UI;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.UI
{
    public sealed class ChronicleUIIndicator : MonoBehaviour
    {
        private ChronicleConfig _config;
        private SessionLifecycleManager _lifecycleManager;
        private SessionManager _sessionManager;

        private GameObject _uiCanvas;
        private Text _statusText;
        private string _lastStateKey = string.Empty;

        public void Initialize(ChronicleConfig config, SessionLifecycleManager lifecycleManager, SessionManager sessionManager)
        {
            _config = config;
            _lifecycleManager = lifecycleManager;
            _sessionManager = sessionManager;

            if (_config.ShowStatusIndicator.Value)
            {
                CreateUI();
            }
        }

        private void CreateUI()
        {
            if (_uiCanvas != null)
            {
                return;
            }

            _uiCanvas = new GameObject("ChronicleUICanvas");
            DontDestroyOnLoad(_uiCanvas);

            Canvas canvas = _uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            _uiCanvas.AddComponent<CanvasScaler>();
            _uiCanvas.AddComponent<GraphicRaycaster>();

            GameObject textObj = new GameObject("ChronicleStatusText");
            textObj.transform.SetParent(_uiCanvas.transform, false);

            _statusText = textObj.AddComponent<Text>();

            // Try to find Valheim's standard font (AveriaSerifLibre-Bold or Arial)
            Font valheimFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (f.name == "AveriaSerifLibre-Bold" || f.name == "AveriaSerifLibre-Regular")
                {
                    valheimFont = f;
                    break;
                }
            }

            _statusText.font = valheimFont;
            _statusText.fontSize = 14;
            _statusText.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            _statusText.alignment = TextAnchor.MiddleRight;
            _statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _statusText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            SetPosition(rect);

            UpdateState();
        }

        private void SetPosition(RectTransform rect)
        {
            string position = _config.StatusIndicatorPosition.Value;
            float offsetX = _config.StatusIndicatorOffsetX.Value;
            float offsetY = _config.StatusIndicatorOffsetY.Value;

            switch (position)
            {
                case "TopLeft":
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    break;
                case "TopCenter":
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    _statusText.alignment = TextAnchor.MiddleCenter;
                    break;
                case "CenterLeft":
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    _statusText.alignment = TextAnchor.MiddleLeft;
                    break;
                case "CenterRight":
                    rect.anchorMin = new Vector2(1, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case "BottomLeft":
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    _statusText.alignment = TextAnchor.LowerLeft;
                    break;
                case "BottomCenter":
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    _statusText.alignment = TextAnchor.LowerCenter;
                    break;
                case "BottomRight":
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    break;
                case "TopRight":
                default:
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    break;
            }

            rect.anchoredPosition = new Vector2(offsetX, offsetY);
        }

        private void Update()
        {
            if (_uiCanvas != null && _config.ShowStatusIndicator.Value && Time.frameCount % 30 == 0)
            {
                UpdateState();
            }
        }

        private void UpdateState()
        {
            if (_lifecycleManager == null || _statusText == null) return;

            string currentStateKey = string.Empty;
            switch (_lifecycleManager.State)
            {
                case SessionLifecycleState.InWorld:
                    currentStateKey = _sessionManager.IsActive ? "Chronicle: REC" : "Chronicle: Idle";
                    break;
                case SessionLifecycleState.Connecting:
                    currentStateKey = "Chronicle: Connecting";
                    break;
                case SessionLifecycleState.TemporaryConnectionLoss:
                    currentStateKey = "Chronicle: Wait";
                    break;
                case SessionLifecycleState.Disconnecting:
                case SessionLifecycleState.Disconnected:
                    currentStateKey = "Chronicle: Off";
                    break;
                default:
                    currentStateKey = "Chronicle: Idle";
                    break;
            }

            if (currentStateKey != _lastStateKey)
            {
                _lastStateKey = currentStateKey;
                _statusText.text = currentStateKey;
            }
        }

        private void OnDestroy()
        {
            if (_uiCanvas != null)
            {
                Destroy(_uiCanvas);
            }
        }
    }
}
