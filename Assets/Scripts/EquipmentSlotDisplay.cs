using System.Collections;
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

    [Header("Hover Effects")]
    [SerializeField] [Range(0f, 1f)] float glowAlpha = 0.55f;
    [SerializeField] float shakeMagnitude = 0.08f;
    [SerializeField] float glowFadeDuration = 0.18f;

    Vector2 _slotSpriteSize;
    Collider2D _col;
    SpriteRenderer _glowRenderer;
    Vector3 _baseLocalPos;
    bool _hovered;
    bool _shaking;
    Coroutine _glowCoroutine;
    Coroutine _shakeCoroutine;

    void Start()
    {
        _col = GetComponent<Collider2D>();
        _baseLocalPos = transform.localPosition;

        Sprite sizeSprite = emptyBackgroundSprite != null ? emptyBackgroundSprite : backgroundRenderer?.sprite;
        _slotSpriteSize = sizeSprite != null ? sizeSprite.bounds.size : Vector2.one;

        if (iconRenderer != null)
            iconRenderer.transform.localPosition = Vector3.zero;

        CreateGlowOverlay();

        if (EquipmentSlotSystem.Instance != null)
            EquipmentSlotSystem.Instance.OnSlotChanged += OnSlotChanged;

        Refresh();
    }

    void CreateGlowOverlay()
    {
        // Build a soft radial-gradient glow texture entirely in code.
        const int texSize = 64;
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center = new Vector2(texSize * 0.5f, texSize * 0.5f);
        float radius = texSize * 0.5f;
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float t = 1f - Mathf.Clamp01(dist / radius);
                // Quadratic falloff → soft edge; warm white tint.
                tex.SetPixel(x, y, new Color(1f, 0.95f, 0.7f, t * t));
            }
        }
        tex.Apply();

        // PPU = texSize makes the sprite exactly 1×1 world unit.
        Sprite glowSprite = Sprite.Create(
            tex,
            new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f),
            texSize);

        var glowGo = new GameObject("_GlowOverlay");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = Vector3.zero;

        // Scale so the glow is ~1.6× the slot's world size.
        float s = Mathf.Max(_slotSpriteSize.x, _slotSpriteSize.y) * 1.6f;
        glowGo.transform.localScale = new Vector3(s, s, 1f);

        _glowRenderer = glowGo.AddComponent<SpriteRenderer>();
        _glowRenderer.sprite = glowSprite;
        _glowRenderer.color = new Color(1f, 0.95f, 0.7f, 0f);

        if (backgroundRenderer != null)
        {
            _glowRenderer.sortingLayerName = backgroundRenderer.sortingLayerName;
            _glowRenderer.sortingOrder = backgroundRenderer.sortingOrder - 1; // behind the slot frame
        }
    }

    void Update()
    {
        HandleClick();
        HandleHover();
    }

    void HandleClick()
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

    void HandleHover()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool nowHovered = _col != null && _col.OverlapPoint(mouseWorld);
        if (nowHovered == _hovered) return;

        _hovered = nowHovered;

        if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
        _glowCoroutine = StartCoroutine(LerpGlow(_hovered ? glowAlpha : 0f));

        if (_hovered && !_shaking)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeOnce());
        }
    }

    IEnumerator LerpGlow(float targetAlpha)
    {
        if (_glowRenderer == null) yield break;
        float startAlpha = _glowRenderer.color.a;
        float elapsed = 0f;
        while (elapsed < glowFadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / glowFadeDuration);
            _glowRenderer.color = new Color(1f, 0.95f, 0.7f, a);
            yield return null;
        }
        _glowRenderer.color = new Color(1f, 0.95f, 0.7f, targetAlpha);
    }

    IEnumerator ShakeOnce()
    {
        _shaking = true;
        float duration = 0.32f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Two full left-right cycles with decaying amplitude.
            float x = Mathf.Sin(t * Mathf.PI * 4f) * shakeMagnitude * (1f - t);
            transform.localPosition = _baseLocalPos + new Vector3(x, 0f, 0f);
            yield return null;
        }
        transform.localPosition = _baseLocalPos;
        _shaking = false;
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
