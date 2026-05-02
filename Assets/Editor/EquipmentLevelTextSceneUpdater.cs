using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EquipmentLevelTextSceneUpdater
{
    const string FontPath = "Assets/Fonts/msyh SDF.asset";

    static EquipmentLevelTextSceneUpdater()
    {
        EditorApplication.delayCall += UpdateOpenScene;
    }

    static void UpdateOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        bool changed = false;

        foreach (var cardUI in Resources.FindObjectsOfTypeAll<EquipmentCardUI>())
        {
            if (!IsSceneObject(cardUI.gameObject)) continue;

            TMP_Text levelText = EnsureLevelText(cardUI.transform, font);
            if (levelText == null) continue;

            var so = new SerializedObject(cardUI);
            var prop = so.FindProperty("levelText");
            if (prop != null && prop.objectReferenceValue != levelText)
            {
                prop.objectReferenceValue = levelText;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        foreach (var compareUI in Resources.FindObjectsOfTypeAll<EquipmentCompareUI>())
        {
            if (!IsSceneObject(compareUI.gameObject)) continue;

            changed |= AssignCompareLevelText(compareUI, "oldLevelText", "ContentRow/Card_当前装备", font);
            changed |= AssignCompareLevelText(compareUI, "newLevelText", "ContentRow/Card_新装备", font);
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static bool AssignCompareLevelText(EquipmentCompareUI compareUI, string fieldName, string cardPath, TMP_FontAsset font)
    {
        Transform card = compareUI.transform.Find(cardPath);
        TMP_Text levelText = EnsureLevelText(card, font);
        if (levelText == null) return false;

        var so = new SerializedObject(compareUI);
        var prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue == levelText) return false;

        prop.objectReferenceValue = levelText;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static TMP_Text EnsureLevelText(Transform cardRoot, TMP_FontAsset font)
    {
        if (cardRoot == null) return null;

        Transform iconSlot = cardRoot.Find("TopRow/IconSlot");
        if (iconSlot == null) return null;

        Transform found = iconSlot.Find("LevelText");
        var text = found != null ? found.GetComponent<TMP_Text>() : null;
        if (text == null)
        {
            var go = new GameObject("LevelText");
            Undo.RegisterCreatedObjectUndo(go, "Add equipment level text");
            go.transform.SetParent(iconSlot, false);

            var rt = go.AddComponent<RectTransform>();
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
        }

        if (font != null) text.font = font;
        text.transform.SetAsLastSibling();
        return text;
    }

    static bool IsSceneObject(GameObject go)
    {
        return go.scene.IsValid() && !EditorUtility.IsPersistent(go);
    }
}
