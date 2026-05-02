using UnityEngine;

public class ExpBarUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] float topOffset = 10f;
    [SerializeField] float barWidthRatio = 0.16f;
    [SerializeField] float minBarWidth = 180f;
    [SerializeField] float maxBarWidth = 230f;
    [SerializeField] float barHeight = 22f;
    [SerializeField] float labelWidth = 55f;
    [SerializeField] float barGap = 24f;

    [Header("Skins")]
    [SerializeField] Texture2D barBackgroundTexture;
    [SerializeField] Texture2D barBorderTexture;
    [SerializeField] Texture2D playerFillTexture;
    [SerializeField] Texture2D treeFillTexture;

    Texture2D _bgTex;
    Texture2D _borderTex;
    Texture2D _fillTex;
    Texture2D _treeFillTex;
    GUIStyle  _labelStyle;
    GUIStyle  _levelStyle;
    static Font _uiFont;

    void Start()
    {
        RefreshTextures();
    }

    void OnValidate()
    {
        minBarWidth = Mathf.Max(1f, minBarWidth);
        maxBarWidth = Mathf.Max(minBarWidth, maxBarWidth);
        barHeight = Mathf.Max(1f, barHeight);
        labelWidth = Mathf.Max(0f, labelWidth);
        barGap = Mathf.Max(0f, barGap);

        if (Application.isPlaying)
            RefreshTextures();
    }

    void RefreshTextures()
    {
        _bgTex       = barBackgroundTexture != null ? barBackgroundTexture : MakeTex(new Color(0.10f, 0.10f, 0.10f, 0.85f));
        _borderTex   = barBorderTexture != null ? barBorderTexture : MakeTex(new Color(0.00f, 0.00f, 0.00f, 1.00f));
        _fillTex     = playerFillTexture != null ? playerFillTexture : MakeTex(new Color(0.20f, 0.75f, 0.25f, 1.00f));
        _treeFillTex = treeFillTexture != null ? treeFillTexture : MakeTex(new Color(0.55f, 0.35f, 0.10f, 1.00f));
    }

    void InitStyles()
    {
        if (_labelStyle != null) return;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };

        _levelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal    = { textColor = Color.white }
        };

        ApplyFont(_labelStyle);
        ApplyFont(_levelStyle);
    }

    void OnGUI()
    {
        var pMgr = PlayerExpManager.Instance;
        if (pMgr == null) return;

        InitStyles();

        float sw     = Screen.width;
        float barW   = Mathf.Clamp(sw * barWidthRatio, minBarWidth, maxBarWidth);
        float barH   = barHeight;
        float pad    = 2f;
        float labelW = labelWidth;
        float gap    = barGap;
        float barY   = topOffset;

        float totalW = labelW + barW + gap + labelW + barW;
        float startX = (sw - totalW) * 0.5f;

        DrawExpBar(
            x: startX, y: barY,
            labelW: labelW, barW: barW, barH: barH, pad: pad,
            levelText: pMgr.Level >= 100 ? "满级" : $"Lv.{pMgr.Level}",
            ratio:     pMgr.Level >= 100 ? 1f : pMgr.CurrentExp / pMgr.ExpToNextLevel,
            barText:   pMgr.Level >= 100 ? "MAX" : $"{pMgr.CurrentExp:0}/{pMgr.ExpToNextLevel:0}",
            fillTex:   _fillTex
        );

        float tStartX = startX + labelW + barW + gap;
        var   tMgr    = TreeExpManager.Instance;

        if (tMgr != null)
        {
            DrawExpBar(
                x: tStartX, y: barY,
                labelW: labelW, barW: barW, barH: barH, pad: pad,
                levelText: tMgr.Level >= 36 ? "树满级" : $"树Lv.{tMgr.Level}",
                ratio:     tMgr.Level >= 36 ? 1f : tMgr.CurrentExp / tMgr.ExpToNextLevel,
                barText:   tMgr.Level >= 36 ? "MAX" : $"{tMgr.CurrentExp:0}/{tMgr.ExpToNextLevel:0}",
                fillTex:   _treeFillTex
            );
        }
        else
        {
            DrawExpBar(
                x: tStartX, y: barY,
                labelW: labelW, barW: barW, barH: barH, pad: pad,
                levelText: "树Lv.1",
                ratio:     0f,
                barText:   "0/1000",
                fillTex:   _treeFillTex
            );
        }
    }

    void DrawExpBar(float x, float y,
                    float labelW, float barW, float barH, float pad,
                    string levelText, float ratio, string barText,
                    Texture2D fillTex)
    {
        float barX = x + labelW;

        GUI.Label(new Rect(x, y, labelW, barH), levelText, _levelStyle);

        GUI.DrawTexture(new Rect(barX - pad, y - pad, barW + pad * 2, barH + pad * 2), _borderTex);
        GUI.DrawTexture(new Rect(barX, y, barW, barH), _bgTex);

        if (ratio > 0f)
            GUI.DrawTexture(new Rect(barX, y, barW * ratio, barH), fillTex);

        GUI.Label(new Rect(barX, y, barW, barH), barText, _labelStyle);
    }

    Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    static void ApplyFont(GUIStyle style)
    {
        if (style == null) return;

        Font font = EnsureUIFont();
        if (font != null)
            style.font = font;
    }

    static Font EnsureUIFont()
    {
        if (_uiFont != null) return _uiFont;

        string[] candidates = { "Microsoft YaHei", "SimHei", "NSimSun", "Heiti SC", "STHeiti" };
        _uiFont = Font.CreateDynamicFontFromOSFont(candidates, 16);
        return _uiFont;
    }
}
