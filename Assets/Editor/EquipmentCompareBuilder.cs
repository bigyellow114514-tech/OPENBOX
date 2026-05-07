using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OpenBox → UI 构建 → 构建装备对比窗口
/// 直接复制场景中已有的 EquipmentCard，并排展示为对比窗口。
/// 依赖：请先执行 "OpenBox → UI 构建 → 构建装备卡片"。
/// </summary>
public static class EquipmentCompareBuilder
{
    const string FontPath = "Assets/Fonts/msyh SDF.asset";

    [MenuItem("OpenBox/UI 构建/构建装备对比窗口")]
    static void Build()
    {
        // ── 找到场景中已有的装备卡片 ──────────────────────────────
        var srcCardUI = Object.FindObjectOfType<EquipmentCardUI>();
        if (srcCardUI == null)
        {
            Debug.LogError("[CompareBuilder] 场景中找不到 EquipmentCardUI，请先执行 OpenBox → UI 构建 → 构建装备卡片");
            return;
        }

        // 从 EquipmentCardUI 取出 statRowPrefab
        var srcSO         = new SerializedObject(srcCardUI);
        var statRowPrefab = srcSO.FindProperty("statRowPrefab").objectReferenceValue as GameObject;
        if (statRowPrefab == null)
        {
            Debug.LogError("[CompareBuilder] EquipmentCardUI.statRowPrefab 未赋值，请重新执行 构建装备卡片");
            return;
        }

        var canvas = srcCardUI.GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("[CompareBuilder] 找不到父级 Canvas"); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning($"[CompareBuilder] 找不到字体 {FontPath}，中文可能显示乱码");

        // 删除旧对比窗口（重复执行时清理）
        var existing = canvas.transform.Find("EquipmentCompareCard");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // ── 构建根节点 ─────────────────────────────────────────────
        // 布局：LabelRow(24) + spacing(8) + ContentRow(280) + spacing(8) + ButtonRow(46) + padding(20) = 386 → 390
        // 宽：420(卡片) + 40(间距) + 420(卡片) + padding(20) = 900
        var root     = MakeUIGO("EquipmentCompareCard", canvas.transform);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta        = new Vector2(900, 390);
        rootRect.anchoredPosition = Vector2.zero;

        root.AddComponent<CanvasGroup>();
        var compareUI = root.AddComponent<EquipmentCompareUI>();

        var rootVLG = root.AddComponent<VerticalLayoutGroup>();
        rootVLG.padding                = new RectOffset(10, 10, 10, 10);
        rootVLG.spacing                = 8;
        rootVLG.childControlWidth      = false;
        rootVLG.childForceExpandWidth  = false;
        rootVLG.childControlHeight     = false;
        rootVLG.childForceExpandHeight = false;

        // ── 标题行：两个标签对齐两张卡片 ─────────────────────────
        var labelRow = MakeUIGO("LabelRow", root.transform);
        labelRow.GetComponent<RectTransform>().sizeDelta = new Vector2(880, 24);
        labelRow.AddComponent<LayoutElement>().preferredHeight = 24;

        var labelHLG = labelRow.AddComponent<HorizontalLayoutGroup>();
        labelHLG.spacing                = 40;
        labelHLG.childAlignment         = TextAnchor.MiddleLeft;
        labelHLG.childControlWidth      = false;
        labelHLG.childForceExpandWidth  = false;
        labelHLG.childControlHeight     = true;
        labelHLG.childForceExpandHeight = true;

        MakeLabel("LabelOld", labelRow.transform, "当前装备", new Color(0.45f, 0.28f, 0.10f), font);
        MakeLabel("LabelNew", labelRow.transform, "新装备",   new Color(0.20f, 0.50f, 0.10f), font);

        // ── 内容行：并排两张复制的卡片 ────────────────────────────
        var contentRow = MakeUIGO("ContentRow", root.transform);
        contentRow.AddComponent<LayoutElement>().preferredHeight = 280;

        var contentHLG = contentRow.AddComponent<HorizontalLayoutGroup>();
        contentHLG.spacing                = 40;
        contentHLG.childAlignment         = TextAnchor.UpperLeft;
        contentHLG.childControlWidth      = false;
        contentHLG.childForceExpandWidth  = false;
        contentHLG.childControlHeight     = false;
        contentHLG.childForceExpandHeight = false;

        var oldCard = DuplicateCard(srcCardUI.gameObject, "Card_当前装备", contentRow.transform);
        var newCard = DuplicateCard(srcCardUI.gameObject, "Card_新装备",   contentRow.transform);
        var oldLevelText = EnsureLevelText(oldCard, font);
        var newLevelText = EnsureLevelText(newCard, font);

        // ── 按钮行 ────────────────────────────────────────────────
        var btnRow = MakeUIGO("ButtonRow", root.transform);
        btnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(880, 46);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 46;

        var btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.spacing                = 20;
        btnHLG.childAlignment         = TextAnchor.MiddleCenter;
        btnHLG.childControlWidth      = false;
        btnHLG.childForceExpandWidth  = false;
        btnHLG.childControlHeight     = true;
        btnHLG.childForceExpandHeight = true;

        var replaceBtn   = CreateButton("ReplaceBtn",   btnRow.transform, new Color(0.20f, 0.55f, 0.20f), "替换装备",   140, font);
        var decomposeBtn = CreateButton("DecomposeBtn", btnRow.transform, new Color(0.75f, 0.35f, 0.10f), "分解新装备", 140, font);
        var keepBtn      = CreateButton("KeepBtn",      btnRow.transform, new Color(0.40f, 0.40f, 0.40f), "保留原装备", 140, font);

        // ── 连线 EquipmentCompareUI ───────────────────────────────
        var so = new SerializedObject(compareUI);
        so.FindProperty("oldCardBackgroundImage").objectReferenceValue = oldCard.GetComponent<Image>();
        so.FindProperty("oldIconImage").objectReferenceValue      = Find<Image>(oldCard,    "TopRow/IconSlot/ItemIcon");
        so.FindProperty("oldLevelText").objectReferenceValue      = oldLevelText;
        so.FindProperty("oldNameText").objectReferenceValue       = Find<TMP_Text>(oldCard, "TopRow/InfoGroup/NameText");
        so.FindProperty("oldSlotText").objectReferenceValue       = Find<TMP_Text>(oldCard, "TopRow/InfoGroup/SlotText");
        so.FindProperty("oldDescText").objectReferenceValue       = Find<TMP_Text>(oldCard, "BottomPanel/DescriptionText");
        so.FindProperty("oldStatsContainer").objectReferenceValue = oldCard.transform.Find("BottomPanel/StatsContainer");
        so.FindProperty("newCardBackgroundImage").objectReferenceValue = newCard.GetComponent<Image>();
        so.FindProperty("newIconImage").objectReferenceValue      = Find<Image>(newCard,    "TopRow/IconSlot/ItemIcon");
        so.FindProperty("newLevelText").objectReferenceValue      = newLevelText;
        so.FindProperty("newNameText").objectReferenceValue       = Find<TMP_Text>(newCard, "TopRow/InfoGroup/NameText");
        so.FindProperty("newSlotText").objectReferenceValue       = Find<TMP_Text>(newCard, "TopRow/InfoGroup/SlotText");
        so.FindProperty("newDescText").objectReferenceValue       = Find<TMP_Text>(newCard, "BottomPanel/DescriptionText");
        so.FindProperty("newStatsContainer").objectReferenceValue = newCard.transform.Find("BottomPanel/StatsContainer");
        so.FindProperty("replaceButton").objectReferenceValue      = replaceBtn;
        so.FindProperty("decomposeButton").objectReferenceValue    = decomposeBtn;
        so.FindProperty("closeButton").objectReferenceValue        = keepBtn;
        so.FindProperty("statRowPrefab").objectReferenceValue      = statRowPrefab;
        so.FindProperty("chineseFontAsset").objectReferenceValue   = font;
        AssignRarityBackgrounds(so.FindProperty("rarityBackgroundSprites"));
        so.ApplyModifiedProperties();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[CompareBuilder] 完成！请按 Ctrl+S 保存场景。");
    }

