using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装备对比窗口：槽位已有装备时，弹出此窗口与新装备并排比较。
/// 需要执行 OpenBox → UI 构建 → 构建装备对比窗口，在场景中创建 UI 节点后才生效。
/// </summary>
public class EquipmentCompareUI : MonoBehaviour
{
    public static EquipmentCompareUI Instance { get; private set; }

    [Header("当前装备（左侧）")]
    [SerializeField] Image       oldIconImage;
    [SerializeField] TMP_Text    oldNameText;
    [SerializeField] TMP_Text    oldSlotText;
    [SerializeField] TMP_Text    oldDescText;
    [SerializeField] Transform   oldStatsContainer;

    [Header("新装备（右侧）")]
    [SerializeField] Image       newIconImage;
    [SerializeField] TMP_Text    newNameText;
    [SerializeField] TMP_Text    newSlotText;
    [SerializeField] TMP_Text    newDescText;
    [SerializeField] Transform   newStatsContainer;

    [Header("按钮")]
    [SerializeField] Button      replaceButton;    // 替换：装备新装备
    [SerializeField] Button      decomposeButton;  // 分解：分解新装备获取经验
    [SerializeField] Button      closeButton;      // 关闭：保留原装备，丢弃新装备

    [Header("共用")]
    [SerializeField] GameObject  statRowPrefab;
    [SerializeField] TMP_FontAsset chineseFontAsset;
    [SerializeField] float       animDuration = 0.25f;

    CanvasGroup     _cg;
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

    // ── 公共接口 ─────────────────────────────────────────────────

    public void Show(EquipmentResult newItem, EquipmentResult oldItem)
    {
        _newItem = newItem;
        _oldItem = oldItem;
        PopulateOld(oldItem);
        PopulateNew(newItem, oldItem);
        ApplyFontToAllText();
        gameObject.SetActive(true);
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

    // ── 按钮回调 ──────────────────────────────────────────────────

    void OnReplace()
    {
        if (_newItem == null) return;
        EquipmentSlotSystem.Instance?.Equip(_newItem);
        Hide();
    }

    void OnDecompose()
    {
        if (_newItem == null) return;
        EquipmentSlotSystem.Instance?.Decompose(_newItem);
        Hide();
    }

    void OnClose() => Hide();

    // ── 填充数据 ──────────────────────────────────────────────────

    void PopulateOld(EquipmentResult old)
    {
        oldIconImage.sprite  = old.icon;
        oldIconImage.enabled = old.icon != null;
        oldNameText.text     = old.itemName;
        oldSlotText.text     = "槽位：" + old.slotName;
        oldDescText.text     = RarityStars(old.rarity);
        BuildStatRows(oldStatsContainer, old.bonusAttr, null);
    }

    void PopulateNew(EquipmentResult newItem, EquipmentResult oldItem)
    {
        newIconImage.sprite  = newItem.icon;
        newIconImage.enabled = newItem.icon != null;
        newNameText.text     = newItem.itemName;
        newSlotText.text     = "槽位：" + newItem.slotName;
        newDescText.text     = RarityStars(newItem.rarity);
        RoleAttr delta       = newItem.bonusAttr - oldItem.bonusAttr;
        BuildStatRows(newStatsContainer, newItem.bonusAttr, delta);
    }

    void BuildStatRows(Transform container, RoleAttr attr, RoleAttr? delta)
    {
        foreach (Transform child in container) Destroy(child.gameObject);

        TryAddRow(container, "攻击力", attr.Attack,        delta?.Attack,        "0");
        TryAddRow(container, "防御力", attr.Defence,       delta?.Defence,       "0");
        TryAddRow(container, "生命值", attr.Hp,            delta?.Hp,            "0");
        TryAddRow(container, "敏捷",   attr.Agility,       delta?.Agility,       "0");
        TryAddRow(container, "暴击率", attr.CritRate,      delta?.CritRate,      "0.##", "%");
        TryAddRow(container, "爆伤",   attr.CritDmg,       delta?.CritDmg,       "0.##", "%");
        TryAddRow(container, "反击",   attr.CounterRate,   delta?.CounterRate,   "0.##", "%");
        TryAddRow(container, "连击",   attr.ComboRate,     delta?.ComboRate,     "0.##", "%");
        TryAddRow(container, "闪避",   attr.DodgeRate,     delta?.DodgeRate,     "0.##", "%");
        TryAddRow(container, "击晕",   attr.StunRate,      delta?.StunRate,      "0.##", "%");
        TryAddRow(container, "吸血",   attr.LifeStealRate, delta?.LifeStealRate, "0.##", "%");
    }

    void TryAddRow(Transform container, string label, float value, float? delta,
                   string fmt, string suffix = "")
    {
        bool hasValue = Mathf.Abs(value) > 0.001f;
        bool hasDelta = delta.HasValue && Mathf.Abs(delta.Value) > 0.001f;
        if (!hasValue && !hasDelta) return;

        var row = Instantiate(statRowPrefab, container);
        var ui  = row.GetComponent<StatRowUI>();
        if (hasDelta)
            ui.SetWithDelta(label, value, delta.Value, fmt, suffix);
        else
            ui.Set(label, value, fmt, suffix);
    }

    static string RarityStars(int rarity)
    {
        int r = Mathf.Clamp(rarity, 1, 6);
        return new string('★', r) + new string('☆', 6 - r);
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
        string[] candidates = { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "NSimSun", "Heiti SC", "STHeiti" };
        Font osFont = Font.CreateDynamicFontFromOSFont(candidates, 32);
        if (osFont == null) return;
        _runtimeFont = TMP_FontAsset.CreateFontAsset(osFont);
    }

    System.Collections.IEnumerator AnimateFade(float from, float to,
                                               System.Action onDone = null)
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
