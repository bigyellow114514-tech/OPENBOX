using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TreeClick : MonoBehaviour
{
    [SerializeField] float squashAmount = 0.1f;
    [SerializeField] Sprite[] boxSprites;
    [SerializeField] float groundYOffset = 0f;
    [SerializeField] float boxSizeMultiplier = 0.12f;

    static readonly (float time, float sx, float sy)[] Keyframes =
    {
        (0.00f, 1.00f, 1.00f),
        (0.08f, 1.10f, 0.90f),
        (0.20f, 0.97f, 1.03f),
        (0.32f, 1.00f, 1.00f),
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

    public static void Unlock() => Locked = false;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || isAnimating) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (col.OverlapPoint(worldPos))
        {
            StartCoroutine(PunchScale());
            SpawnBox();
        }
    }

    void SpawnBox()
    {
        if (boxSprites == null || boxSprites.Length == 0) return;

        Sprite sprite = boxSprites[Random.Range(0, boxSprites.Length)];
        Bounds bounds = col.bounds;

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

        go.AddComponent<BoxDropHelper>().StartDrop(groundY);
    }

    IEnumerator PunchScale()
    {
        isAnimating = true;
        float elapsed = 0f;
        float total = Keyframes[Keyframes.Length - 1].time;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            (float sx, float sy) = Evaluate(Mathf.Min(elapsed, total));
            transform.localScale = new Vector3(restScale.x * sx, restScale.y * sy, 1f);
            yield return null;
        }

        transform.localScale = restScale;
        isAnimating = false;
    }

    (float sx, float sy) Evaluate(float time)
    {
        for (int i = 1; i < Keyframes.Length; i++)
        {
            if (time <= Keyframes[i].time)
            {
                float t = (time - Keyframes[i - 1].time) / (Keyframes[i].time - Keyframes[i - 1].time);
                t = Mathf.SmoothStep(0f, 1f, t);
                return (
                    Mathf.Lerp(Keyframes[i - 1].sx, Keyframes[i].sx, t),
                    Mathf.Lerp(Keyframes[i - 1].sy, Keyframes[i].sy, t)
                );
            }
        }
        return (1f, 1f);
    }
}
