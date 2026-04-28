using UnityEngine;

public class CharacterAttrUI : MonoBehaviour
{
    bool _show;

    Texture2D _bgTex;
    Texture2D _borderTex;
    GUIStyle  _titleStyle;
    GUIStyle  _rowStyle;
    GUIStyle  _btnStyle;

    void Start()
    {
        _bgTex     = MakeTex(new Color(0.08f, 0.08f, 0.08f, 0.92f));
        _borderTex = MakeTex(new Color(0.00f, 0.00f, 0.00f, 1.00f));
    }

    void InitStyles()
    {
        if (_btnStyle != null) return;

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize   = 15,
            fontStyle  = FontStyle.Bold,
            alignment  = TextAnchor.MiddleCenter,
            normal     = { textColor = Color.yellow }
        };

        _rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = Color.white }
        };
    }

    void OnGUI()
    {
        InitStyles();

        // 左上角按钮
        if (GUI.Button(new Rect(10, 10, 70, 28), "角色属性", _btnStyle))
            _show = !_show;

        if (GUI.Button(new Rect(86, 10, 70, 28), "初始化角色", _btnStyle))
        {
            PlayerExpManager.Instance?.ResetToDefault();
            EquipmentSlotSystem.Instance?.ResetToDefault();
        }

        if (!_show) return;

        var chr = PlayerCharacter.Instance;
        var exp = PlayerExpManager.Instance;
        if (chr == null || exp == null) return;

        RoleAttr attr = chr.FinalAttr;

        float panelW = 220f;
        float rowH   = 24f;
        float pad    = 10f;
        int   rows   = 13; // 标题 + 等级 + 11属性
        float panelH = pad * 2 + rows * rowH;
        float panelX = 10f;
        float panelY = 44f;

        // 边框 + 背景
        GUI.DrawTexture(new Rect(panelX - 2, panelY - 2, panelW + 4, panelH + 4), _borderTex);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), _bgTex);

        float y = panelY + pad;
        float lx = panelX + pad;
        float lw = panelW - pad * 2;

        GUI.Label(new Rect(lx, y, lw, rowH), "— 角色属性 —", _titleStyle);
        y += rowH;

        DrawRow(ref y, lx, lw, rowH, "等级",
            exp.Level >= 100 ? "MAX" : exp.Level.ToString());

        DrawRow(ref y, lx, lw, rowH, "攻击力",   attr.Attack.ToString("0"));
        DrawRow(ref y, lx, lw, rowH, "防御力",   attr.Defence.ToString("0"));
        DrawRow(ref y, lx, lw, rowH, "生命值",   attr.Hp.ToString("0"));
        DrawRow(ref y, lx, lw, rowH, "敏捷",     attr.Agility.ToString("0"));
        DrawRow(ref y, lx, lw, rowH, "暴击率",   attr.CritRate.ToString("0.##") + "%");
        DrawRow(ref y, lx, lw, rowH, "爆伤",     attr.CritDmg.ToString("0.##") + "%");
        DrawRow(ref y, lx, lw, rowH, "反击",     attr.CounterRate.ToString("0.##") + "%");
        DrawRow(ref y, lx, lw, rowH, "连击",     attr.ComboRate.ToString("0.##") + "%");
        DrawRow(ref y, lx, lw, rowH, "闪避",     attr.DodgeRate.ToString("0.##") + "%");
        DrawRow(ref y, lx, lw, rowH, "击晕",     attr.StunRate.ToString("0.##") + "%");
        DrawRow(ref y, lx, lw, rowH, "吸血",     attr.LifeStealRate.ToString("0.##") + "%");
    }

    void DrawRow(ref float y, float x, float w, float h, string label, string value)
    {
        GUI.Label(new Rect(x,           y, w * 0.55f, h), label, _rowStyle);
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, h), value, _rowStyle);
        y += h;
    }

    Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
