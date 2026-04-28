using UnityEngine;

/// <summary>
/// 挂在场景中每个装备槽位对象上。
/// backgroundRenderer 是永远可见的背景格，iconRenderer 是叠加其上的装备图标层。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EquipmentSlotDisplay : MonoBehaviour
{
    [SerializeField] int            slotIndex;            // 0-11，与 SlotNames 对应
    [SerializeField] SpriteRenderer backgroundRenderer;   // 背景格，始终显示，不修改
    [SerializeField] SpriteRenderer iconRenderer;         // 覆盖在背景上的装备图标层
    [SerializeField] [Range(0.1f, 1f)] float iconFill = 0.85f; // 图标占背景格的比例，Inspector 可调

    Vector2    _slotSpriteSize; // 背景格的 sprite 原始尺寸（不含世界缩放）
    Collider2D _col;

    void Start()
    {
        _col = GetComponent<Collider2D>();

        // 只取 sprite 的原始尺寸；iconRenderer 与 backgroundRenderer 共享同一父级，
        // 两者的 lossyScale 相同，计算 localScale 时会自然消掉，无需乘入。
        if (backgroundRenderer != null && backgroundRenderer.sprite != null)
            _slotSpriteSize = backgroundRenderer.sprite.bounds.size;
        else
            _slotSpriteSize = Vector2.one;

        // 确保图标层居中在背景格上，修正场景中可能存在的 localPosition 偏移
        if (iconRenderer != null)
            iconRenderer.transform.localPosition = Vector3.zero;

        if (EquipmentSlotSystem.Instance != null)
            EquipmentSlotSystem.Instance.OnSlotChanged += OnSlotChanged;

        Refresh();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || TreeClick.Locked) return;

        var item = EquipmentSlotSystem.Instance?.GetSlot(slotIndex);
        if (item == null) return; // 空槽位不响应点击

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (_col != null && _col.OverlapPoint(worldPos))
        {
            TreeClick.Lock();
            EquipmentCardUI.Instance?.ShowFromSlot(item);
        }
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
            iconRenderer.sprite  = item.icon;
            iconRenderer.enabled = true;
            FitToSlot(item.icon);
        }
        else
        {
            iconRenderer.sprite  = null;
            iconRenderer.enabled = false;
        }
    }

    // 等比缩放图标：长边对齐背景格，再乘 iconFill 留出内边距
    void FitToSlot(Sprite icon)
    {
        Vector2 iconSize = icon.bounds.size;
        if (iconSize.x <= 0 || iconSize.y <= 0) return;

        float scale = Mathf.Min(_slotSpriteSize.x / iconSize.x,
                                 _slotSpriteSize.y / iconSize.y) * iconFill;

        iconRenderer.transform.localScale = Vector3.one * scale;
    }
}
