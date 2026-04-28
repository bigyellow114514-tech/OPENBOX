using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentCardUI : MonoBehaviour
{
    [Header("Top Row")]
    [SerializeField] Image       iconImage;
    [SerializeField] TMP_Text    nameText;
    [SerializeField] TMP_Text    slotText;

    [Header("Stats Area")]
    [SerializeField] Transform   statsContainer; // VerticalLayoutGroup
    [SerializeField] GameObject  statRowPrefab;  // 含两个 TMP_Text 的预制体

    [Header("Bottom")]
    [SerializeField] TMP_Text    descriptionText;

    [Header("Animation")]
    [SerializeField] float       animDuration = 0.25f;

    CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    // ── 公共接口 ──────────────────────────────────────────

    public void Show(EquipmentItem data)
    {
        Populate(data);
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateFade(0f, 1f));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateFade(1f, 0f, onDone: () => gameObject.SetActive(false)));
    }

    // ── 填充数据 ──────────────────────────────────────────

    void Populate(EquipmentItem data)
    {
        iconImage.sprite  = data.icon;
        iconImage.enabled = data.icon != null;
        nameText.text     = data.itemName;
        slotText.text     = "槽位：" + data.slotType;
        descriptionText.text = data.description;

        BuildStatRows(data.bonusAttr);
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
