using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneNavigator : MonoBehaviour
{
    const string RanchSceneName = "RanchScene";

    void Start()
    {
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("NavigatorCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildArrowButton(canvasGO.transform, "◄",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f),
            () => SceneTransitionManager.Instance.LoadScene(RanchSceneName));

        BuildIconButton(canvasGO.transform, "PetButton", "宠物", new Vector2(20f, -92f), "UI/Pets/Pet_Button",
            () => PetPanelUI.ShowOrCreate());
        BuildIconButton(canvasGO.transform, "PetGachaButton", "抽宠", new Vector2(20f, -204f), "UI/Pets/Gacha_Button",
            OnPetGachaClicked);
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

    void BuildIconButton(Transform parent, string name, string label, Vector2 position, string spritePath,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(100f, 100f);

        var img = btnGO.AddComponent<Image>();
        img.sprite = LoadSprite(spritePath);
        img.color = img.sprite != null ? Color.white : new Color(0.16f, 0.35f, 0.42f, 0.92f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 8f);
        textRect.sizeDelta = new Vector2(0f, 24f);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 17f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    void OnPetGachaClicked()
    {
        PetSystemManager manager = PetSystemManager.EnsureInstance();
        if (!manager.CanGacha(out string reason))
        {
            PetPanelUI.ShowOrCreate(null, reason);
            return;
        }

        PetInstance pet = manager.Gacha();
        PetPanelUI.ShowOrCreate(pet, pet != null ? "获得新宠物" : "抽宠失败");
    }

    Sprite LoadSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
