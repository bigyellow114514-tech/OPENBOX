using UnityEngine;

public class ExpBarUI : MonoBehaviour
{
    Texture2D _bgTex;
    Texture2D _borderTex;
    Texture2D _fillTex;
    GUIStyle _labelStyle;
    GUIStyle _levelStyle;

    void Start()
    {
        _bgTex     = MakeTex(new Color(0.10f, 0.10f, 0.10f, 0.85f));
        _borderTex = MakeTex(new Color(0.00f, 0.00f, 0.00f, 1.00f));
        _fillTex   = MakeTex(new Color(0.20f, 0.75f, 0.25f, 1.00f));
    }

    void InitStyles()
    {
        if (_labelStyle != null) return;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };

        _levelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize   = 14,
            fontStyle  = FontStyle.Bold,
            alignment  = TextAnchor.MiddleRight,
            normal     = { textColor = Color.white }
        };
    }

    void OnGUI()
    {
        var mgr = PlayerExpManager.Instance;
        if (mgr == null) return;

        InitStyles();

        float sw    = Screen.width;
        float barW  = sw * 0.45f;
        float barH  = 22f;
        float barX  = (sw - barW) * 0.5f;
        float barY  = 10f;
        float pad   = 2f;
        float labelW = 60f;
        float gap    = 6f;

        // 等级文字（进度条左侧）
        string levelText = mgr.Level >= 100 ? "满级" : $"等级 {mgr.Level}";
        GUI.Label(new Rect(barX - labelW - gap, barY, labelW, barH), levelText, _levelStyle);

        // Border
        GUI.DrawTexture(new Rect(barX - pad, barY - pad, barW + pad * 2, barH + pad * 2), _borderTex);
        // Background
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), _bgTex);

        // Fill
        float ratio = mgr.Level >= 100 ? 1f : mgr.CurrentExp / mgr.ExpToNextLevel;
        if (ratio > 0f)
            GUI.DrawTexture(new Rect(barX, barY, barW * ratio, barH), _fillTex);

        // 进度条内文字
        string text = mgr.Level >= 100
            ? "MAX LEVEL"
            : $"{mgr.CurrentExp:0} / {mgr.ExpToNextLevel:0} EXP";
        GUI.Label(new Rect(barX, barY, barW, barH), text, _labelStyle);
    }

    Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
