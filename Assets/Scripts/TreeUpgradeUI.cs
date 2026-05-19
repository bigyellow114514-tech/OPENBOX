using UnityEngine;

// Upgrade button positioned exactly where the tree exp bar used to be.
// Layout fields must match ExpBarUI's values (defaults kept in sync).
// Attach to any active GameObject in the scene.
public class TreeUpgradeUI : MonoBehaviour
{
    [Header("Exp Bar Layout (keep in sync with ExpBarUI)")]
    [SerializeField] float topOffset     = 10f;
    [SerializeField] float barWidthRatio = 0.16f;
    [SerializeField] float minBarWidth   = 180f;
    [SerializeField] float maxBarWidth   = 230f;
    [SerializeField] float barHeight     = 22f;
    [SerializeField] float labelWidth    = 55f;
    [SerializeField] float barGap        = 24f;

    Texture2D _btnNormalTex;
    Texture2D _btnHoverTex;

    GUIStyle _btnStyle;
    static Font _uiFont;

    void Start()
    {
        _btnNormalTex   = MakeTex(new Color(0.50f, 0.32f, 0.04f, 1f));
        _btnHoverTex    = MakeTex(new Color(0.70f, 0.48f, 0.08f, 1f));
    }

    void InitStyles()
    {
        if (_btnStyle != null) return;

        Font font = EnsureFont();

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            font      = font,
            normal    = { background = _btnNormalTex, textColor = Color.white },
            hover     = { background = _btnHoverTex,  textColor = Color.white },
            active    = { background = _btnNormalTex, textColor = Color.white },
        };

    }

    void OnGUI()
    {
        InitStyles();

        // Mirror ExpBarUI layout to land exactly on the old tree bar position
        Rect area = HudLayoutUtility.Begin();
        try
        {
        bool narrow = HudLayoutUtility.IsNarrow;
        float sw     = area.width;
        float barW   = narrow
            ? Mathf.Clamp(sw - labelWidth - 24f, 150f, 230f)
            : Mathf.Clamp(sw * barWidthRatio, minBarWidth, maxBarWidth);
        float totalW = narrow ? labelWidth + barW : labelWidth + barW + barGap + labelWidth + barW;
        float startX = area.x + (sw - totalW) * 0.5f;
        // Button occupies [tStartX + labelWidth, tStartX + labelWidth + barW]
        float bx = narrow ? startX + labelWidth : startX + labelWidth + barW + barGap + labelWidth;
        float by = area.y + (narrow ? 82f + barHeight + 8f : topOffset);

        var rect = new Rect(bx, by, barW, barHeight);

        if (GUI.Button(rect, "升级大树", _btnStyle))
            TreeUpgradePanelUI.ShowOrCreate();
        }
        finally
        {
            HudLayoutUtility.End();
        }
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    static Font EnsureFont()
    {
        if (_uiFont != null) return _uiFont;
        string[] candidates = { "Microsoft YaHei", "SimHei", "NSimSun", "Heiti SC", "STHeiti" };
        _uiFont = Font.CreateDynamicFontFromOSFont(candidates, 16);
        return _uiFont;
    }
}
