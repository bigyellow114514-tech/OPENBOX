using UnityEngine;

public static class HudLayoutUtility
{
    const float ReferenceWidth = 1080f;
    const float ReferenceHeight = 650f;

    public static float Scale
    {
        get
        {
            float widthScale = Screen.width / ReferenceWidth;
            float heightScale = Screen.height / ReferenceHeight;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.62f, 1.15f);
        }
    }

    public static Rect LogicalSafeArea
    {
        get
        {
            float scale = Scale;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
                safe = new Rect(0f, 0f, Screen.width, Screen.height);

            return new Rect(
                safe.x / scale,
                safe.y / scale,
                safe.width / scale,
                safe.height / scale
            );
        }
    }

    public static bool IsNarrow => LogicalSafeArea.width < 760f;

    public static Rect Begin()
    {
        float scale = Scale;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        return LogicalSafeArea;
    }

    public static void End()
    {
        GUI.matrix = Matrix4x4.identity;
    }
}