    // ── 复制卡片并清理 ─────────────────────────────────────────────

    static void AssignRarityBackgrounds(SerializedProperty property)
    {
        if (property == null || !property.isArray) return;

        string[] paths =
        {
            "Assets/Picture/UI/Equip_y_gray.png",
            "Assets/Picture/UI/Equip_y_green.png",
            "Assets/Picture/UI/Equip_y_blue.png",
            "Assets/Picture/UI/Equip_y_purple.png",
            "Assets/Picture/UI/Equip_y_orange.png",
            "Assets/Picture/UI/Equip_y_pink.png",
        };

        property.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
    }

    static GameObject DuplicateCard(GameObject source, string newName, Transform parent)
    {
        var copy = Object.Instantiate(source, parent, false);
        copy.name = newName;

        // 保留 sizeDelta，防止父级 HLG 通过 LayoutElement 改变尺寸
        var srcSize = source.GetComponent<RectTransform>().sizeDelta;
        var le      = copy.AddComponent<LayoutElement>();
        le.preferredWidth  = srcSize.x;
        le.preferredHeight = srcSize.y;
        le.flexibleWidth   = 0;
        le.flexibleHeight  = 0;

        // 移除 EquipmentCardUI（单例组件，对比卡片只做展示用）
        var cardUI = copy.GetComponent<EquipmentCardUI>();
        if (cardUI != null) Object.DestroyImmediate(cardUI);

        // 移除卡片内置的关闭按钮
        var closeBtn = copy.transform.Find("CloseButton");
        if (closeBtn != null) Object.DestroyImmediate(closeBtn.gameObject);

        return copy;
    }

