using UnityEditor;
using UnityEngine;

/// <summary>
/// 迁移工具：清理旧版本遗留在 Canvas 根层级的孤立装备/分解按钮。
/// 新版本的按钮已由 EquipmentCardBuilder ("OpenBox/UI 构建/构建装备卡片") 一并创建在卡片内部。
/// </summary>
public static class EquipmentCardButtonBuilder
{
    [MenuItem("OpenBox/UI 构建/清理旧装备卡片按钮（迁移用）")]
    static void Cleanup()
    {
        var cardUI = Object.FindObjectOfType<EquipmentCardUI>();
        if (cardUI == null)
        {
            Debug.LogError("[ButtonBuilder] 场景中找不到 EquipmentCardUI，请先执行 OpenBox → UI 构建 → 构建装备卡片");
            return;
        }

        var canvas = cardUI.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ButtonBuilder] 找不到父级 Canvas");
            return;
        }
        var canvasRT = canvas.GetComponent<RectTransform>();

        bool removed = false;
        removed |= RemoveChild(canvasRT, "EquipButton");
        removed |= RemoveChild(canvasRT, "DecomposeButton");

        if (removed)
        {
            EditorUtility.SetDirty(canvas.gameObject);
            Debug.Log("[ButtonBuilder] 已清理 Canvas 根层级的旧按钮。新按钮已内置在卡片内，请重新执行 OpenBox → UI 构建 → 构建装备卡片 完成重建。");
        }
        else
        {
            Debug.Log("[ButtonBuilder] 未发现旧版孤立按钮，无需清理。");
        }
    }

    static bool RemoveChild(RectTransform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing == null) return false;
        Object.DestroyImmediate(existing.gameObject);
        return true;
    }
}
