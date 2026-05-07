using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentCompareUI : MonoBehaviour
{
    public static EquipmentCompareUI Instance { get; private set; }

    public static EquipmentCompareUI GetOrFindInstance()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<EquipmentCompareUI>(true);
        return Instance;
    }

    [Header("当前装备")]
    [SerializeField] Image oldCardBackgroundImage;
    [SerializeField] Image oldIconImage;
    [SerializeField] TMP_Text oldLevelText;
    [SerializeField] TMP_Text oldNameText;
    [SerializeField] TMP_Text oldSlotText;
    [SerializeField] TMP_Text oldDescText;
    [SerializeField] Transform oldStatsContainer;

    [Header("新装备")]
    [SerializeField] Image newCardBackgroundImage;
    [SerializeField] Image newIconImage;
    [SerializeField] TMP_Text newLevelText;
    [SerializeField] TMP_Text newNameText;
    [SerializeField] TMP_Text newSlotText;
    [SerializeField] TMP_Text BattlePowerText;
    [SerializeField] TMP_Text newDescText;
    [SerializeField] Transform newStatsContainer;

    [Header("按钮")]
    [SerializeField] Button replaceButton;
    [SerializeField] Button decomposeButton;
    [SerializeField] Button closeButton;

    [Header("共用")]
    [SerializeField] GameObject statRowPrefab;
    [SerializeField] TMP_FontAsset chineseFontAsset;
    [SerializeField] float animDuration = 0.25f;

    [Header("Quality Background")]
    [SerializeField] Sprite[] rarityBackgroundSprites = new Sprite[6];

    CanvasGroup _cg;
    EquipmentResult _newItem;
    EquipmentResult _oldItem;
    GameObject _vfxInstance;

    static TMP_FontAsset _runtimeFont;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _cg = GetComponent<CanvasGroup>();
        replaceButton?.onClick.AddListener(OnReplace);
        decomposeButton?.onClick.AddListener(OnDecompose);
        closeButton?.onClick.AddListener(OnClose);
        gameObject.SetActive(false);
        ResolveCardBackgrounds();

        oldLevelText = EnsureLevelText(transform.Find("ContentRow/Card_当前装备"), oldLevelText, chineseFontAsset);
        newLevelText = EnsureLevelText(transform.Find("ContentRow/Card_新装备"), newLevelText, chineseFontAsset);
        BattlePowerText = EnsureBattlePowerText(BattlePowerText);
    }

    public void Show(EquipmentResult newItem, EquipmentResult oldItem)
    {
        _newItem = newItem;
        _oldItem = oldItem;
        ResolveCardBackgrounds();
        closeButton?.gameObject.SetActive(false);
        _cg.alpha = 0f;
        gameObject.SetActive(true);
        oldLevelText = EnsureLevelText(transform.Find("ContentRow/Card_当前装备"), oldLevelText, chineseFontAsset);
        newLevelText = EnsureLevelText(transform.Find("ContentRow/Card_新装备"), newLevelText, chineseFontAsset);
        BattlePowerText = EnsureBattlePowerText(BattlePowerText);
        PopulateOld(oldItem);
        PopulateNew(newItem, oldItem);
        ApplyFontToAllText();
        RebuildRootLayout();
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
        EnableUpgradeGlow(newIconImage, CalculateReplacementCombatPowerDelta(newItem, oldItem) > 0);
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (_vfxInstance != null) { Destroy(_vfxInstance); _vfxInstance = null; }
        StartCoroutine(AnimateFade(1f, 0f, () =>
        {
            gameObject.SetActive(false);
            TreeClick.Unlock();
        }));
    }

    void OnDestroy()
    {
        if (_vfxInstance != null) Destroy(_vfxInstance);
    }

    void PopulateOld(EquipmentResult old)
    {
        RefreshBackground(oldCardBackgroundImage, old);
        oldIconImage.sprite = old.icon;
        oldIconImage.enabled = old.icon != null;
        if (oldLevelText != null)
            oldLevelText.text = FormatLevel(old.equipLevel);
        oldNameText.text = old.itemName;
        oldSlotText.text = "槽位：" + old.slotName;
        if (oldDescText != null)
        {
            oldDescText.gameObject.SetActive(true);
            oldDescText.text = RarityStars(old.rarity);
        }
        BuildStatRows(oldStatsContainer, old.bonusAttr, null);
    }

    void PopulateNew(EquipmentResult newItem, EquipmentResult oldItem)
    {
        RefreshBackground(newCardBackgroundImage, newItem);
        newIconImage.sprite = newItem.icon;
        newIconImage.enabled = newItem.icon != null;
        if (newLevelText != null)
            newLevelText.text = FormatLevel(newItem.equipLevel);
        newNameText.text = newItem.itemName;
        newSlotText.text = "槽位：" + newItem.slotName;
        if (newDescText != null)
        {
            newDescText.gameObject.SetActive(true);
            newDescText.text = RarityStars(newItem.rarity);
        }

        RoleAttr delta = newItem.bonusAttr - oldItem.bonusAttr;
        SetBattlePowerText(CalculateReplacementCombatPowerDelta(newItem, oldItem));
        BuildStatRows(newStatsContainer, newItem.bonusAttr, delta);
    }

    void ResolveCardBackgrounds()
    {
        if (oldCardBackgroundImage == null)
            oldCardBackgroundImage = FindCardBackground(oldIconImage);
        if (newCardBackgroundImage == null)
            newCardBackgroundImage = FindCardBackground(newIconImage);
    }

    Image FindCardBackground(Image icon)
    {
        Transform current = icon != null ? icon.transform.parent : null;
        while (current != null && current != transform)
        {
            var image = current.GetComponent<Image>();
            if (image != null)
                return image;

            current = current.parent;
        }

        return null;
    }

    void RefreshBackground(Image backgroundImage, EquipmentResult item)
    {
        if (backgroundImage == null || item == null) return;

        Sprite sprite = GetRarityBackground(item.rarity, backgroundImage.sprite);
        if (sprite != null)
            backgroundImage.sprite = sprite;
    }

    Sprite GetRarityBackground(int rarity, Sprite fallback)
    {
        int index = Mathf.Clamp(rarity, 1, 6) - 1;
        if (rarityBackgroundSprites != null &&
            index < rarityBackgroundSprites.Length &&
            rarityBackgroundSprites[index] != null)
        {
            return rarityBackgroundSprites[index];
        }

        return fallback;
    }

    int CalculateReplacementCombatPowerDelta(EquipmentResult newItem, EquipmentResult oldItem)
    {
        PlayerCharacter chr = PlayerCharacter.Instance;
        if (chr == null || newItem == null || oldItem == null) return 0;

        RoleAttr beforeAttr = chr.FinalAttr;
        RoleAttr afterAttr = beforeAttr - oldItem.bonusAttr + newItem.bonusAttr;
        return PlayerCharacter.CalculateCombatPower(afterAttr) - PlayerCharacter.CalculateCombatPower(beforeAttr);
    }

    void SetBattlePowerText(int delta)
    {
        if (BattlePowerText == null) return;

        string sign = delta > 0 ? "+ " : delta < 0 ? "- " : "";
        BattlePowerText.text = "战斗力：" + sign + Mathf.Abs(delta).ToString("0");
        BattlePowerText.color = delta > 0
            ? new Color(1.00f, 0.55f, 0.00f)
            : delta < 0
                ? new Color(0.12f, 0.39f, 0.86f)
                : new Color(0.45f, 0.28f, 0.10f);

        ApplyChineseFont(BattlePowerText);
        BattlePowerText.enableWordWrapping = false;
        BattlePowerText.overflowMode = TextOverflowModes.Overflow;
    }

    void BuildStatRows(Transform container, RoleAttr attr, RoleAttr? delta)
    {
        ClearStatRows(container);

        AddRow(container, "攻击力", attr.Attack, delta?.Attack, "0");
        AddRow(container, "生命值", attr.Hp, delta?.Hp, "0");
        AddRow(container, "防御力", attr.Defence, delta?.Defence, "0");
        AddRow(container, "敏捷", attr.Agility, delta?.Agility, "0");
        AddExtraRows(container, attr, delta);
        RebuildCardLayout(container);
    }

    void ClearStatRows(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    void RebuildCardLayout(Transform container)
    {
        var rect = container as RectTransform;
        if (rect == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        Transform bottomPanel = container.parent;
        if (bottomPanel is RectTransform bottomRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomRect);

        Transform card = bottomPanel != null ? bottomPanel.parent : null;
        if (card is RectTransform cardRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
    }

    void RebuildRootLayout()
    {
        if (transform is RectTransform rootRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }

    void AddExtraRows(Transform container, RoleAttr attr, RoleAttr? delta)
    {
        if (!TryAddFirstExtra(container, attr, delta, BattleEntries))
            AddEmptyRow(container);

        if (!TryAddFirstExtra(container, attr, delta, AntiBattleEntries))
            AddEmptyRow(container);
    }

    bool TryAddFirstExtra(Transform container, RoleAttr attr, RoleAttr? delta, StatEntry[] entries)
    {
        foreach (var entry in entries)
        {
            float value = entry.Getter(attr);
            float? diff = delta.HasValue ? entry.Getter(delta.Value) : null;
            if (Mathf.Abs(value) <= 0.001f && (!diff.HasValue || Mathf.Abs(diff.Value) <= 0.001f))
                continue;

            AddRow(container, entry.Label, value, diff, "0.#", "%");
            return true;
        }
        return false;
    }

    void AddRow(Transform container, string label, float value, float? delta, string fmt, string suffix = "")
    {
        var row = Instantiate(statRowPrefab, container);
        var ui = row.GetComponent<StatRowUI>();
        if (delta.HasValue && Mathf.Abs(delta.Value) > 0.001f)
            ui.SetWithDelta(label, value, delta.Value, fmt, suffix);
        else
            ui.Set(label, value, fmt, suffix);
    }

    void AddEmptyRow(Transform container)
    {
        var row = Instantiate(statRowPrefab, container);
        row.GetComponent<StatRowUI>().SetEmpty();
    }

    void OnReplace()
    {
        if (_newItem == null) return;
        EquipmentSlotSystem.Instance?.Equip(_newItem);
        _newItem = null;
        Hide();
    }

    void OnDecompose()
    {
        if (_newItem == null) return;
        EquipmentSlotSystem.Instance?.Decompose(_newItem);
        _newItem = null;
        Hide();
    }

    void OnClose()
    {
        // GDD 5.1: newly opened equipment must be handled immediately.
    }

    void ApplyFontToAllText()
    {
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            ApplyChineseFont(tmp);
    }

    void ApplyChineseFont(TMP_Text text)
    {
        if (text == null) return;

        TMP_FontAsset font = chineseFontAsset != null ? chineseFontAsset : _runtimeFont;
        if (font == null)
        {
            EnsureRuntimeFont();
            font = _runtimeFont;
        }

        if (font != null)
            text.font = font;
    }

    static void EnsureRuntimeFont()
    {
        if (_runtimeFont != null) return;

        string[] candidates = { "Microsoft YaHei", "SimHei", "NSimSun", "Heiti SC", "STHeiti" };
        Font osFont = Font.CreateDynamicFontFromOSFont(candidates, 32);
        if (osFont == null) return;
        _runtimeFont = TMP_FontAsset.CreateFontAsset(osFont);
    }

    static string RarityName(int rarity)
    {
        switch (Mathf.Clamp(rarity, 1, 6))
        {
            case 1: return "下品";
            case 2: return "中品";
            case 3: return "上品";
            case 4: return "极品";
            case 5: return "绝品";
            default: return "神品";
        }
    }

    static string RarityStars(int rarity)
    {
        int r = Mathf.Clamp(rarity, 1, 6);
        return new string('★', r) + new string('☆', 6 - r);
    }

    static string FormatLevel(int level)
    {
        return $"Lv.{Mathf.Max(1, level)}";
    }

    static TMP_Text EnsureLevelText(Transform cardRoot, TMP_Text existing, TMP_FontAsset font)
    {
        if (existing != null) return existing;
        if (cardRoot == null) return null;

        Transform iconSlot = cardRoot.Find("TopRow/IconSlot");
        if (iconSlot == null) return null;

        Transform found = iconSlot.Find("LevelText");
        var text = found != null ? found.GetComponent<TMP_Text>() : null;
        if (text == null)
        {
            var go = new GameObject("LevelText");
            go.transform.SetParent(iconSlot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta = new Vector2(0f, 18f);

            text = go.AddComponent<TextMeshProUGUI>();
            text.text = "Lv.1";
            text.fontSize = 13f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.22f, 0.10f, 0.02f);
        }

        if (font != null) text.font = font;
        text.transform.SetAsLastSibling();
        return text;
    }

    TMP_Text EnsureBattlePowerText(TMP_Text existing)
    {
        if (existing != null) return existing;

        foreach (var text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "BattlePowerText")
                return text;
        }

        return null;
    }

    readonly struct StatEntry
    {
        public readonly string Label;
        public readonly System.Func<RoleAttr, float> Getter;

        public StatEntry(string label, System.Func<RoleAttr, float> getter)
        {
            Label = label;
            Getter = getter;
        }
    }

    static readonly StatEntry[] BattleEntries =
    {
        new StatEntry("暴击", a => a.CritRate),
        new StatEntry("反击", a => a.CounterRate),
        new StatEntry("连击", a => a.ComboRate),
        new StatEntry("闪避", a => a.DodgeRate),
        new StatEntry("击晕", a => a.StunRate),
        new StatEntry("吸血", a => a.LifeStealRate),
    };

    static readonly StatEntry[] AntiBattleEntries =
    {
        new StatEntry("抗暴击", a => a.AntiCritRate),
        new StatEntry("抗反击", a => a.AntiCounterRate),
        new StatEntry("抗连击", a => a.AntiComboRate),
        new StatEntry("抗闪避", a => a.AntiDodgeRate),
        new StatEntry("抗击晕", a => a.AntiStunRate),
        new StatEntry("抗吸血", a => a.AntiLifeStealRate),
    };

    void EnableUpgradeGlow(Image iconImg, bool enable)
    {
        if (_vfxInstance != null) { Destroy(_vfxInstance); _vfxInstance = null; }
        if (!enable || iconImg == null) return;
        _vfxInstance = CreateRainbowVFX(iconImg);
    }

    static GameObject CreateRainbowVFX(Image iconImg)
    {
        var corners = new Vector3[4];
        iconImg.rectTransform.GetWorldCorners(corners);

        Canvas rootCanvas = iconImg.GetComponentInParent<Canvas>()?.rootCanvas;
        bool isOverlay = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay;

        Vector3 worldCenter;
        float iconWidth;

        if (isOverlay && Camera.main != null)
        {
            // Overlay 模式：corners 是屏幕像素坐标，需转换为世界坐标
            float depth = Camera.main.nearClipPlane + 0.5f;
            Vector2 sc = new Vector2((corners[0].x + corners[2].x) * 0.5f,
                                     (corners[0].y + corners[2].y) * 0.5f);
            worldCenter = Camera.main.ScreenToWorldPoint(new Vector3(sc.x, sc.y, depth));
            Vector3 wl = Camera.main.ScreenToWorldPoint(new Vector3(corners[0].x, sc.y, depth));
            Vector3 wr = Camera.main.ScreenToWorldPoint(new Vector3(corners[2].x, sc.y, depth));
            iconWidth = Vector3.Distance(wl, wr);
        }
        else
        {
            // Screen Space Camera / World Space：corners 已是世界坐标
            worldCenter = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
            iconWidth   = Vector3.Distance(corners[0], corners[3]);
            // 向摄像机偏移一点，确保在 Canvas 平面前方
            if (Camera.main != null)
                worldCenter += (Camera.main.transform.position - worldCenter).normalized * 0.05f;
        }

        var go = new GameObject("RainbowEquipmentVFX");
        go.transform.position = worldCenter;
        go.AddComponent<ParticleSystem>();
        var vfx = go.AddComponent<RainbowEquipmentVFX>();
        vfx.sizeScale = iconWidth;

        // 把渲染排序设到 Canvas 之上，避免被 UI 压住
        var rend = go.GetComponent<ParticleSystemRenderer>();
        if (rootCanvas != null)
        {
            rend.sortingLayerID = rootCanvas.sortingLayerID;
            rend.sortingOrder   = rootCanvas.sortingOrder + 100;
        }

        return go;
    }

    System.Collections.IEnumerator AnimateFade(float from, float to, System.Action onDone = null)
    {
        float elapsed = 0f;
        _cg.alpha = from;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            _cg.alpha = Mathf.Lerp(from, to, elapsed / animDuration);
            yield return null;
        }

        _cg.alpha = to;
        onDone?.Invoke();
    }
}
