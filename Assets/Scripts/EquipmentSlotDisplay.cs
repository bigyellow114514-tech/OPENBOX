using UnityEngine;

/// <summary>
/// Displays one equipment slot: a persistent background plus an optional equipped item icon.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EquipmentSlotDisplay : MonoBehaviour
{
    [SerializeField] int slotIndex; // 0-11, matches EquipmentSlotSystem slots.
    [SerializeField] SpriteRenderer backgroundRenderer;
    [SerializeField] SpriteRenderer iconRenderer;
    [SerializeField] [Range(0.1f, 1f)] float iconFill = 0.85f;
    [SerializeField] Sprite emptyBackgroundSprite;
    [SerializeField] Sprite[] rarityBackgroundSprites = new Sprite[6]; // Rarity 1-6.

    Vector2 _slotSpriteSize;
    Collider2D _col;

    void Start()
    {
        _col = GetComponent<Collider2D>();

        Sprite sizeSprite = emptyBackgroundSprite != null ? emptyBackgroundSprite : backgroundRenderer?.sprite;
        _slotSpriteSize = sizeSprite != null ? sizeSprite.bounds.size : Vector2.one;

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
        if (item == null) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (_col != null && _col.OverlapPoint(worldPos))
        {
            SFXManager.PlayDianji();
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
        var item = EquipmentSlotSystem.Instance?.GetSlot(slotIndex);
        RefreshBackground(item);

        if (iconRenderer == null) return;

        if (item != null && item.icon != null)
        {
            iconRenderer.sprite = item.icon;
            iconRenderer.enabled = true;
            FitToSlot(item.icon);
        }
        else
        {
            iconRenderer.sprite = null;
            iconRenderer.enabled = false;
        }
    }

    void RefreshBackground(EquipmentResult item)
    {
        if (backgroundRenderer == null) return;

        Sprite sprite = item == null
            ? emptyBackgroundSprite
            : GetRarityBackground(item.rarity);

        if (sprite != null)
            backgroundRenderer.sprite = sprite;
    }

    Sprite GetRarityBackground(int rarity)
    {
        int index = Mathf.Clamp(rarity, 1, 6) - 1;
        if (rarityBackgroundSprites != null &&
            index < rarityBackgroundSprites.Length &&
            rarityBackgroundSprites[index] != null)
        {
            return rarityBackgroundSprites[index];
        }

        return emptyBackgroundSprite;
    }

    void FitToSlot(Sprite icon)
    {
        Vector2 iconSize = icon.bounds.size;
        if (iconSize.x <= 0 || iconSize.y <= 0) return;

        float scale = Mathf.Min(_slotSpriteSize.x / iconSize.x,
                                _slotSpriteSize.y / iconSize.y) * iconFill;

        iconRenderer.transform.localScale = Vector3.one * scale;
    }
}
