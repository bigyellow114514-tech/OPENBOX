using UnityEngine;
using System.Collections.Generic;

public class CombatWindowUI : MonoBehaviour
{
    public bool IsOpen => _isOpen;

    [SerializeField] bool useSceneObjectBattleView = true;
    [SerializeField] Sprite playerSprite = null;
    [SerializeField] Sprite enemySprite = null;
    [SerializeField] Sprite playerAttackSheet = null;
    [SerializeField] CombatHealVFXSettings healVFXSettings = null;
    [SerializeField] int animationColumns = 4;
    [SerializeField] int animationRows = 2;
    [SerializeField] int animationFrameCount = 8;

    StageData _stage;
    CombatResult _result;
    MonsterAnimationSet _monsterAnimations;
    PetAnimationSet _playerPetAnimations;
    PetAnimationSet _enemyPetAnimations;
    bool _isOpen;
    bool _showResult;
    bool _playingEnemyDeath;
    bool _rewardApplied;
    int _eventIndex;
    float _eventStartTime;
    float _deathStartTime;
    float _battleSpeed = 1f;
    CombatSceneBattleView _sceneView;
    CombatBattleOverlayCanvas _overlayCanvas;

    Texture2D _panelTex;
    Texture2D _barBgTex;
    Texture2D _playerHpTex;
    Texture2D _enemyHpTex;
    Texture2D _buffTex;
    Texture2D _healGlowTex;
    Texture2D _healSparkTex;
    Texture2D _healCrossTex;
    Texture2D _stunBuffSheet;

    GUIStyle _titleStyle;
    GUIStyle _labelStyle;
    GUIStyle _roundStyle;
    GUIStyle _buttonStyle;
    GUIStyle _damageStyle;
    GUIStyle _healStyle;
    GUIStyle _popupStyle;

    const float EventDuration = 0.85f;
    const float AttackReachTime = 0.18f;
    const float AttackReturnTime = 0.42f;
    const float EnemyIdleFps = 16f;
    const float PetIdleFps = 12f;
    const float EnemyDeathDuration = 0.9f;
    const float BaseSpeedScale = 1.3f;
    const float PlayerSheetFrameScale = 1.5f;
    const float PlayerBaselineYOffset = 0.16f;
    const float PlayerIdleYOffset = -0.1f;
    const int StunBuffColumns = 3;
    const float StunBuffFps = 8f;

    public void Open(StageData stage, CombatResult result)
    {
        ResolveUnitSprites();
        EnsureSceneView();

        _stage = stage;
        _result = result;
        _monsterAnimations = MonsterAnimationSet.Load(stage != null ? stage.MonsterAvatar : "");
        _playerPetAnimations = PetAnimationSet.Load(result != null ? result.PlayerPetResource : "");
        _enemyPetAnimations = PetAnimationSet.Load(result != null ? result.EnemyPetResource : "");
        _isOpen = true;
        _showResult = result.Logs.Count == 0;
        _playingEnemyDeath = false;
        _rewardApplied = false;
        _eventIndex = 0;
        _eventStartTime = Time.unscaledTime;
        _deathStartTime = 0f;
        _battleSpeed = 1f;

        if (useSceneObjectBattleView)
        {
            _sceneView.Open(stage, result);
            _overlayCanvas.Open(stage, result);
            _sceneView.SetBattleSpeed(_battleSpeed);
            PushSceneViewState();
        }
    }

    void Update()
    {
        if (!_isOpen || _result == null)
            return;

        if (!_showResult)
        {
            if (_playingEnemyDeath)
            {
                if (Time.unscaledTime - _deathStartTime >= ScaledDuration(EnemyDeathDuration))
                    _showResult = true;
            }
            else if (Time.unscaledTime - _eventStartTime >= ScaledDuration(EventDuration))
            {
                _eventIndex++;
                _eventStartTime = Time.unscaledTime;

                if (_eventIndex >= _result.Logs.Count)
                {
                    if (_result.PlayerWon && _monsterAnimations != null && _monsterAnimations.HasDeath)
                    {
                        _playingEnemyDeath = true;
                        _deathStartTime = Time.unscaledTime;
                    }
                    else
                    {
                        _showResult = true;
                    }
                }
            }
        }

        if (useSceneObjectBattleView)
            PushSceneViewState();
    }

