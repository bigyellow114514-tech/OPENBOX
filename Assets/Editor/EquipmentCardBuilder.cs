using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class EquipmentCardBuilder
{
    const string StatRowPrefabPath = "Assets/Prefabs/StatRow.prefab";

    [MenuItem("OpenBox/Build Equipment Card UI")]
    static void Build()
    {
        EnsureEquipYIsSprite();
        EnsureEquipXIsSprite();

        var canvas        = GetOrCreateCanvas();
        var statRowPrefab = CreateStatRowPrefab();
        var card          = BuildCardHierarchy(canvas.transform, statRowPrefab);

        Selection.activeGameObject = card;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[EquipmentCardBuilder] 完成！请按 Ctrl+S 保存场景。");
    }

    // ── 1. 确保 Equip_y.png 是 Sprite 类型 ────────────────────────────

    static void EnsureEquipYIsSprite()
    {
        string path = "Assets/Picture/UI/Equip_y.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning("找不到 Equip_y.png，请确认路径。"); return; }
        if (importer.textureType == TextureImporterType.Sprite) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }

    static void EnsureEquipXIsSprite()
    {
        string path = "Assets/Picture/UI/Equip_x.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning("找不到 Equip_x.png，请确认路径。"); return; }
        if (importer.textureType == TextureImporterType.Sprite) return;

        importer.textureType      = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }

    // ── 2. 专属 Canvas（Screen Space Overlay，Sort Order 100）────────────

    const string CanvasName = "EquipmentCardCanvas";

    static Canvas GetOrCreateCanvas()
    {
        // 按名字找，避免误用场景里 World Space 等其他 Canvas
        var existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            var c = existing.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var go     = new GameObject(CanvasName);
        go.transform.SetParent(null);   // 强制根节点，CanvasScaler 才生效
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        return canvas;
    }

    // ── 测试：直接在运行时强制弹出卡片 ───────────────────────────────────

    [MenuItem("OpenBox/Test Equipment Card (Runtime only)")]
    static void TestShow()
    {
        var ui = Object.FindObjectOfType<EquipmentCardUI>();
        if (ui == null) { Debug.LogError("场景中没有 EquipmentCardUI，请先执行 Build Equipment Card UI"); return; }

        var dummy = new EquipmentResult
        {
            itemName  = "测试之剑",
            slotName  = "武器",
            icon      = null,
            rarity    = 3,
            bonusAttr = new RoleAttr { Attack = 350, CritRate = 8, CritDmg = 50 }
        };
        ui.Show(dummy);
        Debug.Log("[Test] EquipmentCardUI.Show() 已调用");
    }

    // ── 3. StatRow 预制体 ──────────────────────────────────────────────

    static GameObject CreateStatRowPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var row = new GameObject("StatRow");
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 24;
        rowLE.flexibleWidth   = 1;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childControlWidth    = true;
        hlg.childForceExpandWidth = false;
        hlg.childControlHeight   = true;
        hlg.childForceExpandHeight = false;

        // Icon（可选，默认隐藏）
        var iconGO  = MakeUIGO("Icon", row.transform);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.enabled = false;
        var iconLE  = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = 18;
        iconLE.preferredHeight = 18;
        iconLE.flexibleWidth   = 0;

        // LabelText
        var labelGO   = MakeUIGO("LabelText", row.transform);
        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text      = "词条名";
        labelText.fontSize  = 13;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color     = new Color(0.85f, 0.75f, 0.50f);
        labelGO.AddComponent<LayoutElement>().flexibleWidth = 1;

        // ValueText
        var valueGO   = MakeUIGO("ValueText", row.transform);
        var valueText = valueGO.AddComponent<TextMeshProUGUI>();
        valueText.text      = "+0";
        valueText.fontSize  = 13;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.color     = new Color(0.20f, 0.78f, 0.35f);
        var valueLE = valueGO.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 70;
        valueLE.flexibleWidth  = 0;

        // 挂 StatRowUI 并连线
        var statRowUI = row.AddComponent<StatRowUI>();
        var so = new SerializedObject(statRowUI);
        so.FindProperty("labelText").objectReferenceValue = labelText;
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.FindProperty("iconImage").objectReferenceValue = iconImg;
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, StatRowPrefabPath);
        Object.DestroyImmediate(row);
        return prefab;
    }

    // ── 4. 卡片主体 ────────────────────────────────────────────────────

    static GameObject BuildCardHierarchy(Transform canvasParent, GameObject statRowPrefab)
    {
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Picture/UI/Equip_y.png");

        // ── EquipmentCard 根节点 ──
        var card     = MakeUIGO("EquipmentCard", canvasParent);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta        = new Vector2(420, 280);
        cardRect.anchoredPosition = Vector2.zero;

        var cardImg  = card.AddComponent<Image>();
        cardImg.sprite = bgSprite;
        cardImg.type   = Image.Type.Sliced;
        cardImg.color  = Color.white;

        card.AddComponent<CanvasGroup>();
        var cardUI = card.AddComponent<EquipmentCardUI>();

        // 用 VLG 把 TopRow / BottomPanel 上下排列
        var rootVLG = card.AddComponent<VerticalLayoutGroup>();
        rootVLG.padding               = new RectOffset(14, 14, 10, 10);
        rootVLG.spacing               = 8;
        rootVLG.childControlWidth     = true;
        rootVLG.childForceExpandWidth = true;
        rootVLG.childControlHeight    = false;
        rootVLG.childForceExpandHeight = false;

        // ── TopRow ──
        var topRow    = MakeUIGO("TopRow", card.transform);
        var topRowLE  = topRow.AddComponent<LayoutElement>();
        topRowLE.preferredHeight = 82;
        topRowLE.flexibleHeight  = 0;

        var topHLG = topRow.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing               = 10;
        topHLG.childAlignment        = TextAnchor.MiddleLeft;
        topHLG.childControlWidth     = false;
        topHLG.childForceExpandWidth = false;
        topHLG.childControlHeight    = true;
        topHLG.childForceExpandHeight = true;

        // IconSlot — 不加 Image，Equip_y.png 背景已包含框体
        var iconSlot   = MakeUIGO("IconSlot", topRow.transform);
        var iconSlotLE = iconSlot.AddComponent<LayoutElement>();
        iconSlotLE.preferredWidth  = 78;
        iconSlotLE.preferredHeight = 78;
        iconSlotLE.flexibleWidth   = 0;

        // ItemIcon — 锚点撑满父级，用 offsetMin/Max 留 4px 内边距
        var itemIcon     = MakeUIGO("ItemIcon", iconSlot.transform);
        var itemIconRect = itemIcon.GetComponent<RectTransform>();
        itemIconRect.anchorMin = Vector2.zero;
        itemIconRect.anchorMax = Vector2.one;
        itemIconRect.offsetMin = new Vector2(4, 4);
        itemIconRect.offsetMax = new Vector2(-4, -4);
        var itemIconImg = itemIcon.AddComponent<Image>();
        itemIconImg.preserveAspect = true;

        // InfoGroup
        var infoGroup  = MakeUIGO("InfoGroup", topRow.transform);
        var infoGroupLE = infoGroup.AddComponent<LayoutElement>();
        infoGroupLE.flexibleWidth = 1;

        var infoVLG = infoGroup.AddComponent<VerticalLayoutGroup>();
        infoVLG.childAlignment        = TextAnchor.MiddleLeft;
        infoVLG.padding               = new RectOffset(4, 0, 8, 8);
        infoVLG.spacing               = 6;
        infoVLG.childControlWidth     = true;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childControlHeight    = false;
        infoVLG.childForceExpandHeight = false;

        var nameGO   = MakeUIGO("NameText", infoGroup.transform);
        var nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text      = "装备名称";
        nameText.fontSize  = 17;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color     = new Color(0.22f, 0.10f, 0.02f);
        nameGO.AddComponent<LayoutElement>().preferredHeight = 26;

        var slotGO   = MakeUIGO("SlotText", infoGroup.transform);
        var slotText = slotGO.AddComponent<TextMeshProUGUI>();
        slotText.text     = "槽位：武器";
        slotText.fontSize = 13;
        slotText.color    = new Color(0.45f, 0.28f, 0.10f);
        slotGO.AddComponent<LayoutElement>().preferredHeight = 20;

        // ── BottomPanel ──
        var bottomPanel  = MakeUIGO("BottomPanel", card.transform);
        var bottomPanelLE = bottomPanel.AddComponent<LayoutElement>();
        bottomPanelLE.flexibleHeight = 1;

        var bottomVLG = bottomPanel.AddComponent<VerticalLayoutGroup>();
        bottomVLG.spacing               = 4;
        bottomVLG.padding               = new RectOffset(2, 2, 2, 2);
        bottomVLG.childAlignment        = TextAnchor.UpperLeft;
        bottomVLG.childControlWidth     = true;
        bottomVLG.childForceExpandWidth = true;
        bottomVLG.childControlHeight    = false;
        bottomVLG.childForceExpandHeight = false;

        // StatsContainer
        var statsContainer = MakeUIGO("StatsContainer", bottomPanel.transform);
        var statsVLG       = statsContainer.AddComponent<VerticalLayoutGroup>();
        statsVLG.spacing               = 2;
        statsVLG.childControlWidth     = true;
        statsVLG.childForceExpandWidth = true;
        statsVLG.childControlHeight    = false;
        statsVLG.childForceExpandHeight = false;
        var statsCSF = statsContainer.AddComponent<ContentSizeFitter>();
        statsCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        statsContainer.AddComponent<LayoutElement>().flexibleHeight = 1;

        // DescriptionText
        var descGO   = MakeUIGO("DescriptionText", bottomPanel.transform);
        var descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text              = "「这里是装备描述文字」";
        descText.fontSize          = 12;
        descText.fontStyle         = FontStyles.Italic;
        descText.color             = new Color(0.45f, 0.28f, 0.10f);
        descText.enableWordWrapping = true;
        var descLE = descGO.AddComponent<LayoutElement>();
        descLE.preferredHeight = 36;
        descLE.flexibleHeight  = 0;

        // ── CloseButton（锚定右上角，浮在 VLG 之外）──
        Sprite closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Picture/UI/Equip_x.png");

        var closeBtnGO   = MakeUIGO("CloseButton", card.transform);
        var closeBtnRect = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRect.anchorMin        = new Vector2(1, 1);
        closeBtnRect.anchorMax        = new Vector2(1, 1);
        closeBtnRect.pivot            = new Vector2(1, 1);
        closeBtnRect.anchoredPosition = new Vector2(-6, -6);
        closeBtnRect.sizeDelta        = new Vector2(36, 36);

        var closeBtnLE = closeBtnGO.AddComponent<LayoutElement>();
        closeBtnLE.ignoreLayout = true;   // 不参与 VLG 排列

        var closeBtnImg = closeBtnGO.AddComponent<Image>();
        closeBtnImg.sprite           = closeSprite;
        closeBtnImg.preserveAspect   = true;
        closeBtnImg.raycastTarget    = true;

        var closeBtn = closeBtnGO.AddComponent<Button>();
        var colors   = closeBtn.colors;
        colors.highlightedColor = new Color(1f, 0.75f, 0.55f);
        colors.pressedColor     = new Color(0.75f, 0.35f, 0.15f);
        closeBtn.colors = colors;

        // ── 连线 EquipmentCardUI 的 SerializedField ──
        var cardSO = new SerializedObject(cardUI);
        cardSO.FindProperty("iconImage").objectReferenceValue       = itemIconImg;
        cardSO.FindProperty("nameText").objectReferenceValue        = nameText;
        cardSO.FindProperty("slotText").objectReferenceValue        = slotText;
        cardSO.FindProperty("statsContainer").objectReferenceValue  = statsContainer.transform;
        cardSO.FindProperty("statRowPrefab").objectReferenceValue   = statRowPrefab;
        cardSO.FindProperty("descriptionText").objectReferenceValue = descText;
        cardSO.FindProperty("closeButton").objectReferenceValue     = closeBtn;
        cardSO.ApplyModifiedProperties();

        return card;
    }

    // ── 工具 ──────────────────────────────────────────────────────────

    static GameObject MakeUIGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
