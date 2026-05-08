using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RanchSceneController : MonoBehaviour
{
    const string NextScene = "MainScene";

    void Start()
    {
        EnsureEventSystem();
        BuildBackground();
        BuildUI();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildBackground()
    {
        var bgGO = new GameObject("Background");
        var sr = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Main_Ranch");
        bgGO.AddComponent<BackgroundFill>();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("RanchCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildArrowButton(canvasGO.transform, "►",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f),
            () => SceneTransitionManager.Instance.LoadScene(NextScene));
    }

    void BuildArrowButton(Transform parent, string label, Vector2 anchor, Vector2 pivot,
        Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject("ArrowButton");
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(100f, 100f);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 56f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
