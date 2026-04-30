using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text labelText;
    [SerializeField] TMP_Text valueText;
    [SerializeField] Image    iconImage;   // 可选，留空则隐藏

    static readonly Color ColorPositive = new Color(1.00f, 0.55f, 0.00f); // 橙
    static readonly Color ColorNegative = new Color(0.12f, 0.39f, 0.86f); // 蓝
    static readonly Color ColorNeutral  = new Color(0.00f, 0.00f, 0.00f); // 黑

    public void Set(string label, float value, string fmt = "0", string suffix = "",
                    Sprite icon = null)
    {
        labelText.margin = Vector4.zero;
        labelText.text = label;

        string prefix  = value > 0f ? "+" : "";
        valueText.text = prefix + value.ToString(fmt) + suffix;
        valueText.color = value > 0f ? ColorPositive
                        : value < 0f ? ColorNegative
                        : ColorNeutral;

        if (iconImage != null)
        {
            iconImage.sprite  = icon;
            iconImage.enabled = icon != null;
        }
    }

    // 带增减对比的显示：在数值后附加彩色 delta 标注
    public void SetWithDelta(string label, float value, float delta,
                             string fmt = "0", string suffix = "")
    {
        labelText.text = label;

        string prefix = value > 0f ? "+" : "";
        string text   = prefix + value.ToString(fmt) + suffix;

        if (delta > 0.001f)
            text += $" <color=#00C832>(▲+{delta.ToString(fmt)}{suffix})</color>";
        else if (delta < -0.001f)
            text += $" <color=#C83200>(▼{(-delta).ToString(fmt)}{suffix})</color>";

        valueText.text  = text;
        valueText.color = value > 0f ? ColorPositive
                        : value < 0f ? ColorNegative
                        : ColorNeutral;

        if (iconImage != null) iconImage.enabled = false;
    }
}
