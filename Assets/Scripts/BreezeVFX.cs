using UnityEngine;

public class BreezeVFX : MonoBehaviour
{
    public static BreezeVFX Instance { get; private set; }

    // ── 触发间隔 ──────────────────────────────────────────────────────────────
    [Header("Trigger")]
    public float minInterval    = 3f;
    public float maxInterval    = 8f;
    public float firstDelay     = 0.3f;

    // ── 发射器形状（手动在 Inspector 调整 Transform 位置） ─────────────────────
    [Header("Emitter Shape")]
    public float shapeWidth     = 0.4f;   // 发射器宽度（世界单位）
    public float shapeHeight    = 12f;    // 发射器高度（世界单位）

    // ── 粒子生命与数量 ────────────────────────────────────────────────────────
    [Header("Emission")]
    public float burstMin       = 15f;
    public float burstMax       = 25f;
    public float rateOverTime   = 12f;
    public float duration       = 8f;
    public int   maxParticles   = 200;
    public float lifetimeMin    = 4f;
    public float lifetimeMax    = 8f;

    // ── 速度 ──────────────────────────────────────────────────────────────────
    [Header("Velocity (world units/s)")]
    public float speedXMin      = 2f;
    public float speedXMax      = 6f;
    public float speedYMin      = -0.4f;
    public float speedYMax      = 0.8f;

    // ── 大小 ──────────────────────────────────────────────────────────────────
    [Header("Size (world units)")]
    public float sizeMin        = 0.18f;
    public float sizeMax        = 0.45f;

    // ── 噪声（漂移感） ────────────────────────────────────────────────────────
    [Header("Noise")]
    public float noiseStrength  = 0.25f;
    public float noiseFrequency = 0.4f;
    public float noiseScroll    = 0.3f;

    // ── 重力与旋转 ────────────────────────────────────────────────────────────
    [Header("Gravity & Rotation")]
    public float gravityModifier    = 0.02f;
    [Tooltip("叶片自转速度范围，单位 deg/s")]
    public float rotationSpeedMax   = 120f;

    // ── 渲染排序 ──────────────────────────────────────────────────────────────
    [Header("Rendering")]
    public int sortingOrder = 100;

    // ── 内部状态 ──────────────────────────────────────────────────────────────
    ParticleSystem         _ps;
    ParticleSystemRenderer _rend;
    float                  _nextTriggerTime;

    public static void Trigger() => Instance?.PlayBreeze();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _ps   = gameObject.AddComponent<ParticleSystem>();
        _rend = GetComponent<ParticleSystemRenderer>();       
        _ps.Stop();
        ApplyConfig();
        _ps.Play();
        _nextTriggerTime = Time.time + firstDelay;
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.F2))
        //     PlayBreeze();

        // if (Time.time >= _nextTriggerTime)
        // {
        //     PlayBreeze();
        //     _nextTriggerTime = Time.time + Random.Range(minInterval, maxInterval);
        // }
    }

    public void PlayBreeze()
    {       
        //_ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        //  ApplyConfig();
        // _ps.Play();
    }

    // 将 Inspector 上的所有参数应用到粒子系统
    void ApplyConfig()
    {
        // 渲染
        _rend.renderMode     = ParticleSystemRenderMode.Billboard;
        _rend.sortingLayerID = 0;
        _rend.sortingOrder   = sortingOrder;
        if (_rend.sharedMaterial == null)
            _rend.material = BuildMaterial();

        // Main
        var main = _ps.main;
        main.loop            = true;
        main.duration        = duration;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed      = 0f;
        main.startSize       = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = gravityModifier;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = maxParticles;

        // Emission
        var emission = _ps.emission;
        emission.rateOverTime = rateOverTime;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)burstMin, (short)burstMax)
        });

        // Shape
        var shape = _ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale     = new Vector3(shapeWidth, shapeHeight, 0.01f);

        // Velocity
        var vel = _ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x       = new ParticleSystem.MinMaxCurve(speedXMin, speedXMax);
        vel.y       = new ParticleSystem.MinMaxCurve(speedYMin, speedYMax);
        vel.z       = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.space   = ParticleSystemSimulationSpace.World;

        // Noise
        var noise = _ps.noise;
        noise.enabled     = true;
        noise.strength    = noiseStrength;
        noise.frequency   = noiseFrequency;
        noise.scrollSpeed = noiseScroll;
        noise.quality     = ParticleSystemNoiseQuality.Medium;

        // Rotation over lifetime
        var rot = _ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z       = new ParticleSystem.MinMaxCurve(
                          -rotationSpeedMax * Mathf.Deg2Rad,
                           rotationSpeedMax * Mathf.Deg2Rad);

        // Color over lifetime（固定渐变，不暴露）
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.82f, 0.94f, 0.58f), 0.00f),
                new GradientColorKey(new Color(0.96f, 0.90f, 0.52f), 0.35f),
                new GradientColorKey(new Color(0.88f, 0.72f, 0.38f), 0.70f),
                new GradientColorKey(new Color(0.80f, 0.60f, 0.30f), 1.00f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f,   0.00f),
                new GradientAlphaKey(1f,   0.08f),
                new GradientAlphaKey(0.8f, 0.80f),
                new GradientAlphaKey(0f,   1.00f),
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over lifetime
        var sot = _ps.sizeOverLifetime;
        sot.enabled = true;
        var curve = new AnimationCurve();
        curve.AddKey(0f,    0.2f);
        curve.AddKey(0.1f,  1.0f);
        curve.AddKey(0.85f, 0.9f);
        curve.AddKey(1f,    0.0f);
        sot.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    Material BuildMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                     ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.mainTexture = MakePetal(48);
        return mat;
    }

    static Texture2D MakePetal(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - cx) / (cx * 0.65f);
                float ny = (y - cy) / cy;
                float d  = Mathf.Sqrt(nx * nx + ny * ny);
                float a  = Mathf.Clamp01(1.1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return tex;
    }
}
