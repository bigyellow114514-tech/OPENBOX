using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentCardUI : MonoBehaviour
{
    public static EquipmentCardUI Instance { get; private set; }

    [Header("Top Row")]
    [SerializeField] Image cardBackgroundImage;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text slotText;

    [Header("Quality Background")]
    [SerializeField] Sprite[] rarityBackgroundSprites = new Sprite[6];

    [Header("Stats Area")]
    [SerializeField] Transform statsContainer;
    [SerializeField] GameObject statRowPrefab;

    [Header("Bottom")]
    [SerializeField] TMP_Text descriptionText;

    [Header("Buttons")]
    [SerializeField] Button equipButton;
    [SerializeField] Button decomposeButton;
    [SerializeField] Button closeButton;

    [Header("Animation")]
    [SerializeField] float animDuration = 0.25f;

    [Header("字体")]
    [SerializeField] TMP_FontAsset chineseFontAsset;

    CanvasGroup _canvasGroup;
    EquipmentResult _currentItem;
    bool _forced;
    GameObject _vfxInstance;

    static TMP_FontAsset _runtimeFont;

    void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (cardBackgroundImage == null)
            cardBackgroundImage = GetComponent<Image>();

        equipButton?.onClick.AddListener(OnEquip);
        decomposeButton?.onClick.AddListener(OnDecompose);
        closeButton?.onClick.AddListener(OnCloseButton);
        gameObject.SetActive(false);

        if (chineseFontAsset == null)
            EnsureRuntimeFont();

        levelText = EnsureLevelText(transform, levelText, chineseFontAsset);
    }

    void Update()
    {
        if (_forced || !Input.GetMouseButtonDown(0)) return;

        RectTransform rt = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam))
            Hide();
    }

    public void Show(EquipmentResult data)
    {
        if (data == null)
        {
            Debug.LogError("EquipmentCardUI.Show: data 为 null，无法显示");
            TreeClick.Unlock();
            return;
        }

        _forced = true;
        decomposeButton?.gameObject.SetActive(true);
        equipButton?.gameObject.SetActive(true);
        closeButton?.gameObject.SetActive(false);
        ShowInternal(data);
        EnableUpgradeGlow(iconImage, IsUpgrade(data));
    }

    public void ShowFromSlot(EquipmentResult data)
    {
        if (data == null)
        {
            TreeClick.Unlock();
            return;
        }

        _forced = false;
        equipButton?.gameObject.SetActive(false);
        decomposeButton?.gameObject.SetActive(false);
        closeButton?.gameObject.SetActive(true);
        ShowInternal(data);
        EnableUpgradeGlow(iconImage, false);
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

    void ShowInternal(EquipmentResult data)
    {
        _currentItem = data;
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        levelText = EnsureLevelText(transform, levelText, chineseFontAsset);
        Populate(data);
        ApplyFontToAllText();
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
    }

    void Populate(EquipmentResult data)
    {
        RefreshBackground(data);
        iconImage.sprite = data.icon;
        iconImage.enabled = data.icon != null;
        if (levelText != null)
            levelText.text = FormatLevel(data.equipLevel);
        nameText.text = data.itemName;
        slotText.text = "槽位：" + data.slotName;
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.text = RarityStars(data.rarity);
        }

        BuildStatRows(data.bonusAttr);
    }

    void RefreshBackground(EquipmentResult data)
    {
        if (cardBackgroundImage == null)
            cardBackgroundImage = GetComponent<Image>();
        if (cardBackgroundImage == null || data == null) return;

        Sprite sprite = GetRarityBackground(data.rarity);
        if (sprite != null)
            cardBackgroundImage.sprite = sprite;
    }

    Sprite GetRarityBackground(int rarity)
    {
        int index = Mathf.Clamp(rarity, 1, 6) - 1;
        if (rarityBackgroundSprites != null &&
            index < rarityBackgroundSprites.Length &&
            rarityBackgroundSprites[index] != null)
        {
            return rarityBackgroundSprites[index];
        }

        return cardBackgroundImage != null ? cardBackgroundImage.sprite : null;
    }

    void BuildStatRows(RoleAttr attr)
    {
        foreach (Transform child in statsContainer)
            Destroy(child.gameObject);

        AddRow("攻击力", attr.Attack, "0");
        AddRow("生命值", attr.Hp, "0");
        AddRow("防御力", attr.Defence, "0");
        AddRow("敏捷", attr.Agility, "0");
        AddExtraRows(attr);
    }

    void AddExtraRows(RoleAttr attr)
    {
        if (!TryAddFirstExtra(attr, BattleEntries))
            AddEmptyRow();

        if (!TryAddFirstExtra(attr, AntiBattleEntries))
            AddEmptyRow();
    }

    bool TryAddFirstExtra(RoleAttr attr, StatEntry[] entries)
    {
        foreach (var entry in entries)
        {
            float value = entry.Getter(attr);
            if (Mathf.Abs(value) <= 0.001f) continue;
            AddRow(entry.Label, value, "0.#", "%");
            return true;
        }
        return false;
    }

    void AddRow(string label, float value, string fmt, string suffix = "")
    {
        var row = Instantiate(statRowPrefab, statsContainer);
        row.GetComponent<StatRowUI>().Set(label, value, fmt, suffix);
    }

    void AddEmptyRow()
    {
        var row = Instantiate(statRowPrefab, statsContainer);
        row.GetComponent<StatRowUI>().SetEmpty();
    }

    void OnCloseButton()
    {
        if (_forced) return;
        SFXManager.PlayDianji();
        Hide();
    }

    void OnEquip()
    {
        if (_currentItem == null) return;
        EquipmentSlotSystem.Instance?.Equip(_currentItem);
        Hide();
    }

    void OnDecompose()
    {
        if (_currentItem == null) return;
        EquipmentSlotSystem.Instance?.Decompose(_currentItem);
        _currentItem = null;
        Hide();
    }

    void ApplyFontToAllText()
    {
        TMP_FontAsset font = chineseFontAsset != null ? chineseFontAsset : _runtimeFont;
        if (font == null) return;

        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            tmp.font = font;
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

    bool IsUpgrade(EquipmentResult newItem)
    {
        var chr = PlayerCharacter.Instance;
        if (chr == null || newItem == null) return false;
        var existing = EquipmentSlotSystem.Instance?.GetSlot(newItem.slotIndex);
        RoleAttr oldAttr = existing?.bonusAttr ?? default;
        RoleAttr afterAttr = chr.FinalAttr - oldAttr + newItem.bonusAttr;
        return PlayerCharacter.CalculateCombatPower(afterAttr) > PlayerCharacter.CalculateCombatPower(chr.FinalAttr);
    }

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
        _canvasGroup.alpha = from;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / animDuration);
            yield return null;
        }

        _canvasGroup.alpha = to;
        onDone?.Invoke();
    }
}
