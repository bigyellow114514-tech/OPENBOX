using UnityEngine;

public sealed class PetWalkerBehaviour : MonoBehaviour
{
    const float AnimFps          = 8f;
    const float WalkSpeed        = 1.2f;
    const float MinWaitTime      = 1.5f;
    const float MaxWaitTime      = 4.5f;
    const float ArrivalThreshold = 0.1f;
    const int   SampleAttempts   = 50;

    Sprite[]       _frames;
    SpriteRenderer _renderer;

    // Fence polygon in world space — cached once at init, no physics dependency
    Vector2[] _poly;
    Bounds    _polyBounds;

    Vector3 _targetPos;
    float   _waitTimer;
    bool    _waiting = true;
    int     _frameIndex;
    float   _animTimer;

    public void Initialize(Sprite[] frames, PolygonCollider2D fence)
    {
        _frames   = frames;
        _renderer = GetComponent<SpriteRenderer>();

        if (fence != null)
            CacheFence(fence);

        _waitTimer = Random.Range(0f, 2.5f);
        _waiting   = true;
    }

    void CacheFence(PolygonCollider2D fence)
    {
        Vector2[]  local = fence.GetPath(0);
        Transform  t     = fence.transform;
        _poly = new Vector2[local.Length];

        float xMin = float.MaxValue, yMin = float.MaxValue;
        float xMax = float.MinValue, yMax = float.MinValue;

        for (int i = 0; i < local.Length; i++)
        {
            Vector2 w = t.TransformPoint(local[i]);
            _poly[i] = w;
            if (w.x < xMin) xMin = w.x;
            if (w.y < yMin) yMin = w.y;
            if (w.x > xMax) xMax = w.x;
            if (w.y > yMax) yMax = w.y;
        }

        _polyBounds = new Bounds(
            new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f),
            new Vector3(xMax - xMin,           yMax - yMin));
    }

    // Ray-casting point-in-polygon — works regardless of Physics2D settings
    bool IsInside(Vector2 p)
    {
        if (_poly == null || _poly.Length < 3) return true;

        // Fast AABB reject
        if (p.x < _polyBounds.min.x || p.x > _polyBounds.max.x ||
            p.y < _polyBounds.min.y || p.y > _polyBounds.max.y)
            return false;

        bool inside = false;
        int  n      = _poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = _poly[i], b = _poly[j];
            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    void Update()
    {
        UpdateAnimation();
        UpdateMovement();
    }

    void UpdateAnimation()
    {
        if (_frames == null || _frames.Length <= 1 || _renderer == null) return;

        _animTimer += Time.deltaTime;
        if (_animTimer >= 1f / AnimFps)
        {
            _animTimer -= 1f / AnimFps;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            _renderer.sprite = _frames[_frameIndex];
        }
    }

    void UpdateMovement()
    {
        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                PickTarget();
            }
            return;
        }

        Vector3 diff = _targetPos - transform.position;
        diff.z = 0f;

        if (diff.magnitude < ArrivalThreshold)
        {
            _waiting   = true;
            _waitTimer = Random.Range(MinWaitTime, MaxWaitTime);
            return;
        }

        Vector3 next = transform.position + diff.normalized * WalkSpeed * Time.deltaTime;

        if (!IsInside(new Vector2(next.x, next.y)))
        {
            _waiting   = true;
            _waitTimer = Random.Range(0.5f, 1.5f);
            return;
        }

        transform.position = next;

        if (_renderer != null && Mathf.Abs(diff.x) > 0.05f)
            _renderer.flipX = diff.x > 0;
    }

    void PickTarget()
    {
        if (_poly == null)
        {
            _targetPos = transform.position;
            return;
        }

        for (int i = 0; i < SampleAttempts; i++)
        {
            var candidate = new Vector2(
                Random.Range(_polyBounds.min.x, _polyBounds.max.x),
                Random.Range(_polyBounds.min.y, _polyBounds.max.y));

            if (IsInside(candidate))
            {
                _targetPos = new Vector3(candidate.x, candidate.y, transform.position.z);
                return;
            }
        }

        _targetPos = new Vector3(_polyBounds.center.x, _polyBounds.center.y, transform.position.z);
    }
}