    void OnGUI()
    {
        if (useSceneObjectBattleView)
            return;

        if (!_isOpen || _stage == null || _result == null) return;
        InitStyles();

        float w = Mathf.Min(Screen.width * 0.82f, 900f);
        float h = Mathf.Min(Screen.height * 0.78f, 600f);
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        GUI.DrawTexture(new Rect(x, y, w, h), _panelTex);
        GUI.Box(new Rect(x, y, w, h), GUIContent.none);

        GUI.Label(new Rect(x + 16f, y + 14f, w - 32f, 30f), _stage.StageName, _titleStyle);
        GUI.Label(new Rect(x + 16f, y + 40f, w - 32f, 22f), RoundText(), _roundStyle);
        DrawSpeedButtons(new Rect(x + 28f, y + 42f, 214f, 26f));

        Rect arena = new Rect(x + 28f, y + 72f, w - 56f, h - 156f);
        DrawArena(arena);

        if (_showResult)
            DrawResult(x + 28f, y + h - 66f, w - 56f);
        else if (!_playingEnemyDeath && GUI.Button(new Rect(x + w - 126f, y + h - 50f, 98f, 32f), "Skip", _buttonStyle))
        {
            _eventIndex = _result.Logs.Count;
            if (_result.PlayerWon && _monsterAnimations != null && _monsterAnimations.HasDeath)
            {
                _playingEnemyDeath = true;
                _deathStartTime = Time.unscaledTime;
            }
            else
            {
                _showResult = true;
            }
        }
    }

    void DrawSceneBattleOverlay()
    {
        if (!_isOpen || _stage == null || _result == null) return;
        InitStyles();

        float w = Mathf.Min(Screen.width * 0.82f, 900f);
        float h = Mathf.Min(Screen.height * 0.78f, 600f);
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;
        Rect arena = new Rect(x + 28f, y + 72f, w - 56f, h - 156f);

        float progress = CurrentProgress();
        float playerHp = CurrentPlayerHp(progress);
        float enemyHp = CurrentEnemyHp(progress);
        Rect playerHome = CharacterRect(arena, true);
        Rect enemyHome = CharacterRect(arena, false);

        GUI.Label(new Rect(x + 16f, y + 14f, w - 32f, 30f), _stage.StageName, _titleStyle);
        GUI.Label(new Rect(x + 16f, y + 40f, w - 32f, 22f), RoundText(), _roundStyle);
        DrawSpeedButtons(new Rect(x + 28f, y + 42f, 214f, 26f));

        DrawHpBlock(new Rect(playerHome.x, arena.y, playerHome.width, 48f), "Player", playerHp, _result.PlayerMaxHp, _playerHpTex);
        DrawHpBlock(new Rect(enemyHome.x, arena.y, enemyHome.width, 48f), "Enemy", enemyHp, _result.EnemyMaxHp, _enemyHpTex);

        CombatLogEntry current = CurrentEvent();
        if (current != null)
        {
            Rect playerRect = playerHome;
            Rect enemyRect = enemyHome;
            Rect playerPetRect = PetRect(playerHome, true);
            Rect enemyPetRect = PetRect(enemyHome, false);
            DrawEventText(current, progress, playerRect, enemyRect, playerPetRect, enemyPetRect);
        }

        if (_showResult)
        {
            DrawResult(x + 28f, y + h - 66f, w - 56f);
        }
        else if (!_playingEnemyDeath && GUI.Button(new Rect(x + w - 126f, y + h - 50f, 98f, 32f), "Skip", _buttonStyle))
        {
            SkipToResult();
        }
    }

    void InitStyles()
    {
        if (_panelTex != null) return;

        ResolveUnitSprites();
        ResolveVFXSettings();

        _panelTex = MakeTex(new Color(0.05f, 0.06f, 0.07f, 0.94f));
        _barBgTex = MakeTex(new Color(0.12f, 0.12f, 0.12f, 1f));
        _playerHpTex = MakeTex(new Color(0.16f, 0.72f, 0.32f, 1f));
        _enemyHpTex = MakeTex(new Color(0.86f, 0.22f, 0.18f, 1f));
        _buffTex = MakeTex(new Color(0.95f, 0.72f, 0.12f, 1f));
        _healGlowTex = MakeRadialTex(96, 0.18f);
        _healSparkTex = MakeRadialTex(32, 0.42f);
        _healCrossTex = MakeCrossTex(32);
        _stunBuffSheet = Resources.Load<Texture2D>("Sprites/Buff/Buff_Stun");
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

        _roundStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
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

    }

    void ResolveVFXSettings()
    {
        if (healVFXSettings == null)
            healVFXSettings = CombatHealVFXSettings.LoadDefault();
    }

