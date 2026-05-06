using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainbowEquipmentVFX : MonoBehaviour
{
    // 由外部在 Start() 之前赋值，等于图标在世界坐标下的宽度
    public float sizeScale = 1f;

    ParticleSystem ps;
    ParticleSystem.MainModule main;
    ParticleSystem.EmissionModule emission;
    ParticleSystem.ColorOverLifetimeModule colorOverLifetime;
    ParticleSystem.VelocityOverLifetimeModule velocity;
    ParticleSystem.SizeOverLifetimeModule sizeOverTime;
    ParticleSystem.ShapeModule shape;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        // --- Renderer: 软圆形粒子，避免默认白色方块 ---
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = MakeSoftCircle(64);
        rend.material = mat;

        // --- Main ---
        main = ps.main;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * sizeScale, 2f * sizeScale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f * sizeScale, 0.15f * sizeScale);
        main.gravityModifier = -0.15f;  // 向上飘
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;

        // --- Emission ---
        emission = ps.emission;
        emission.rateOverTime = 40f;

        // --- Shape: 从图标底部边缘发射 ---
        shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(sizeScale, 0.05f * sizeScale, sizeScale);

        // --- 彩虹色渐变 ---
        colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.red,     0.00f),
                new GradientColorKey(new Color(1f,0.5f,0f), 0.17f),
                new GradientColorKey(Color.yellow,  0.33f),
                new GradientColorKey(Color.green,   0.50f),
                new GradientColorKey(Color.cyan,    0.67f),
                new GradientColorKey(Color.blue,    0.83f),
                new GradientColorKey(Color.magenta, 1.00f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,   0.0f),
                new GradientAlphaKey(1f,   0.1f),
                new GradientAlphaKey(1f,   0.7f),
                new GradientAlphaKey(0f,   1.0f),
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // --- 大小随生命周期缩小 ---
        sizeOverTime = ps.sizeOverLifetime;
        sizeOverTime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.2f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverTime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // --- 速度：轻微向外扩散 ---
        velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f * sizeScale,  0.3f * sizeScale);
        velocity.y = new ParticleSystem.MinMaxCurve( 0.2f * sizeScale,  1.0f * sizeScale);
        velocity.space = ParticleSystemSimulationSpace.Local;

        ps.Play();
    }

    static Texture2D MakeSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        return tex;
    }
}