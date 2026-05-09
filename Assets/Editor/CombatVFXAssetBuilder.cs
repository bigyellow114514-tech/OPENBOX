using System.IO;
using UnityEditor;
using UnityEngine;

public static class CombatVFXAssetBuilder
{
    const string VfxFolder = "Assets/Resources/VFX";
    const string PrefabPath = VfxFolder + "/PetHeal.prefab";
    const string StunPrefabPath = VfxFolder + "/StunBuff.prefab";
    const string SoftDotTexturePath = VfxFolder + "/PetHeal_SoftDot.png";
    const string CrossTexturePath = VfxFolder + "/PetHeal_Cross.png";
    const string StunStarTexturePath = VfxFolder + "/StunBuff_Star.png";
    const string SoftDotMaterialPath = VfxFolder + "/PetHeal_SoftDot.mat";
    const string CrossMaterialPath = VfxFolder + "/PetHeal_Cross.mat";
    const string StunStarMaterialPath = VfxFolder + "/StunBuff_Star.mat";

    [MenuItem("OpenBox/VFX/Rebuild Pet Heal Prefab")]
    public static void RebuildPetHealPrefab()
    {
        EnsureFolder(VfxFolder);

        Texture2D softDot = SaveTexture(SoftDotTexturePath, MakeSoftDot(64));
        Texture2D cross = SaveTexture(CrossTexturePath, MakeCross(64));
        Material softDotMat = SaveMaterial(SoftDotMaterialPath, softDot);
        Material crossMat = SaveMaterial(CrossMaterialPath, cross);

        var root = new GameObject("PetHeal");
        root.AddComponent<CombatHealParticleVFX>();

        ConfigureGlow(NewSystem(root.transform, "Glow", softDotMat));
        ConfigureRisingSparks(NewSystem(root.transform, "RisingSparks", softDotMat));
        ConfigureCrosses(NewSystem(root.transform, "HealingCrosses", crossMat));
        ConfigureOrbit(NewSystem(root.transform, "OrbitSparks", softDotMat));

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("[CombatVFXAssetBuilder] Rebuilt " + PrefabPath);
    }

    [MenuItem("OpenBox/VFX/Rebuild Stun Buff Prefab")]
    public static void RebuildStunBuffPrefab()
    {
        EnsureFolder(VfxFolder);

        Texture2D star = SaveTexture(StunStarTexturePath, MakeStar(64));
        Texture2D softDot = SaveTexture(SoftDotTexturePath, MakeSoftDot(64));
        Material starMat = SaveMaterial(StunStarMaterialPath, star);
        Material softDotMat = SaveMaterial(SoftDotMaterialPath, softDot);

        var root = new GameObject("StunBuff");
        AddRuntimeComponent(root, "CombatStunParticleVFX");

        ConfigureStunStars(NewSystem(root.transform, "OrbitStars", starMat));
        ConfigureStunPulse(NewSystem(root.transform, "Pulse", softDotMat));

        PrefabUtility.SaveAsPrefabAsset(root, StunPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("[CombatVFXAssetBuilder] Rebuilt " + StunPrefabPath);
    }

    static void AddRuntimeComponent(GameObject go, string typeName)
    {
        System.Type type = System.Type.GetType(typeName + ", Assembly-CSharp");
        if (type != null)
            go.AddComponent(type);
        else
            Debug.LogWarning("[CombatVFXAssetBuilder] Runtime component not found: " + typeName);
    }

    static ParticleSystem NewSystem(Transform parent, string name, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 300;
        renderer.sharedMaterial = material;
        return ps;
    }

    static void ConfigureGlow(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.65f;
        main.startLifetime = 0.58f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.35f, 1.65f);
        main.startColor = new Color(0.45f, 1f, 0.55f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 8;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0.2f, 1f, 0.86f, 1.15f, 1f, 0f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(
            new Color(0.62f, 1f, 0.62f), new Color(0.78f, 1f, 0.5f),
            0f, 0.34f, 0.2f, 0f));
    }

    static void ConfigureRisingSparks(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.72f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.72f, 0.94f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.64f, 1f, 0.42f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 32;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.44f;
        shape.radiusThickness = 1f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.72f, 1.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 1.6f;
        noise.scrollSpeed = 0.4f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(
            new Color(0.72f, 1f, 0.54f), new Color(0.95f, 1f, 0.62f),
            0f, 1f, 0.65f, 0f));
    }

    static void ConfigureCrosses(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.74f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.68f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-18f * Mathf.Deg2Rad, 18f * Mathf.Deg2Rad);
        main.startColor = new Color(0.92f, 1f, 0.55f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 16;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.06f, 7) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.38f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 0.88f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(
            new Color(0.84f, 1f, 0.54f), new Color(0.98f, 1f, 0.75f),
            0f, 1f, 0.48f, 0f));
    }

    static void ConfigureOrbit(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.65f;
        main.startLifetime = 0.48f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.15f);
        main.startColor = new Color(0.9f, 1f, 0.62f, 0.88f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 16;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.52f;
        shape.radiusThickness = 0.04f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(2.8f, 4.6f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(
            new Color(0.95f, 1f, 0.64f), new Color(0.62f, 1f, 0.52f),
            0f, 0.9f, 0.55f, 0f));
    }

    static void ConfigureStunStars(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.duration = 0.9f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.15f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.17f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);
        main.startColor = new Color(1f, 0.88f, 0.16f, 0.95f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 12;

        var emission = ps.emission;
        emission.rateOverTime = 8f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.34f;
        shape.radiusThickness = 0.06f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(3.8f, 5.4f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.15f, 0.18f, 1f, 1f, 0.2f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(
            new Color(1f, 0.82f, 0.08f), new Color(1f, 1f, 0.34f),
            0f, 1f, 0.88f, 0f));
    }

    static void ConfigureStunPulse(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.duration = 0.8f;
        main.startLifetime = 0.42f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.58f, 0.74f);
        main.startColor = new Color(1f, 0.82f, 0.1f, 0.18f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 4;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1), new ParticleSystem.Burst(0.4f, 1) });

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.35f, 0.5f, 1f, 1f, 0f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(
            new Color(1f, 0.72f, 0.08f), new Color(1f, 1f, 0.28f),
            0f, 0.22f, 0.1f, 0f));
    }

    static AnimationCurve Curve(float t0, float v0, float t1, float v1, float t2, float v2)
    {
        return new AnimationCurve(new Keyframe(t0, v0), new Keyframe(t1, v1), new Keyframe(t2, v2));
    }

    static Gradient Gradient(Color a, Color b, float alpha0, float alpha1, float alpha2, float alpha3)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(a, 0f),
                new GradientColorKey(b, 0.55f),
                new GradientColorKey(a, 1f),
            },
            new[]
            {
                new GradientAlphaKey(alpha0, 0f),
                new GradientAlphaKey(alpha1, 0.14f),
                new GradientAlphaKey(alpha2, 0.72f),
                new GradientAlphaKey(alpha3, 1f),
            });
        return gradient;
    }

    static Texture2D MakeSoftDot(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 1.4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D MakeCross(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        float arm = size * 0.11f;
        float length = size * 0.34f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                bool inside = (dx <= arm && dy <= length) || (dy <= arm && dx <= length);
                float alpha = inside ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D MakeStar(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        const int points = 5;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float angle = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float starRadius = Mathf.Lerp(0.32f, 0.78f, Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * points)), 0.7f));
                float alpha = Mathf.Clamp01((starRadius - radius) * 18f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    static Texture2D SaveTexture(string path, Texture2D tex)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material SaveMaterial(string path, Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Sprites/Default");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.mainTexture = texture;
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
