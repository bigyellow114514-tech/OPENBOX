using System.Collections;
using UnityEngine;

public class EquipmentPanel : MonoBehaviour
{
    public static EquipmentPanel Instance { get; private set; }

    [SerializeField] GameObject panelRoot;      // 拖入场景中的 Equip 对象
    [SerializeField] Sprite[] itemSprites;
    [SerializeField] float itemLocalScale = 1.5f;

    Vector3 panelRestScale;
    bool isVisible;

    static readonly Vector2[] SlotLocalPositions =
    {
        new Vector2(2.47f, 4.5f),
        new Vector2(5.95f, 4.5f),
        new Vector2(9.52f, 4.5f),
    };

    void Awake()
    {
        Instance = this;
        panelRestScale = panelRoot.transform.localScale;
        panelRoot.transform.localScale = Vector3.zero;
    }

    public void ShowPanel()
    {
        if (isVisible) return;
        isVisible = true;
        SpawnItems();
        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

    public void HidePanel()
    {
        if (!isVisible) return;
        StopAllCoroutines();
        StartCoroutine(AnimateOut());
    }

    void Update()
    {
        if (!isVisible || !Input.GetMouseButtonDown(0)) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        foreach (Transform child in panelRoot.transform)
        {
            if (!child.name.StartsWith("Equip_x")) continue;
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null && sr.bounds.Contains(new Vector3(worldPos.x, worldPos.y, sr.bounds.center.z)))
            {
                HidePanel();
                return;
            }
        }
    }

    void SpawnItems()
    {
        Transform root = panelRoot.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("DropItem_"))
                Destroy(child.gameObject);
        }

        if (itemSprites == null || itemSprites.Length == 0) return;

        int count = Mathf.Min(Random.Range(1, 4), SlotLocalPositions.Length);
        for (int i = 0; i < count; i++)
        {
            Sprite spr = itemSprites[Random.Range(0, itemSprites.Length)];
            var item = new GameObject("DropItem_" + i);
            item.transform.SetParent(root, false);
            item.transform.localPosition = new Vector3(SlotLocalPositions[i].x, SlotLocalPositions[i].y, -0.1f);
            item.transform.localScale = Vector3.one * itemLocalScale;
            var sr = item.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = 3;
        }
    }

    IEnumerator AnimateIn()
    {
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed / duration, 1f);
            float scale = t < 0.7f
                ? Mathf.Sin(t / 0.7f * Mathf.PI * 0.5f) * 1.12f
                : Mathf.Lerp(1.12f, 1f, (t - 0.7f) / 0.3f);
            panelRoot.transform.localScale = panelRestScale * scale;
            yield return null;
        }
        panelRoot.transform.localScale = panelRestScale;
    }

    IEnumerator AnimateOut()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed / duration, 1f);
            panelRoot.transform.localScale = panelRestScale * (1f - t);
            yield return null;
        }
        panelRoot.transform.localScale = Vector3.zero;
        isVisible = false;
        TreeClick.Unlock();

        Transform root = panelRoot.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("DropItem_"))
                Destroy(child.gameObject);
        }
    }
}
