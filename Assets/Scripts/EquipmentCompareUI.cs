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
    [SerializeField] Image oldIconImage;
    [SerializeField] TMP_Text oldNameText;
    [SerializeField] TMP_Text oldSlotText;
    [SerializeField] TMP_Text oldDescText;
    [SerializeField] Transform oldStatsContainer;

    [Header("新装备")]
    [SerializeField] Image newIconImage;
    [SerializeField] TMP_Text newNameText;
    [SerializeField] TMP_Text newSlotText;
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

    CanvasGroup _cg;
    EquipmentResult _newItem;
    EquipmentResult _oldItem;

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
    }

    public void Show(EquipmentResult newItem, EquipmentResult oldItem)
    {
        _newItem = newItem;
        _oldItem = oldItem;
        closeButton?.gameObject.SetActive(false);
        _cg.alpha = 0f;
        gameObject.SetActive(true);
        PopulateOld(oldItem);
        PopulateNew(newItem, oldItem);
        ApplyFontToAllText();
        RebuildRootLayout();
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFade(1f, 0f, () =>
        {
            gameObject.SetActive(false);
            TreeClick.Unlock();
        }));
    }

    void PopulateOld(EquipmentResult old)
    {
        oldIconImage.sprite = old.icon;
        oldIconImage.enabled = old.icon != null;
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
        newIconImage.sprite = newItem.icon;
        newIconImage.enabled = newItem.icon != null;
        newNameText.text = newItem.itemName;
        newSlotText.text = "槽位：" + newItem.slotName;
        if (newDescText != null)
        {
            newDescText.gameObject.SetActive(true);
            newDescText.text = RarityStars(newItem.rarity);
        }

        RoleAttr delta = newItem.bonusAttr - oldItem.bonusAttr;
        BuildStatRows(newStatsContainer, newItem.bonusAttr, delta);
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
        TMP_FontAsset font = chineseFontAsset != null ? chineseFontAsset : _runtimeFont;
        if (font == null)
        {
            EnsureRuntimeFont();
            font = _runtimeFont;
        }

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