    // ── 工具 ───────────────────────────────────────────────────────

    static T Find<T>(GameObject root, string path) where T : Component
    {
        var t = root.transform.Find(path);
        if (t == null) { Debug.LogWarning($"[CompareBuilder] 找不到路径 {root.name}/{path}"); return null; }
        return t.GetComponent<T>();
    }

    static TMP_Text EnsureLevelText(GameObject card, TMP_FontAsset font)
    {
        var iconSlot = card.transform.Find("TopRow/IconSlot");
        if (iconSlot == null)
        {
            Debug.LogWarning($"[CompareBuilder] 找不到路径 {card.name}/TopRow/IconSlot");
            return null;
        }

        var existing = iconSlot.Find("LevelText");
        var text = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (text != null)
        {
            if (font != null) text.font = font;
            text.transform.SetAsLastSibling();
            return text;
        }

        var go = MakeUIGO("LevelText", iconSlot);
        var rt = go.GetComponent<RectTransform>();
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
        if (font != null) text.font = font;
        return text;
    }

    static void MakeLabel(string goName, Transform parent, string text, Color color,
                          TMP_FontAsset font)
    {
        var go  = MakeUIGO(goName, parent);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 420;
        le.flexibleWidth   = 0;

        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        if (font != null) tmp.font = font;
    }

    static Button CreateButton(string goName, Transform parent, Color color,
                               string label, float width, TMP_FontAsset font)
    {
        var go  = MakeUIGO(goName, parent);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = 38;
        le.flexibleWidth   = 0;

        var img   = go.AddComponent<Image>();
        img.color = color;

        var btn    = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors              = btn.colors;
        colors.highlightedColor = color * 1.15f;
        colors.pressedColor     = color * 0.75f;
        btn.colors              = colors;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRT       = labelGo.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var tmp       = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        if (font != null) tmp.font = font;

        return btn;
    }

    static GameObject MakeUIGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
