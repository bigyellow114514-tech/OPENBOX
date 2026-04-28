using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text labelText;
    [SerializeField] TMP_Text valueText;
    [SerializeField] Image    iconImage;   // 可选，留空则隐藏

    static readonly Color ColorPositive = new Color(0.20f, 0.78f, 0.35f); // 绿
    static readonly Color ColorNegative = new Color(0.90f, 0.25f, 0.25f); // 红
    static readonly Color ColorNeutral  = new Color(0.85f, 0.75f, 0.50f); // 暖白（与木质背景协调）

    public void Set(string label, float value, string fmt = "0", string suffix = "",
                    Sprite icon = null)
    {
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
}
