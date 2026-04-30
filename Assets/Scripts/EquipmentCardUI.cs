using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentCardUI : MonoBehaviour
{
    public static EquipmentCardUI Instance { get; private set; }

    [Header("Top Row")]
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text slotText;

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

    static TMP_FontAsset _runtimeFont;

    void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        equipButton?.onClick.AddListener(OnEquip);
        decomposeButton?.onClick.AddListener(OnDecompose);
        closeButton?.onClick.AddListener(OnCloseButton);
        gameObject.SetActive(false);

        if (chineseFontAsset == null)
            EnsureRuntimeFont();
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

    void ShowInternal(EquipmentResult data)
    {
        _currentItem = data;
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        Populate(data);
        ApplyFontToAllText();
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
    }

    void Populate(EquipmentResult data)
    {
        iconImage.sprite = data.icon;
        iconImage.enabled = data.icon != null;
        nameText.text = data.itemName;
        slotText.text = "槽位：" + data.slotName;
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.text = RarityStars(data.rarity);
        }

        BuildStatRows(data.bonusAttr);
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
        TryAddFirstExtra(attr, BattleEntries);
        TryAddFirstExtra(attr, AntiBattleEntries);
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

    void OnCloseButton()
    {
        if (_forced) return;
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
