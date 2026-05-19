using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TreeUpgradePanelSceneBuilder
{
    const string CanvasName = "TreeUpgradePanelCanvas";

    [MenuItem("OpenBox/UI 构建/构建升级大树面板")]
    public static void BuildTreeUpgradePanelCanvas()
    {
        GameObject oldCanvas = GameObject.Find(CanvasName);
        if (oldCanvas == null)
        {
            TreeUpgradePanelUI existingInactive = Object.FindObjectOfType<TreeUpgradePanelUI>(true);
            if (existingInactive != null && existingInactive.name == CanvasName)
                oldCanvas = existingInactive.gameObject;
        }

        if (oldCanvas != null)
            Object.DestroyImmediate(oldCanvas);

        GameObject canvasGO = new GameObject(CanvasName);
        RectTransform rect = canvasGO.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        TreeUpgradePanelUI panel = canvasGO.AddComponent<TreeUpgradePanelUI>();
        panel.RebuildShellForEditor();
        canvasGO.SetActive(false);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        EditorSceneManager.SaveScene(canvasGO.scene);
        Debug.Log("[TreeUpgradePanelSceneBuilder] TreeUpgradePanelCanvas 已生成为场景内默认隐藏、可调的 UGUI。");
    }
}