    void EnsureSceneView()
    {
        if (_sceneView != null) return;

        _sceneView = new CombatSceneBattleView(this);
        _sceneView.SkipRequested += SkipToResult;
        _sceneView.ReturnRequested += CloseAndApply;
        _sceneView.SpeedRequested += SetBattleSpeed;

        _overlayCanvas = new CombatBattleOverlayCanvas();
        _overlayCanvas.SkipRequested += SkipToResult;
        _overlayCanvas.ReturnRequested += CloseAndApply;
        _overlayCanvas.SpeedRequested += SetBattleSpeed;
    }

    void PushSceneViewState()
    {
        if (_sceneView == null || _stage == null || _result == null) return;

        float progress = CurrentProgress();
        _sceneView.UpdateView(
            CurrentEvent(),
            _eventIndex,
            progress,
            _showResult,
            _playingEnemyDeath,
            DeathProgress(),
            CurrentRound(),
            CurrentPlayerHp(progress),
            CurrentEnemyHp(progress));

        _overlayCanvas?.UpdateView(
            CurrentEvent(),
            _eventIndex,
            progress,
            _showResult,
            _playingEnemyDeath,
            CurrentRound(),
            CurrentPlayerHp(progress),
            CurrentEnemyHp(progress));
    }

    void SkipToResult()
    {
        if (_result == null || _showResult) return;

        _eventIndex = _result.Logs.Count;
        if (_result.PlayerWon && _monsterAnimations != null && _monsterAnimations.HasDeath)
        {
            _playingEnemyDeath = true;
            _deathStartTime = Time.unscaledTime;
        }
        else
        {
            _showResult = true;
        }
    }

    void SetBattleSpeed(float speed)
    {
        _battleSpeed = Mathf.Max(0.01f, speed);
        _sceneView?.SetBattleSpeed(_battleSpeed);
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
        Rect playerPetHome = PetRect(playerHome, true);
        Rect enemyPetHome = PetRect(enemyHome, false);
        Rect playerRect = playerHome;
        Rect enemyRect = enemyHome;
        Rect playerPetRect = playerPetHome;
        Rect enemyPetRect = enemyPetHome;

        if (current != null && current.Type == CombatEventType.Attack)
        {
            float move = AttackOffset(progress);
            float distance = arena.width * 0.26f;

            if (current.ActorIsPlayer)
                playerRect.x += distance * move;
            else
                enemyRect.x -= distance * move;
        }
        else if (current != null && current.Type == CombatEventType.PetSkill)
        {
            float move = AttackOffset(progress);
            float distance = arena.width * 0.18f;

            if (PetShouldMoveForSkill(current))
            {
                if (current.PetActorIsPlayer)
                    playerPetRect.x += distance * move;
                else
                    enemyPetRect.x -= distance * move;
            }
        }

        DrawHpBlock(new Rect(playerHome.x, arena.y, playerHome.width, 48f), "Player", playerHp, _result.PlayerMaxHp, _playerHpTex);
        DrawHpBlock(new Rect(enemyHome.x, arena.y, enemyHome.width, 48f), "Enemy", enemyHp, _result.EnemyMaxHp, _enemyHpTex);

        DrawPetCharacter(playerPetRect, _playerPetAnimations, current, progress, true);
        DrawPetCharacter(enemyPetRect, _enemyPetAnimations, current, progress, false);
        DrawPlayerCharacter(playerRect, current, progress);
        DrawEnemyCharacter(enemyRect, current, progress);

        bool playerStunned = current != null ? current.PlayerStunned : LastPlayerStunned();
        bool enemyStunned = current != null ? current.EnemyStunned : LastEnemyStunned();
        if (playerStunned) DrawStunBuff(playerRect);
        if (enemyStunned) DrawStunBuff(enemyRect);

        if (current != null)
        {
            DrawHealVFX(current, progress, playerRect, enemyRect);
            DrawEventText(current, progress, playerRect, enemyRect, playerPetRect, enemyPetRect);
        }
    }

    Rect CharacterRect(Rect arena, bool player)
    {
        float size = Mathf.Min(arena.height * 0.68f, arena.width * 0.34f);
        float y = arena.y + arena.height * 0.28f;
        if (player)
            y += size * PlayerBaselineYOffset;

        float x = player ? arena.x + arena.width * 0.08f : arena.xMax - arena.width * 0.08f - size;
        return new Rect(x, y, size, size);
    }

