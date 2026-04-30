using UnityEngine;

public class StageEntryUI : MonoBehaviour
{
    CombatWindowUI _combatWindow;
    GUIStyle _buttonStyle;
    GUIStyle _labelStyle;
    GUIStyle _staminaStyle;
    Texture2D _heartTex;

    void Awake()
    {
        _combatWindow = GetComponent<CombatWindowUI>();
    }

    void Start()
    {
        _heartTex = Resources.Load<Texture2D>("UI/Heart");
    }

    void InitStyles()
    {
        if (_buttonStyle != null) return;

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
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
    }

    void OnGUI()
    {
        InitStyles();

        if (_combatWindow != null && _combatWindow.IsOpen) return;

        var stageManager = StageManager.Instance;
        var resources    = PlayerResourceManager.Instance;
        var stamina      = PlayerStaminaManager.Instance;
        if (stageManager == null) return;

        float w    = 210f;
        float x    = Screen.width - w - 14f;
        float y    = 12f;
        float iconS = 20f;

        GUI.Box(new Rect(x - 8f, y - 8f, w + 16f, 132f), GUIContent.none);
        GUI.Label(new Rect(x, y,       w, 24f), "Current stage: " + stageManager.CurrentStageId, _labelStyle);
        GUI.Label(new Rect(x, y + 24f, w, 24f), "Pet tickets: " + (resources != null ? resources.PetTickets : 0), _labelStyle);

        // Stamina row: heart icon + "current/max"
        float rowY = y + 48f;
        if (_heartTex != null)
            GUI.DrawTexture(new Rect(x, rowY + 2f, iconS, iconS), _heartTex, ScaleMode.ScaleToFit);
        string staminaText = stamina != null
            ? $"体力: {stamina.CurrentStamina}/{stamina.MaxStamina}"
            : "体力: --";
        GUI.Label(new Rect(x + ((_heartTex != null) ? iconS + 4f : 0f), rowY, w, 24f), staminaText, _staminaStyle);

        if (GUI.Button(new Rect(x, y + 82f, w, 34f), "Challenge", _buttonStyle))
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
}
