using UnityEngine;

public sealed class CombatSceneUnitView
{
    readonly GameObject _root;
    readonly SpriteRenderer _renderer;
    readonly Transform _vfxAnchor;
    readonly float _maxWidth;
    readonly float _maxHeight;
    readonly bool _defaultFlip;
    readonly bool _lockScaleToFirstSprite;

    Sprite[] _idleFrames;
    Sprite[] _attackFrames;
    Sprite[] _hurtFrames;
    Sprite[] _deathFrames;
    Sprite _fallback;
    bool _hasLockedScale;
    Vector3 _lockedScale;

    public Transform Transform => _root.transform;
    public Transform VFXAnchor => _vfxAnchor;

    public CombatSceneUnitView(string name, Transform parent, int sortingOrder, float maxWidth, float maxHeight, bool defaultFlip, bool lockScaleToFirstSprite = false)
    {
        _maxWidth = Mathf.Max(0.01f, maxWidth);
        _maxHeight = Mathf.Max(0.01f, maxHeight);
        _defaultFlip = defaultFlip;
        _lockScaleToFirstSprite = lockScaleToFirstSprite;

        _root = new GameObject(name);
        _root.transform.SetParent(parent, false);

        _renderer = _root.AddComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;

        var anchor = new GameObject("VFXAnchor");
        anchor.transform.SetParent(_root.transform, false);
        anchor.transform.localPosition = new Vector3(0f, _maxHeight * 0.26f, 0f);
        _vfxAnchor = anchor.transform;
    }

    public void SetFrames(Sprite fallback, Sprite[] idle, Sprite[] attack, Sprite[] hurt, Sprite[] death)
    {
        _fallback = fallback;
        _idleFrames = idle;
        _attackFrames = attack;
        _hurtFrames = hurt;
        _deathFrames = death;
        SetSprite(FirstSprite(_idleFrames) ?? _fallback);
    }

    public void SetHome(Vector3 position)
    {
        _root.transform.localPosition = position;
    }

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
    }

    public Vector3 VisualPoint(float normalizedHeight)
    {
        if (_renderer.sprite == null)
            return _root.transform.position;

        Bounds bounds = _renderer.bounds;
        return new Vector3(
            bounds.center.x,
            Mathf.Lerp(bounds.min.y, bounds.max.y, Mathf.Clamp01(normalizedHeight)),
            _root.transform.position.z);
    }

    public void ShowIdle(float time, float fps)
    {
        SetSprite(FrameAt(_idleFrames, time, fps, true) ?? _fallback);
    }

    public void ShowAttack(float progress)
    {
        SetSprite(FrameAtProgress(_attackFrames, progress) ?? FirstSprite(_attackFrames) ?? FirstSprite(_idleFrames) ?? _fallback);
    }

    public void ShowHurt(float progress)
    {
        SetSprite(FrameAtProgress(_hurtFrames, progress) ?? FirstSprite(_hurtFrames) ?? FirstSprite(_idleFrames) ?? _fallback);
        ApplyHurtFeedback(progress);
    }

    public void ShowDeath(float progress)
    {
        SetSprite(FrameAtProgress(_deathFrames, progress) ?? FirstSprite(_deathFrames) ?? FirstSprite(_idleFrames) ?? _fallback);
    }

    void SetSprite(Sprite sprite)
    {
        if (sprite == null) return;

        _renderer.sprite = sprite;
        _renderer.flipX = _defaultFlip;
        _renderer.color = Color.white;

        Vector2 size = sprite.bounds.size;
        float scale = Mathf.Min(_maxWidth / Mathf.Max(0.01f, size.x), _maxHeight / Mathf.Max(0.01f, size.y));
        Vector3 localScale = Vector3.one * scale;
        if (_lockScaleToFirstSprite)
        {
            if (!_hasLockedScale)
            {
                _lockedScale = localScale;
                _hasLockedScale = true;
            }

            localScale = _lockedScale;
        }

        _root.transform.localScale = localScale;
    }

    void ApplyHurtFeedback(float progress)
    {
        float t = Mathf.Clamp01(progress);
        float pulse = 1f - Mathf.Clamp01(Mathf.Abs(t - 0.18f) / 0.18f);
        if (pulse <= 0f) return;

        _renderer.color = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.42f, 1f), pulse);
        _root.transform.localScale = new Vector3(
            _root.transform.localScale.x * (1f + pulse * 0.04f),
            _root.transform.localScale.y * (1f - pulse * 0.06f),
            _root.transform.localScale.z);
    }

    static Sprite FirstSprite(Sprite[] frames)
    {
        return frames != null && frames.Length > 0 ? frames[0] : null;
    }

    static Sprite FrameAt(Sprite[] frames, float time, float fps, bool loop)
    {
        if (frames == null || frames.Length == 0) return null;
        if (frames.Length == 1) return frames[0];

        int index = Mathf.FloorToInt(time * Mathf.Max(1f, fps));
        if (loop)
            index %= frames.Length;
        else
            index = Mathf.Clamp(index, 0, frames.Length - 1);

        return frames[index];
    }

    static Sprite FrameAtProgress(Sprite[] frames, float progress)
    {
        if (frames == null || frames.Length == 0) return null;
        if (frames.Length == 1) return frames[0];

        int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length), 0, frames.Length - 1);
        return frames[index];
    }
}
