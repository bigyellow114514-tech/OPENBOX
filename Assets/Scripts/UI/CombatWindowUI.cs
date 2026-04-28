using UnityEngine;

public class CombatWindowUI : MonoBehaviour
{
    public bool IsOpen => _isOpen;

    [SerializeField] Sprite playerSprite = null;
    [SerializeField] Sprite enemySprite = null;
    [SerializeField] Sprite playerAttackSheet = null;
    [SerializeField] Sprite playerAttackedSheet = null;
    [SerializeField] int animationColumns = 4;
    [SerializeField] int animationRows = 2;
    [SerializeField] int animationFrameCount = 8;

    StageData _stage;
    CombatResult _result;
    bool _isOpen;
    bool _showResult;
    bool _rewardApplied;
    int _eventIndex;
    float _eventStartTime;

    Texture2D _panelTex;
    Texture2D _barBgTex;
    Texture2D _playerHpTex;
    Texture2D _enemyHpTex;
    Texture2D _buffTex;

    GUIStyle _titleStyle;
    GUIStyle _labelStyle;
    GUIStyle _buttonStyle;
    GUIStyle _damageStyle;
    GUIStyle _healStyle;
    GUIStyle _popupStyle;
    GUIStyle _buffStyle;

    const float EventDuration = 0.85f;
    const float AttackReachTime = 0.18f;
    const float AttackReturnTime = 0.42f;

    public void Open(StageData stage, CombatResult result)
    {
        ResolveUnitSprites();

        _stage = stage;
        _result = result;
        _isOpen = true;
        _showResult = result.Logs.Count == 0;
        _rewardApplied = false;
        _eventIndex = 0;
        _eventStartTime = Time.unscaledTime;
    }

    void Update()
    {
        if (!_isOpen || _result == null || _showResult) return;

        if (Time.unscaledTime - _eventStartTime < EventDuration) return;

        _eventIndex++;
        _eventStartTime = Time.unscaledTime;

        if (_eventIndex >= _result.Logs.Count)
            _showResult = true;
    }

    void OnGUI()
    {
        if (!_isOpen || _stage == null || _result == null) return;
        InitStyles();

        float w = Mathf.Min(Screen.width * 0.82f, 900f);
        float h = Mathf.Min(Screen.height * 0.78f, 600f);
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.DrawTexture(new Rect(x, y, w, h), _panelTex);
        GUI.Box(new Rect(x, y, w, h), GUIContent.none);

        GUI.Label(new Rect(x + 16f, y + 14f, w - 32f, 30f), _stage.StageName, _titleStyle);

        Rect arena = new Rect(x + 28f, y + 58f, w - 56f, h - 142f);
        DrawArena(arena);

        if (_showResult)
            DrawResult(x + 28f, y + h - 66f, w - 56f);
        else if (GUI.Button(new Rect(x + w - 126f, y + h - 50f, 98f, 32f), "Skip", _buttonStyle))
        {
            _eventIndex = _result.Logs.Count;
            _showResult = true;
        }
    }

    void InitStyles()
    {
        if (_panelTex != null) return;

        ResolveUnitSprites();

        _panelTex = MakeTex(new Color(0.05f, 0.06f, 0.07f, 0.94f));
        _barBgTex = MakeTex(new Color(0.12f, 0.12f, 0.12f, 1f));
        _playerHpTex = MakeTex(new Color(0.16f, 0.72f, 0.32f, 1f));
        _enemyHpTex = MakeTex(new Color(0.86f, 0.22f, 0.18f, 1f));
        _buffTex = MakeTex(new Color(0.95f, 0.72f, 0.12f, 1f));
        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _damageStyle = FloatingStyle(new Color(1f, 0.22f, 0.18f, 1f), 24);
        _healStyle = FloatingStyle(new Color(0.25f, 1f, 0.35f, 1f), 23);
        _popupStyle = FloatingStyle(new Color(1f, 0.88f, 0.18f, 1f), 22);
        _buffStyle = FloatingStyle(Color.black, 15);
    }

    void ResolveUnitSprites()
    {
        if (playerSprite == null)
            playerSprite = FindChildSprite("CombatPlayerUnit")
                ?? Resources.Load<Sprite>("Sprites/Knight");

        if (enemySprite == null)
            enemySprite = FindChildSprite("CombatEnemyUnit")
                ?? Resources.Load<Sprite>("Sprites/Slam");

        if (playerAttackSheet == null)
            playerAttackSheet = Resources.Load<Sprite>("Sprites/knight_attack");

        if (playerAttackedSheet == null)
            playerAttackedSheet = Resources.Load<Sprite>("Sprites/knight_attacked");
    }

    Sprite FindChildSprite(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null) return null;

        SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }

    void DrawArena(Rect arena)
    {
        CombatLogEntry current = CurrentEvent();
        float progress = CurrentProgress();

        float playerHp = CurrentPlayerHp(progress);
        float enemyHp = CurrentEnemyHp(progress);

        Rect playerHome = CharacterRect(arena, true);
        Rect enemyHome = CharacterRect(arena, false);
        Rect playerRect = playerHome;
        Rect enemyRect = enemyHome;

        if (current != null && current.Type == CombatEventType.Attack)
        {
            float move = AttackOffset(progress);
            float distance = arena.width * 0.26f;

            if (current.ActorIsPlayer)
                playerRect.x += distance * move;
            else
                enemyRect.x -= distance * move;
        }

        DrawHpBlock(new Rect(playerHome.x, arena.y, playerHome.width, 48f), "Player", playerHp, _result.PlayerMaxHp, _playerHpTex);
        DrawHpBlock(new Rect(enemyHome.x, arena.y, enemyHome.width, 48f), "Enemy", enemyHp, _result.EnemyMaxHp, _enemyHpTex);

        DrawPlayerCharacter(playerRect, current, progress);
        DrawCharacter(enemyRect, enemySprite, false);

        bool playerStunned = current != null ? current.PlayerStunned : LastPlayerStunned();
        bool enemyStunned = current != null ? current.EnemyStunned : LastEnemyStunned();
        if (playerStunned) DrawStunBuff(playerRect);
        if (enemyStunned) DrawStunBuff(enemyRect);

        if (current != null)
            DrawEventText(current, progress, playerRect, enemyRect);
    }

    Rect CharacterRect(Rect arena, bool player)
    {
        float size = Mathf.Min(arena.height * 0.68f, arena.width * 0.34f);
        float y = arena.y + arena.height * 0.28f;
        float x = player ? arena.x + arena.width * 0.08f : arena.xMax - arena.width * 0.08f - size;
        return new Rect(x, y, size, size);
    }

    void DrawHpBlock(Rect rect, string name, float hp, float maxHp, Texture2D hpTex)
    {
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 22f),
            name + "  " + Mathf.Ceil(hp).ToString("0") + "/" + Mathf.Ceil(maxHp).ToString("0"),
            _labelStyle);

        GUI.DrawTexture(new Rect(rect.x, rect.y + 26f, rect.width, 18f), _barBgTex);
        float pct = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
        GUI.DrawTexture(new Rect(rect.x, rect.y + 26f, rect.width * pct, 18f), hpTex);
    }

    void DrawCharacter(Rect rect, Sprite sprite, bool flip)
    {
        if (sprite == null || sprite.texture == null)
        {
            GUI.Box(rect, GUIContent.none);
            return;
        }

        if (!flip)
        {
            GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true);
            return;
        }

        Matrix4x4 old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), rect.center);
        GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true);
        GUI.matrix = old;
    }

    void DrawPlayerCharacter(Rect rect, CombatLogEntry current, float progress)
    {
        if (current != null && current.Type == CombatEventType.Attack)
        {
            if (current.ActorIsPlayer && playerAttackSheet != null)
            {
                DrawSheetFrame(rect, playerAttackSheet, AnimationFrame(progress), false);
                return;
            }

            if (current.TargetIsPlayer && playerAttackedSheet != null && !current.Dodged)
            {
                DrawSheetFrame(rect, playerAttackedSheet, AnimationFrame(progress), false);
                return;
            }
        }

        DrawCharacter(rect, playerSprite, false);
    }

    void DrawSheetFrame(Rect rect, Sprite sheet, int frameIndex, bool flip)
    {
        if (sheet == null || sheet.texture == null)
        {
            GUI.Box(rect, GUIContent.none);
            return;
        }

        int cols = Mathf.Max(1, animationColumns);
        int rows = Mathf.Max(1, animationRows);
        int maxFrames = Mathf.Max(1, Mathf.Min(animationFrameCount, cols * rows));
        int frame = Mathf.Clamp(frameIndex, 0, maxFrames - 1);
        int col = frame % cols;
        int row = frame / cols;

        Rect uv = new Rect(
            col / (float)cols,
            1f - ((row + 1f) / rows),
            1f / cols,
            1f / rows);

        if (!flip)
        {
            GUI.DrawTextureWithTexCoords(rect, sheet.texture, uv, true);
            return;
        }

        Matrix4x4 old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), rect.center);
        GUI.DrawTextureWithTexCoords(rect, sheet.texture, uv, true);
        GUI.matrix = old;
    }

    int AnimationFrame(float progress)
    {
        int maxFrames = Mathf.Max(1, Mathf.Min(animationFrameCount, Mathf.Max(1, animationColumns) * Mathf.Max(1, animationRows)));
        return Mathf.Clamp(Mathf.FloorToInt(progress * maxFrames), 0, maxFrames - 1);
    }

    void DrawStunBuff(Rect characterRect)
    {
        Rect icon = new Rect(characterRect.center.x - 16f, characterRect.y - 18f, 32f, 32f);
        GUI.DrawTexture(icon, _buffTex);
        GUI.Label(icon, "Z", _buffStyle);
    }

    void DrawEventText(CombatLogEntry entry, float progress, Rect playerRect, Rect enemyRect)
    {
        if (entry.Type == CombatEventType.Attack && progress < AttackReachTime)
            return;

        Rect actor = entry.ActorIsPlayer ? playerRect : enemyRect;
        Rect target = entry.TargetIsPlayer ? playerRect : enemyRect;
        float lift = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * 34f;
        float alpha = 1f - Mathf.Clamp01((progress - 0.62f) / 0.38f);

        if (entry.Type == CombatEventType.Popup)
        {
            string text = entry.Combo ? "Combo" : entry.Text;
            DrawFloatingText(text, actor.center.x, actor.y - lift, alpha, _popupStyle);
            return;
        }

        if (entry.Dodged)
            DrawFloatingText("Dodge", target.center.x, target.y - lift, alpha, _popupStyle);

        if (entry.Crit)
            DrawFloatingText("Crit", target.center.x, target.y + 18f - lift, alpha, _popupStyle);

        if (entry.Stunned)
            DrawFloatingText("Stun", target.center.x, target.y + 46f - lift, alpha, _popupStyle);

        if (entry.Damage > 0f)
            DrawFloatingText("-" + entry.Damage.ToString("0"), target.center.x, target.y + target.height * 0.38f - lift, alpha, _damageStyle);

        if (entry.Heal > 0f)
            DrawFloatingText("+" + entry.Heal.ToString("0"), actor.center.x, actor.y + actor.height * 0.32f - lift, alpha, _healStyle);
    }

    void DrawFloatingText(string text, float centerX, float y, float alpha, GUIStyle baseStyle)
    {
        Color old = baseStyle.normal.textColor;
        Color c = old;
        c.a *= alpha;
        baseStyle.normal.textColor = c;
        GUI.Label(new Rect(centerX - 70f, y, 140f, 32f), text, baseStyle);
        baseStyle.normal.textColor = old;
    }

    void DrawResult(float x, float y, float w)
    {
        string summary = _result.PlayerWon
            ? "Victory  Reward: pet ticket +" + _stage.PetTicketReward
            : "Defeat  No reward";

        GUI.Label(new Rect(x, y, w - 120f, 32f), summary, _labelStyle);

        if (GUI.Button(new Rect(x + w - 112f, y, 112f, 34f), "Return", _buttonStyle))
            CloseAndApply();
    }

    void CloseAndApply()
    {
        if (!_rewardApplied && _result.PlayerWon)
        {
            PlayerResourceManager.Instance?.AddPetTickets(_stage.PetTicketReward);
            StageManager.Instance?.CompleteCurrentStage();
            _rewardApplied = true;
        }

        _isOpen = false;
        _stage = null;
        _result = null;
    }

    CombatLogEntry CurrentEvent()
    {
        if (_result == null || _eventIndex < 0 || _eventIndex >= _result.Logs.Count) return null;
        return _result.Logs[_eventIndex];
    }

    float CurrentProgress()
    {
        return Mathf.Clamp01((Time.unscaledTime - _eventStartTime) / EventDuration);
    }

    float CurrentPlayerHp(float progress)
    {
        if (_showResult) return _result.PlayerHp;
        CombatLogEntry current = CurrentEvent();
        if (current != null && current.Type == CombatEventType.Attack && progress < AttackReachTime)
            return PreviousPlayerHp();
        return current != null ? current.PlayerHp : _result.PlayerMaxHp;
    }

    float CurrentEnemyHp(float progress)
    {
        if (_showResult) return _result.EnemyHp;
        CombatLogEntry current = CurrentEvent();
        if (current != null && current.Type == CombatEventType.Attack && progress < AttackReachTime)
            return PreviousEnemyHp();
        return current != null ? current.EnemyHp : _result.EnemyMaxHp;
    }

    float PreviousPlayerHp()
    {
        if (_result == null || _eventIndex <= 0) return _result.PlayerMaxHp;
        return _result.Logs[_eventIndex - 1].PlayerHp;
    }

    float PreviousEnemyHp()
    {
        if (_result == null || _eventIndex <= 0) return _result.EnemyMaxHp;
        return _result.Logs[_eventIndex - 1].EnemyHp;
    }

    bool LastPlayerStunned()
    {
        return _result != null && _result.Logs.Count > 0 && _result.Logs[_result.Logs.Count - 1].PlayerStunned;
    }

    bool LastEnemyStunned()
    {
        return _result != null && _result.Logs.Count > 0 && _result.Logs[_result.Logs.Count - 1].EnemyStunned;
    }

    float AttackOffset(float progress)
    {
        if (progress <= AttackReachTime)
            return Mathf.SmoothStep(0f, 1f, progress / AttackReachTime);

        if (progress <= AttackReturnTime)
            return Mathf.SmoothStep(1f, 0f, (progress - AttackReachTime) / (AttackReturnTime - AttackReachTime));

        return 0f;
    }

    GUIStyle FloatingStyle(Color color, int fontSize)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = color }
        };
    }

    Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
