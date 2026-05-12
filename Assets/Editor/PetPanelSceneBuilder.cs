using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PetPanelSceneBuilder
{
    const string CanvasName = "PetPanelCanvas";

    [MenuItem("OpenBox/UI 构建/构建宠物面板")]
    public static void BuildPetPanelCanvas()
    {
        GameObject oldCanvas = GameObject.Find(CanvasName);
        if (oldCanvas == null)
        {
            PetPanelUI existingInactive = Object.FindObjectOfType<PetPanelUI>(true);
            if (existingInactive != null && existingInactive.name == CanvasName)
                oldCanvas = existingInactive.gameObject;
        }

        if (oldCanvas != null)
            Object.DestroyImmediate(oldCanvas);

        GameObject canvasGO = new GameObject(CanvasName);
        var rect = canvasGO.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        PetPanelUI panel = canvasGO.AddComponent<PetPanelUI>();
        panel.RebuildShellForEditor();
        canvasGO.SetActive(false);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        EditorSceneManager.SaveScene(canvasGO.scene);
        Debug.Log("[PetPanelSceneBuilder] PetPanelCanvas 已生成为场景内默认可调的 UGUI。");
    }
}
