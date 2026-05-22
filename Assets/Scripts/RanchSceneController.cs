using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RanchSceneController : MonoBehaviour
{
    const string NextScene  = "MainScene";
    const string FenceName  = "FenceBounds";
    const string BgName     = "Background";

    readonly List<GameObject> _petObjects = new List<GameObject>();
    Text _emptyLabel;

    void Start()
    {
        EnsureEventSystem();
        PetSystemManager.EnsureInstance();
        EnsureBackground();
        BuildUI();
        SpawnPets();

        if (PetSystemManager.Instance != null)
            PetSystemManager.Instance.OnPetsChanged += OnPetsChanged;
    }

    void OnDestroy()
    {
        if (PetSystemManager.Instance != null)
            PetSystemManager.Instance.OnPetsChanged -= OnPetsChanged;
    }

    void OnPetsChanged() => SpawnPets();

    // ── Background ────────────────────────────────────────────────────────────

    void EnsureBackground()
    {
        // If the Editor script already placed a Background object, leave it alone.
        if (GameObject.Find(BgName) != null) return;

        var bgGO = new GameObject(BgName);
        var sr = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Main_Ranch");
        bgGO.AddComponent<BackgroundFill>();
    }

    // ── Pet spawning ──────────────────────────────────────────────────────────

    void SpawnPets()
    {
        foreach (GameObject go in _petObjects)
            if (go != null) Destroy(go);
        _petObjects.Clear();

        if (PetSystemManager.Instance == null) return;

        PolygonCollider2D fence = FindFenceCollider();

        foreach (PetInstance pet in PetSystemManager.Instance.Pets)
        {
            PetConfig config = PetSystemManager.Instance.GetPetConfig(pet.PetId);
            if (config == null) continue;
            GameObject go = SpawnPetObject(config, fence);
            if (go != null) _petObjects.Add(go);
        }

        if (_emptyLabel != null)
            _emptyLabel.gameObject.SetActive(_petObjects.Count == 0);
    }

    PolygonCollider2D FindFenceCollider()
    {
        GameObject fenceGO = GameObject.Find(FenceName);
        return fenceGO != null ? fenceGO.GetComponent<PolygonCollider2D>() : null;
    }

    GameObject SpawnPetObject(PetConfig config, PolygonCollider2D fence)
    {
        var go = new GameObject("RanchPet_" + config.PetResource);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;

        Sprite[] frames = LoadIdleFrames(config.PetResource);
        if (frames == null || frames.Length == 0)
            frames = LoadAvatarFrame(config);

        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
            ScalePetToScreenHeight(go.transform, frames[0]);
        }

        // Place pet at a random point inside the fence (or screen center as fallback)
        go.transform.position = fence != null
            ? RandomPointInFence(fence)
            : Vector3.zero;

        var walker = go.AddComponent<PetWalkerBehaviour>();
        walker.Initialize(frames, fence);

        return go;
    }

    static Vector3 RandomPointInFence(PolygonCollider2D fence)
    {
        // Build world-space polygon — same math as PetWalkerBehaviour, no physics dependency
        Vector2[]  local = fence.GetPath(0);
        Transform  t     = fence.transform;
        var poly = new Vector2[local.Length];
        float xMin = float.MaxValue, yMin = float.MaxValue;
        float xMax = float.MinValue, yMax = float.MinValue;
        for (int i = 0; i < local.Length; i++)
        {
            Vector2 w = t.TransformPoint(local[i]);
            poly[i] = w;
            if (w.x < xMin) xMin = w.x;
            if (w.y < yMin) yMin = w.y;
            if (w.x > xMax) xMax = w.x;
            if (w.y > yMax) yMax = w.y;
        }

        for (int attempt = 0; attempt < 50; attempt++)
        {
            var candidate = new Vector2(
                Random.Range(xMin, xMax),
                Random.Range(yMin, yMax));
            if (PointInPolygon(poly, candidate))
                return new Vector3(candidate.x, candidate.y, 0f);
        }
        return new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
    }

    static bool PointInPolygon(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        int  n      = poly.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = poly[i], b = poly[j];
            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    void ScalePetToScreenHeight(Transform t, Sprite sprite)
    {
        Camera cam = Camera.main;
        if (cam == null || sprite == null) return;

        float targetHeight = cam.orthographicSize * 2f * 0.15f;
        float spriteHeight = sprite.bounds.size.y;
        if (spriteHeight <= 0f) return;

        float s = targetHeight / spriteHeight;
        t.localScale = new Vector3(s, s, 1f);
    }

    // ── Sprite loading ────────────────────────────────────────────────────────

    Sprite[] LoadIdleFrames(string resource)
    {
        if (string.IsNullOrEmpty(resource)) return null;

        Sprite[] all = Resources.LoadAll<Sprite>("Sprites/PetsAnim/" + resource);
        if (all == null || all.Length == 0)
            all = Resources.LoadAll<Sprite>("Sprites/Pets/" + resource);
        if (all == null || all.Length == 0) return null;

        string idlePrefix = resource + "_Idle_";
        var idle = new List<Sprite>();
        foreach (Sprite s in all)
        {
            if (s != null && s.name.StartsWith(idlePrefix, System.StringComparison.OrdinalIgnoreCase))
                idle.Add(s);
        }
        if (idle.Count == 0) idle.AddRange(all);

        idle.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return idle.ToArray();
    }

    Sprite[] LoadAvatarFrame(PetConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.PetResource)) return null;

        var digits = new System.Text.StringBuilder();
        foreach (char c in config.PetResource)
            if (char.IsDigit(c)) digits.Append(c);
        if (digits.Length == 0) return null;

        Sprite avatar = Resources.Load<Sprite>("Sprites/Avatar/Pet/Pet_Avatar_" + digits);
        return avatar != null ? new[] { avatar } : null;
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("RanchCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildArrowButton(canvasGO.transform, "►",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f),
            () => SceneTransitionManager.Instance.LoadScene(NextScene));

        BuildEmptyHint(canvasGO.transform);
    }

    void BuildEmptyHint(Transform parent)
    {
        var hintGO = new GameObject("EmptyHint");
        hintGO.transform.SetParent(parent, false);
        var rt = hintGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500f, 80f);
        _emptyLabel = hintGO.AddComponent<Text>();
        _emptyLabel.text = "背包中暂无宠物，快去抽卡吧！";
        _emptyLabel.fontSize = 28;
        _emptyLabel.alignment = TextAnchor.MiddleCenter;
        _emptyLabel.color = new Color(0.95f, 0.88f, 0.65f, 1f);
        _emptyLabel.font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "Arial" }, 28);
        hintGO.SetActive(false);
    }

    void BuildArrowButton(Transform parent, string label, Vector2 anchor, Vector2 pivot,
        Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject("ArrowButton");
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(100f, 100f);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 56f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
