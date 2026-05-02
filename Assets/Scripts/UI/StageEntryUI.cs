using UnityEngine;

public class StageEntryUI : MonoBehaviour
{
    [Header("Resource Bar Layout")]
    [SerializeField] float resourceWidth = 360f;
    [SerializeField] float resourceHeight = 44f;
    [SerializeField] float resourceRightOffset = 14f;
    [SerializeField] float resourceTopOffset = 12f;
    [SerializeField] float resourcePadding = 8f;
    [SerializeField] float staminaColumnWidth = 128f;
    [SerializeField] float ticketColumnWidth = 122f;

    [Header("Stage Button Layout")]
    [SerializeField] float stageButtonWidth = 160f;
    [SerializeField] float stageButtonHeight = 52f;
    [SerializeField] float stageButtonTopGap = 24f;
    [SerializeField] float stageButtonRightOffset = 14f;

    [Header("Skins")]
    [SerializeField] Texture2D resourcePanelTexture;
    [SerializeField] Texture2D stageButtonTexture;

    CombatWindowUI _combatWindow;
    GUIStyle _buttonStyle;
    GUIStyle _resourceStyle;
    GUIStyle _staminaStyle;
    Texture2D _heartTex;
    static Font _uiFont;

    void Awake()
    {
        _combatWindow = GetComponent<CombatWindowUI>();
    }

    void Start()
    {
        _heartTex = Resources.Load<Texture2D>("UI/Heart");
    }

    void OnValidate()
    {
        resourceWidth = Mathf.Max(1f, resourceWidth);
        resourceHeight = Mathf.Max(1f, resourceHeight);
        resourcePadding = Mathf.Max(0f, resourcePadding);
        staminaColumnWidth = Mathf.Max(1f, staminaColumnWidth);
        ticketColumnWidth = Mathf.Max(1f, ticketColumnWidth);
        stageButtonWidth = Mathf.Max(1f, stageButtonWidth);
        stageButtonHeight = Mathf.Max(1f, stageButtonHeight);
        stageButtonTopGap = Mathf.Max(0f, stageButtonTopGap);

        _buttonStyle = null;
    }

    void InitStyles()
    {
        if (_buttonStyle != null) return;

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            hover = { textColor = Color.white },
            active = { textColor = Color.white }
        };
        if (stageButtonTexture != null)
        {
            _buttonStyle.normal.background = stageButtonTexture;
            _buttonStyle.hover.background = stageButtonTexture;
            _buttonStyle.active.background = stageButtonTexture;
        }

        _resourceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        _staminaStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1f, 0.4f, 0.4f) }
        };

        ApplyFont(_buttonStyle);
        ApplyFont(_resourceStyle);
        ApplyFont(_staminaStyle);
    }

    void OnGUI()
    {
        InitStyles();

        if (_combatWindow != null && _combatWindow.IsOpen) return;

        var stageManager = StageManager.Instance;
        var resources    = PlayerResourceManager.Instance != null
            ? PlayerResourceManager.Instance
            : EnsureResourceManager();
        var stamina      = PlayerStaminaManager.Instance;
        if (stageManager == null) return;

        float resourceX = Screen.width - resourceWidth - resourceRightOffset;
        float resourceY = resourceTopOffset;
        float iconS = 20f;

        Rect panelRect = new Rect(
            resourceX - resourcePadding,
            resourceY - resourcePadding,
            resourceWidth + resourcePadding * 2f,
            resourceHeight + resourcePadding * 2f
        );
        if (resourcePanelTexture != null)
            GUI.DrawTexture(panelRect, resourcePanelTexture, ScaleMode.StretchToFill);
        else
            GUI.Box(panelRect, GUIContent.none);

        float staminaX = resourceX;
        float ticketX = resourceX + staminaColumnWidth;
        float goldX = ticketX + ticketColumnWidth;
        float rowY = resourceY + (resourceHeight - 24f) * 0.5f;
        if (_heartTex != null)
            GUI.DrawTexture(new Rect(staminaX, rowY + 2f, iconS, iconS), _heartTex, ScaleMode.ScaleToFit);

        string staminaText = stamina != null
            ? $"体力 {stamina.CurrentStamina}/{stamina.MaxStamina}"
            : "体力 --";
        GUI.Label(new Rect(staminaX + ((_heartTex != null) ? iconS + 4f : 0f), rowY, staminaColumnWidth, 24f), staminaText, _staminaStyle);
        GUI.Label(new Rect(ticketX, rowY, ticketColumnWidth, 24f), "宠物券 " + (resources != null ? resources.PetTickets : 0), _resourceStyle);
        GUI.Label(new Rect(goldX, rowY, resourceWidth - (goldX - resourceX), 24f), "金币 " + (resources != null ? resources.Gold : 0), _resourceStyle);

        float buttonX = Screen.width - stageButtonWidth - stageButtonRightOffset;
        float buttonY = resourceY + resourceHeight + stageButtonTopGap;
        string stageText = "第 " + stageManager.CurrentStageId + " 关\n挑战";
        if (GUI.Button(new Rect(buttonX, buttonY, stageButtonWidth, stageButtonHeight), stageText, _buttonStyle))
            StartChallenge();
    }

    void StartChallenge()
    {
        var stageManager = StageManager.Instance;
        var player = PlayerCharacter.Instance;

        if (stageManager == null || player == null || _combatWindow == null) return;

        StageData stage = stageManager.CurrentStage;
        CombatResult result = CombatSimulator.Run(player.FinalAttr, stage);
        _combatWindow.Open(stage, result);
    }

    static PlayerResourceManager EnsureResourceManager()
    {
        var go = new GameObject("PlayerResourceManager");
        DontDestroyOnLoad(go);
        return go.AddComponent<PlayerResourceManager>();
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
