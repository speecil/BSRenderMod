using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RenderMod.Render
{
    internal class ReplayProgressUI
    {
        private Canvas _canvas;
        private TextMeshProUGUI _progressText;

        public void Show()
        {
            if (_canvas != null)
                return;

            var canvasGO = new GameObject("ReplayProgressCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var background = new GameObject("Background").AddComponent<Image>();
            background.transform.SetParent(canvasGO.transform, false);
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;
            background.color = Color.black;

            _progressText = new GameObject("ProgressText").AddComponent<TextMeshProUGUI>();
            _progressText.transform.SetParent(canvasGO.transform, false);
            _progressText.rectTransform.sizeDelta = new Vector2(600, 100);
            _progressText.alignment = TextAlignmentOptions.Center;
            _progressText.fontSize = 60;
            _progressText.color = Color.white;
            UpdateProgress(0f);
        }

        public void UpdateProgress(float progress01)
        {
            if (_progressText != null)
                _progressText.text = $"Rendering Replay: {Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f)}%";
        }

        public void Hide()
        {
            if (_canvas == null)
                return;
            Object.Destroy(_canvas.gameObject);
            _canvas = null;
            _progressText = null;
        }
    }
}
