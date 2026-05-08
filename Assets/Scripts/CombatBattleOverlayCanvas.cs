using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CombatBattleOverlayCanvas
{
    readonly GameObject _root;
    readonly RectTransform _panel;
    readonly TMP_Text _titleText;
    readonly TMP_Text _roundText;
    readonly TMP_Text _playerHpText;
    readonly TMP_Text _enemyHpText;
    readonly TMP_Text _resultText;
    readonly Image _playerHpFill;
    readonly Image _enemyHpFill;
    readonly Button _skipButton;
    readonly Button _returnButton;
    readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
    readonly CombatBattleLayoutSettings _layout;

    StageData _stage;
    CombatResult _result;
    int _lastFloatingEventIndex = -1;

    public Action SkipRequested;
    public Action ReturnRequested;
    public Action<float> SpeedRequested;

    public CombatBattleOverlayCanvas()
    {
        _layout = CombatBattleLayoutSettings.LoadDefault();
        _root = new GameObject("CombatBattleOverlayCanvas");
        UnityEngine.Object.DontDestroyOnLoad(_root);

        Canvas canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        _panel = NewRect("Panel", _root.transform);
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = _layout.canvasPanelSize;
        _panel.anchoredPosition = Vector2.zero;

        _titleText = NewText("Title", _panel, _layout.titleFontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(_titleText.rectTransform, _layout.titlePosition, new Vector2(520f, 40f), new Vector2(0.5f, 0.5f));

        _roundText = NewText("Round", _panel, _layout.roundFontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(_roundText.rectTransform, _layout.roundPosition, new Vector2(360f, 30f), new Vector2(0.5f, 0.5f));

        float[] speeds = { 0.2f, 0.5f, 1f, 2f };
        for (int i = 0; i < speeds.Length; i++)
        {
            float speed = speeds[i];
            Button button = NewButton("Speed" + i, _panel, speed.ToString("0.##") + "x", _layout.speedFontSize);
            SetRect((RectTransform)button.transform, _layout.speedStartPosition + _layout.speedStep * i, _layout.speedButtonSize, new Vector2(0.5f, 0.5f));
            button.onClick.AddListener(() => SpeedRequested?.Invoke(speed));
        }

        _playerHpText = NewText("PlayerHp", _panel, _layout.hpFontSize, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(_playerHpText.rectTransform, _layout.playerHpTextPosition, new Vector2(320f, 32f), new Vector2(0.5f, 0.5f));
        Image playerBg = NewImage("PlayerHpBg", _panel, new Color(0.04f, 0.05f, 0.05f, 0.9f));
        SetRect((RectTransform)playerBg.transform, _layout.playerHpBarPosition, _layout.hpBarSize, new Vector2(0.5f, 0.5f));
        _playerHpFill = NewImage("PlayerHpFill", playerBg.transform, new Color(0.16f, 0.72f, 0.32f, 1f));
        StretchFill(_playerHpFill.rectTransform);

        _enemyHpText = NewText("EnemyHp", _panel, _layout.hpFontSize, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(_enemyHpText.rectTransform, _layout.enemyHpTextPosition, new Vector2(320f, 32f), new Vector2(0.5f, 0.5f));
        Image enemyBg = NewImage("EnemyHpBg", _panel, new Color(0.04f, 0.05f, 0.05f, 0.9f));
        SetRect((RectTransform)enemyBg.transform, _layout.enemyHpBarPosition, _layout.hpBarSize, new Vector2(0.5f, 0.5f));
        _enemyHpFill = NewImage("EnemyHpFill", enemyBg.transform, new Color(0.86f, 0.22f, 0.18f, 1f));
        StretchFill(_enemyHpFill.rectTransform);

        _skipButton = NewButton("SkipButton", _panel, "Skip", _layout.actionFontSize);
        SetRect((RectTransform)_skipButton.transform, _layout.actionButtonPosition, new Vector2(96f, 36f), new Vector2(0.5f, 0.5f));
        _skipButton.onClick.AddListener(() => SkipRequested?.Invoke());

        _returnButton = NewButton("ReturnButton", _panel, "Return", _layout.actionFontSize);
        SetRect((RectTransform)_returnButton.transform, _layout.actionButtonPosition, new Vector2(108f, 36f), new Vector2(0.5f, 0.5f));
        _returnButton.onClick.AddListener(() => ReturnRequested?.Invoke());

        _resultText = NewText("Result", _panel, _layout.resultFontSize, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(_resultText.rectTransform, _layout.resultTextPosition, new Vector2(520f, 38f), new Vector2(0.5f, 0.5f));

        _root.SetActive(false);
    }

    public void Open(StageData stage, CombatResult result)
    {
        _stage = stage;
        _result = result;
        _lastFloatingEventIndex = -1;
        ClearFloatingTexts();
        _root.SetActive(true);
    }

    public void Close()
    {
        _stage = null;
        _result = null;
        ClearFloatingTexts();
        _root.SetActive(false);
    }

    public void UpdateView(
        CombatLogEntry current,
        int eventIndex,
        float progress,
        bool showResult,
        bool playingEnemyDeath,
        int round,
        float playerHp,
        float enemyHp)
    {
        if (!_root.activeSelf || _stage == null || _result == null) return;

        _titleText.text = _stage.StageName;
        _roundText.text = "Round " + round + "/" + Mathf.Max(1, _stage.MaxRound);
        _playerHpText.text = "Player  " + Mathf.Ceil(playerHp).ToString("0") + "/" + Mathf.Ceil(_result.PlayerMaxHp).ToString("0");
        _enemyHpText.text = "Enemy  " + Mathf.Ceil(enemyHp).ToString("0") + "/" + Mathf.Ceil(_result.EnemyMaxHp).ToString("0");

        SetFill(_playerHpFill.rectTransform, _result.PlayerMaxHp > 0f ? Mathf.Clamp01(playerHp / _result.PlayerMaxHp) : 0f);
        SetFill(_enemyHpFill.rectTransform, _result.EnemyMaxHp > 0f ? Mathf.Clamp01(enemyHp / _result.EnemyMaxHp) : 0f);

        _skipButton.gameObject.SetActive(!showResult && !playingEnemyDeath);
        _returnButton.gameObject.SetActive(showResult);
        _resultText.gameObject.SetActive(showResult);
        _resultText.text = _result.PlayerWon
            ? "Victory  Reward: pet ticket +" + _stage.PetTicketReward
            : "Defeat  No reward";

        if (current != null && eventIndex != _lastFloatingEventIndex && progress >= 0.18f)
        {
            SpawnFloatingText(current);
            _lastFloatingEventIndex = eventIndex;
        }

        UpdateFloatingTexts();
    }

    void SpawnFloatingText(CombatLogEntry entry)
    {
        if (entry.Damage > 0f)
            AddFloatingText("-" + entry.Damage.ToString("0"), entry.TargetIsPlayer ? new Vector2(-250f, -38f) : new Vector2(250f, -28f), new Color(1f, 0.22f, 0.18f));

        if (entry.Heal > 0f)
            AddFloatingText("+" + entry.Heal.ToString("0"), entry.ActorIsPlayer ? new Vector2(-250f, -62f) : new Vector2(250f, -54f), new Color(0.25f, 1f, 0.35f));

        if (entry.Dodged)
            AddFloatingText("Dodge", entry.TargetIsPlayer ? new Vector2(-250f, 8f) : new Vector2(250f, 8f), new Color(1f, 0.88f, 0.18f));

        if (entry.Crit)
            AddFloatingText("Crit", entry.TargetIsPlayer ? new Vector2(-250f, 38f) : new Vector2(250f, 38f), new Color(1f, 0.88f, 0.18f));

        if (entry.Stunned)
            AddFloatingText("Stun", entry.TargetIsPlayer ? new Vector2(-250f, 68f) : new Vector2(250f, 68f), new Color(1f, 0.88f, 0.18f));
    }

    void AddFloatingText(string text, Vector2 anchoredPosition, Color color)
    {
        TMP_Text label = NewText("FloatingText", _panel, _layout.floatingFontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = text;
        label.color = color;
        SetRect(label.rectTransform, anchoredPosition, new Vector2(180f, 42f), new Vector2(0.5f, 0.5f));
        _floatingTexts.Add(new FloatingText(label, anchoredPosition, Time.unscaledTime, color));
    }

    void UpdateFloatingTexts()
    {
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            FloatingText item = _floatingTexts[i];
            float age = Time.unscaledTime - item.StartTime;
            if (age > 0.75f)
            {
                UnityEngine.Object.Destroy(item.Text.gameObject);
                _floatingTexts.RemoveAt(i);
                continue;
            }

            float t = age / 0.75f;
            item.Text.rectTransform.anchoredPosition = item.StartPosition + Vector2.up * Mathf.Sin(t * Mathf.PI) * 38f;
            item.Text.color = new Color(item.Color.r, item.Color.g, item.Color.b, 1f - t);
        }
    }

    void ClearFloatingTexts()
    {
        for (int i = 0; i < _floatingTexts.Count; i++)
        {
            if (_floatingTexts[i].Text != null)
                UnityEngine.Object.Destroy(_floatingTexts[i].Text.gameObject);
        }
        _floatingTexts.Clear();
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static TMP_Text NewText(string name, Transform parent, int fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static Button NewButton(string name, Transform parent, string label, int fontSize)
    {
        Image bg = NewImage(name, parent, new Color(0.08f, 0.18f, 0.27f, 0.92f));
        Button button = bg.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.16f, 0.34f, 0.48f, 0.95f);
        colors.pressedColor = new Color(0.05f, 0.12f, 0.18f, 1f);
        button.colors = colors;

        TMP_Text text = NewText("Label", bg.transform, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = label;
        StretchFill(text.rectTransform);
        return button;
    }

    static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void StretchFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetFill(RectTransform rect, float pct)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    sealed class FloatingText
    {
        public readonly TMP_Text Text;
        public readonly Vector2 StartPosition;
        public readonly float StartTime;
        public readonly Color Color;

        public FloatingText(TMP_Text text, Vector2 startPosition, float startTime, Color color)
        {
            Text = text;
            StartPosition = startPosition;
            StartTime = startTime;
            Color = color;
        }
    }
}
