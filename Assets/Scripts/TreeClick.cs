using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TreeClick : MonoBehaviour
{
    [SerializeField] float squashAmount = 0.1f;
    [SerializeField] Sprite[] boxSprites;
    [SerializeField] float groundYOffset = 0f;
    [SerializeField] float boxSizeMultiplier = 0.12f;
    [SerializeField] GameObject chopHitVfxPrefab;
    [SerializeField] float chopHitVfxScale = 50f;
    [SerializeField] int chopHitVfxSortingOrder = 1000;
    [SerializeField] float chopHitVfxZOffset = -0.5f;

    static readonly (float normalizedTime, float sx, float sy)[] Keyframes =
    {
        (0.00f, 1.00f, 1.00f),
        (0.25f, 1.10f, 0.90f),
        (0.63f, 0.97f, 1.03f),
        (1.00f, 1.00f, 1.00f),
    };

    Collider2D col;
    Vector3 restScale;
    bool isAnimating;

    void Start()
    {
        col = GetComponent<Collider2D>();
        restScale = transform.localScale;
    }

    public static bool Locked { get; private set; }

    public static void Lock()   => Locked = true;
    public static void Unlock() => Locked = false;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || isAnimating || Locked) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (col.OverlapPoint(worldPos))
        {
            if (PlayerStaminaManager.Instance != null && !PlayerStaminaManager.Instance.TryConsume())
                return;

            SFXManager.PlayKanshu();
            StartCoroutine(ChopThenDrop());
        }
    }

    static readonly int[] _weightBuf = new int[6];

    IEnumerator ChopThenDrop()
    {
        isAnimating = true;

        int rarityIndex = PickBoxRarity();
        int chopCycles = KnightWalker.Instance != null
            ? KnightWalker.Instance.GetTreeChopCycleCount(rarityIndex)
            : 2 + Mathf.Max(0, rarityIndex);

        Coroutine knightChop = KnightWalker.Instance != null
            ? StartCoroutine(KnightWalker.Instance.PlayTreeChopSequence(rarityIndex))
            : null;

        yield return StartCoroutine(PlayTreeShakeSequence(chopCycles));

        if (knightChop != null)
            yield return knightChop;

        transform.localScale = restScale;
        isAnimating = false;

        SpawnBox(rarityIndex);
        AwardItemDrops();
        PlayerExpManager.Instance?.AddExp(10f);
    }

    int PickBoxRarity()
    {
        if (boxSprites == null || boxSprites.Length == 0) return 0;

        int treeLevel = TreeExpManager.Instance != null ? TreeExpManager.Instance.UpgradeLevel : 1;
        TreeExpManager.GetBoxWeights(treeLevel, _weightBuf);

        int count       = Mathf.Min(boxSprites.Length, 6);
        return PickWeighted(_weightBuf, count);
    }

    void SpawnBox(int rarityIndex)
    {
        if (boxSprites == null || boxSprites.Length == 0) return;

        rarityIndex = Mathf.Clamp(rarityIndex, 0, Mathf.Min(boxSprites.Length, 6) - 1);
        Sprite sprite   = boxSprites[rarityIndex];
        Bounds bounds   = col.bounds;

        float offsetX = Random.Range(-bounds.size.x * 0.25f, bounds.size.x * 0.25f);
        float spawnY = bounds.max.y - bounds.size.y * 0.2f;
        float boxSize = bounds.size.x * boxSizeMultiplier;
        float groundY = bounds.min.y + boxSize * 0.5f + groundYOffset;

        var go = new GameObject("BoxDrop");
        go.transform.position = new Vector3(transform.position.x + offsetX, spawnY, transform.position.z - 0.1f);
        go.transform.localScale = Vector3.one * boxSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        TreeClick.Lock();
        go.AddComponent<BoxDropHelper>().StartDrop(groundY, rarityIndex);
    }

    void AwardItemDrops()
    {
        int treeLevel = TreeExpManager.Instance != null ? TreeExpManager.Instance.UpgradeLevel : 1;
        var drops = TreeExpManager.RollItemDrops(treeLevel);
        if (drops.Count == 0) return;

        PlayerResourceManager resources = EnsureResourceManager();
        for (int i = 0; i < drops.Count; i++)
        {
            TreeItemDrop drop = drops[i];
            if (resources.AddItem(drop.ItemId, drop.Count))
                Debug.Log($"[TreeClick] Drop item id={drop.ItemId}, count={drop.Count}");
        }
    }

    static PlayerResourceManager EnsureResourceManager()
    {
        if (PlayerResourceManager.Instance != null)
            return PlayerResourceManager.Instance;

        var existing = FindObjectOfType<PlayerResourceManager>();
        if (existing != null)
            return existing;

        var go = new GameObject("PlayerResourceManager");
        return go.AddComponent<PlayerResourceManager>();
    }

    IEnumerator PlayTreeShakeSequence(int cycles)
    {
        float cycleDuration = Mathf.Max(0.01f, KnightWalker.ChopCycleDuration);
        float treeShakeDuration = cycleDuration * 0.5f;
        float startedAt = Time.time;
        Coroutine lastShake = null;

        for (int i = 0; i < cycles; i++)
        {
            float shakeStartTime = startedAt + (i + 0.5f) * cycleDuration;
            float wait = shakeStartTime - Time.time;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);

            PlayChopHitVfx();
            lastShake = StartCoroutine(PunchScale());
        }

        float sequenceEndTime = startedAt + cycles * cycleDuration;
        float remaining = sequenceEndTime - Time.time;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (lastShake != null)
            yield return lastShake;
    }

    void PlayChopHitVfx()
    {
        if (chopHitVfxPrefab == null || KnightWalker.Instance == null) return;

        Vector3 position = KnightWalker.Instance.GetTreeChopHitVfxPosition();
        position.z += chopHitVfxZOffset;
        GameObject instance = Instantiate(chopHitVfxPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * chopHitVfxScale;
        ConfigureChopHitVfxInstance(instance);
        Destroy(instance, 1.2f);
    }

    void ConfigureChopHitVfxInstance(GameObject instance)
    {
        if (instance == null) return;

        ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = chopHitVfxSortingOrder;

        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystems[i].Play(true);
        }
    }

    IEnumerator PunchScale()
    {
        float elapsed = 0f;
        float total = Mathf.Max(0.01f, KnightWalker.ChopCycleDuration * 0.5f);

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            (float sx, float sy) = Evaluate(Mathf.Clamp01(elapsed / total));
            transform.localScale = new Vector3(restScale.x * sx, restScale.y * sy, 1f);
            yield return null;
        }

        transform.localScale = restScale;
    }

    (float sx, float sy) Evaluate(float normalizedTime)
    {
        for (int i = 1; i < Keyframes.Length; i++)
        {
            if (normalizedTime <= Keyframes[i].normalizedTime)
            {
                float t = (normalizedTime - Keyframes[i - 1].normalizedTime) / (Keyframes[i].normalizedTime - Keyframes[i - 1].normalizedTime);
                t = Mathf.SmoothStep(0f, 1f, t);
                return (
                    Mathf.Lerp(Keyframes[i - 1].sx, Keyframes[i].sx, t),
                    Mathf.Lerp(Keyframes[i - 1].sy, Keyframes[i].sy, t)
                );
            }
        }
        return (1f, 1f);
    }

    // Weighted random: returns index in [0, count) using first `count` values of weights[]
    static int PickWeighted(int[] weights, int count)
    {
        int total = 0;
        for (int i = 0; i < count; i++) total += weights[i];
        if (total <= 0) return 0;

        int roll = Random.Range(0, total);
        for (int i = 0; i < count; i++)
        {
            if (roll < weights[i]) return i;
            roll -= weights[i];
        }
        return count - 1;
    }
}
