using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TreeClick : MonoBehaviour
{
    [SerializeField] float squashAmount = 0.1f;

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

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || isAnimating) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (col.OverlapPoint(worldPos))
            StartCoroutine(PunchScale());
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
