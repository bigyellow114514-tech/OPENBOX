using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainSceneNavigatorSceneBuilder
{
    const string CanvasName = "NavigatorCanvas";

    [MenuItem("OpenBox/UI 构建/构建主界面导航按钮")]
    public static void BuildMainSceneNavigatorButtons()
    {
        MainSceneNavigator navigator = Object.FindObjectOfType<MainSceneNavigator>();
        if (navigator == null)
        {
            var navigatorGO = new GameObject("MainSceneNavigator");
            navigator = navigatorGO.AddComponent<MainSceneNavigator>();
        }

        GameObject oldCanvas = GameObject.Find(CanvasName);
        if (oldCanvas != null)
            Object.DestroyImmediate(oldCanvas);

        GameObject canvasGO = new GameObject(CanvasName);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        Button arrowButton = CreateArrowButton(canvasGO.transform);
        Button petButton = CreateIconButton(canvasGO.transform, "PetButton", "宠物", new Vector2(20f, -92f), "Assets/Resources/UI/Pets/Pet_Button.png");
        Button petGachaButton = CreateIconButton(canvasGO.transform, "PetGachaButton", "抽宠", new Vector2(20f, -204f), "Assets/Resources/UI/Pets/Gacha_Button.png");

        var serialized = new SerializedObject(navigator);
        serialized.FindProperty("arrowButton").objectReferenceValue = arrowButton;
        serialized.FindProperty("petButton").objectReferenceValue = petButton;
        serialized.FindProperty("petGachaButton").objectReferenceValue = petGachaButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(navigator);
        EditorSceneManager.MarkSceneDirty(navigator.gameObject.scene);
        EditorSceneManager.SaveScene(navigator.gameObject.scene);
        Debug.Log("[MainSceneNavigatorSceneBuilder] 主界面宠物按钮已生成为默认显示的 UGUI Button。");
    }

    static Button CreateArrowButton(Transform parent)
    {
        GameObject buttonGO = NewUI("ArrowButton", parent);
        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(20f, 0f);
        rect.sizeDelta = new Vector2(100f, 100f);

        var image = buttonGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.45f);
        var button = buttonGO.AddComponent<Button>();
        CreateLabel(buttonGO.transform, "Label", "◄", 56f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    static Button CreateIconButton(Transform parent, string name, string label, Vector2 position, string spritePath)
    {
        GameObject buttonGO = NewUI(name, parent);
        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(100f, 100f);

        var image = buttonGO.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        image.color = image.sprite != null ? Color.white : new Color(0.16f, 0.35f, 0.42f, 0.92f);

        var button = buttonGO.AddComponent<Button>();
        CreateLabel(buttonGO.transform, "Label", label, 17f, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 24f));
        return button;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void CreateLabel(Transform parent, string name, string label, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject labelGO = NewUI(name, parent);
        var rect = labelGO.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.offsetMin = anchorMin == Vector2.zero && anchorMax == Vector2.one ? Vector2.zero : rect.offsetMin;
        rect.offsetMax = anchorMin == Vector2.zero && anchorMax == Vector2.one ? Vector2.zero : rect.offsetMax;

        var text = labelGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }
}
