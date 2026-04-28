using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class EquipmentCardButtonBuilder
{
    const string FontPath = "Assets/Fonts/msyh SDF.asset";

    [MenuItem("OpenBox/UI 构建/构建装备卡片按钮")]
    static void Build()
    {
        var cardUI = Object.FindObjectOfType<EquipmentCardUI>();
        if (cardUI == null)
        {
            Debug.LogError("[ButtonBuilder] 场景中找不到 EquipmentCardUI");
            return;
        }

        // 挂在 Canvas 下，避免受 EquipmentCard 布局组影响
        var canvas = cardUI.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ButtonBuilder] 找不到父级 Canvas");
            return;
        }
        var canvasRT = canvas.GetComponent<RectTransform>();

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning($"[ButtonBuilder] 找不到字体 {FontPath}，中文可能显示乱码");

        // 删除旧按钮（重复执行时清理）
        RemoveChild(canvasRT, "EquipButton");
        RemoveChild(canvasRT, "DecomposeButton");

        // 创建两个按钮，锚定到 Canvas 中心偏下
        var equipBtn      = CreateButton("EquipButton",      canvasRT, -90f, new Color(0.20f, 0.55f, 0.20f), "装备",  font);
        var decomposeBtn  = CreateButton("DecomposeButton",  canvasRT,  90f, new Color(0.75f, 0.35f, 0.10f), "分解",  font);

        // 绑定到 EquipmentCardUI
        var so = new SerializedObject(cardUI);
        so.FindProperty("equipButton").objectReferenceValue     = equipBtn;
        so.FindProperty("decomposeButton").objectReferenceValue = decomposeBtn;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ButtonBuilder] 完成！已创建装备/分解按钮，请 Ctrl+S 保存场景。");
        Selection.activeGameObject = cardUI.gameObject;
    }

    // ── 工具函数 ──────────────────────────────────────────────────────

    static Button CreateButton(string goName, RectTransform parent, float offsetX, Color color, string label,
                               TMP_FontAsset font)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        // 锚定到卡片底部中心，方便上下左右拖动
        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0.5f, 0f);
        rt.anchorMax    = new Vector2(0.5f, 0f);
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta    = new Vector2(120f, 36f);
        rt.anchoredPosition = new Vector2(offsetX, -148f); // Canvas 中心偏下，与卡片底部对齐

        var img   = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // 设置 hover/press 颜色变深
        var colors               = btn.colors;
        colors.highlightedColor  = color * 1.15f;
        colors.pressedColor      = color * 0.75f;
        btn.colors               = colors;

        // 文字标签
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);

        var labelRT         = labelGo.AddComponent<RectTransform>();
        labelRT.anchorMin   = Vector2.zero;
        labelRT.anchorMax   = Vector2.one;
        labelRT.offsetMin   = Vector2.zero;
        labelRT.offsetMax   = Vector2.zero;

        var tmp       = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        if (font != null) tmp.font = font;

        return btn;
    }

    static void RemoveChild(RectTransform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }
}
