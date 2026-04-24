using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFill : MonoBehaviour
{
    void Awake()
    {
        Fit();
    }

    void Fit()
    {
        var sr = GetComponent<SpriteRenderer>();
        var cam = Camera.main;
        if (sr == null || sr.sprite == null || cam == null) return;

        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        float scaleX = camW / spriteSize.x;
        float scaleY = camH / spriteSize.y;
        float scale = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 1f);
    }
}
