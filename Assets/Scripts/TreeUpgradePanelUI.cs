using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class TreeUpgradePanelUI : MonoBehaviour
{
    const string CanvasName = "TreeUpgradePanelCanvas";
    const float LayoutScale = 0.72f;
    const int MaxTreeLevel = TreeExpManager.MaxUpgradeTier + 1;

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
    static readonly Dictionary<string, Sprite> ItemSpriteCache = new Dictionary<string, Sprite>();

    RectTransform _root;
    RectTransform _layout;
    RectTransform _panel;
    RectTransform _timePanel;
    RectTransform _magicBottleGroup;
    Font _font;

    Text _currentTitleText;
    Text _nextTitleText;
    Text _timeLabelText;
    Text _timeValueText;
    Text _coinValueText;
    Text _upgradeButtonText;
    Text _magicBottleCountText;
    Text _unlockPreviewTitleText;
    Image _coinIconImage;
    Image _magicBottleIconImage;
    Button _upgradeButton;

    readonly Text[] _currentRateTexts = new Text[6];
    readonly Text[] _nextRateTexts = new Text[6];
    readonly Image[] _unlockSlotImages = new Image[6];
    readonly Image[] _unlockIconImages = new Image[6];
    readonly Text[] _unlockLabelTexts = new Text[6];

    float _nextRefreshTime;

    public static TreeUpgradePanelUI ShowOrCreate()
    {
        TreeUpgradePanelUI existing = FindExistingInLoadedScene();
        if (existing == null)
        {
            Debug.LogError("[TreeUpgradePanelUI] Scene is missing TreeUpgradePanelCanvas. Add one disabled TreeUpgradePanelUI canvas to the scene.");
            return null;
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
        if (Application.isPlaying)
            EnsureEventSystem();

        InitializeShell();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            _font = ResolveFont();
            InitializeShell();
            return;
        }

        Refresh();
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;

        _font = ResolveFont();
        BindExistingShell();
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (Time.unscaledTime < _nextRefreshTime) return;

        _nextRefreshTime = Time.unscaledTime + 0.25f;
        Refresh();
    }

    public void RebuildShellForEditor()
    {
        _font = ResolveFont();
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyChild(transform.GetChild(i).gameObject);

        _root = null;
        _layout = null;
        _panel = null;
        _timePanel = null;
        BuildShell();
    }

    void InitializeShell()
    {
        if (BindExistingShell())
            return;

        BuildShell();
    }

    bool BindExistingShell()
    {
        _root = transform.Find("Root") as RectTransform;
        if (_root == null) return false;

        _layout = _root.Find("Layout") as RectTransform;
        if (_layout == null) return false;

        _panel = _layout.Find("PanelMain") as RectTransform;
        _timePanel = _layout.Find("TimePanel") as RectTransform;
        _magicBottleGroup = _layout.Find("MagicBottleCost") as RectTransform;

        _currentTitleText = FindText(_layout, "CurrentTitle");
        _nextTitleText = FindText(_layout, "NextTitle");
        _timeLabelText = FindText(_timePanel, "TimeLabel") ?? FindText(_layout, "TimeLabel");
        _timeValueText = FindText(_timePanel, "TimeValue") ?? FindText(_layout, "TimeValue");
        _coinValueText = FindText(_layout, "CoinValue");
        _upgradeButtonText = FindText(_layout, "UpgradeButton/UpgradeButtonText");
        _magicBottleCountText = FindText(_layout, "MagicBottleCost/MagicBottleCount");
        _unlockPreviewTitleText = FindText(_layout, "UnlockPreviewTitle");

        _coinIconImage = FindImage(_layout, "CoinIcon");
        _magicBottleIconImage = FindImage(_layout, "MagicBottleCost/MagicBottleIcon");
        _upgradeButton = FindButton(_layout, "UpgradeButton");

        Button close = FindButton(_layout, "CloseButton");
        if (close != null)
        {
            close.onClick.RemoveListener(Hide);
            close.onClick.AddListener(Hide);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(OnUpgradeButtonClicked);
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }

        for (int i = 0; i < 6; i++)
        {
            _currentRateTexts[i] = FindText(_layout, "CurrentRate_" + i);
            _nextRateTexts[i] = FindText(_layout, "NextRate_" + i);
            _unlockSlotImages[i] = FindImage(_layout, "UnlockSlot_" + i);
            _unlockIconImages[i] = FindImage(_layout, "UnlockIcon_" + i);
            _unlockLabelTexts[i] = FindText(_layout, "UnlockLabel_" + i);
        }

        return true;
    }

    void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

    void BuildShell()
    {
        if (_root != null) return;

        ClearShellChildren();

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
        Refresh();
    }

    void BuildTable()
    {
        NewImage("TabCurrent", _layout, "UI/Tree/table/tab_current_240x50", 214f, 132f, 240f, 50f);
        NewImage("TabNext", _layout, "UI/Tree/table/tab_next_240x50", 460f, 132f, 240f, 50f);
        _currentTitleText = NewText("CurrentTitle", _layout, "当前等级: 1", 214f, 134f, 240f, 42f, 23, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);
        _nextTitleText = NewText("NextTitle", _layout, "下一等级: 2", 460f, 134f, 240f, 42f, 23, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

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
            _currentRateTexts[i] = NewText("CurrentRate_" + i, _layout, "--", 304f, y, 116f, 50f, 22, TableText, TextAnchor.MiddleCenter, FontStyle.Bold);
            _nextRateTexts[i] = NewText("NextRate_" + i, _layout, "--", 532f, y, 116f, 50f, 22, TableText, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }

    void BuildCostArea()
    {
        _timePanel = NewImage("TimePanel", _layout, "UI/Tree/core/time_panel_250x116", 724f, 188f, 250f, 116f);
        _timeLabelText = NewText("TimeLabel", _timePanel, "升级所需时间", 0f, 28f, 250f, 28f, 22, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);
        _timeValueText = NewText("TimeValue", _timePanel, "00:00:00", 0f, 64f, 250f, 36f, 28, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

        _coinIconImage = NewImage("CoinIcon", _layout, "UI/Tree/icons/coin_42x42", 724f, 338f, 42f, 42f).GetComponent<Image>();
        _coinValueText = NewText("CoinValue", _layout, "0", 772f, 334f, 190f, 50f, 30, TextDark, TextAnchor.MiddleLeft, FontStyle.Bold);

        _magicBottleGroup = NewUI("MagicBottleCost", _layout);
        SetTopLeft(_magicBottleGroup, 775f, 360f, 150f, 40f);
        _magicBottleIconImage = NewUI("MagicBottleIcon", _magicBottleGroup).gameObject.AddComponent<Image>();
        SetTopLeft(_magicBottleIconImage.rectTransform, 0f, 0f, 34f, 34f);
        _magicBottleIconImage.sprite = LoadItemSprite("item_jiasu");
        _magicBottleIconImage.raycastTarget = false;
        _magicBottleCountText = NewText("MagicBottleCount", _magicBottleGroup, "1/0", 36f, -2f, 110f, 36f, 23, new Color(0.12f, 0.55f, 0.18f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);

        Image upgradeImage = NewImage("UpgradeButton", _layout, "UI/Tree/buttons/upgrade_button_normal_246x82", 724f, 410f, 246f, 82f).GetComponent<Image>();
        upgradeImage.raycastTarget = true;
        _upgradeButton = upgradeImage.gameObject.AddComponent<Button>();
        _upgradeButton.targetGraphic = upgradeImage;
        _upgradeButton.spriteState = new SpriteState
        {
            pressedSprite = LoadSprite("UI/Tree/buttons/upgrade_button_pressed_246x82"),
            disabledSprite = LoadSprite("UI/Tree/buttons/upgrade_button_disabled_246x82")
        };
        _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        _upgradeButtonText = NewText("UpgradeButtonText", upgradeImage.transform, "升级", 0f, 0f, 246f, 76f, 34, TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
    }

    void BuildUnlockPreview()
    {
        NewImage("UnlockPreviewPanel", _layout, "UI/Tree/core/unlock_preview_panel_850x170", 87f, 540f, 850f, 170f);
        _unlockPreviewTitleText = NewText("UnlockPreviewTitle", _layout, "升级后，持续将有概率额外产出道具", 108f, 546f, 808f, 36f, 22, TextDark, TextAnchor.MiddleCenter, FontStyle.Bold);

        string[] slotSprites =
        {
            "UI/Tree/slots/item_slot_green_100x100",
            "UI/Tree/slots/item_slot_purple_100x100",
            "UI/Tree/slots/item_slot_blue_100x100",
            "UI/Tree/slots/item_slot_gold_100x100",
            "UI/Tree/slots/item_slot_purple_100x100",
            "UI/Tree/slots/item_slot_red_100x100",
        };

        for (int i = 0; i < slotSprites.Length; i++)
        {
            float x = 122f + i * 133f;
            _unlockSlotImages[i] = NewImage("UnlockSlot_" + i, _layout, slotSprites[i], x, 602f, 100f, 100f).GetComponent<Image>();
            _unlockIconImages[i] = NewImage("UnlockIcon_" + i, _layout, "", x + 9f, 611f, 82f, 82f).GetComponent<Image>();
            _unlockLabelTexts[i] = NewText("UnlockLabel_" + i, _layout, "--", x - 8f, 574f, 116f, 28f, 18, TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }

    void Refresh()
    {
        if (_root == null) return;

        TreeExpManager manager = TreeExpManager.Instance;
        int currentLevel = manager != null ? manager.UpgradeLevel : 1;
        bool isMax = manager != null && manager.UpgradeTier >= TreeExpManager.MaxUpgradeTier;
        bool upgrading = manager != null && manager.IsUpgradeInProgress;
        int nextLevel = isMax ? currentLevel : Mathf.Min(currentLevel + 1, MaxTreeLevel);

        TreeLevelView current = TreeExpManager.GetLevelView(currentLevel);
        TreeLevelView next = TreeExpManager.GetLevelView(nextLevel);

        SetText(_currentTitleText, "当前等级: " + currentLevel);
        SetText(_nextTitleText, isMax ? "已满级" : "下一等级: " + nextLevel);
        RefreshRates(current, next, isMax);
        RefreshTimeAndButton(manager, current, upgrading, isMax);
        RefreshUnlockPreview(upgrading || isMax ? current : next, isMax);
    }

    void RefreshRates(TreeLevelView current, TreeLevelView next, bool isMax)
    {
        for (int i = 0; i < 6; i++)
        {
            SetText(_currentRateTexts[i], FormatPercent(current.RareRates[i]));
            SetText(_nextRateTexts[i], isMax ? "--" : FormatPercent(next.RareRates[i]));
        }
    }

    void RefreshTimeAndButton(TreeExpManager manager, TreeLevelView current, bool upgrading, bool isMax)
    {
        PlayerResourceManager resources = PlayerResourceManager.Instance;
        int gold = resources != null ? resources.Gold : 0;
        int bottles = resources != null ? resources.MagicBottles : 0;

        if (isMax)
        {
            SetActive(_timePanel, false);
            SetText(_timeLabelText, "已满级");
            SetText(_timeValueText, "MAX");
            SetText(_coinValueText, "--");
            SetText(_upgradeButtonText, "满级");
            SetActive(_magicBottleGroup, false);
            SetActive(_coinIconImage, false);
            SetActive(_coinValueText, false);
            if (_upgradeButton != null) _upgradeButton.interactable = false;
            return;
        }

        SetActive(_coinIconImage, !upgrading);
        SetActive(_coinValueText, !upgrading);
        SetActive(_timePanel, upgrading);
        SetActive(_magicBottleGroup, upgrading);

        if (upgrading && manager != null)
        {
            SetText(_timeLabelText, "升级中");
            SetText(_timeValueText, FormatDuration(Mathf.CeilToInt(manager.UpgradeRemainingSeconds)));
            SetText(_upgradeButtonText, "加速");
            SetText(_magicBottleCountText, FormatMagicBottleCount(manager.UpgradeRemainingSeconds, bottles));
            if (_upgradeButton != null) _upgradeButton.interactable = bottles > 0;
            return;
        }

        SetText(_timeLabelText, "升级所需时间");
        SetText(_timeValueText, FormatDuration(current.LvUpTime));
        SetText(_coinValueText, current.LvUpCost.ToString("N0", CultureInfo.InvariantCulture));
        SetText(_upgradeButtonText, "升级");
        if (_upgradeButton != null) _upgradeButton.interactable = manager != null && gold >= current.LvUpCost;
    }

    void RefreshUnlockPreview(TreeLevelView previewLevel, bool isMax)
    {
        SetText(_unlockPreviewTitleText, isMax ? "已解锁全部额外产出" : "升级后，持续将有概率额外产出道具");
        for (int i = 0; i < _unlockSlotImages.Length; i++)
        {
            bool hasDrop = i < previewLevel.ItemDrops.Count;
            SetActive(_unlockSlotImages[i], hasDrop);
            SetActive(_unlockIconImages[i], hasDrop);
            SetActive(_unlockLabelTexts[i], hasDrop);
            if (!hasDrop) continue;

            TreeItemDropView drop = previewLevel.ItemDrops[i];
            ItemView item = TreeExpManager.GetItemView(drop.ItemId);
            _unlockIconImages[i].sprite = LoadItemSprite(item.IconName);
            _unlockIconImages[i].color = _unlockIconImages[i].sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            SetText(_unlockLabelTexts[i], FormatDropRate(drop.DropRate) + " x" + drop.DropNum);
        }
    }

    void OnUpgradeButtonClicked()
    {
        TreeExpManager manager = TreeExpManager.Instance;
        if (manager == null) return;

        if (manager.IsUpgradeInProgress)
            manager.TrySpeedUpUpgrade(1);
        else
            manager.TryStartUpgrade();

        Refresh();
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

    static Text FindText(Transform root, string path)
    {
        Transform child = root != null ? root.Find(path) : null;
        return child != null ? child.GetComponent<Text>() : null;
    }

    static Image FindImage(Transform root, string path)
    {
        Transform child = root != null ? root.Find(path) : null;
        return child != null ? child.GetComponent<Image>() : null;
    }

    static Button FindButton(Transform root, string path)
    {
        Transform child = root != null ? root.Find(path) : null;
        return child != null ? child.GetComponent<Button>() : null;
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
        if (string.IsNullOrWhiteSpace(path)) return null;
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite LoadItemSprite(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return null;
        if (ItemSpriteCache.TryGetValue(iconName, out Sprite cached))
            return cached;

        string path = Path.Combine(Application.dataPath, "Picture/Item/" + iconName + ".png");
        if (!File.Exists(path)) return null;

        byte[] bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes)) return null;
        texture.name = iconName;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        ItemSpriteCache[iconName] = sprite;
        return sprite;
    }

    static string FormatDuration(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        int h = seconds / 3600;
        int m = seconds % 3600 / 60;
        int s = seconds % 60;
        return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
    }

    static string FormatPercent(float rate)
    {
        return (rate * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";
    }

    static string FormatDropRate(float rate)
    {
        float percent = rate > 1f ? rate : rate * 100f;
        return percent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
    }

    static string FormatMagicBottleCount(float remainingSeconds, int ownedBottles)
    {
        float bottleSeconds = TreeExpManager.GetMagicBottleMinutes() * 60f;
        int neededBottles = bottleSeconds > 0f
            ? Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds) / bottleSeconds)
            : 0;
        int usableBottles = Mathf.Min(neededBottles, Mathf.Max(0, ownedBottles));
        return usableBottles + "/" + Mathf.Max(0, ownedBottles);
    }

    static void SetText(Text text, string value)
    {
        if (text != null) text.text = value;
    }

    static void SetActive(Component component, bool active)
    {
        if (component != null) component.gameObject.SetActive(active);
    }

    static void SetActive(RectTransform rect, bool active)
    {
        if (rect != null) rect.gameObject.SetActive(active);
    }

    void ClearShellChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyChild(transform.GetChild(i).gameObject);

        _layout = null;
        _panel = null;
        _timePanel = null;
        _magicBottleGroup = null;
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
