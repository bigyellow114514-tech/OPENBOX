using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    const string MainSceneName = "MainScene";

    TextMeshProUGUI _touchText;
    bool _loading;

    void Start()
    {
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("TitleCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.sprite = Resources.Load<Sprite>("Sprites/OpenPicture");
        bgImage.preserveAspect = false;

        // "TOUCH TO START" — center, lower third
        var textGO = new GameObject("TouchToStart");
        textGO.transform.SetParent(canvasGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.15f);
        textRect.anchorMax = new Vector2(1f, 0.28f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        _touchText = textGO.AddComponent<TextMeshProUGUI>();
        _touchText.text = "TOUCH TO START";
        _touchText.fontSize = 72f;
        _touchText.alignment = TextAlignmentOptions.Center;
        _touchText.color = Color.white;
        _touchText.fontStyle = FontStyles.Bold;
    }

    void Update()
    {
        if (_loading) return;

        // Smooth blink: alpha oscillates 0.1 → 1.0
        if (_touchText != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * 0.9f));
            var c = _touchText.color;
            c.a = Mathf.Lerp(0.1f, 1f, alpha);
            _touchText.color = c;
        }

        bool clicked = Input.GetMouseButtonDown(0);
        bool touched = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        if (clicked || touched)
        {
            _loading = true;
            SceneManager.LoadScene(MainSceneName);
        }
    }
}
