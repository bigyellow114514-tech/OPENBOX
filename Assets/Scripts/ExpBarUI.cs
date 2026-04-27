using UnityEngine;

public class ExpBarUI : MonoBehaviour
{
    Texture2D _bgTex;
    Texture2D _borderTex;
    Texture2D _fillTex;
    Texture2D _treeFillTex;
    GUIStyle  _labelStyle;
    GUIStyle  _levelStyle;

    void Start()
    {
        _bgTex       = MakeTex(new Color(0.10f, 0.10f, 0.10f, 0.85f));
        _borderTex   = MakeTex(new Color(0.00f, 0.00f, 0.00f, 1.00f));
        _fillTex     = MakeTex(new Color(0.20f, 0.75f, 0.25f, 1.00f));
        _treeFillTex = MakeTex(new Color(0.55f, 0.35f, 0.10f, 1.00f));
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
    }

    void OnGUI()
    {
        var pMgr = PlayerExpManager.Instance;
        if (pMgr == null) return;

        InitStyles();

        float sw     = Screen.width;
        float barW   = sw * 0.225f;   // 原来 0.45f 的一半
        float barH   = 22f;
        float pad    = 2f;
        float labelW = 55f;
        float gap    = 10f;
        float barY   = 10f;

        // 两条进度条 + 两个标签居中排列
        float totalW = labelW + barW + gap + labelW + barW;
        float startX = (sw - totalW) * 0.5f;

        // ---- 人物经验条 ----
        DrawExpBar(
            x: startX, y: barY,
            labelW: labelW, barW: barW, barH: barH, pad: pad,
            levelText: pMgr.Level >= 100 ? "满级" : $"Lv.{pMgr.Level}",
            ratio:     pMgr.Level >= 100 ? 1f : pMgr.CurrentExp / pMgr.ExpToNextLevel,
            barText:   pMgr.Level >= 100 ? "MAX" : $"{pMgr.CurrentExp:0}/{pMgr.ExpToNextLevel:0}",
            fillTex:   _fillTex
        );

        // ---- 大树经验条 ----
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
            // 预留 UI —— TreeExpManager 未挂载时显示占位符
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
}
