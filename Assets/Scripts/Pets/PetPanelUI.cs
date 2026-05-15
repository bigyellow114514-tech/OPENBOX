using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class PetPanelUI : MonoBehaviour
{
    enum PanelMode
    {
        Detail,
        Absorb,
    }

    const string CanvasName = "PetPanelCanvas";
    static readonly Color PaperColor = new Color(0.92f, 0.93f, 0.86f, 0.98f);
    static readonly Color PaperSoft = new Color(0.86f, 0.88f, 0.78f, 0.94f);
    static readonly Color TabColor = new Color(0.87f, 0.32f, 0.42f, 1f);
    static readonly Color InkColor = new Color(0.22f, 0.18f, 0.14f, 1f);
    static readonly Color MutedInk = new Color(0.46f, 0.38f, 0.28f, 1f);
    static readonly Color GreenButton = new Color(0.34f, 0.66f, 0.43f, 1f);
    static readonly Color BlueTalent = new Color(0.33f, 0.56f, 0.68f, 1f);
    static readonly Color GoldTalent = new Color(0.70f, 0.53f, 0.30f, 1f);
    static readonly Color PurpleTalent = new Color(0.57f, 0.39f, 0.72f, 1f);
    static readonly Color OrangeTalent = new Color(0.82f, 0.49f, 0.20f, 1f);
    static readonly Color SelectedColor = new Color(0.20f, 0.68f, 0.84f, 1f);
    const float PortraitIdleFps = 12f;

    RectTransform _panel;
    RectTransform _body;
    RectTransform _absorbPanel;
    RectTransform _absorbBody;
    Text _titleText;
    Text _resourceText;
    Text _messageText;

    readonly List<GameObject> _spawned = new List<GameObject>();
    GameObject _talentTip;
    PetInstance _selected;
    PetInstance _selectedMaterial;
    PanelMode _mode = PanelMode.Detail;
    Font _font;

    public static PetPanelUI ShowOrCreate(PetInstance focus = null, string message = "")
    {
        PetSystemManager.EnsureInstance();

        PetPanelUI existing = FindObjectOfType<PetPanelUI>(true);
        if (existing == null)
        {
            var canvasGO = new GameObject(CanvasName);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            existing = canvasGO.AddComponent<PetPanelUI>();
        }

        existing.Show(focus, message);
        return existing;
    }

    void Awake()
    {
        _font = ResolveFont();
        InitializeShell();
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;

        _font = ResolveFont();
        if (BindExistingShell())
            ApplySkinToExistingHierarchy();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            _font = ResolveFont();
            if (BindExistingShell())
                ApplySkinToExistingHierarchy();
            return;
        }

        if (PetSystemManager.Instance != null)
            PetSystemManager.Instance.OnPetsChanged += Refresh;
        if (PlayerResourceManager.Instance != null)
            PlayerResourceManager.Instance.OnResourceChanged += Refresh;
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;

        if (PetSystemManager.Instance != null)
            PetSystemManager.Instance.OnPetsChanged -= Refresh;
        if (PlayerResourceManager.Instance != null)
            PlayerResourceManager.Instance.OnResourceChanged -= Refresh;
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        PetSystemManager manager = PetSystemManager.Instance;
        PetConfig config = _selected != null && manager != null ? manager.GetPetConfig(_selected.PetId) : null;
        AnimatePortrait(_body, config);

        if (_absorbPanel != null && _absorbPanel.gameObject.activeSelf)
            AnimatePortrait(_absorbBody, config);
    }

    public void Show(PetInstance focus = null, string message = "")
    {
        gameObject.SetActive(true);
        _mode = PanelMode.Detail;
        if (focus != null) _selected = focus;
        if (_selected == null && PetSystemManager.Instance.Pets.Count > 0)
            _selected = PetSystemManager.Instance.Pets[0];
        SetMessage(message);
        Refresh();
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

    public void RebuildShellForEditor()
    {
        _font = ResolveFont();
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        BuildShell();
        EnsureDetailHierarchy(null);
        EnsureAbsorbHierarchy(null);
        if (_absorbPanel != null)
            _absorbPanel.gameObject.SetActive(false);
    }

    void InitializeShell()
    {
        if (BindExistingShell())
            return;

        BuildShell();
    }

    bool BindExistingShell()
    {
        Transform panel = transform.Find("PetPanel");
        if (panel == null) return false;

        _panel = panel as RectTransform;
        Transform body = panel.Find("Body");
        _body = body as RectTransform;
        _absorbPanel = transform.Find("PetAbsorbPanel") as RectTransform;
        _absorbBody = _absorbPanel != null ? _absorbPanel.Find("Body") as RectTransform : null;
        _titleText = panel.Find("TitleTab/Title")?.GetComponent<Text>();
        _resourceText = panel.Find("ResourcePill/ResourceText")?.GetComponent<Text>();
        _messageText = panel.Find("Message")?.GetComponent<Text>();

        Button dim = transform.Find("Dim")?.GetComponent<Button>();
        if (dim != null)
        {
            dim.onClick.RemoveListener(Hide);
            dim.onClick.AddListener(Hide);
        }

        Button close = panel.Find("CloseButton")?.GetComponent<Button>();
        if (close != null)
        {
            close.onClick.RemoveListener(Hide);
            close.onClick.AddListener(Hide);
        }

        Button back = _absorbPanel?.Find("Body/BackButton")?.GetComponent<Button>();
        if (back != null)
        {
            back.onClick.RemoveListener(HideAbsorbPanel);
            back.onClick.AddListener(HideAbsorbPanel);
        }

        bool bound = _body != null && _titleText != null && _resourceText != null && _messageText != null;
        if (bound)
            ApplySkinToExistingHierarchy();
        return bound;
    }

    void ApplySkinToExistingHierarchy()
    {
        SetImage(_panel, "UI/Pets/pet_panel_bg", Image.Type.Sliced, Color.white);
        SetImage(_panel?.Find("TitleTab"), "UI/Pets/pet_tab_title_bg", Image.Type.Sliced, Color.white);
        SetImage(_panel?.Find("ResourcePill"), "UI/Pets/pet_resource_pill_bg", Image.Type.Sliced, Color.white);
        SetImage(_panel?.Find("CloseButton"), "UI/Pets/pet_button_bg", Image.Type.Sliced, Color.white);

        SetImage(_absorbPanel, "UI/Pets/pet_panel_bg", Image.Type.Sliced, Color.white);
        SetImage(_absorbPanel?.Find("TitleTab"), "UI/Pets/pet_tab_title_bg", Image.Type.Sliced, Color.white);
        SetImage(_absorbBody?.Find("Hint"), "UI/Pets/pet_hint_bg", Image.Type.Sliced, Color.white);

        ApplyBodySkin(_body);
        ApplyBodySkin(_absorbBody);
    }

    void ApplyBodySkin(Transform body)
    {
        if (body == null) return;

        EnsurePortraitFrame(body);
        SetImage(body.Find("Quality"), "UI/Pets/pet_quality_badge_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("UpgradeButton"), "UI/Pets/pet_button_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("AbsorbButton"), "UI/Pets/pet_button_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("DeployButton"), "UI/Pets/pet_button_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("SkillBox"), "UI/Pets/pet_card_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("TargetInfo"), "UI/Pets/pet_card_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("Benefit"), "UI/Pets/pet_card_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("ConfirmAbsorb"), "UI/Pets/pet_button_bg", Image.Type.Sliced, Color.white);
        SetImage(body.Find("BackButton"), "UI/Pets/pet_button_bg", Image.Type.Sliced, Color.white);

        for (int i = 0; i < 4; i++)
            SetImage(body.Find("Talent_" + i), "UI/Pets/pet_talent_slot_bg", Image.Type.Sliced, Color.white);

        for (int i = 0; i < 16; i++)
            SetImage(body.Find("GridSlot_" + i), "UI/Pets/pet_slot_bg", Image.Type.Sliced, Color.white);
    }

    void EnsurePortraitFrame(Transform body)
    {
        Transform portrait = body.Find("Portrait");
        if (portrait == null || body.Find("PortraitFrame") != null) return;

        Image frame = NewUI("PortraitFrame", body).AddComponent<Image>();
        frame.sprite = LoadSprite("UI/Pets/pet_portrait_frame");
        frame.preserveAspect = true;
        RectTransform source = portrait as RectTransform;
        if (source != null)
        {
            RectTransform rt = frame.rectTransform;
            rt.anchorMin = source.anchorMin;
            rt.anchorMax = source.anchorMax;
            rt.pivot = source.pivot;
            rt.anchoredPosition = source.anchoredPosition;
            rt.sizeDelta = source.sizeDelta;
        }
        frame.transform.SetSiblingIndex(portrait.GetSiblingIndex());
    }

    void SetImage(Transform target, string path, Image.Type type, Color color)
    {
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        if (image == null) return;
        image.sprite = LoadSprite(path);
        image.type = type;
        image.color = color;
    }

    void BuildShell()
    {
        EnsureEventSystem();

        RectTransform canvasRect = GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject dim = NewUI("Dim", transform);
        Stretch(dim.GetComponent<RectTransform>());
        var dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.42f);
        dim.AddComponent<Button>().onClick.AddListener(Hide);

        GameObject panelGO = NewUI("PetPanel", transform);
        _panel = panelGO.GetComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0.1f, 0.1f);
        _panel.anchorMax = new Vector2(0.9f, 0.9f);
        _panel.offsetMin = Vector2.zero;
        _panel.offsetMax = Vector2.zero;
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.sprite = LoadSprite("UI/Pets/pet_panel_bg");
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;

        GameObject tab = NewUI("TitleTab", _panel);
        RectTransform tabRt = tab.GetComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0f, 1f);
        tabRt.anchorMax = new Vector2(0f, 1f);
        tabRt.pivot = new Vector2(0f, 1f);
        tabRt.anchoredPosition = new Vector2(0f, 0f);
        tabRt.sizeDelta = new Vector2(142f, 34f);
        Image tabImage = tab.AddComponent<Image>();
        tabImage.sprite = LoadSprite("UI/Pets/pet_tab_title_bg");
        tabImage.type = Image.Type.Sliced;
        tabImage.color = Color.white;

        _titleText = NewText("Title", tab.transform, "宠物", 18, TextAnchor.MiddleCenter, Color.white);
        Stretch(_titleText.rectTransform);

        GameObject resourcePill = NewUI("ResourcePill", _panel);
        SetRect(resourcePill.GetComponent<RectTransform>(), new Vector2(215f, -44f), new Vector2(210f, 34f), new Vector2(0f, 1f));
        Image resourceBg = resourcePill.AddComponent<Image>();
        resourceBg.sprite = LoadSprite("UI/Pets/pet_resource_pill_bg");
        resourceBg.type = Image.Type.Sliced;
        resourceBg.color = Color.white;
        _resourceText = NewText("ResourceText", resourcePill.transform, "", 17, TextAnchor.MiddleCenter, Color.white);
        Stretch(_resourceText.rectTransform);

        Button close = NewPlainButton("CloseButton", _panel, "关闭", new Vector2(-28f, -28f), new Vector2(64f, 64f), new Vector2(1f, 1f), Color.white, InkColor);
        close.onClick.AddListener(Hide);

        _messageText = NewText("Message", _panel, "", 16, TextAnchor.MiddleRight, new Color(0.68f, 0.25f, 0.20f, 1f));
        SetRect(_messageText.rectTransform, new Vector2(-120f, -84f), new Vector2(400f, 28f), new Vector2(1f, 1f));

        GameObject bodyGO = NewUI("Body", _panel);
        _body = bodyGO.GetComponent<RectTransform>();
        Stretch(_body, 26f, 26f, 62f, 26f);

        BuildAbsorbShell();
    }

    void BuildAbsorbShell()
    {
        GameObject panelGO = NewUI("PetAbsorbPanel", transform);
        _absorbPanel = panelGO.GetComponent<RectTransform>();
        _absorbPanel.anchorMin = new Vector2(0.12f, 0.12f);
        _absorbPanel.anchorMax = new Vector2(0.88f, 0.88f);
        _absorbPanel.offsetMin = Vector2.zero;
        _absorbPanel.offsetMax = Vector2.zero;
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.sprite = LoadSprite("UI/Pets/pet_panel_bg");
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;

        GameObject tab = NewUI("TitleTab", _absorbPanel);
        RectTransform tabRt = tab.GetComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0f, 1f);
        tabRt.anchorMax = new Vector2(0f, 1f);
        tabRt.pivot = new Vector2(0f, 1f);
        tabRt.anchoredPosition = Vector2.zero;
        tabRt.sizeDelta = new Vector2(170f, 34f);
        Image tabImage = tab.AddComponent<Image>();
        tabImage.sprite = LoadSprite("UI/Pets/pet_tab_title_bg");
        tabImage.type = Image.Type.Sliced;
        tabImage.color = Color.white;
        Text title = NewText("Title", tab.transform, "宠物吞噬", 18, TextAnchor.MiddleCenter, Color.white);
        Stretch(title.rectTransform);

        GameObject bodyGO = NewUI("Body", _absorbPanel);
        _absorbBody = bodyGO.GetComponent<RectTransform>();
        Stretch(_absorbBody, 26f, 26f, 54f, 26f);

        panelGO.SetActive(false);
    }

    void EnsureAbsorbHierarchy(PetSystemManager manager)
    {
        if (_absorbPanel == null)
            BuildAbsorbShell();
        if (_absorbBody == null)
            _absorbBody = _absorbPanel.Find("Body") as RectTransform;
        if (_absorbBody == null || _absorbBody.Find("ConfirmAbsorb") != null)
            return;

        bool active = _absorbPanel.gameObject.activeSelf;
        _absorbPanel.gameObject.SetActive(true);
        BuildAbsorbMode(manager);
        _absorbPanel.gameObject.SetActive(active);
    }

    void Refresh()
    {
        if (!gameObject.activeInHierarchy) return;

        PetSystemManager manager = PetSystemManager.EnsureInstance();
        if (_selected != null && FindById(manager.Pets, _selected.InstanceId) == null)
            _selected = manager.Pets.Count > 0 ? manager.Pets[0] : null;

        int tickets = PlayerResourceManager.Instance != null ? PlayerResourceManager.Instance.PetTickets : 0;
        int food = PlayerResourceManager.Instance != null ? PlayerResourceManager.Instance.PetFood : 0;
        _resourceText.text = "券 " + tickets + "   饲料 " + food;
        _titleText.text = "宠物";
        EnsureDetailHierarchy(manager);
        UpdateDetailMode(manager);

        if (_absorbPanel != null && _absorbPanel.gameObject.activeSelf)
            UpdateAbsorbPanel(manager);
    }

    void BuildDetailMode(PetSystemManager manager)
    {
        EnsureDetailHierarchy(manager);
        UpdateDetailMode(manager);
    }

    void EnsureDetailHierarchy(PetSystemManager manager)
    {
        if (_body.Find("UpgradeButton") != null && _body.Find("GridSlot_0") != null)
            return;

        ClearBody();
        BuildDetailHierarchy(manager);
    }

    void BuildDetailHierarchy(PetSystemManager manager)
    {
        DrawPetPortraitBlock(_body, _selected, new Vector2(155f, -132f), true);

        Button upgrade = NewPaperButton("UpgradeButton", _body, "升级", new Vector2(365f, -92f), new Vector2(130f, 38f));
        upgrade.onClick.AddListener(OnUpgradeClicked);

        Button absorb = NewPaperButton("AbsorbButton", _body, "吞噬", new Vector2(365f, -142f), new Vector2(130f, 38f));
        absorb.onClick.AddListener(() =>
        {
            ShowAbsorbPanel();
        });

        Button deploy = NewPaperButton("DeployButton", _body, "上阵", new Vector2(365f, -192f), new Vector2(130f, 38f));
        deploy.onClick.AddListener(() =>
        {
            if (_selected != null && manager != null) manager.Deploy(_selected.InstanceId);
        });

        if (_selected == null)
        {
            upgrade.interactable = false;
            absorb.interactable = false;
            deploy.interactable = false;
        }
        else
        {
            upgrade.interactable = _selected.Level < 100;
            deploy.interactable = !_selected.IsDeployed;
        }

        BuildSkillBox(_body, manager, _selected, new Vector2(185f, -288f), new Vector2(330f, 86f));
        BuildTalentRow(_body, manager, _selected, new Vector2(185f, -405f));

        Text listTitle = NewText("ListTitle", _body, "宠物列表\n(" + (manager != null ? manager.PetCount : 0) + "/" + (manager != null ? manager.MaxCount : 30) + ")", 18, TextAnchor.MiddleCenter, InkColor);
        SetRect(listTitle.rectTransform, new Vector2(770f, -48f), new Vector2(220f, 52f), new Vector2(0f, 1f));

        IReadOnlyList<PetInstance> pets = manager != null ? manager.Pets : new List<PetInstance>();
        BuildPetGrid(_body, pets, new Vector2(770f, -238f), 4, 4, false);
    }

    void UpdateDetailMode(PetSystemManager manager)
    {
        PetConfig config = _selected != null ? manager.GetPetConfig(_selected.PetId) : null;

        Image portrait = _body.Find("Portrait")?.GetComponent<Image>();
        if (portrait != null)
        {
            portrait.sprite = LoadPetIdleFrame(config) ?? LoadPetSprite(config);
            portrait.preserveAspect = true;
        }

        Image qualityBg = _body.Find("Quality")?.GetComponent<Image>();
        if (qualityBg != null)
            qualityBg.color = QualityColor(config != null ? config.Rarity : 0);

        Text qualityText = _body.Find("Quality/QualityText")?.GetComponent<Text>();
        if (qualityText != null)
            qualityText.text = QualityName(config != null ? config.Rarity : 0);

        Text stars = _body.Find("Stars")?.GetComponent<Text>();
        if (stars != null)
            stars.text = _selected != null ? StarsText(_selected.AbsorbCount) : "☆☆☆☆☆";

        Button upgrade = _body.Find("UpgradeButton")?.GetComponent<Button>();
        if (upgrade != null)
        {
            upgrade.onClick.RemoveAllListeners();
            upgrade.onClick.AddListener(OnUpgradeClicked);
            upgrade.interactable = _selected != null && _selected.Level < 100;
        }

        Button absorb = _body.Find("AbsorbButton")?.GetComponent<Button>();
        if (absorb != null)
        {
            absorb.onClick.RemoveAllListeners();
            absorb.onClick.AddListener(ShowAbsorbPanel);
            absorb.interactable = _selected != null;
        }

        Button deploy = _body.Find("DeployButton")?.GetComponent<Button>();
        if (deploy != null)
        {
            deploy.onClick.RemoveAllListeners();
            deploy.onClick.AddListener(() =>
            {
                if (_selected != null) manager.Deploy(_selected.InstanceId);
            });
            deploy.interactable = _selected != null && !_selected.IsDeployed;
        }

        Text skill = _body.Find("SkillBox/SkillText")?.GetComponent<Text>();
        if (skill != null)
            skill.text = config != null ? "参战技能：" + config.Description : "参战技能：暂无";

        Text listTitle = _body.Find("ListTitle")?.GetComponent<Text>();
        if (listTitle != null)
            listTitle.text = "宠物列表\n(" + manager.PetCount + "/" + manager.MaxCount + ")";

        UpdateTalentRow(_body, manager, _selected);
        UpdatePetGrid(_body, manager.Pets, false);
    }

    void BuildAbsorbMode(PetSystemManager manager)
    {
        Transform parent = _absorbBody != null ? _absorbBody : _body;
        DrawPetPortraitBlock(parent, _selected, new Vector2(170f, -170f), true);

        GameObject info = NewPaper("TargetInfo", parent, new Vector2(185f, -430f), new Vector2(360f, 118f));
        PetConfig config = _selected != null && manager != null ? manager.GetPetConfig(_selected.PetId) : null;
        Text name = NewText("TargetName", info.transform, _selected != null ? PetName(config) + "  " + _selected.Level + "级" : "暂无宠物", 18, TextAnchor.MiddleLeft, MutedInk);
        SetRect(name.rectTransform, new Vector2(70f, -28f), new Vector2(180f, 26f), new Vector2(0f, 1f));
        Text progress = NewText("AbsorbProgress", info.transform, _selected != null ? "星级： " + _selected.AbsorbCount + "/40" : "星级：0/40", 18, TextAnchor.MiddleRight, new Color(0.62f, 0.18f, 0.18f, 1f));
        SetRect(progress.rectTransform, new Vector2(-95f, -28f), new Vector2(180f, 26f), new Vector2(1f, 1f));
        BuildTalentRow(info.transform, manager, _selected, new Vector2(180f, -86f), 70f, 54f);

        GameObject hintGO = NewUI("Hint", parent);
        SetRect(hintGO.GetComponent<RectTransform>(), new Vector2(760f, -70f), new Vector2(300f, 42f), new Vector2(0f, 1f));
        Image hintBg = hintGO.AddComponent<Image>();
        hintBg.sprite = LoadSprite("UI/Pets/pet_hint_bg");
        hintBg.type = Image.Type.Sliced;
        hintBg.color = Color.white;
        Text hint = NewText("HintText", hintGO.transform, "只可吞噬未上阵同名灵兽", 18, TextAnchor.MiddleCenter, new Color(0.70f, 0.38f, 0.12f, 1f));
        Stretch(hint.rectTransform);

        List<PetInstance> materials = _selected != null && manager != null ? manager.GetAbsorbMaterials(_selected) : new List<PetInstance>();
        if (_selectedMaterial == null && materials.Count > 0)
            _selectedMaterial = materials[0];
        BuildPetGrid(parent, materials, new Vector2(760f, -190f), 4, 2, true);

        GameObject benefit = NewPaper("Benefit", parent, new Vector2(760f, -405f), new Vector2(330f, 92f));
        Text benefitText = NewText("BenefitText", benefit.transform, "吞噬收益\n星级 +1\n随机提升1个技能", 18, TextAnchor.MiddleCenter, MutedInk);
        Stretch(benefitText.rectTransform);

        Button confirm = NewPlainButton("ConfirmAbsorb", parent, "吞噬", new Vector2(760f, -510f), new Vector2(190f, 62f), new Vector2(0f, 1f), GreenButton, Color.white);
        confirm.interactable = _selected != null && _selectedMaterial != null;
        confirm.onClick.AddListener(OnAbsorbClicked);

        Button back = NewPaperButton("BackButton", parent, "返回", new Vector2(545f, -510f), new Vector2(116f, 42f));
        back.onClick.AddListener(HideAbsorbPanel);
    }

    void ShowAbsorbPanel()
    {
        if (_selected == null) return;
        PetSystemManager manager = PetSystemManager.EnsureInstance();
        EnsureAbsorbHierarchy(manager);
        _selectedMaterial = null;
        if (_absorbPanel != null)
            _absorbPanel.gameObject.SetActive(true);
        UpdateAbsorbPanel(manager);
    }

    void HideAbsorbPanel()
    {
        _selectedMaterial = null;
        if (_absorbPanel != null)
            _absorbPanel.gameObject.SetActive(false);
    }

    void UpdateAbsorbPanel(PetSystemManager manager)
    {
        EnsureAbsorbHierarchy(manager);
        if (_absorbBody == null) return;

        PetConfig config = _selected != null ? manager.GetPetConfig(_selected.PetId) : null;
        Image portrait = _absorbBody.Find("Portrait")?.GetComponent<Image>();
        if (portrait != null)
        {
            portrait.sprite = LoadPetIdleFrame(config) ?? LoadPetSprite(config);
            portrait.preserveAspect = true;
        }

        Image qualityBg = _absorbBody.Find("Quality")?.GetComponent<Image>();
        if (qualityBg != null)
            qualityBg.color = QualityColor(config != null ? config.Rarity : 0);

        Text qualityText = _absorbBody.Find("Quality/QualityText")?.GetComponent<Text>();
        if (qualityText != null)
            qualityText.text = QualityName(config != null ? config.Rarity : 0);

        Text stars = _absorbBody.Find("Stars")?.GetComponent<Text>();
        if (stars != null)
            stars.text = _selected != null ? StarsText(_selected.AbsorbCount) : "☆☆☆☆☆";

        Text name = _absorbBody.Find("TargetInfo/TargetName")?.GetComponent<Text>();
        if (name != null)
            name.text = _selected != null ? PetName(config) + "  " + _selected.Level + "级" : "暂无宠物";

        Text progress = _absorbBody.Find("TargetInfo/AbsorbProgress")?.GetComponent<Text>();
        if (progress != null)
            progress.text = _selected != null ? "星级： " + _selected.AbsorbCount + "/40" : "星级：0/40";

        UpdateTalentRow(_absorbBody.Find("TargetInfo") ?? _absorbBody, manager, _selected);
        List<PetInstance> materials = _selected != null ? manager.GetAbsorbMaterials(_selected) : new List<PetInstance>();
        if (_selectedMaterial == null && materials.Count > 0)
            _selectedMaterial = materials[0];
        UpdatePetGrid(_absorbBody, materials, true);

        Button confirm = _absorbBody.Find("ConfirmAbsorb")?.GetComponent<Button>();
        if (confirm != null)
        {
            confirm.onClick.RemoveAllListeners();
            confirm.onClick.AddListener(OnAbsorbClicked);
            confirm.interactable = _selected != null && _selectedMaterial != null;
        }

        Button back = _absorbBody.Find("BackButton")?.GetComponent<Button>();
        if (back != null)
        {
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(HideAbsorbPanel);
        }
    }

    void DrawPetPortraitBlock(Transform parent, PetInstance pet, Vector2 center, bool showQuality)
    {
        PetConfig config = pet != null ? PetSystemManager.Instance.GetPetConfig(pet.PetId) : null;

        Image portraitFrame = NewUI("PortraitFrame", parent).AddComponent<Image>();
        portraitFrame.sprite = LoadSprite("UI/Pets/pet_portrait_frame");
        portraitFrame.preserveAspect = true;
        SetRect(portraitFrame.rectTransform, center, new Vector2(210f, 210f), new Vector2(0f, 1f));

        Image portrait = NewUI("Portrait", parent).AddComponent<Image>();
        portrait.sprite = LoadPetIdleFrame(config) ?? LoadPetSprite(config);
        portrait.preserveAspect = true;
        SetRect(portrait.rectTransform, center, new Vector2(210f, 210f), new Vector2(0f, 1f));

        if (showQuality)
        {
            GameObject qualityGO = NewUI("Quality", parent);
            SetRect(qualityGO.GetComponent<RectTransform>(), center + new Vector2(-106f, 80f), new Vector2(66f, 66f), new Vector2(0f, 1f));
            Image qualityBg = qualityGO.AddComponent<Image>();
            qualityBg.sprite = LoadSprite("UI/Pets/pet_quality_badge_bg");
            qualityBg.type = Image.Type.Sliced;
            qualityBg.color = Color.white;
            Text quality = NewText("QualityText", qualityGO.transform, QualityName(config != null ? config.Rarity : 0), 16, TextAnchor.MiddleCenter, Color.white);
            quality.fontStyle = FontStyle.Bold;
            Stretch(quality.rectTransform);
        }

        Text stars = NewText("Stars", parent, pet != null ? StarsText(pet.AbsorbCount) : "☆☆☆☆☆", 22, TextAnchor.MiddleCenter, new Color(0.27f, 0.74f, 0.70f, 1f));
        SetRect(stars.rectTransform, center + new Vector2(0f, -104f), new Vector2(160f, 32f), new Vector2(0f, 1f));
    }

    void BuildSkillBox(Transform parent, PetSystemManager manager, PetInstance pet, Vector2 center, Vector2 size)
    {
        GameObject box = NewPaper("SkillBox", parent, center, size);
        PetConfig config = pet != null ? manager.GetPetConfig(pet.PetId) : null;
        string text = config != null ? "参战技能：" + config.Description : "参战技能：暂无";
        Text skill = NewText("SkillText", box.transform, text, 16, TextAnchor.UpperLeft, InkColor);
        Stretch(skill.rectTransform, 12f, 12f, 10f, 8f);
    }

    void BuildTalentRow(Transform parent, PetSystemManager manager, PetInstance pet, Vector2 center, float width = 74f, float height = 70f)
    {
        int count = pet != null && pet.Talents != null ? pet.Talents.Count : 0;
        for (int i = 0; i < 4; i++)
        {
            GameObject slot = NewUI("Talent_" + i, parent);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center + new Vector2((i - 1.5f) * (width + 10f), 0f);
            rt.sizeDelta = new Vector2(width, height);
            Image img = slot.AddComponent<Image>();
            img.sprite = LoadSprite("UI/Pets/pet_talent_slot_bg");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            Button slotButton = slot.AddComponent<Button>();

            string label = "?";
            if (i < count)
            {
                PetTalentInstance talent = pet.Talents[i];
                TalentSkillConfig talentConfig = manager.GetTalentConfig(talent.Attr);
                float value = talentConfig != null ? talentConfig.GetValue(talent.Rarity, talent.Level) : 0f;

                Text level = NewText("LevelBadge", slot.transform, Mathf.Max(1, talent.Level).ToString(), 12, TextAnchor.MiddleCenter, Color.white);
                level.fontStyle = FontStyle.Bold;
                SetRect(level.rectTransform, new Vector2(width * 0.36f, height * 0.36f), new Vector2(24f, 20f), new Vector2(0.5f, 0.5f));

                string talentName = TalentDisplayName(talent.Attr, talentConfig);
                label = TalentIconName(talentName);

                string tipTitle = talentName + "  " + Mathf.Max(1, talent.Level) + "级";
                string tipBody = talentName + "+" + FormatTalentValue(talent.Attr, value);
                RectTransform slotRect = rt;
                slotButton.onClick.AddListener(() => ShowTalentTip(parent, slotRect, tipTitle, tipBody, height));
            }
            else
            {
                slotButton.onClick.AddListener(HideTalentTip);
            }

            Text text = NewText("Label", slot.transform, label, i < count ? 17 : 24, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = i < count ? FontStyle.Bold : FontStyle.Normal;
            Stretch(text.rectTransform, 6f, 6f, 6f, 6f);
        }
    }

    void UpdateTalentRow(Transform parent, PetSystemManager manager, PetInstance pet)
    {
        int count = pet != null && pet.Talents != null ? pet.Talents.Count : 0;
        for (int i = 0; i < 4; i++)
        {
            Transform slot = parent.Find("Talent_" + i);
            if (slot == null) continue;

            ClearChildren(slot);
            RectTransform rt = (RectTransform)slot;
            float width = rt.sizeDelta.x > 0f ? rt.sizeDelta.x : 74f;
            float height = rt.sizeDelta.y > 0f ? rt.sizeDelta.y : 70f;

            Image img = slot.GetComponent<Image>();
            if (img != null)
                img.color = Color.white;

            Button slotButton = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
            slotButton.onClick.RemoveAllListeners();

            string label = "?";
            if (i < count)
            {
                PetTalentInstance talent = pet.Talents[i];
                TalentSkillConfig talentConfig = manager.GetTalentConfig(talent.Attr);
                float value = talentConfig != null ? talentConfig.GetValue(talent.Rarity, talent.Level) : 0f;

                Text level = NewText("LevelBadge", slot, Mathf.Max(1, talent.Level).ToString(), 12, TextAnchor.MiddleCenter, Color.white);
                level.fontStyle = FontStyle.Bold;
                SetRect(level.rectTransform, new Vector2(width * 0.36f, height * 0.36f), new Vector2(24f, 20f), new Vector2(0.5f, 0.5f));

                string talentName = TalentDisplayName(talent.Attr, talentConfig);
                label = TalentIconName(talentName);

                string tipTitle = talentName + "  " + Mathf.Max(1, talent.Level) + "级";
                string tipBody = talentName + "+" + FormatTalentValue(talent.Attr, value);
                RectTransform slotRect = rt;
                slotButton.onClick.AddListener(() => ShowTalentTip(parent, slotRect, tipTitle, tipBody, height));
            }
            else
            {
                slotButton.onClick.AddListener(HideTalentTip);
            }

            Text text = NewText("Label", slot, label, i < count ? 17 : 24, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = i < count ? FontStyle.Bold : FontStyle.Normal;
            Stretch(text.rectTransform, 6f, 6f, 6f, 6f);
        }
    }

    void ShowTalentTip(Transform parent, RectTransform source, string title, string body, float sourceHeight)
    {
        HideTalentTip();

        _talentTip = NewUI("TalentTip", parent);
        RectTransform tipRt = _talentTip.GetComponent<RectTransform>();
        tipRt.anchorMin = source.anchorMin;
        tipRt.anchorMax = source.anchorMax;
        tipRt.pivot = new Vector2(0.5f, 1f);
        tipRt.anchoredPosition = source.anchoredPosition + new Vector2(0f, -sourceHeight * 0.58f);
        tipRt.sizeDelta = new Vector2(154f, 60f);

        Image bg = _talentTip.AddComponent<Image>();
        bg.sprite = LoadSprite("UI/Pets/pet_card_bg");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.12f, 0.20f, 0.17f, 0.94f);

        Text titleText = NewText("Title", _talentTip.transform, title, 15, TextAnchor.MiddleCenter, Color.white);
        titleText.fontStyle = FontStyle.Bold;
        SetRect(titleText.rectTransform, new Vector2(0f, -17f), new Vector2(136f, 24f), new Vector2(0.5f, 1f));

        Text bodyText = NewText("Body", _talentTip.transform, body, 17, TextAnchor.MiddleCenter, new Color(0.46f, 0.88f, 0.43f, 1f));
        bodyText.fontStyle = FontStyle.Bold;
        SetRect(bodyText.rectTransform, new Vector2(0f, -42f), new Vector2(136f, 26f), new Vector2(0.5f, 1f));

        _talentTip.transform.SetAsLastSibling();
    }

    void HideTalentTip()
    {
        if (_talentTip == null) return;
        Destroy(_talentTip);
        _talentTip = null;
    }

    void BuildPetGrid(Transform parent, IReadOnlyList<PetInstance> pets, Vector2 center, int columns, int rows, bool materialMode)
    {
        int total = columns * rows;
        float cell = 82f;
        float gap = 10f;
        float width = columns * cell + (columns - 1) * gap;
        float height = rows * cell + (rows - 1) * gap;

        for (int i = 0; i < total; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Vector2 pos = center + new Vector2(col * (cell + gap) - width * 0.5f + cell * 0.5f, -row * (cell + gap) + height * 0.5f - cell * 0.5f);

            GameObject slot = NewUI("GridSlot_" + i, parent);
            SetRect(slot.GetComponent<RectTransform>(), pos, new Vector2(cell, cell), new Vector2(0f, 1f));
            Image bg = slot.AddComponent<Image>();
            bg.sprite = LoadSprite("UI/Pets/pet_slot_bg");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            if (i >= pets.Count)
            {
                Text plus = NewText("Empty", slot.transform, materialMode ? "+" : "", 42, TextAnchor.MiddleCenter, new Color(0.82f, 0.78f, 0.68f, 0.7f));
                Stretch(plus.rectTransform);
                continue;
            }

            PetInstance pet = pets[i];
            PetConfig config = PetSystemManager.Instance.GetPetConfig(pet.PetId);
            Image icon = NewUI("Icon", slot.transform).AddComponent<Image>();
            icon.sprite = LoadPetAvatarSprite(config);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform, 4f, 4f, 4f, 4f);

            GameObject qualityGO = NewUI("Quality", slot.transform);
            SetRect(qualityGO.GetComponent<RectTransform>(), new Vector2(18f, -12f), new Vector2(34f, 22f), new Vector2(0f, 1f));
            Image qualityBg = qualityGO.AddComponent<Image>();
            qualityBg.sprite = LoadSprite("UI/Pets/pet_quality_badge_bg");
            qualityBg.type = Image.Type.Sliced;
            qualityBg.color = Color.white;
            Text quality = NewText("Label", qualityGO.transform, ShortQualityName(config != null ? config.Rarity : 0), 13, TextAnchor.MiddleCenter, Color.white);
            quality.fontStyle = FontStyle.Bold;
            Stretch(quality.rectTransform);

            if (pet == _selected || pet == _selectedMaterial)
            {
                Image outline = NewUI("Selected", slot.transform).AddComponent<Image>();
                outline.color = SelectedColor;
                outline.raycastTarget = false;
                Stretch(outline.rectTransform);
                outline.transform.SetAsFirstSibling();
            }

            if (pet.IsDeployed)
            {
                GameObject deployedGO = NewUI("Deployed", slot.transform);
                SetRect(deployedGO.GetComponent<RectTransform>(), new Vector2(25f, -70f), new Vector2(45f, 24f), new Vector2(0f, 1f));
                deployedGO.AddComponent<Image>().color = new Color(0.75f, 0.16f, 0.20f, 0.85f);
                Text deployed = NewText("Label", deployedGO.transform, "已上阵", 14, TextAnchor.MiddleCenter, Color.white);
                Stretch(deployed.rectTransform);
            }

            Text level = NewText("Level", slot.transform, pet.Level + "级", 15, TextAnchor.MiddleRight, Color.white);
            level.fontStyle = FontStyle.Bold;
            SetRect(level.rectTransform, new Vector2(-24f, 12f), new Vector2(56f, 22f), new Vector2(1f, 0f));

            Button button = slot.AddComponent<Button>();
            string id = pet.InstanceId;
            button.onClick.AddListener(() =>
            {
                if (materialMode)
                    _selectedMaterial = FindById(pets, id);
                else
                {
                    _selected = FindById(PetSystemManager.Instance.Pets, id);
                    _selectedMaterial = null;
                }
                SetMessage("");
                Refresh();
            });
        }
    }

    void OnUpgradeClicked()
    {
        if (_selected == null) return;
        if (!PetSystemManager.Instance.Upgrade(_selected.InstanceId, out string reason))
            SetMessage(reason);
        else
            SetMessage("升级成功");
        Refresh();
    }

    void OnAbsorbClicked()
    {
        if (_selected == null || _selectedMaterial == null)
        {
            SetMessage("请选择可吞噬素材");
            return;
        }

        if (!PetSystemManager.Instance.Absorb(_selected.InstanceId, _selectedMaterial.InstanceId, out string reason))
            SetMessage(reason);
        else
            SetMessage("吞噬成功");

        _selectedMaterial = null;
        Refresh();
    }

    void ClearBody()
    {
        _talentTip = null;
        for (int i = _body.childCount - 1; i >= 0; i--)
        {
            GameObject child = _body.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
        _spawned.Clear();
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    void SetMessage(string text)
    {
        if (_messageText != null)
            _messageText.text = text ?? "";
    }

    string PetName(PetConfig config)
    {
        return config != null && !string.IsNullOrEmpty(config.PetName) ? config.PetName : "未知宠物";
    }

    string StarsText(int absorbCount)
    {
        int lit = Mathf.Clamp(absorbCount % 5, 0, 5);
        if (absorbCount > 0 && lit == 0) lit = 5;
        return new string('★', lit) + new string('☆', 5 - lit);
    }

    string FormatTalentValue(string attr, float value)
    {
        if (attr == "AttackRate" || attr == "DefenceRate" || attr == "HpRate" || attr.EndsWith("Rate") ||
            attr.Contains("Dmg") || attr.Contains("Healing") || attr.Contains("Increase") || attr.Contains("Decrease"))
            return value.ToString("0.##") + "%";
        return value.ToString("0.##");
    }

    string AttrName(string key)
    {
        switch (key)
        {
            case "AttackRate": return "强化灵兽";
            case "DefenceRate": return "抵抗闪避";
            case "HpRate": return "生命强化";
            case "Agility": return "敏捷";
            case "CritRate": return "暴击";
            case "CounterRate": return "反击";
            case "ComboRate": return "连击";
            case "DodgeRate": return "闪避";
            case "StunRate": return "击晕";
            case "LifeStealRate": return "吸血";
            case "PetIncrease": return "强化灵兽";
            case "PetDecrease": return "弱化灵兽";
            default: return key;
        }
    }

    void UpdatePetGrid(Transform parent, IReadOnlyList<PetInstance> pets, bool materialMode)
    {
        for (int i = 0; i < 16; i++)
        {
            Transform slot = parent.Find("GridSlot_" + i);
            if (slot == null) continue;

            ClearChildren(slot);
            Button button = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.interactable = i < pets.Count;

            if (i >= pets.Count)
            {
                Text plus = NewText("Empty", slot, materialMode ? "+" : "", 42, TextAnchor.MiddleCenter, new Color(0.82f, 0.78f, 0.68f, 0.7f));
                Stretch(plus.rectTransform);
                continue;
            }

            PetInstance pet = pets[i];
            PetConfig config = PetSystemManager.Instance.GetPetConfig(pet.PetId);
            Image icon = NewUI("Icon", slot).AddComponent<Image>();
            icon.sprite = LoadPetAvatarSprite(config);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform, 4f, 4f, 4f, 4f);

            GameObject qualityGO = NewUI("Quality", slot);
            SetRect(qualityGO.GetComponent<RectTransform>(), new Vector2(18f, -12f), new Vector2(34f, 22f), new Vector2(0f, 1f));
            Image qualityBg = qualityGO.AddComponent<Image>();
            qualityBg.sprite = LoadSprite("UI/Pets/pet_quality_badge_bg");
            qualityBg.type = Image.Type.Sliced;
            qualityBg.color = Color.white;
            Text quality = NewText("Label", qualityGO.transform, ShortQualityName(config != null ? config.Rarity : 0), 13, TextAnchor.MiddleCenter, Color.white);
            quality.fontStyle = FontStyle.Bold;
            Stretch(quality.rectTransform);

            if (pet == _selected || pet == _selectedMaterial)
            {
                Image outline = NewUI("Selected", slot).AddComponent<Image>();
                outline.color = SelectedColor;
                outline.raycastTarget = false;
                Stretch(outline.rectTransform);
                outline.transform.SetAsFirstSibling();
            }

            if (pet.IsDeployed)
            {
                GameObject deployedGO = NewUI("Deployed", slot);
                SetRect(deployedGO.GetComponent<RectTransform>(), new Vector2(25f, -70f), new Vector2(45f, 24f), new Vector2(0f, 1f));
                deployedGO.AddComponent<Image>().color = new Color(0.75f, 0.16f, 0.20f, 0.85f);
                Text deployed = NewText("Label", deployedGO.transform, "已上阵", 14, TextAnchor.MiddleCenter, Color.white);
                Stretch(deployed.rectTransform);
            }

            Text level = NewText("Level", slot, pet.Level + "级", 15, TextAnchor.MiddleRight, Color.white);
            level.fontStyle = FontStyle.Bold;
            SetRect(level.rectTransform, new Vector2(-24f, 12f), new Vector2(56f, 22f), new Vector2(1f, 0f));

            string id = pet.InstanceId;
            button.onClick.AddListener(() =>
            {
                if (materialMode)
                    _selectedMaterial = FindById(pets, id);
                else
                {
                    _selected = FindById(PetSystemManager.Instance.Pets, id);
                    _selectedMaterial = null;
                }
                SetMessage("");
                Refresh();
            });
        }
    }

    string TalentDisplayName(string attr, TalentSkillConfig config)
    {
        return config != null && !string.IsNullOrEmpty(config.Name) ? config.Name : AttrName(attr);
    }

    string TalentIconName(string name)
    {
        if (name.Length <= 2) return name;
        if (name.Length <= 4) return name.Insert(2, "\n");
        return name.Substring(0, 4).Insert(2, "\n");
    }

    string QualityName(int rarity)
    {
        switch (rarity)
        {
            case 1: return "凡品";
            case 2: return "良品";
            case 3: return "珍品";
            case 4: return "极品";
            case 5: return "神品";
            default: return "未知";
        }
    }

    string ShortQualityName(int rarity)
    {
        switch (rarity)
        {
            case 1: return "凡";
            case 2: return "良";
            case 3: return "珍";
            case 4: return "极";
            case 5: return "神";
            default: return "?";
        }
    }

    Color QualityColor(int rarity)
    {
        switch (rarity)
        {
            case 1: return new Color(0.46f, 0.50f, 0.46f, 0.95f);
            case 2: return new Color(0.24f, 0.62f, 0.44f, 0.95f);
            case 3: return new Color(0.28f, 0.54f, 0.78f, 0.95f);
            case 4: return new Color(0.62f, 0.38f, 0.76f, 0.95f);
            case 5: return new Color(0.84f, 0.49f, 0.20f, 0.95f);
            default: return new Color(0.42f, 0.39f, 0.32f, 0.95f);
        }
    }

    Color TalentColor(int rarity)
    {
        switch (rarity)
        {
            case 1: return GoldTalent;
            case 2: return BlueTalent;
            case 3: return PurpleTalent;
            case 4: return OrangeTalent;
            default: return GoldTalent;
        }
    }

    static PetInstance FindById(IReadOnlyList<PetInstance> pets, string id)
    {
        for (int i = 0; i < pets.Count; i++)
            if (pets[i].InstanceId == id) return pets[i];
        return null;
    }

    Sprite LoadPetSprite(PetConfig config)
    {
        string resource = config != null && !string.IsNullOrEmpty(config.PetResource) ? config.PetResource : "Pet_Empty";
        return LoadSprite("UI/Pets/" + resource) ?? LoadSprite("UI/Pets/Pet_Empty");
    }

    Sprite LoadPetAvatarSprite(PetConfig config)
    {
        string resource = config != null && !string.IsNullOrEmpty(config.PetResource) ? config.PetResource : "";
        string id = ExtractPetId(resource);
        if (!string.IsNullOrEmpty(id))
        {
            Sprite avatar = LoadSprite("Sprites/Avatar/Pet/Pet_Avatar_" + id);
            if (avatar != null) return avatar;
        }

        return LoadPetSprite(config);
    }

    static string ExtractPetId(string resource)
    {
        if (string.IsNullOrWhiteSpace(resource)) return "";

        string digits = "";
        for (int i = 0; i < resource.Length; i++)
        {
            if (char.IsDigit(resource[i]))
                digits += resource[i];
        }

        return digits;
    }

    void AnimatePortrait(Transform parent, PetConfig config)
    {
        if (parent == null) return;

        Image portrait = parent.Find("Portrait")?.GetComponent<Image>();
        if (portrait == null) return;

        Sprite frame = LoadPetIdleFrame(config);
        if (frame == null) return;

        portrait.sprite = frame;
        portrait.preserveAspect = true;
    }

    Sprite LoadPetIdleFrame(PetConfig config)
    {
        string resource = config != null && !string.IsNullOrEmpty(config.PetResource) ? config.PetResource : "";
        Sprite[] frames = PetPortraitAnimationSet.LoadIdle(resource);
        if (frames == null || frames.Length == 0) return null;

        int frameIndex = Mathf.FloorToInt(Time.unscaledTime * PortraitIdleFps) % frames.Length;
        return frames[frameIndex];
    }

    Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    GameObject NewPaper(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = NewUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), pos, size, new Vector2(0f, 1f));
        Image image = go.AddComponent<Image>();
        image.sprite = LoadSprite("UI/Pets/pet_card_bg");
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        return go;
    }

    Button NewPaperButton(string name, Transform parent, string label, Vector2 pos, Vector2 size)
    {
        return NewPlainButton(name, parent, label, pos, size, new Vector2(0f, 1f), Color.white, InkColor);
    }

    Button NewPlainButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, Vector2 anchor, Color bg, Color fg)
    {
        GameObject go = NewUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), pos, size, anchor);
        Image image = go.AddComponent<Image>();
        image.sprite = LoadSprite("UI/Pets/pet_button_bg");
        image.type = Image.Type.Sliced;
        image.color = bg;
        Button button = go.AddComponent<Button>();

        Text text = NewText("Label", go.transform, label, 18, TextAnchor.MiddleCenter, fg);
        Stretch(text.rectTransform, 4f, 4f, 2f, 2f);
        return button;
    }

    Text NewText(string name, Transform parent, string text, int size, TextAnchor alignment, Color color)
    {
        GameObject go = NewUI(name, parent);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = _font;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
    Font ResolveFont()
{
    string[] chineseFonts =
    {
        "Microsoft YaHei",
        "SimHei",
        "PingFang SC",
        "Heiti SC",
        "Noto Sans CJK SC",
        "Arial Unicode MS"
    };

    Font osFont = Font.CreateDynamicFontFromOSFont(chineseFonts, 16);
    if (osFont != null)
        return osFont;

    return Resources.GetBuiltinResource<Font>("Arial.ttf");
}

    sealed class PetPortraitAnimationSet
    {
        const string CurrentResourceRoot = "Sprites/PetsAnim";
        const string LegacyResourceRoot = "Sprites/Pets";
        static readonly Dictionary<string, Sprite[]> IdleCache = new Dictionary<string, Sprite[]>();

        public static Sprite[] LoadIdle(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) return null;

            string id = resource.Trim();
            if (IdleCache.TryGetValue(id, out Sprite[] cached))
                return cached;

            Sprite[] allFrames = LoadAllFrames(CurrentResourceRoot, id);
            if (allFrames == null || allFrames.Length == 0)
                allFrames = LoadAllFrames(LegacyResourceRoot, id);

            Sprite[] idleFrames = FilterFrames(allFrames, id + "_Idle_");
            IdleCache[id] = idleFrames;
            return idleFrames;
        }

        static Sprite[] LoadAllFrames(string root, string id)
        {
            return Resources.LoadAll<Sprite>(root + "/" + id);
        }

        static Sprite[] FilterFrames(Sprite[] frames, string prefix)
        {
            if (frames == null || frames.Length == 0) return null;

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
