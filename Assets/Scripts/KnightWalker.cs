using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class KnightWalker : MonoBehaviour
{
    public static KnightWalker Instance { get; private set; }
    public const float ChopCycleDuration = 0.35f;

    [Header("Walking")]
    public float walkSpeed = 1.5f;
    public float screenEdgeMargin = 0.08f;
    public Sprite[] walkFrames;
    public float walkFrameRate = 10f;
    public string walkFramesResourcePath = "Sprites/KnightWalk6";

    [Header("Tree Chop")]
    public Sprite[] treeChopFrames;
    public string treeChopFramesResourcePath = "Sprites/KnightChopTree";
    public Vector2 chopPositionXRange = new Vector2(1.75f, 4.15f);
    public Vector2 chopPositionYRange = new Vector2(-3.5f, -2f);
    public Vector2 chopHitVfxOffset = new Vector2(0.55f, 0.65f);

    [Header("Walk Segment")]
    public float minWalkTime = 1.0f;
    public float maxWalkTime = 4.0f;

    [Header("Idle")]
    public float minIdleTime = 0.8f;
    public float maxIdleTime = 3.0f;
    public float idleChance = 0.45f;

    [Header("Footstep Bob")]
    public float bobAmplitude  = 0.06f;
    public float bobFrequency  = 2.5f;

    [Header("Shadow")]
    public float shadowAlpha   = 0.35f;
    public float shadowScaleY  = 0.12f;
    public Vector2 shadowOffset = new Vector2(0f, -0.45f);

    enum State { Walking, Idle, Chopping }

    SpriteRenderer _sr;
    SpriteRenderer _shadow;
    Coroutine      _chopRoutine;
    Vector3        _baseScale;
    float          _baseY;
    State          _state;
    float          _stateTimer;
    float          _dir = 1f;
    float          _leftBound;
    float          _rightBound;
    float          _bobPhase;
    float          _animTimer;
    int            _animIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[KnightWalker] Multiple instances found. The latest instance will handle tree chopping.");
        }
        Instance = this;

        _sr        = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
        LoadWalkFramesIfNeeded();
        LoadTreeChopFramesIfNeeded();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        _baseY = transform.position.y;
        CalcBounds();
        CreateShadow();
        EnterWalk(Random.value < 0.5f ? 1f : -1f);
    }

    void CalcBounds()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float halfW  = cam.orthographicSize * cam.aspect;
        float margin = halfW * screenEdgeMargin;
        float cx     = cam.transform.position.x;
        _leftBound   = cx - halfW + margin;
        _rightBound  = cx + halfW - margin;
    }

    void CreateShadow()
    {
        var go = new GameObject("_shadow");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(shadowOffset.x, shadowOffset.y, 0.01f);
        go.transform.localScale    = new Vector3(1f, shadowScaleY, 1f);

        _shadow               = go.AddComponent<SpriteRenderer>();
        _shadow.sprite        = _sr.sprite;
        _shadow.color         = new Color(0f, 0f, 0f, shadowAlpha);
        _shadow.sortingOrder  = _sr.sortingOrder - 1;
    }

    void Update()
    {
        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Walking: UpdateWalking(); break;
            case State.Idle:    UpdateIdle();    break;
            case State.Chopping: break;
        }

        transform.localScale = _baseScale;
        UpdateShadow();
    }

    void UpdateWalking()
    {
        transform.Translate(_dir * walkSpeed * Time.deltaTime, 0f, 0f);

        if (_dir > 0f && transform.position.x >= _rightBound) { EnterWalk(-1f); return; }
        if (_dir < 0f && transform.position.x <= _leftBound)  { EnterWalk( 1f); return; }

        if (_stateTimer <= 0f)
        {
            if (Random.value < idleChance) EnterIdle();
            else                           EnterWalk(RandomDir());
        }

        _bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
        float bob = Mathf.Abs(Mathf.Sin(_bobPhase)) * bobAmplitude;
        var pos = transform.position;
        pos.y = _baseY + bob;
        transform.position = pos;

        AnimateWalk();
    }

    void UpdateIdle()
    {
        if (_stateTimer <= 0f) EnterWalk(RandomDir());

        var pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, _baseY, Time.deltaTime * 10f);
        transform.position = pos;

        _bobPhase = 0f;
        SetWalkFrame(0);
    }

    void EnterWalk(float dir)
    {
        _state      = State.Walking;
        _dir        = dir;
        _sr.flipX   = dir < 0f;
        _stateTimer = Random.Range(minWalkTime, maxWalkTime);
    }

    void EnterIdle()
    {
        _state      = State.Idle;
        _stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    float RandomDir() => (Random.value < 0.4f) ? -_dir : _dir;

    void LoadWalkFramesIfNeeded()
    {
        if (walkFrames != null && walkFrames.Length > 0) return;

        walkFrames = Resources.LoadAll<Sprite>(walkFramesResourcePath);
        System.Array.Sort(walkFrames, (a, b) => string.CompareOrdinal(a.name, b.name));
        SetWalkFrame(0);
    }

    void AnimateWalk()
    {
        if (walkFrames == null || walkFrames.Length == 0) return;

        _animTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, walkFrameRate);
        while (_animTimer >= frameDuration)
        {
            _animTimer -= frameDuration;
            _animIndex = (_animIndex + 1) % walkFrames.Length;
            SetWalkFrame(_animIndex);
        }
    }

    void SetWalkFrame(int index)
    {
        if (walkFrames == null || walkFrames.Length == 0) return;

        _animIndex = Mathf.Clamp(index, 0, walkFrames.Length - 1);
        _sr.sprite = walkFrames[_animIndex];
    }

    void LoadTreeChopFramesIfNeeded()
    {
        if (treeChopFrames != null && treeChopFrames.Length > 0) return;

        treeChopFrames = Resources.LoadAll<Sprite>(treeChopFramesResourcePath);
        System.Array.Sort(treeChopFrames, (a, b) => string.CompareOrdinal(a.name, b.name));
    }

    public int GetTreeChopCycleCount(int rarityIndex)
    {
        return 2 + Mathf.Max(0, rarityIndex);
    }

    public Vector3 GetTreeChopHitVfxPosition()
    {
        float facingSign = _sr != null && _sr.flipX ? -1f : 1f;
        Vector3 offset = new Vector3(chopHitVfxOffset.x * facingSign, chopHitVfxOffset.y, 0f);
        return transform.position + offset;
    }

    public Coroutine PlayTreeChop(int rarityIndex)
    {
        if (_chopRoutine != null) StopCoroutine(_chopRoutine);
        _chopRoutine = StartCoroutine(PlayTreeChopSequence(rarityIndex));
        return _chopRoutine;
    }

    public System.Collections.IEnumerator PlayTreeChopSequence(int rarityIndex)
    {
        LoadTreeChopFramesIfNeeded();
        if (treeChopFrames == null || treeChopFrames.Length == 0)
        {
            Debug.LogWarning("[KnightWalker] Tree chop frames are missing.");
            yield break;
        }

        yield return TreeChopRoutine(Mathf.Max(0, rarityIndex));
    }

    System.Collections.IEnumerator TreeChopRoutine(int rarityIndex)
    {
        _state = State.Chopping;
        _bobPhase = 0f;
        _animTimer = 0f;

        float x = Random.Range(chopPositionXRange.x, chopPositionXRange.y);
        float y = Random.Range(chopPositionYRange.x, chopPositionYRange.y);
        transform.position = new Vector3(x, y, transform.position.z);

        float centerX = (chopPositionXRange.x + chopPositionXRange.y) * 0.5f;
        _sr.flipX = x >= centerX;

        int cycles = GetTreeChopCycleCount(rarityIndex);
        float frameDuration = Mathf.Max(0.01f, ChopCycleDuration) / treeChopFrames.Length;

        for (int cycle = 0; cycle < cycles; cycle++)
        {
            for (int frameIndex = 0; frameIndex < treeChopFrames.Length; frameIndex++)
            {
                _sr.sprite = treeChopFrames[frameIndex];
                UpdateShadow();
                yield return new WaitForSeconds(frameDuration);
            }
        }

        _chopRoutine = null;
        var pos = transform.position;
        pos.y = _baseY;
        transform.position = pos;
        transform.localScale = _baseScale;
        SetWalkFrame(0);
        EnterWalk(_sr.flipX ? -1f : 1f);
    }

    void UpdateShadow()
    {
        if (_shadow == null) return;

        _shadow.flipX = _sr.flipX;
        _shadow.sprite = _sr.sprite;

        float rise        = (transform.position.y - _baseY) / (bobAmplitude + 0.001f);
        float scaleX      = Mathf.Lerp(1f, 0.75f, rise);
        _shadow.transform.localScale = new Vector3(scaleX, shadowScaleY, 1f);
    }
}
