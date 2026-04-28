using UnityEngine;

/// <summary>
/// 挂在场景中每个 Equip_x 槽位对象上。
/// Inspector 里设定 slotIndex（0=武器 … 11=圣物），运行时自动显示已装备图标并适配大小。
/// </summary>
public class EquipmentSlotDisplay : MonoBehaviour
{
    [SerializeField] int            slotIndex;    // 0-11，与 SlotNames 对应
    [SerializeField] SpriteRenderer iconRenderer; // 显示装备图标（与 X 标同一个）
    [SerializeField] Sprite         emptySprite;  // 空槽时显示的 X 图标

    // 记录空槽时的初始缩放和世界尺寸，装备图标以此为目标大小
    Vector3 _originalScale;
    Vector2 _slotWorldSize; // 世界空间下槽位的宽高

    void Start()
    {
        _originalScale = iconRenderer != null
            ? iconRenderer.transform.localScale
            : transform.localScale;

        // 以空槽图标的边界计算槽位世界尺寸
        var refSprite = emptySprite ?? iconRenderer?.sprite;
        if (refSprite != null)
        {
            _slotWorldSize = new Vector2(
                refSprite.bounds.size.x * _originalScale.x,
                refSprite.bounds.size.y * _originalScale.y);
        }
        else
        {
            _slotWorldSize = Vector2.one;
        }

        if (EquipmentSlotSystem.Instance != null)
            EquipmentSlotSystem.Instance.OnSlotChanged += OnSlotChanged;

        Refresh();
    }

    void OnDestroy()
    {
        if (EquipmentSlotSystem.Instance != null)
            EquipmentSlotSystem.Instance.OnSlotChanged -= OnSlotChanged;
    }

    void OnSlotChanged(int changedIndex)
    {
        if (changedIndex == slotIndex)
            Refresh();
    }

    void Refresh()
    {
        if (iconRenderer == null) return;

        var item = EquipmentSlotSystem.Instance?.GetSlot(slotIndex);
        if (item != null && item.icon != null)
        {
            iconRenderer.sprite = item.icon;
            FitToSlot(item.icon);
        }
        else
        {
            iconRenderer.sprite = emptySprite;
            iconRenderer.transform.localScale = _originalScale; // 还原 X 标大小
        }
    }

    // 等比缩放图标，使其长边与槽位对齐
    void FitToSlot(Sprite icon)
    {
        Vector2 iconNaturalSize = icon.bounds.size;
        if (iconNaturalSize.x <= 0 || iconNaturalSize.y <= 0) return;

        float scaleX = _slotWorldSize.x / iconNaturalSize.x;
        float scaleY = _slotWorldSize.y / iconNaturalSize.y;
        float scale  = Mathf.Min(scaleX, scaleY); // 取小值保持比例，不溢出槽位

        iconRenderer.transform.localScale = Vector3.one * scale;
    }
}