    Rect PetRect(Rect ownerRect, bool player)
    {
        float size = ownerRect.width / 3f;
        float x = player ? ownerRect.x - size * 0.65f : ownerRect.xMax - size * 0.35f;
        float y = ownerRect.y + ownerRect.height * 0.58f - size * 0.5f;
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

    void DrawSpeedButtons(Rect rect)
    {
        float[] speeds = { 0.2f, 0.5f, 1f, 2f };
        float buttonWidth = rect.width / speeds.Length;

        for (int i = 0; i < speeds.Length; i++)
        {
            float speed = speeds[i];
            Rect buttonRect = new Rect(rect.x + buttonWidth * i, rect.y, buttonWidth - 4f, rect.height);
            bool selected = Mathf.Approximately(_battleSpeed, speed);
            string label = speed.ToString("0.##") + "x";

            Color oldColor = GUI.color;
            if (selected)
                GUI.color = new Color(0.7f, 0.9f, 1f, 1f);

            if (GUI.Button(buttonRect, label, _buttonStyle))
                _battleSpeed = speed;

            GUI.color = oldColor;
        }
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
                if (progress <= AttackReturnTime)
                    DrawPlayerSheetFrame(rect, playerAttackSheet, AnimationFrame(progress), false);
                else
                    DrawPlayerIdle(rect);
                return;
            }

            if (current.TargetIsPlayer && !current.Dodged)
            {
                DrawPlayerHurt(rect, progress);
                return;
            }
        }

