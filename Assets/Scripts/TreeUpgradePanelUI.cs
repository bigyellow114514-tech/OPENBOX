using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class TreeUpgradePanelUI : MonoBehaviour
{
    const string CanvasName = "TreeUpgradePanelCanvas";
    const float LayoutScale = 0.72f;
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.58f);
    static readonly Color TextDark = new Color(0.33f, 0.20f, 0.11f, 1f);
    static readonly Color TextLight = new Color(0.96f, 0.86f, 0.58f, 1f);
    static readonly Color TextMuted = new Color(0.62f, 0.42f, 0.24f, 1f);
    static readonly Color TableText = Color.white;

    static readonly string[] RowSprites =
    {
        "UI/Tree/table/table_row_rough_594x50",
        "UI/Tree/table/table_row_normal_594x50",
        "UI/Tree/table/table_row_fine_594x50",
        "UI/Tree/table/table_row_excellent_594x50",
        "UI/Tree/table/table_row_epic_594x50",
        "UI/Tree/table/table_row_legendary_594x50",
    };

    static readonly string[] QualityLabels = { "粗糙", "普通", "精良", "优质", "史诗", "传世" };
    static readonly string[] CurrentRates = { "42%", "30%", "22%", "5.4%", "0.6%", "0%" };
    static readonly string[] NextRates = { "31%", "34.99%", "26%", "7.01%", "0.9%", "0.1%" };

    RectTransform _root;
    RectTransform _layout;
    RectTransform _panel;
    Font _font;

    public static TreeUpgradePanelUI ShowOrCreate()
    {
        TreeUpgradePanelUI existing = FindExistingInLoadedScene();
        if (existing == null)
        {
            GameObject canvasGO = new GameObject(CanvasName);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            existing = canvasGO.AddComponent<TreeUpgradePanelUI>();
        }

        existing.Show();
        return existing;
    }

    static TreeUpgradePanelUI FindExistingInLoadedScene()
    {
        TreeUpgradePanelUI[] panels = Resources.FindObjectsOfTypeAll<TreeUpgradePanelUI>();
        for (int i = 0; i < panels.Length; i++)
        {
            TreeUpgradePanelUI panel = panels[i];
            if (panel == null) continue;
            if (!panel.gameObject.scene.IsValid() || !panel.gameObject.scene.isLoaded) continue;
            if (panel.name == CanvasName)
                return panel;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            TreeUpgradePanelUI panel = panels[i];
            if (panel == null) continue;
            if (!panel.gameObject.scene.IsValid() || !panel.gameObject.scene.isLoaded) continue;
            return panel;
        }

        return null;
    }

    void Awake()
    {
        _font = ResolveFont();
        EnsureEventSystem();
        BuildShell();
    }

    public void RebuildShellForEditor()
    {
        _font = ResolveFont();
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyChild(transform.GetChild(i).gameObject);

        _root = null;
        _layout = null;
        _panel = null;
        BuildShell();
    }

    void Show()
    {
        gameObject.SetActive(true);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

    void BuildShell()
    {
        if (_root != null) return;

        _root = NewUI("Root", transform);
        Stretch(_root);

        Image dim = NewUI("Dim", _root).gameObject.AddComponent<Image>();
        Stretch(dim.rectTransform);
        dim.color = DimColor;

        _layout = NewUI("Layout", _root);
        _layout.anchorMin = new Vector2(0.5f, 0.5f);
        _layout.anchorMax = new Vector2(0.5f, 0.5f);
        _layout.pivot = new Vector2(0.5f, 0.5f);
        _layout.anchoredPosition = Vector2.zero;
        _layout.sizeDelta = new Vector2(1024f, 768f);
        _layout.localScale = Vector3.one * LayoutScale;

        _panel = NewImage("PanelMain", _layout, "UI/Tree/core/panel_main_936x680", 44f, 56f, 936f, 680f);

        NewImage("TitleBanner", _layout, "UI/Tree/core/title_banner_376x86", 324f, 18f, 376f, 86f);
        NewText("Title", _layout, "仙树升级", 324f, 30f, 376f, 54f, 34, TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);

        Image closeImage = NewImage("CloseButton", _layout, "UI/Tree/core/close_button_54x54", 928f, 62f, 54f, 54f).GetComponent<Image>();
        closeImage.raycastTarget = true;
        Button close = closeImage.gameObject.AddComponent<Button>();
        close.targetGraphic = closeImage;
        close.onClick.AddListener(Hide);

        BuildTable();
        BuildCostArea();
        BuildUnlockPreview();
    }

    void BuildTable()
    {
        NewImage("TabCurrent", _layout, "UI/Tree/table/tab_current_240x50", 214f, 132f, 240f, 50f);
        NewImage("TabNext", _layout, "UI/Tree/table/tab_next_240x50", 460f, 132f, 240f, 50f);
        NewText("CurrentTitle", _layout, "当前等级: 3", 214f, 134f, 240f, 42f, 23, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);
        NewText("NextTitle", _layout, "下一等级: 4", 460f, 134f, 240f, 42f, 23, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

        for (int i = 0; i < RowSprites.Length; i++)
        {
            float y = 178f + i * 50f;
            NewImage("TableRow_" + i, _layout, RowSprites[i], 90f, y, 594f, 50f);
        }

        NewImage("TableOuterFrame", _layout, "UI/Tree/table/table_outer_frame_594x310", 90f, 178f, 594f, 310f);

        for (int i = 0; i < QualityLabels.Length; i++)
        {
            float y = 178f + i * 50f;
            NewText("Quality_" + i, _layout, QualityLabels[i], 104f, y, 100f, 50f, 22, TableText, TextAnchor.MiddleCenter, FontStyle.Bold);
            NewText("CurrentRate_" + i, _layout, CurrentRates[i], 304f, y, 116f, 50f, 22, TableText, TextAnchor.MiddleCenter, FontStyle.Bold);
            NewText("NextRate_" + i, _layout, NextRates[i], 532f, y, 116f, 50f, 22, TableText, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }

    void BuildCostArea()
    {
        NewImage("TimePanel", _layout, "UI/Tree/core/time_panel_250x116", 724f, 188f, 250f, 116f);
        NewText("TimeLabel", _layout, "升级所需时间", 724f, 216f, 250f, 28f, 22, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);
        NewText("TimeValue", _layout, "00:30:00", 724f, 252f, 250f, 36f, 28, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

        NewImage("CoinIcon", _layout, "UI/Tree/icons/coin_42x42", 724f, 338f, 42f, 42f);
        NewText("CoinValue", _layout, "1,000", 772f, 334f, 190f, 50f, 30, TextDark, TextAnchor.MiddleLeft, FontStyle.Bold);

        Image upgradeImage = NewImage("UpgradeButton", _layout, "UI/Tree/buttons/upgrade_button_normal_246x82", 724f, 410f, 246f, 82f).GetComponent<Image>();
        upgradeImage.raycastTarget = true;
        Button upgrade = upgradeImage.gameObject.AddComponent<Button>();
        upgrade.targetGraphic = upgradeImage;
        upgrade.spriteState = new SpriteState
        {
            pressedSprite = LoadSprite("UI/Tree/buttons/upgrade_button_pressed_246x82"),
            disabledSprite = LoadSprite("UI/Tree/buttons/upgrade_button_disabled_246x82")
        };
        NewText("UpgradeButtonText", upgradeImage.transform, "升级", 0f, 0f, 246f, 76f, 34, TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
    }

    void BuildUnlockPreview()
    {
        NewImage("UnlockPreviewPanel", _layout, "UI/Tree/core/unlock_preview_panel_850x170", 87f, 540f, 850f, 170f);
        NewText("UnlockPreviewTitle", _layout, "升级后，持续将有概率额外产出道具", 108f, 546f, 808f, 36f, 22, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

        string[] slotSprites =
        {
            "UI/Tree/slots/item_slot_green_100x100",
            "UI/Tree/slots/item_slot_purple_100x100",
            "UI/Tree/slots/item_slot_blue_100x100",
            "UI/Tree/slots/item_slot_gold_100x100",
            "UI/Tree/slots/item_slot_purple_100x100",
            "UI/Tree/slots/item_slot_red_100x100",
        };
        string[] iconSprites =
        {
            "UI/Tree/icons/item_scroll_82x82",
            "UI/Tree/icons/item_bamboo_slips_82x82",
            "UI/Tree/icons/item_spirit_fruit_82x82",
            "UI/Tree/icons/item_jade_tag_82x82",
            "UI/Tree/icons/item_purple_flower_82x82",
            "UI/Tree/icons/item_jade_pendant_82x82",
        };
        string[] labels = { "已解锁", "5级解锁", "7级解锁", "9级解锁", "11级解锁", "16级解锁" };

        for (int i = 0; i < slotSprites.Length; i++)
        {
            float x = 122f + i * 133f;
            NewImage("UnlockSlot_" + i, _layout, slotSprites[i], x, 602f, 100f, 100f);
            NewImage("UnlockIcon_" + i, _layout, iconSprites[i], x + 9f, 611f, 82f, 82f);
            NewText("UnlockLabel_" + i, _layout, labels[i], x - 8f, 574f, 116f, 28f, 18, TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }

    RectTransform NewImage(string name, Transform parent, string spritePath, float x, float y, float width, float height)
    {
        RectTransform rt = NewUI(name, parent);
        SetTopLeft(rt, x, y, width, height);
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = LoadSprite(spritePath);
        image.color = image.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        image.raycastTarget = false;
        return rt;
    }

    Text NewText(string name, Transform parent, string content, float x, float y, float width, float height,
        int fontSize, Color color, TextAnchor anchor, FontStyle style)
    {
        RectTransform rt = NewUI(name, parent);
        SetTopLeft(rt, x, y, width, height);
        Text text = rt.gameObject.AddComponent<Text>();
        text.text = content;
        text.font = _font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void SetTopLeft(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite LoadSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Font ResolveFont()
    {
        string[] candidates = { "Microsoft YaHei", "SimHei", "NSimSun", "Heiti SC", "STHeiti" };
        return Font.CreateDynamicFontFromOSFont(candidates, 24);
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    static void DestroyChild(GameObject child)
    {
        if (Application.isPlaying)
            Destroy(child);
        else
            DestroyImmediate(child);
    }
}
