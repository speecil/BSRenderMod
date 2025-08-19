using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayProgressUI
{
    private Canvas _canvas;
    private TextMeshProUGUI _progressText;

    // TODO: investigate whether this doesnt show in a BL replay (shows in a scoresaber and beatleader replay fine for me :cool:)
    public void Show()
    {
        if (_canvas != null) return; // already shown

        // canvas
        GameObject canvasGO = new GameObject("ReplayProgressCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // big image for background lol
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;

        // progress
        GameObject textGO = new GameObject("ProgressText");
        textGO.transform.SetParent(canvasGO.transform, false);
        _progressText = textGO.AddComponent<TextMeshProUGUI>();
        RectTransform textRT = _progressText.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(600, 100);

        _progressText.alignment = TextAlignmentOptions.Center;
        _progressText.fontSize = 60;
        _progressText.color = Color.white;
        _progressText.text = "Rendering Replay: 0%";
    }

    public void UpdateProgress(float progress01, string message = null)
    {
        if (_progressText == null) return;
        if(message != null)
        {
            _progressText.text = message;
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f);
        _progressText.text = $"Rendering Replay: {percent}%";
    }

    public void Hide()
    {
        if (_canvas != null)
        {
            UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
            _progressText = null;
        }
    }
}