        DrawPlayerIdle(rect);
    }

    void DrawPlayerIdle(Rect rect)
    {
        DrawCharacter(OffsetRectY(rect, rect.height * PlayerIdleYOffset), playerSprite, false);
    }

    void DrawPlayerHurt(Rect rect, float progress)
    {
        float pulse = HurtPulse(progress);
        Rect hurtRect = HurtRect(OffsetRectY(rect, rect.height * PlayerIdleYOffset), pulse);
        Color oldColor = GUI.color;
        GUI.color = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.42f, 1f), pulse);
        DrawCharacter(hurtRect, playerSprite, false);
        GUI.color = oldColor;
    }

    void DrawEnemyCharacter(Rect rect, CombatLogEntry current, float progress)
    {
        if (_monsterAnimations == null)
        {
            DrawCharacter(rect, enemySprite, false);
            return;
        }

        if (_playingEnemyDeath)
        {
            DrawAnimationFrame(rect, _monsterAnimations.Die, DeathFrame(), false);
            return;
        }

        if (current != null && current.Type == CombatEventType.Attack && !current.ActorIsPlayer)
        {
            DrawAnimationFrame(rect, _monsterAnimations.Attack, ProgressFrame(_monsterAnimations.Attack, progress), false);
            return;
        }

        if (EnemyShouldShowHurt(current))
        {
            DrawEnemyHurt(rect, progress);
            return;
        }

        DrawEnemyIdle(rect);
    }

    bool EnemyShouldShowHurt(CombatLogEntry current)
    {
        if (current == null || current.TargetIsPlayer || current.Dodged)
            return false;
        if (current.Type == CombatEventType.Attack)
            return true;
        return current.Type == CombatEventType.PetSkill && current.Damage > 0f;
    }

    void DrawEnemyIdle(Rect rect)
    {
        DrawAnimationFrame(rect, _monsterAnimations.Idle, LoopFrame(_monsterAnimations.Idle, EnemyIdleFps), false);
    }

    void DrawEnemyHurt(Rect rect, float progress)
    {
        float pulse = HurtPulse(progress);
        Rect hurtRect = HurtRect(rect, pulse);
        Color oldColor = GUI.color;
        GUI.color = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.42f, 1f), pulse);
        DrawEnemyIdle(hurtRect);
        GUI.color = oldColor;
    }

    float HurtPulse(float progress)
    {
        return 1f - Mathf.Clamp01(Mathf.Abs(Mathf.Clamp01(progress) - 0.18f) / 0.18f);
    }

    Rect HurtRect(Rect rect, float pulse)
    {
        return new Rect(
            rect.x - rect.width * pulse * 0.02f,
            rect.y + rect.height * pulse * 0.03f,
            rect.width * (1f + pulse * 0.04f),
            rect.height * (1f - pulse * 0.06f));
    }

    void DrawPetCharacter(Rect rect, PetAnimationSet animations, CombatLogEntry current, float progress, bool playerSide)
    {
        if (animations == null)
            return;

        bool attacking = current != null
            && current.Type == CombatEventType.PetSkill
            && current.PetActorIsPlayer == playerSide;
        bool flip = playerSide;

        if (attacking)
        {
            DrawPetAnimationFrame(rect, animations, animations.Attack, ProgressFrame(animations.Attack, progress), flip);
            return;
        }

        DrawPetAnimationFrame(rect, animations, animations.Idle, LoopFrame(animations.Idle, PetIdleFps), flip);
    }

    void DrawPetAnimationFrame(Rect rect, PetAnimationSet animations, Sprite[] frames, int frameIndex, bool flip)
    {
        if (frames == null || frames.Length == 0)
        {
            if (animations.Idle != null && animations.Idle.Length > 0)
                DrawSprite(rect, animations.Idle[0], flip);
            return;
        }

        DrawSprite(rect, frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)], flip);
    }

    bool PetShouldMoveForSkill(CombatLogEntry entry)
    {
        return entry != null && entry.PetId != 102;
    }

    int DeathFrame()
    {
        if (_monsterAnimations == null || _monsterAnimations.Die == null || _monsterAnimations.Die.Length == 0)
            return 0;

        float progress = Mathf.Clamp01((Time.unscaledTime - _deathStartTime) / ScaledDuration(EnemyDeathDuration));
        return ProgressFrame(_monsterAnimations.Die, progress);
    }

    float DeathProgress()
    {
        if (!_playingEnemyDeath) return 0f;
        return Mathf.Clamp01((Time.unscaledTime - _deathStartTime) / ScaledDuration(EnemyDeathDuration));
    }

    int ProgressFrame(Sprite[] frames, float progress)
    {
        int count = frames != null ? frames.Length : 0;
        if (count <= 1) return 0;
        return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * count), 0, count - 1);
    }

    int LoopFrame(Sprite[] frames, float fps)
    {
        int count = frames != null ? frames.Length : 0;
        if (count <= 1) return 0;
        return Mathf.FloorToInt(Time.unscaledTime * Mathf.Max(1f, fps)) % count;
    }

    void DrawAnimationFrame(Rect rect, Sprite[] frames, int frameIndex, bool flip)
    {
        if (frames == null || frames.Length == 0)
        {
            DrawCharacter(rect, enemySprite, flip);
            return;
        }

        DrawSprite(rect, frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)], flip);
    }

    void DrawSprite(Rect rect, Sprite sprite, bool flip)
    {
        if (sprite == null || sprite.texture == null)
        {
            GUI.Box(rect, GUIContent.none);
            return;
        }

        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);
        Rect drawRect = FitRectToAspect(rect, textureRect.width / textureRect.height);

        if (!flip)
        {
            GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
            return;
        }

        Matrix4x4 old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), drawRect.center);
        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
        GUI.matrix = old;
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
        Rect drawRect = FitRectToAspect(rect, (sheet.texture.width / (float)cols) / (sheet.texture.height / (float)rows));

        if (!flip)
        {
            GUI.DrawTextureWithTexCoords(drawRect, sheet.texture, uv, true);
            return;
        }

        Matrix4x4 old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), drawRect.center);
        GUI.DrawTextureWithTexCoords(drawRect, sheet.texture, uv, true);
        GUI.matrix = old;
    }

    void DrawPlayerSheetFrame(Rect rect, Sprite sheet, int frameIndex, bool flip)
    {
        DrawSheetFrame(ScaleRectFromBottom(rect, PlayerSheetFrameScale), sheet, frameIndex, flip);
    }

    Rect FitRectToAspect(Rect rect, float aspect)
    {
        if (aspect <= 0f) return rect;

        float rectAspect = rect.width / rect.height;
        if (rectAspect > aspect)
        {
            float width = rect.height * aspect;
            return new Rect(rect.center.x - width * 0.5f, rect.y, width, rect.height);
        }

        float height = rect.width / aspect;
        return new Rect(rect.x, rect.center.y - height * 0.5f, rect.width, height);
    }

    Rect ScaleRectFromBottom(Rect rect, float scale)
    {
        scale = Mathf.Max(0.01f, scale);
        return new Rect(
            rect.center.x - rect.width * scale * 0.5f,
            rect.yMax - rect.height * scale,
            rect.width * scale,
            rect.height * scale);
    }

    Rect OffsetRectY(Rect rect, float yOffset)
    {
        rect.y += yOffset;
        return rect;
    }

    int AnimationFrame(float progress)
    {
        int maxFrames = Mathf.Max(1, Mathf.Min(animationFrameCount, Mathf.Max(1, animationColumns) * Mathf.Max(1, animationRows)));
        return Mathf.Clamp(Mathf.FloorToInt(progress * maxFrames), 0, maxFrames - 1);
    }

    void DrawStunBuff(Rect characterRect)
    {
        Rect icon = new Rect(characterRect.center.x - 16f, characterRect.y - 18f, 32f, 32f);
        if (_stunBuffSheet == null)
        {
            GUI.DrawTexture(icon, _buffTex);
            return;
        }

        int frame = Mathf.FloorToInt(Time.unscaledTime * StunBuffFps) % StunBuffColumns;
        Rect uv = new Rect(frame / (float)StunBuffColumns, 0f, 1f / StunBuffColumns, 1f);
        GUI.DrawTextureWithTexCoords(icon, _stunBuffSheet, uv, true);
    }

    void DrawHealVFX(CombatLogEntry entry, float progress, Rect playerRect, Rect enemyRect)
    {
        if (entry == null || entry.Type != CombatEventType.PetSkill || entry.Heal <= 0f)
            return;

        CombatHealVFXSettings vfx = healVFXSettings != null ? healVFXSettings : CombatHealVFXSettings.LoadDefault();
        float t = Mathf.Clamp01((progress - AttackReachTime - vfx.startDelayAfterImpact) / vfx.duration);
        if (t <= 0f || t >= 1f)
            return;

        Rect target = HealTargetRect(entry, playerRect, enemyRect);
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / vfx.fadeInPortion));
        float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - vfx.fadeOutStart) / (1f - vfx.fadeOutStart)));
        float alpha = fadeIn * fadeOut;

        Color oldColor = GUI.color;
        Vector2 center = new Vector2(target.center.x, target.y + target.height * vfx.centerY);
        float pulse = 1f + Mathf.Sin(t * Mathf.PI * vfx.pulseFrequency) * vfx.pulseStrength;

        GUI.color = WithAlpha(vfx.outerGlowColor, vfx.outerGlowColor.a * alpha);
        DrawCenteredTexture(_healGlowTex, center, target.width * vfx.outerGlowWidth * pulse, target.height * vfx.outerGlowHeight * pulse);

        GUI.color = WithAlpha(vfx.innerGlowColor, vfx.innerGlowColor.a * alpha);
        DrawCenteredTexture(_healGlowTex, center + new Vector2(0f, target.height * vfx.innerGlowYOffset), target.width * vfx.innerGlowWidth, target.height * vfx.innerGlowHeight);

        for (int i = 0; i < vfx.risingParticleCount; i++)
        {
            float seed = Mathf.Repeat((_eventIndex + 1) * 0.173f + i * 0.317f, 1f);
            float life = Mathf.Repeat(t + seed, 1f);
            float side = Mathf.Sin((seed * 11.7f + t * 1.6f) * Mathf.PI * 2f);
            float x = center.x + side * target.width * (vfx.risingMinSideOffset + seed * vfx.risingSideOffsetRange);
            float y = target.yMax - target.height * (vfx.risingBottomOffset + life * vfx.risingTravelHeight);
            float size = Mathf.Lerp(target.width * vfx.risingMinSize, target.width * vfx.risingMaxSize, seed);
            float particleAlpha = alpha * Mathf.Sin(life * Mathf.PI);

            GUI.color = WithAlpha(vfx.risingParticleColor, vfx.risingParticleColor.a * particleAlpha);
            DrawCenteredTexture(i % vfx.crossEvery == 0 ? _healCrossTex : _healSparkTex, new Vector2(x, y), size, size);
        }

        for (int i = 0; i < vfx.orbitSparkCount; i++)
        {
            float orbit = t * Mathf.PI * 2f * vfx.orbitSpeed + i * Mathf.PI * 2f / Mathf.Max(1, vfx.orbitSparkCount);
            float radiusX = target.width * (vfx.orbitRadiusX + vfx.orbitRadiusXPulse * Mathf.Sin(t * Mathf.PI));
            float radiusY = target.height * vfx.orbitRadiusY;
            Vector2 pos = center + new Vector2(Mathf.Cos(orbit) * radiusX, Mathf.Sin(orbit) * radiusY);
            float size = target.width * vfx.orbitSparkSize;

            GUI.color = WithAlpha(vfx.orbitSparkColor, vfx.orbitSparkColor.a * alpha);
            DrawCenteredTexture(_healSparkTex, pos, size, size);
        }

        GUI.color = oldColor;
    }

    void DrawCenteredTexture(Texture2D tex, Vector2 center, float width, float height)
    {
        if (tex == null) return;
        GUI.DrawTexture(new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height), tex);
    }

    Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    void DrawEventText(CombatLogEntry entry, float progress, Rect playerRect, Rect enemyRect, Rect playerPetRect, Rect enemyPetRect)
    {
        if ((entry.Type == CombatEventType.Attack || entry.Type == CombatEventType.PetSkill) && progress < AttackReachTime)
            return;

        Rect actor = entry.ActorIsPlayer ? playerRect : enemyRect;
        Rect target = entry.TargetIsPlayer ? playerRect : enemyRect;
        if (entry.Type == CombatEventType.PetSkill)
            actor = entry.PetActorIsPlayer ? playerPetRect : enemyPetRect;
        float lift = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * 34f;
        float alpha = 1f - Mathf.Clamp01((progress - 0.62f) / 0.38f);

        if (entry.Type == CombatEventType.Popup)
        {
            string text = entry.Combo ? "连击" : LocalizePopupText(entry.Text);
            DrawFloatingText(text, actor.center.x, actor.y - lift, alpha, _popupStyle);
            return;
        }

        if (entry.Combo)
            DrawFloatingText("连击", actor.center.x, actor.y - 28f - lift, alpha, _popupStyle);

        if (entry.Counter)
            DrawFloatingText("反击", actor.center.x, actor.y - 28f - lift, alpha, _popupStyle);

        if (entry.Dodged)
            DrawFloatingText("闪避", target.center.x, target.y - lift, alpha, _popupStyle);

        if (entry.Crit)
            DrawFloatingText("暴击", target.center.x, target.y + 18f - lift, alpha, _popupStyle);

        if (entry.Stunned)
            DrawFloatingText("击晕", target.center.x, target.y + 46f - lift, alpha, _popupStyle);

        if (entry.Damage > 0f)
            DrawFloatingText("-" + entry.Damage.ToString("0"), target.center.x, target.y + target.height * 0.38f - lift, alpha, _damageStyle);

        if (entry.Heal > 0f)
        {
            Rect healTarget = HealTargetRect(entry, playerRect, enemyRect);
            DrawFloatingText("+" + entry.Heal.ToString("0"), healTarget.center.x, healTarget.y + healTarget.height * 0.32f - lift, alpha, _healStyle);
        }

        if (entry.BuffValue > 0f)
        {
            Rect buffTarget = entry.ActorIsPlayer ? playerRect : enemyRect;
            DrawFloatingText(LocalizeAttrName(entry.BuffAttrName), buffTarget.center.x, buffTarget.y + buffTarget.height * 0.1f - lift, alpha, _popupStyle);
        }
    }

    string LocalizePopupText(string text)
    {
        switch (text)
        {
            case "Combo": return "连击";
            case "Counter": return "反击";
            case "Stunned": return "击晕";
            default: return text;
        }
    }

    Rect HealTargetRect(CombatLogEntry entry, Rect playerRect, Rect enemyRect)
    {
        if (entry != null && entry.Type == CombatEventType.PetSkill)
            return entry.PetActorIsPlayer ? playerRect : enemyRect;

        return entry != null && entry.ActorIsPlayer ? playerRect : enemyRect;
    }

    string LocalizeAttrName(string attrName)
    {
        switch (attrName)
        {
            case "Attack": return "攻击";
            case "Defence": return "防御";
            case "Hp": return "生命";
            case "Agility": return "敏捷";
            case "CritRate": return "暴击";
            case "CounterRate": return "反击";
            case "ComboRate": return "连击";
            case "DodgeRate": return "闪避";
            case "StunRate": return "击晕";
            case "LifeStealRate": return "吸血";
            case "DamageIncrease": return "最终增伤";
            case "DamageDecrease": return "最终减伤";
            case "Healing": return "强化治疗";
            case "PetIncrease": return "强化宠物";
            default: return string.IsNullOrEmpty(attrName) ? "强化" : attrName;
        }
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
        _monsterAnimations = null;
        _playerPetAnimations = null;
        _enemyPetAnimations = null;
        _sceneView?.Close();
        _overlayCanvas?.Close();
    }

    CombatLogEntry CurrentEvent()
    {
        if (_result == null || _eventIndex < 0 || _eventIndex >= _result.Logs.Count) return null;
        return _result.Logs[_eventIndex];
    }

    string RoundText()
    {
        int maxRound = _stage != null ? Mathf.Max(1, _stage.MaxRound) : 1;
        int round = CurrentRound();
        return "Round " + round + "/" + maxRound;
    }

    int CurrentRound()
    {
        if (_result == null) return 1;

        if (_showResult || _playingEnemyDeath)
            return Mathf.Clamp(_result.LastRound, 1, _stage != null ? Mathf.Max(1, _stage.MaxRound) : _result.LastRound);

        CombatLogEntry current = CurrentEvent();
        if (current != null)
            return Mathf.Clamp(current.Round, 1, _stage != null ? Mathf.Max(1, _stage.MaxRound) : current.Round);

        return 1;
    }

    float CurrentProgress()
    {
        return Mathf.Clamp01((Time.unscaledTime - _eventStartTime) / ScaledDuration(EventDuration));
    }

    float CurrentPlayerHp(float progress)
    {
        if (_playingEnemyDeath) return _result.PlayerHp;
        if (_showResult) return _result.PlayerHp;
        CombatLogEntry current = CurrentEvent();
        if (current != null && (current.Type == CombatEventType.Attack || current.Type == CombatEventType.PetSkill) && progress < AttackReachTime)
            return PreviousPlayerHp();
        return current != null ? current.PlayerHp : _result.PlayerMaxHp;
    }

    float CurrentEnemyHp(float progress)
    {
        if (_playingEnemyDeath) return _result.EnemyHp;
        if (_showResult) return _result.EnemyHp;
        CombatLogEntry current = CurrentEvent();
        if (current != null && (current.Type == CombatEventType.Attack || current.Type == CombatEventType.PetSkill) && progress < AttackReachTime)
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

    float ScaledDuration(float duration)
    {
        return duration * BaseSpeedScale / Mathf.Max(0.01f, _battleSpeed);
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

    Texture2D MakeRadialTex(int size, float hardness)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        float radius = Mathf.Max(1f, center);
        hardness = Mathf.Clamp01(hardness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, Mathf.Lerp(3.2f, 0.8f, hardness));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    Texture2D MakeCrossTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        float arm = size * 0.12f;
        float length = size * 0.34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                bool inside = (dx <= arm && dy <= length) || (dy <= arm && dx <= length);
                float edge = Mathf.Min(Mathf.Abs(dx - arm), Mathf.Abs(dy - arm));
                float alpha = inside ? Mathf.Clamp01(0.85f + edge / Mathf.Max(1f, arm) * 0.15f) : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    sealed class MonsterAnimationSet
    {
        const string ResourceRoot = "Sprites/Monster";
        static readonly Dictionary<string, MonsterAnimationSet> Cache = new Dictionary<string, MonsterAnimationSet>();

        public Sprite[] Idle;
        public Sprite[] Attack;
        public Sprite[] Die;
        public bool HasDeath => Die != null && Die.Length > 0;

        public static MonsterAnimationSet Load(string avatar)
        {
            string id = NormalizeAvatarId(avatar);
            if (string.IsNullOrEmpty(id)) id = "1001";

            if (Cache.TryGetValue(id, out MonsterAnimationSet cached))
                return cached;

            string folderName = "Monster_Boss_" + id;
            string path = ResourceRoot + "/" + folderName;
            Sprite[] allFrames = Resources.LoadAll<Sprite>(path);
            if (allFrames == null || allFrames.Length == 0)
            {
                Debug.LogWarning("[CombatWindowUI] Monster animation frames not found: " + path);
                Cache[id] = null;
                return null;
            }

            var set = new MonsterAnimationSet
            {
                Idle = FilterFrames(allFrames, folderName + "-Idle_"),
                Attack = FilterFrames(allFrames, folderName + "-Attack_"),
                Die = FilterFrames(allFrames, folderName + "-Die_"),
            };

            Cache[id] = set;
            return set;
        }

        static Sprite[] FilterFrames(Sprite[] frames, string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite sprite = frames[i];
                if (sprite != null && sprite.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    list.Add(sprite);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }

        static string NormalizeAvatarId(string avatar)
        {
            if (string.IsNullOrWhiteSpace(avatar)) return "";

            string digits = "";
            for (int i = 0; i < avatar.Length; i++)
            {
                if (char.IsDigit(avatar[i]))
                    digits += avatar[i];
            }

            if (digits.Length <= 4) return digits;
            return digits.Substring(digits.Length - 4);
        }
    }

    sealed class PetAnimationSet
    {
        const string ResourceRoot = "Sprites/Pets";
        static readonly Dictionary<string, PetAnimationSet> Cache = new Dictionary<string, PetAnimationSet>();

        public Sprite[] Idle;
        public Sprite[] Attack;

        public static PetAnimationSet Load(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) return null;

            string id = resource.Trim();
            if (Cache.TryGetValue(id, out PetAnimationSet cached))
                return cached;

            string path = ResourceRoot + "/" + id;
            Sprite[] allFrames = Resources.LoadAll<Sprite>(path);
            if (allFrames == null || allFrames.Length == 0)
            {
                Debug.LogWarning("[CombatWindowUI] Pet animation frames not found: " + path);
                Cache[id] = null;
                return null;
            }

            var set = new PetAnimationSet
            {
                Idle = FilterFrames(allFrames, id + "_Idle_"),
                Attack = FilterFrames(allFrames, id + "_Attack_"),
            };

            Cache[id] = set;
            return set;
        }

        static Sprite[] FilterFrames(Sprite[] frames, string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite sprite = frames[i];
                if (sprite != null && sprite.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    list.Add(sprite);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }
    }
}
