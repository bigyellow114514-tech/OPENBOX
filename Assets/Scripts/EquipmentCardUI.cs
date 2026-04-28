using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentCardUI : MonoBehaviour
{
    public static EquipmentCardUI Instance { get; private set; }

    [Header("Top Row")]
    [SerializeField] Image       iconImage;
    [SerializeField] TMP_Text    nameText;
    [SerializeField] TMP_Text    slotText;

    [Header("Stats Area")]
    [SerializeField] Transform   statsContainer;
    [SerializeField] GameObject  statRowPrefab;

    [Header("Bottom")]
    [SerializeField] TMP_Text    descriptionText;

    [Header("Buttons")]
    [SerializeField] Button      equipButton;
    [SerializeField] Button      decomposeButton;
    [SerializeField] Button      closeButton;

    [Header("Animation")]
    [SerializeField] float       animDuration = 0.25f;

    [Header("字体 —— 拖入支持中文的 TMP Font Asset")]
    [SerializeField] TMP_FontAsset chineseFontAsset;

    CanvasGroup    _canvasGroup;
    EquipmentResult _currentItem;

    // 运行时兜底：从系统字体动态生成（Inspector 已赋字体时不走此路）
    static TMP_FontAsset _runtimeFont;

    void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        equipButton?.onClick.AddListener(OnEquip);
        decomposeButton?.onClick.AddListener(OnDecompose);
        closeButton?.onClick.AddListener(Hide);
        gameObject.SetActive(false);

        if (chineseFontAsset == null)
            EnsureRuntimeFont();
    }

    static void EnsureRuntimeFont()
    {
        if (_runtimeFont != null) return;
        string[] candidates = { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "NSimSun", "Heiti SC", "STHeiti" };
        Font osFont = Font.CreateDynamicFontFromOSFont(candidates, 32);
        if (osFont == null) return;
        _runtimeFont = TMP_FontAsset.CreateFontAsset(osFont);
    }

    void ApplyFontToAllText()
    {
        TMP_FontAsset font = chineseFontAsset != null ? chineseFontAsset : _runtimeFont;
        if (font == null) return;
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            tmp.font = font;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        RectTransform rt = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam))
            Hide();
    }

    // ── 公共接口 ──────────────────────────────────────────

    public void Show(EquipmentResult data)
    {
        if (data == null)
        {
            Debug.LogError("EquipmentCardUI.Show: data 为 null，无法显示");
            TreeClick.Unlock();
            return;
        }
        decomposeButton?.gameObject.SetActive(true);
        equipButton?.gameObject.SetActive(true);
        _currentItem = data;
        Populate(data);
        // 在所有 StatRow 实例化完成后统一赋字体
        ApplyFontToAllText();
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
    }

    // 从装备槽位点击查看：只显示属性，隐藏"装备"按钮（已装备）
    public void ShowFromSlot(EquipmentResult data)
    {
        if (data == null)
        {
            TreeClick.Unlock();
            return;
        }
        equipButton?.gameObject.SetActive(false);
        decomposeButton?.gameObject.SetActive(false);
        _currentItem = data;
        Populate(data);
        ApplyFontToAllText();
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFade(1f, 0f, onDone: () =>
        {
            gameObject.SetActive(false);
            TreeClick.Unlock();
        }));
    }

    // ── 填充数据 ──────────────────────────────────────────

    void Populate(EquipmentResult data)
    {
        iconImage.sprite     = data.icon;
        iconImage.enabled    = data.icon != null;
        nameText.text        = data.itemName;
        slotText.text        = "槽位：" + data.slotName;
        descriptionText.text = RarityStars(data.rarity);

        BuildStatRows(data.bonusAttr);
    }

    static string RarityStars(int rarity)
    {
        int r = Mathf.Clamp(rarity, 1, 6);
        return new string('★', r) + new string('☆', 6 - r);
    }

    void BuildStatRows(RoleAttr attr)
    {
        foreach (Transform child in statsContainer)
            Destroy(child.gameObject);

        TryAddRow("攻击力",   attr.Attack,        "0");
        TryAddRow("防御力",   attr.Defence,       "0");
        TryAddRow("生命值",   attr.Hp,            "0");
        TryAddRow("敏捷",     attr.Agility,       "0");
        TryAddRow("暴击率",   attr.CritRate,      "0.##", "%");
        TryAddRow("爆伤",     attr.CritDmg,       "0.##", "%");
        TryAddRow("反击",     attr.CounterRate,   "0.##", "%");
        TryAddRow("连击",     attr.ComboRate,     "0.##", "%");
        TryAddRow("闪避",     attr.DodgeRate,     "0.##", "%");
        TryAddRow("击晕",     attr.StunRate,      "0.##", "%");
        TryAddRow("吸血",     attr.LifeStealRate, "0.##", "%");
    }

    void TryAddRow(string label, float value, string fmt, string suffix = "")
    {
        if (value == 0f) return;

        var row = Instantiate(statRowPrefab, statsContainer);
        row.GetComponent<StatRowUI>().Set(label, value, fmt, suffix);
    }

    // ── 按钮回调 ─────────────────────────────────────────

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

    // ── 动画 ─────────────────────────────────────────────

    System.Collections.IEnumerator AnimateFade(float from, float to,
                                               System.Action onDone = null)
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
