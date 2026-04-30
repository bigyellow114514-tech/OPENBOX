using UnityEngine;

public class CharacterAttrUI : MonoBehaviour
{
    bool _show;

    Texture2D _bgTex;
    Texture2D _sectionTex;
    Texture2D _buttonTex;
    GUIStyle _buttonStyle;
    GUIStyle _titleStyle;
    GUIStyle _sectionTitleStyle;
    GUIStyle _labelStyle;
    GUIStyle _valueStyle;

    void Start()
    {
        _bgTex = MakeTex(new Color(0.06f, 0.16f, 0.18f, 0.88f));
        _sectionTex = MakeTex(new Color(0.05f, 0.12f, 0.14f, 0.70f));
        _buttonTex = MakeTex(new Color(0.10f, 0.25f, 0.28f, 0.92f));
    }

    void InitStyles()
    {
        if (_buttonStyle != null) return;

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white, background = _buttonTex },
            hover = { textColor = Color.white, background = _buttonTex },
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.94f, 0.96f, 0.92f) },
        };

        _sectionTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.78f, 0.84f, 0.82f) },
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.78f, 0.84f, 0.82f) },
        };

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.96f, 0.90f, 0.58f) },
        };
    }

    void OnGUI()
    {
        InitStyles();

        if (GUI.Button(new Rect(10, 10, 88, 30), "角色属性", _buttonStyle))
            _show = !_show;

        if (GUI.Button(new Rect(104, 10, 88, 30), "初始化", _buttonStyle))
        {
            PlayerExpManager.Instance?.ResetToDefault();
            EquipmentSlotSystem.Instance?.ResetToDefault();
            PlayerStaminaManager.Instance?.ResetToDefault();
        }

        if (!_show) return;

        PlayerCharacter chr = PlayerCharacter.Instance;
        PlayerExpManager exp = PlayerExpManager.Instance;
        if (chr == null || exp == null) return;

        RoleAttr attr = chr.FinalAttr;
        float panelW = Mathf.Min(430f, Screen.width - 20f);
        float panelX = 10f;
        float panelY = 48f;
        float pad = 10f;
        float titleH = 30f;
        float sectionGap = 8f;
        float panelH = 510f;

        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), _bgTex);
        GUI.Label(new Rect(panelX + pad, panelY + 6f, panelW - pad * 2f, titleH), "详细属性", _titleStyle);
        GUI.Label(new Rect(panelX + pad, panelY + 32f, panelW - pad * 2f, 22f),
            "等级 " + (exp.Level >= 100 ? "MAX" : exp.Level.ToString()), _sectionTitleStyle);

        float y = panelY + 58f;
        DrawSection(panelX + pad, ref y, panelW - pad * 2f, "基础属性", new[]
        {
            Stat("攻击", attr.Attack, false),
            Stat("生命", attr.Hp, false),
            Stat("防御", attr.Defence, false),
            Stat("敏捷", attr.Agility, false),
        }, 3);

        y += sectionGap;
        DrawSection(panelX + pad, ref y, panelW - pad * 2f, "战斗属性", new[]
        {
            Stat("暴击", attr.CritRate),
            Stat("反击", attr.CounterRate),
            Stat("连击", attr.ComboRate),
            Stat("闪避", attr.DodgeRate),
            Stat("击晕", attr.StunRate),
            Stat("吸血", attr.LifeStealRate),
        }, 3);

        y += sectionGap;
        DrawSection(panelX + pad, ref y, panelW - pad * 2f, "战斗抗性", new[]
        {
            Stat("抗暴击", attr.AntiCritRate),
            Stat("抗反击", attr.AntiCounterRate),
            Stat("抗连击", attr.AntiComboRate),
            Stat("抗闪避", attr.AntiDodgeRate),
            Stat("抗击晕", attr.AntiStunRate),
            Stat("抗吸血", attr.AntiLifeStealRate),
        }, 3);

        y += sectionGap;
        DrawSection(panelX + pad, ref y, panelW - pad * 2f, "特殊属性", new[]
        {
            Stat("强化爆伤", attr.CritDmg),
            Stat("弱化爆伤", attr.AntiCritDmg),
            Stat("最终增伤", attr.DamageIncrease),
            Stat("最终减伤", attr.DamageDecrease),
            Stat("强化治疗", attr.Healing),
            Stat("弱化治疗", attr.AntiHealing),
            Stat("强化宠物", attr.PetIncrease),
            Stat("弱化宠物", attr.PetDecrease),
        }, 3);
    }

    void DrawSection(float x, ref float y, float w, string title, StatItem[] items, int columns)
    {
        float titleH = 24f;
        float rowH = 24f;
        int rows = Mathf.CeilToInt(items.Length / (float)columns);
        float h = titleH + rows * rowH + 10f;

        GUI.DrawTexture(new Rect(x, y, w, h), _sectionTex);
        GUI.Label(new Rect(x, y + 4f, w, titleH), title, _sectionTitleStyle);

        float cellW = w / columns;
        for (int i = 0; i < items.Length; i++)
        {
            int row = i / columns;
            int col = i % columns;
            float cx = x + col * cellW;
            float cy = y + titleH + row * rowH + 6f;
            DrawStat(cx, cy, cellW, rowH, items[i]);
        }

        y += h;
    }

    void DrawStat(float x, float y, float w, float h, StatItem item)
    {
        GUI.Label(new Rect(x, y, w * 0.50f, h), item.Label, _labelStyle);
        GUI.Label(new Rect(x + w * 0.52f, y, w * 0.48f, h), item.Text, _valueStyle);
    }

    static StatItem Stat(string label, float value, bool percent = true)
    {
        return new StatItem
        {
            Label = label,
            Text = percent ? value.ToString("0.##") + "%" : value.ToString("0"),
        };
    }

    Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    struct StatItem
    {
        public string Label;
        public string Text;
    }
}
