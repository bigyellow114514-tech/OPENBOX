#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TreeChopHitVFXBuilder
{
    const string Root = "Assets/VFX_AI/TreeChopHit";
    const string TextureFolder = Root + "/Textures";
    const string MaterialFolder = Root + "/Materials";
    const string PrefabFolder = Root + "/Prefabs";
    const string SceneFolder = Root + "/Scenes";

    const string WoodTexturePath = TextureFolder + "/T_WoodChip.png";
    const string DustTexturePath = TextureFolder + "/T_BarkDust.png";
    const string LeafTexturePath = TextureFolder + "/T_LeafBit.png";
    const string SparkTexturePath = TextureFolder + "/T_TinySpark.png";

    const string WoodMaterialPath = MaterialFolder + "/M_WoodChip.mat";
    const string DustMaterialPath = MaterialFolder + "/M_BarkDust.mat";
    const string LeafMaterialPath = MaterialFolder + "/M_LeafBit.mat";
    const string SparkMaterialPath = MaterialFolder + "/M_TinySpark.mat";
    const string PreviewTrunkMaterialPath = MaterialFolder + "/M_Preview_Trunk.mat";
    const string PreviewMarkerMaterialPath = MaterialFolder + "/M_Preview_ImpactMarker.mat";

    const string PrefabPath = PrefabFolder + "/VFX_Tree_ChopHit.prefab";
    const string PreviewScenePath = SceneFolder + "/VFX_Tree_ChopHit_Preview.unity";

    [MenuItem("OpenBox/VFX AI/Rebuild Tree Chop Hit")]
    public static void Build()
    {
        EnsureFolders();

        Texture2D wood = SaveTexture(WoodTexturePath, MakeWoodChipTexture());
        Texture2D dust = SaveTexture(DustTexturePath, MakeDustTexture());
        Texture2D leaf = SaveTexture(LeafTexturePath, MakeLeafTexture());
        Texture2D spark = SaveTexture(SparkTexturePath, MakeSparkTexture());

        Material woodMat = SaveParticleMaterial(WoodMaterialPath, wood);
        Material dustMat = SaveParticleMaterial(DustMaterialPath, dust);
        Material leafMat = SaveParticleMaterial(LeafMaterialPath, leaf);
        Material sparkMat = SaveParticleMaterial(SparkMaterialPath, spark);

        GameObject prefabRoot = BuildPrefabObject(woodMat, dustMat, leafMat, sparkMat);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        BuildPreviewScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TreeChopHitVFXBuilder] Built " + PrefabPath);
    }

    static GameObject BuildPrefabObject(Material woodMat, Material dustMat, Material leafMat, Material sparkMat)
    {
        var root = new GameObject("VFX_Tree_ChopHit");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        ConfigureWoodSplinters(NewSystem(root.transform, "WoodSplinters", woodMat, 160));
        ConfigureBarkDust(NewSystem(root.transform, "BarkDust", dustMat, 150));
        ConfigureLeafBits(NewSystem(root.transform, "LeafBits", leafMat, 155));
        ConfigureTinyMagicSparks(NewSystem(root.transform, "TinyMagicSparks", sparkMat, 170));

        return root;
    }

    static ParticleSystem NewSystem(Transform parent, string name, Material material, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
        return ps;
    }

    static void ConfigureWoodSplinters(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.58f;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.32f, 0.12f, 1f), new Color(0.96f, 0.69f, 0.33f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 28;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(0f, 82f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.05f, 0.95f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.72f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-7.5f, 7.5f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 1f, 0.72f, 0.8f, 1f, 0f));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(new Color(0.94f, 0.67f, 0.3f), new Color(0.52f, 0.27f, 0.11f), 1f, 1f, 0.65f, 0f));
    }

    static void ConfigureBarkDust(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.52f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
        main.startColor = new Color(0.77f, 0.55f, 0.34f, 0.32f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 22;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 10) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.arc = 160f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.28f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.34f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.45f, 0.32f, 1.05f, 1f, 0f));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(new Color(0.82f, 0.62f, 0.42f), new Color(0.52f, 0.34f, 0.2f), 0.36f, 0.5f, 0.18f, 0f));
    }

    static void ConfigureLeafBits(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.72f;
        main.startDelay = 0.05f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.68f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.085f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.36f, 0.72f, 0.25f, 0.95f), new Color(0.72f, 0.9f, 0.32f, 0.95f));
        main.gravityModifier = 0.75f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 12;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.08f, 5) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.035f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.28f, 0.36f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.42f, 0.78f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-4.8f, 4.8f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(new Color(0.55f, 0.86f, 0.26f), new Color(0.26f, 0.56f, 0.18f), 0.95f, 1f, 0.82f, 0f));
    }

    static void ConfigureTinyMagicSparks(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.56f;
        main.startDelay = 0.04f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.014f, 0.032f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 1f, 1f, 0.46f), new Color(0.78f, 0.45f, 1f, 0.38f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 6;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.03f, 3) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.02f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.36f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.8f, 0.35f, 1f, 1f, 0f));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(Gradient(new Color(0.38f, 1f, 1f), new Color(0.85f, 0.42f, 1f), 0.0f, 0.38f, 0.16f, 0f));
    }

    static void BuildPreviewScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("PreviewCamera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 2.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.72f, 0.38f, 1f);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null)
            PrefabUtility.InstantiatePrefab(prefab);

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Quad);
        trunk.name = "Preview_Trunk_Placeholder";
        trunk.transform.position = new Vector3(0.26f, -0.08f, 0.12f);
        trunk.transform.localScale = new Vector3(0.26f, 1.25f, 1f);
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        trunkRenderer.sharedMaterial = SavePreviewMaterial(PreviewTrunkMaterialPath, new Color(0.42f, 0.22f, 0.1f, 1f));

        var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = "Impact_Origin_Marker";
        marker.transform.position = new Vector3(0f, 0f, 0.05f);
        marker.transform.localScale = new Vector3(0.035f, 0.035f, 1f);
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.sharedMaterial = SavePreviewMaterial(PreviewMarkerMaterialPath, new Color(1f, 0.95f, 0.2f, 0.65f));

        EditorSceneManager.SaveScene(scene, PreviewScenePath);
    }

    static Material SavePreviewMaterial(string path, Color color)
    {
        Shader shader = FindUnlitShader();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Texture2D MakeWoodChipTexture()
    {
        var tex = NewClearTexture(8, 8);
        SetRect(tex, 1, 3, 6, 2, new Color(0.78f, 0.48f, 0.2f, 1f));
        SetRect(tex, 2, 2, 4, 1, new Color(0.98f, 0.72f, 0.34f, 1f));
        SetRect(tex, 2, 5, 4, 1, new Color(0.42f, 0.22f, 0.08f, 1f));
        tex.Apply();
        return tex;
    }

    static Texture2D MakeDustTexture()
    {
        var tex = NewClearTexture(16, 16);
        float center = 7.5f;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - radius);
                alpha = Mathf.Round(alpha * 4f) / 4f;
                tex.SetPixel(x, y, new Color(0.78f, 0.58f, 0.38f, alpha * 0.72f));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D MakeLeafTexture()
    {
        var tex = NewClearTexture(8, 8);
        SetRect(tex, 3, 1, 2, 6, new Color(0.24f, 0.58f, 0.2f, 1f));
        SetRect(tex, 2, 3, 4, 3, new Color(0.48f, 0.82f, 0.25f, 1f));
        tex.SetPixel(1, 3, Color.clear);
        tex.SetPixel(6, 5, Color.clear);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeSparkTexture()
    {
        var tex = NewClearTexture(8, 8);
        tex.SetPixel(3, 3, new Color(0.72f, 1f, 1f, 1f));
        tex.SetPixel(4, 3, new Color(0.72f, 1f, 1f, 1f));
        tex.SetPixel(3, 4, new Color(0.86f, 0.55f, 1f, 0.85f));
        tex.SetPixel(4, 4, new Color(0.86f, 0.55f, 1f, 0.85f));
        tex.Apply();
        return tex;
    }

    static Texture2D NewClearTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, Color.clear);
        return tex;
    }

    static void SetRect(Texture2D tex, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
            for (int px = x; px < x + width; px++)
                if (px >= 0 && py >= 0 && px < tex.width && py < tex.height)
                    tex.SetPixel(px, py, color);
    }

    static Texture2D SaveTexture(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material SaveParticleMaterial(string path, Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? FindUnlitShader();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
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

    static Shader FindUnlitShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Color");
    }

    static AnimationCurve Curve(float t0, float v0, float t1, float v1, float t2, float v2)
    {
        return new AnimationCurve(new Keyframe(t0, v0), new Keyframe(t1, v1), new Keyframe(t2, v2));
    }

    static Gradient Gradient(Color colorA, Color colorB, float alpha0, float alpha1, float alpha2, float alpha3)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(colorA, 0f),
                new GradientColorKey(colorB, 0.55f),
                new GradientColorKey(colorA, 1f),
            },
            new[]
            {
                new GradientAlphaKey(alpha0, 0f),
                new GradientAlphaKey(alpha1, 0.15f),
                new GradientAlphaKey(alpha2, 0.72f),
                new GradientAlphaKey(alpha3, 1f),
            });
        return gradient;
    }

    static void EnsureFolders()
    {
        EnsureFolder(Root);
        EnsureFolder(TextureFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        if (Directory.Exists(path))
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.IsValidFolder(path)) return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
