using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CombatSceneBattleView
{
    const bool ShowSceneHud = false;
    const float AttackReachTime = 0.18f;
    const float AttackStrikeEndTime = 0.45f;
    const float AttackReturnTime = 0.58f;
    const float EnemyIdleFps = 16f;
    const float PetIdleFps = 12f;

    readonly MonoBehaviour _owner;
    readonly Camera _camera;
    readonly Sprite _rectSprite;
    readonly GameObject _root;
    readonly Transform _unitLayer;
    readonly Transform _vfxLayer;
    readonly SpriteRenderer _panel;
    readonly SpriteRenderer _playerHpBg;
    readonly SpriteRenderer _playerHpFill;
    readonly SpriteRenderer _enemyHpBg;
    readonly SpriteRenderer _enemyHpFill;
    readonly TextMeshPro _titleText;
    readonly TextMeshPro _roundText;
    readonly TextMeshPro _playerHpText;
    readonly TextMeshPro _enemyHpText;
    readonly TextMeshPro _resultText;
    readonly SceneButton _skipButton;
    readonly SceneButton _returnButton;
    readonly SceneButton[] _speedButtons;
    readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
    readonly List<Sprite> _runtimeSprites = new List<Sprite>();
    readonly CombatBattleLayoutSettings _layout;

    CombatSceneUnitView _player;
    CombatSceneUnitView _enemy;
    CombatSceneUnitView _playerPet;
    CombatSceneUnitView _enemyPet;
    GameObject _healVFXPrefab;
    CombatLogEntry _lastEventWithVFX;
    StageData _stage;
    CombatResult _result;
    float _battleSpeed = 1f;
    int _lastPopupEventIndex = -1;
    int _layoutW;
    int _layoutH;

    public Action SkipRequested;
    public Action ReturnRequested;
    public Action<float> SpeedRequested;

    public CombatSceneBattleView(MonoBehaviour owner)
    {
        _owner = owner;
        _camera = Camera.main;
        _rectSprite = CreateRectSprite();
        _healVFXPrefab = Resources.Load<GameObject>("VFX/PetHeal");
        _layout = CombatBattleLayoutSettings.LoadDefault();

        _root = new GameObject("CombatScenePopup");
        _root.SetActive(false);
        _unitLayer = NewChild(_root.transform, "Units");
        _vfxLayer = NewChild(_root.transform, "VFX");

        _panel = NewRect("Panel", _root.transform, new Color(0.05f, 0.06f, 0.07f, 0.94f), 200);
        _titleText = NewText("Title", _root.transform, 0.2f, FontStyles.Bold, TextAlignmentOptions.Center, 260);
        _roundText = NewText("Round", _root.transform, 0.14f, FontStyles.Bold, TextAlignmentOptions.Center, 260);
        _playerHpText = NewText("PlayerHp", _root.transform, 0.13f, FontStyles.Bold, TextAlignmentOptions.Left, 260);
        _enemyHpText = NewText("EnemyHp", _root.transform, 0.13f, FontStyles.Bold, TextAlignmentOptions.Left, 260);
        _resultText = NewText("Result", _root.transform, 0.15f, FontStyles.Bold, TextAlignmentOptions.Left, 260);

        _playerHpBg = NewRect("PlayerHpBg", _root.transform, new Color(0.12f, 0.12f, 0.12f, 1f), 210);
        _playerHpFill = NewRect("PlayerHpFill", _root.transform, new Color(0.16f, 0.72f, 0.32f, 1f), 211);
        _enemyHpBg = NewRect("EnemyHpBg", _root.transform, new Color(0.12f, 0.12f, 0.12f, 1f), 210);
        _enemyHpFill = NewRect("EnemyHpFill", _root.transform, new Color(0.86f, 0.22f, 0.18f, 1f), 211);

        _skipButton = new SceneButton("SkipButton", _root.transform, _rectSprite, "Skip", 0.13f, 250);
        _returnButton = new SceneButton("ReturnButton", _root.transform, _rectSprite, "Return", 0.13f, 250);
        _speedButtons = new[]
        {
            new SceneButton("Speed02Button", _root.transform, _rectSprite, "0.2x", 0.1f, 250),
            new SceneButton("Speed05Button", _root.transform, _rectSprite, "0.5x", 0.1f, 250),
            new SceneButton("Speed10Button", _root.transform, _rectSprite, "1x", 0.1f, 250),
            new SceneButton("Speed20Button", _root.transform, _rectSprite, "2x", 0.1f, 250),
        };

        SetSceneHudVisible(false);
    }

    public void Open(StageData stage, CombatResult result)
    {
        _stage = stage;
        _result = result;
        _lastEventWithVFX = null;
        _lastPopupEventIndex = -1;

        _root.SetActive(true);
        BuildUnits();
        Relayout(true);
    }

    public void Close()
    {
        _stage = null;
        _result = null;
        ClearFloatingTexts();
        _root.SetActive(false);
    }

    public void SetBattleSpeed(float speed)
    {
        _battleSpeed = Mathf.Max(0.01f, speed);
    }

    public void UpdateView(
        CombatLogEntry current,
        int eventIndex,
        float progress,
        bool showResult,
        bool playingEnemyDeath,
        float deathProgress,
        int round,
        float playerHp,
        float enemyHp)
    {
        if (!_root.activeSelf || _result == null || _stage == null) return;

        Relayout(false);
        HandleInput(showResult);
        UpdateTexts(showResult, round, playerHp, enemyHp);
        UpdateUnits(current, progress, playingEnemyDeath, deathProgress);
        UpdateHpBars(playerHp, enemyHp);
        UpdateFloatingTexts();

        if (current != null && eventIndex != _lastPopupEventIndex && progress >= AttackReachTime)
        {
            SpawnEventText(current);
            _lastPopupEventIndex = eventIndex;
        }

        if (current != null && current.Type == CombatEventType.PetSkill && current.Heal > 0f && progress >= AttackReachTime && _lastEventWithVFX != current)
        {
            SpawnHealVFX(current.ActorIsPlayer ? _player : _enemy);
            _lastEventWithVFX = current;
        }

        if (ShowSceneHud)
        {
            _skipButton.SetVisible(!showResult && !playingEnemyDeath);
            _returnButton.SetVisible(showResult);
            _resultText.gameObject.SetActive(showResult);
        }
    }

    void BuildUnits()
    {
        DestroyChildren(_unitLayer);
        DestroyChildren(_vfxLayer);

        Rect arena = ArenaRect();
        float unitW = arena.width * _layout.unitWidthRatio;
        float unitH = arena.height * _layout.unitHeightRatio;
        float petW = unitW * _layout.petSizeRatio;
        float petH = unitH * _layout.petSizeRatio;
        Rect playerHome = PlayerHome(arena);
        Rect enemyHome = EnemyHome(arena);

        _player = new CombatSceneUnitView("Player", _unitLayer, 230, playerHome.width, playerHome.height, false, true);
        _enemy = new CombatSceneUnitView("Enemy", _unitLayer, 230, enemyHome.width, enemyHome.height, false);
        _playerPet = new CombatSceneUnitView("PlayerPet", _unitLayer, 225, petW, petH, true);
        _enemyPet = new CombatSceneUnitView("EnemyPet", _unitLayer, 225, petW, petH, false);

        Sprite playerSprite = Resources.Load<Sprite>("Sprites/Knight");
        Sprite playerAttackSheet = Resources.Load<Sprite>("Sprites/knight_attack");
        _player.SetFrames(playerSprite, new[] { playerSprite }, SheetSprites(playerAttackSheet, 4, 1, 4), null, null);

        MonsterAnimationSet monster = MonsterAnimationSet.Load(_stage != null ? _stage.MonsterAvatar : "");
        Sprite enemyFallback = Resources.Load<Sprite>("Sprites/Slam");
        _enemy.SetFrames(enemyFallback, monster?.Idle, monster?.Attack, null, monster?.Die);

        PetAnimationSet playerPet = PetAnimationSet.Load(_result != null ? _result.PlayerPetResource : "");
        PetAnimationSet enemyPet = PetAnimationSet.Load(_result != null ? _result.EnemyPetResource : "");
        _playerPet.SetFrames(null, playerPet?.Idle, playerPet?.Attack, null, null);
        _enemyPet.SetFrames(null, enemyPet?.Idle, enemyPet?.Attack, null, null);
        _playerPet.SetVisible(playerPet != null);
        _enemyPet.SetVisible(enemyPet != null);
    }

    void Relayout(bool force)
    {
        if (_camera == null) return;
        if (!force && _layoutW == Screen.width && _layoutH == Screen.height) return;

        _layoutW = Screen.width;
        _layoutH = Screen.height;

        Rect panel = PanelRect();
        SetRect(_panel, panel.center, panel.size);

        _titleText.text = _stage != null ? _stage.StageName : "";
        PlaceText(_titleText, new Vector2(panel.center.x, panel.yMax - 0.34f), panel.width - 0.4f, 0.28f);
        PlaceText(_roundText, new Vector2(panel.center.x, panel.yMax - 0.65f), panel.width - 0.4f, 0.24f);

        Rect arena = ArenaRect();
        Rect playerHome = PlayerHome(arena);
        Rect enemyHome = EnemyHome(arena);
        Rect playerPetHome = PlayerPetHome(playerHome);
        Rect enemyPetHome = EnemyPetHome(enemyHome);

        _player?.SetHome(new Vector3(playerHome.center.x, playerHome.yMin + playerHome.height * _layout.unitPivotHeight, -0.1f));
        _enemy?.SetHome(new Vector3(enemyHome.center.x, enemyHome.yMin + enemyHome.height * _layout.unitPivotHeight, -0.1f));
        _playerPet?.SetHome(new Vector3(playerPetHome.center.x, playerPetHome.center.y, -0.08f));
        _enemyPet?.SetHome(new Vector3(enemyPetHome.center.x, enemyPetHome.center.y, -0.08f));

        PlaceText(_playerHpText, new Vector2(playerHome.xMin, arena.yMax - 0.18f), playerHome.width, 0.22f);
        PlaceText(_enemyHpText, new Vector2(enemyHome.xMin, arena.yMax - 0.18f), enemyHome.width, 0.22f);
        SetRect(_playerHpBg, new Vector2(playerHome.center.x, arena.yMax - 0.46f), new Vector2(playerHome.width, 0.16f));
        SetRect(_enemyHpBg, new Vector2(enemyHome.center.x, arena.yMax - 0.46f), new Vector2(enemyHome.width, 0.16f));

        _skipButton.SetRect(new Rect(panel.xMax - 1.32f, panel.yMin + 0.26f, 0.95f, 0.34f));
        _returnButton.SetRect(new Rect(panel.xMax - 1.45f, panel.yMin + 0.26f, 1.08f, 0.36f));
        PlaceText(_resultText, new Vector2(panel.xMin + 0.36f, panel.yMin + 0.43f), panel.width - 1.8f, 0.28f);

        float speedX = panel.xMin + 0.36f;
        float speedY = panel.yMax - 0.82f;
        for (int i = 0; i < _speedButtons.Length; i++)
            _speedButtons[i].SetRect(new Rect(speedX + i * 0.58f, speedY, 0.5f, 0.28f));

        if (!ShowSceneHud)
            SetSceneHudVisible(false);
    }

    void UpdateTexts(bool showResult, int round, float playerHp, float enemyHp)
    {
        int maxRound = _stage != null ? Mathf.Max(1, _stage.MaxRound) : 1;
        _roundText.text = "Round " + round + "/" + maxRound + "    " + _battleSpeed.ToString("0.##") + "x";
        _playerHpText.text = "Player  " + Mathf.Ceil(playerHp).ToString("0") + "/" + Mathf.Ceil(_result.PlayerMaxHp).ToString("0");
        _enemyHpText.text = "Enemy  " + Mathf.Ceil(enemyHp).ToString("0") + "/" + Mathf.Ceil(_result.EnemyMaxHp).ToString("0");
        _resultText.text = _result.PlayerWon
            ? "Victory  Reward: pet ticket +" + _stage.PetTicketReward
            : "Defeat  No reward";
    }

    void UpdateUnits(CombatLogEntry current, float progress, bool playingEnemyDeath, float deathProgress)
    {
        Rect arena = ArenaRect();
        Rect playerHome = PlayerHome(arena);
        Rect enemyHome = EnemyHome(arena);
        Rect playerPetHome = PlayerPetHome(playerHome);
        Rect enemyPetHome = EnemyPetHome(enemyHome);
        Vector3 playerPos = new Vector3(playerHome.center.x, playerHome.yMin + playerHome.height * _layout.unitPivotHeight, -0.1f);
        Vector3 enemyPos = new Vector3(enemyHome.center.x, enemyHome.yMin + enemyHome.height * _layout.unitPivotHeight, -0.1f);
        Vector3 playerPetPos = new Vector3(playerPetHome.center.x, playerPetHome.center.y, -0.08f);
        Vector3 enemyPetPos = new Vector3(enemyPetHome.center.x, enemyPetHome.center.y, -0.08f);
        bool playerAnimated = false;
        bool enemyAnimated = false;
        bool playerPetAnimated = false;
        bool enemyPetAnimated = false;

        if (current != null && current.Type == CombatEventType.Attack)
        {
            float move = AttackOffset(progress);
            float distance = arena.width * 0.26f;
            if (current.ActorIsPlayer)
            {
                playerPos.x += distance * move;
                if (IsAttackStriking(progress))
                {
                    _player.ShowAttack(AttackStrikeProgress(progress));
                    playerAnimated = true;
                }
            }
            else
            {
                enemyPos.x -= distance * move;
                if (IsAttackStriking(progress))
                {
                    _enemy.ShowAttack(AttackStrikeProgress(progress));
                    enemyAnimated = true;
                }
            }

            if (current.TargetIsPlayer && !current.Dodged)
            {
                _player.ShowHurt(progress);
                playerAnimated = true;
            }
            else if (!current.TargetIsPlayer && !current.Dodged)
            {
                _enemy.ShowHurt(progress);
                enemyAnimated = true;
            }
        }
        else if (current != null && current.Type == CombatEventType.PetSkill)
        {
            float move = AttackOffset(progress);
            float distance = arena.width * 0.18f;
            bool shouldMove = PetShouldMoveForSkill(current);
            if (current.PetActorIsPlayer)
            {
                if (shouldMove)
                    playerPetPos.x += distance * move;
                _playerPet.ShowAttack(progress);
                playerPetAnimated = true;
            }
            else
            {
                if (shouldMove)
                    enemyPetPos.x -= distance * move;
                _enemyPet.ShowAttack(progress);
                enemyPetAnimated = true;
            }

            if (current.Damage > 0f && !current.Dodged)
            {
                if (current.TargetIsPlayer)
                {
                    _player.ShowHurt(progress);
                    playerAnimated = true;
                }
                else
                {
                    _enemy.ShowHurt(progress);
                    enemyAnimated = true;
                }
            }
        }

        _player.Transform.localPosition = playerPos;
        _enemy.Transform.localPosition = enemyPos;
        _playerPet.Transform.localPosition = playerPetPos;
        _enemyPet.Transform.localPosition = enemyPetPos;

        if (playingEnemyDeath)
            _enemy.ShowDeath(deathProgress);
        else if (!enemyAnimated)
            _enemy.ShowIdle(Time.unscaledTime, EnemyIdleFps);

        if (!playerAnimated)
            _player.ShowIdle(Time.unscaledTime, 1f);

        if (!playerPetAnimated)
            _playerPet.ShowIdle(Time.unscaledTime, PetIdleFps);
        if (!enemyPetAnimated)
            _enemyPet.ShowIdle(Time.unscaledTime, PetIdleFps);
    }

    static bool PetShouldMoveForSkill(CombatLogEntry entry)
    {
        return entry != null && entry.PetId != 102;
    }

    void UpdateHpBars(float playerHp, float enemyHp)
    {
        Rect arena = ArenaRect();
        Rect playerHome = PlayerHome(arena);
        Rect enemyHome = EnemyHome(arena);
        float playerPct = _result.PlayerMaxHp > 0f ? Mathf.Clamp01(playerHp / _result.PlayerMaxHp) : 0f;
        float enemyPct = _result.EnemyMaxHp > 0f ? Mathf.Clamp01(enemyHp / _result.EnemyMaxHp) : 0f;
        SetRect(_playerHpFill, new Vector2(playerHome.xMin + playerHome.width * playerPct * 0.5f, arena.yMax - 0.46f), new Vector2(playerHome.width * playerPct, 0.16f));
        SetRect(_enemyHpFill, new Vector2(enemyHome.xMin + enemyHome.width * enemyPct * 0.5f, arena.yMax - 0.46f), new Vector2(enemyHome.width * enemyPct, 0.16f));
    }

    void HandleInput(bool showResult)
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Vector2 mouse = ScreenToWorld(Input.mousePosition);

        if (!showResult && _skipButton.Contains(mouse))
            SkipRequested?.Invoke();
        if (showResult && _returnButton.Contains(mouse))
            ReturnRequested?.Invoke();

        float[] speeds = { 0.2f, 0.5f, 1f, 2f };
        for (int i = 0; i < _speedButtons.Length; i++)
        {
            if (_speedButtons[i].Contains(mouse))
            {
                SpeedRequested?.Invoke(speeds[i]);
                break;
            }
        }
    }

    void SpawnHealVFX(CombatSceneUnitView target)
    {
        if (target == null || _healVFXPrefab == null) return;
        GameObject go = UnityEngine.Object.Instantiate(_healVFXPrefab, target.VFXAnchor.position, Quaternion.identity, _vfxLayer);
        go.transform.localScale = Vector3.one * 0.9f;
    }

    void SpawnEventText(CombatLogEntry entry)
    {
        if (entry == null) return;
        CombatSceneUnitView target = entry.TargetIsPlayer ? _player : _enemy;
        CombatSceneUnitView actor = entry.ActorIsPlayer ? _player : _enemy;
        if (entry.Type == CombatEventType.PetSkill)
            actor = entry.PetActorIsPlayer ? _playerPet : _enemyPet;

        if (entry.Damage > 0f)
            AddFloatingText("-" + entry.Damage.ToString("0"), target.Transform.position + Vector3.up * 0.55f, new Color(1f, 0.22f, 0.18f));
        if (entry.Heal > 0f)
            AddFloatingText("+" + entry.Heal.ToString("0"), actor.Transform.position + Vector3.up * 0.65f, new Color(0.25f, 1f, 0.35f));
        if (entry.Dodged)
            AddFloatingText("Dodge", target.Transform.position + Vector3.up * 0.85f, new Color(1f, 0.88f, 0.18f));
        if (entry.Crit)
            AddFloatingText("Crit", target.Transform.position + Vector3.up * 1.05f, new Color(1f, 0.88f, 0.18f));
        if (entry.Stunned)
            AddFloatingText("Stun", target.Transform.position + Vector3.up * 1.25f, new Color(1f, 0.88f, 0.18f));
    }

    void AddFloatingText(string text, Vector3 position, Color color)
    {
        TextMeshPro label = NewText("FloatingText", _root.transform, 0.32f, FontStyles.Bold, TextAlignmentOptions.Center, 270);
        label.text = text;
        label.color = color;
        label.transform.position = position;
        _floatingTexts.Add(new FloatingText(label, position, Time.unscaledTime, color));
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
            item.Text.transform.position = item.StartPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.58f;
            item.Text.color = new Color(item.Color.r, item.Color.g, item.Color.b, 1f - t);
        }
    }

    void ClearFloatingTexts()
    {
        for (int i = 0; i < _floatingTexts.Count; i++)
            if (_floatingTexts[i].Text != null)
                UnityEngine.Object.Destroy(_floatingTexts[i].Text.gameObject);
        _floatingTexts.Clear();
    }

    Rect PanelRect()
    {
        Vector2 half = CameraHalfSize();
        float width = Mathf.Min(half.x * 2f * 0.82f, _layout.panelSize.x);
        float height = Mathf.Min(half.y * 2f * 0.78f, _layout.panelSize.y);
        return RectFromCenter(Vector2.zero, new Vector2(width, height));
    }

    Rect ArenaRect()
    {
        Rect p = PanelRect();
        return new Rect(
            p.xMin + _layout.arenaPadding.x,
            p.yMin + _layout.arenaPadding.y,
            p.width - _layout.arenaPadding.x * 2f,
            p.height - _layout.arenaPadding.y - _layout.arenaBottomPadding);
    }

    Rect PlayerHome(Rect arena)
    {
        float scale = Mathf.Max(0.01f, _layout.playerUnitSizeScale);
        float width = arena.width * _layout.unitWidthRatio * scale;
        float height = arena.height * _layout.unitHeightRatio * scale;
        return new Rect(arena.xMin + arena.width * _layout.playerXRatio - width * 0.5f, arena.yMin + arena.height * _layout.unitBottomRatio, width, height);
    }

    Rect EnemyHome(Rect arena)
    {
        float scale = Mathf.Max(0.01f, _layout.enemyUnitSizeScale);
        float width = arena.width * _layout.unitWidthRatio * scale;
        float height = arena.height * _layout.unitHeightRatio * scale;
        return new Rect(arena.xMin + arena.width * _layout.enemyXRatio - width * 0.5f, arena.yMin + arena.height * _layout.unitBottomRatio, width, height);
    }

    Rect PlayerPetHome(Rect owner)
    {
        float size = owner.width * _layout.petSizeRatio;
        Vector2 center = new Vector2(owner.center.x, owner.yMin) + new Vector2(owner.width * _layout.playerPetOffset.x, owner.height * _layout.playerPetOffset.y);
        return RectFromCenter(center, new Vector2(size, size));
    }

    Rect EnemyPetHome(Rect owner)
    {
        float size = owner.width * _layout.petSizeRatio;
        Vector2 center = new Vector2(owner.center.x, owner.yMin) + new Vector2(owner.width * _layout.enemyPetOffset.x, owner.height * _layout.enemyPetOffset.y);
        return RectFromCenter(center, new Vector2(size, size));
    }

    float AttackOffset(float progress)
    {
        if (progress <= AttackReachTime)
            return Mathf.SmoothStep(0f, 1f, progress / AttackReachTime);
        if (progress <= AttackStrikeEndTime)
            return 1f;
        if (progress <= AttackReturnTime)
            return Mathf.SmoothStep(1f, 0f, (progress - AttackStrikeEndTime) / (AttackReturnTime - AttackStrikeEndTime));
        return 0f;
    }

    bool IsAttackStriking(float progress)
    {
        return progress >= AttackReachTime && progress <= AttackStrikeEndTime;
    }

    float AttackStrikeProgress(float progress)
    {
        return Mathf.Clamp01((progress - AttackReachTime) / Mathf.Max(0.01f, AttackStrikeEndTime - AttackReachTime));
    }

    Vector2 CameraHalfSize()
    {
        if (_camera == null || !_camera.orthographic)
            return new Vector2(8f, 4.5f);

        return new Vector2(_camera.orthographicSize * _camera.aspect, _camera.orthographicSize);
    }

    Vector2 ScreenToWorld(Vector3 screen)
    {
        if (_camera == null) return Vector2.zero;
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(_camera.transform.position.z)));
        return world;
    }

    Rect RectFromCenter(Vector2 center, Vector2 size)
    {
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    SpriteRenderer NewRect(string name, Transform parent, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _rectSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    TextMeshPro NewText(string name, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshPro>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.sortingOrder = sortingOrder;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    void SetRect(SpriteRenderer renderer, Vector2 center, Vector2 size)
    {
        renderer.transform.localPosition = new Vector3(center.x, center.y, 0f);
        renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    void SetSceneHudVisible(bool visible)
    {
        SetRendererVisible(_playerHpBg, visible);
        SetRendererVisible(_playerHpFill, visible);
        SetRendererVisible(_enemyHpBg, visible);
        SetRendererVisible(_enemyHpFill, visible);

        if (_titleText != null) _titleText.gameObject.SetActive(visible);
        if (_roundText != null) _roundText.gameObject.SetActive(visible);
        if (_playerHpText != null) _playerHpText.gameObject.SetActive(visible);
        if (_enemyHpText != null) _enemyHpText.gameObject.SetActive(visible);
        if (_resultText != null) _resultText.gameObject.SetActive(visible);

        _skipButton?.SetVisible(visible);
        _returnButton?.SetVisible(visible);
        if (_speedButtons != null)
        {
            for (int i = 0; i < _speedButtons.Length; i++)
                _speedButtons[i]?.SetVisible(visible);
        }
    }

    void SetRendererVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer != null)
            renderer.gameObject.SetActive(visible);
    }

    void PlaceText(TextMeshPro text, Vector2 position, float width, float height)
    {
        text.transform.localPosition = new Vector3(position.x, position.y, -0.2f);
        text.rectTransform.sizeDelta = new Vector2(width, height);
    }

    Sprite[] SheetSprites(Sprite sheet, int columns, int rows, int frameCount)
    {
        if (sheet == null || sheet.texture == null) return null;

        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        frameCount = Mathf.Max(1, Mathf.Min(frameCount, columns * rows));

        var frames = new Sprite[frameCount];
        float frameW = sheet.texture.width / (float)columns;
        float frameH = sheet.texture.height / (float)rows;

        for (int i = 0; i < frameCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Rect rect = new Rect(col * frameW, sheet.texture.height - (row + 1) * frameH, frameW, frameH);
            Sprite frame = Sprite.Create(sheet.texture, rect, new Vector2(0.5f, 0.5f), sheet.pixelsPerUnit);
            frame.name = sheet.name + "_Runtime_" + i.ToString("00");
            frames[i] = frame;
            _runtimeSprites.Add(frame);
        }

        return frames;
    }

    Sprite CreateRectSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    void DestroyChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }

    sealed class SceneButton
    {
        readonly SpriteRenderer _bg;
        readonly TextMeshPro _label;
        Rect _rect;

        public SceneButton(string name, Transform parent, Sprite sprite, string text, float fontSize, int sortingOrder)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            _bg = root.AddComponent<SpriteRenderer>();
            _bg.sprite = sprite;
            _bg.color = new Color(0.14f, 0.18f, 0.22f, 0.96f);
            _bg.sortingOrder = sortingOrder;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.text = text;
            _label.fontSize = fontSize;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.sortingOrder = sortingOrder + 1;
        }

        public void SetRect(Rect rect)
        {
            _rect = rect;
            _bg.transform.localPosition = new Vector3(rect.center.x, rect.center.y, -0.2f);
            _bg.transform.localScale = new Vector3(rect.width, rect.height, 1f);
            _label.transform.localPosition = new Vector3(rect.center.x, rect.center.y - 0.06f, -0.3f);
            _label.rectTransform.sizeDelta = rect.size;
        }

        public void SetVisible(bool visible)
        {
            _bg.gameObject.SetActive(visible);
        }

        public bool Contains(Vector2 point)
        {
            return _bg.gameObject.activeSelf && _rect.Contains(point);
        }
    }

    sealed class FloatingText
    {
        public readonly TextMeshPro Text;
        public readonly Vector3 StartPosition;
        public readonly float StartTime;
        public readonly Color Color;

        public FloatingText(TextMeshPro text, Vector3 startPosition, float startTime, Color color)
        {
            Text = text;
            StartPosition = startPosition;
            StartTime = startTime;
            Color = color;
        }
    }

    sealed class MonsterAnimationSet
    {
        const string ResourceRoot = "Sprites/Monster";
        static readonly Dictionary<string, MonsterAnimationSet> Cache = new Dictionary<string, MonsterAnimationSet>();

        public Sprite[] Idle;
        public Sprite[] Attack;
        public Sprite[] Die;

        public static MonsterAnimationSet Load(string avatar)
        {
            string id = NormalizeAvatarId(avatar);
            if (string.IsNullOrEmpty(id)) id = "1001";
            if (Cache.TryGetValue(id, out MonsterAnimationSet cached)) return cached;

            string folderName = "Monster_Boss_" + id;
            Sprite[] allFrames = Resources.LoadAll<Sprite>(ResourceRoot + "/" + folderName);
            if (allFrames == null || allFrames.Length == 0)
            {
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
                if (frames[i] != null && frames[i].name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    list.Add(frames[i]);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }

        static string NormalizeAvatarId(string avatar)
        {
            if (string.IsNullOrWhiteSpace(avatar)) return "";
            string digits = "";
            for (int i = 0; i < avatar.Length; i++)
                if (char.IsDigit(avatar[i]))
                    digits += avatar[i];
            return digits.Length <= 4 ? digits : digits.Substring(digits.Length - 4);
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
            if (Cache.TryGetValue(id, out PetAnimationSet cached)) return cached;

            Sprite[] allFrames = Resources.LoadAll<Sprite>(ResourceRoot + "/" + id);
            if (allFrames == null || allFrames.Length == 0)
            {
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
                if (frames[i] != null && frames[i].name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    list.Add(frames[i]);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }
    }
}
