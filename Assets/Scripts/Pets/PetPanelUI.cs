using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    static readonly Color SelectedColor = new Color(0.20f, 0.68f, 0.84f, 1f);

    RectTransform _panel;
    RectTransform _body;
    Text _titleText;
    Text _resourceText;
    Text _messageText;

    readonly List<GameObject> _spawned = new List<GameObject>();
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
        BuildShell();
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (PetSystemManager.Instance != null)
            PetSystemManager.Instance.OnPetsChanged += Refresh;
        if (PlayerResourceManager.Instance != null)
            PlayerResourceManager.Instance.OnResourceChanged += Refresh;
    }

    void OnDisable()
    {
        if (PetSystemManager.Instance != null)
            PetSystemManager.Instance.OnPetsChanged -= Refresh;
        if (PlayerResourceManager.Instance != null)
            PlayerResourceManager.Instance.OnResourceChanged -= Refresh;
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
        panelImage.sprite = LoadSprite("UI/Pets/Panel_Back");
        panelImage.type = Image.Type.Sliced;
        panelImage.color = PaperColor;

        GameObject tab = NewUI("TitleTab", _panel);
        RectTransform tabRt = tab.GetComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0f, 1f);
        tabRt.anchorMax = new Vector2(0f, 1f);
        tabRt.pivot = new Vector2(0f, 1f);
        tabRt.anchoredPosition = new Vector2(0f, 0f);
        tabRt.sizeDelta = new Vector2(142f, 34f);
        tab.AddComponent<Image>().color = TabColor;

        _titleText = NewText("Title", tab.transform, "宠物", 18, TextAnchor.MiddleCenter, Color.white);
        Stretch(_titleText.rectTransform);

        GameObject resourcePill = NewUI("ResourcePill", _panel);
        SetRect(resourcePill.GetComponent<RectTransform>(), new Vector2(215f, -44f), new Vector2(210f, 34f), new Vector2(0f, 1f));
        Image resourceBg = resourcePill.AddComponent<Image>();
        resourceBg.color = new Color(0.12f, 0.38f, 0.42f, 0.92f);
        _resourceText = NewText("ResourceText", resourcePill.transform, "", 17, TextAnchor.MiddleCenter, Color.white);
        Stretch(_resourceText.rectTransform);

        Button close = NewPlainButton("CloseButton", _panel, "关闭", new Vector2(-28f, -28f), new Vector2(64f, 64f), new Vector2(1f, 1f), Color.white, InkColor);
        close.onClick.AddListener(Hide);

        _messageText = NewText("Message", _panel, "", 16, TextAnchor.MiddleRight, new Color(0.68f, 0.25f, 0.20f, 1f));
        SetRect(_messageText.rectTransform, new Vector2(-120f, -84f), new Vector2(400f, 28f), new Vector2(1f, 1f));

        GameObject bodyGO = NewUI("Body", _panel);
        _body = bodyGO.GetComponent<RectTransform>();
        Stretch(_body, 26f, 26f, 62f, 26f);
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
        _titleText.text = _mode == PanelMode.Detail ? "宠物" : "宠物吞噬";

        ClearBody();
        if (_mode == PanelMode.Detail)
            BuildDetailMode(manager);
        else
            BuildAbsorbMode(manager);
    }

    void BuildDetailMode(PetSystemManager manager)
    {
        DrawPetPortraitBlock(_body, _selected, new Vector2(155f, -132f), true);

        Button upgrade = NewPaperButton("UpgradeButton", _body, "升级", new Vector2(365f, -92f), new Vector2(130f, 38f));
        upgrade.onClick.AddListener(OnUpgradeClicked);

        Button absorb = NewPaperButton("AbsorbButton", _body, "吞噬", new Vector2(365f, -142f), new Vector2(130f, 38f));
        absorb.onClick.AddListener(() =>
        {
            _mode = PanelMode.Absorb;
            _selectedMaterial = null;
            SetMessage("");
            Refresh();
        });

        Button deploy = NewPaperButton("DeployButton", _body, "上阵", new Vector2(365f, -192f), new Vector2(130f, 38f));
        deploy.onClick.AddListener(() =>
        {
            if (_selected != null) manager.Deploy(_selected.InstanceId);
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

        Text listTitle = NewText("ListTitle", _body, "宠物列表\n(" + manager.PetCount + "/" + manager.MaxCount + ")", 18, TextAnchor.MiddleCenter, InkColor);
        SetRect(listTitle.rectTransform, new Vector2(770f, -48f), new Vector2(220f, 52f), new Vector2(0f, 1f));

        BuildPetGrid(_body, manager.Pets, new Vector2(770f, -238f), 4, 4, false);
    }

    void BuildAbsorbMode(PetSystemManager manager)
    {
        DrawPetPortraitBlock(_body, _selected, new Vector2(170f, -170f), true);

        GameObject info = NewPaper("TargetInfo", _body, new Vector2(185f, -430f), new Vector2(360f, 118f));
        PetConfig config = _selected != null ? manager.GetPetConfig(_selected.PetId) : null;
        Text name = NewText("TargetName", info.transform, _selected != null ? PetName(config) + "  " + _selected.Level + "级" : "暂无宠物", 18, TextAnchor.MiddleLeft, MutedInk);
        SetRect(name.rectTransform, new Vector2(70f, -28f), new Vector2(180f, 26f), new Vector2(0f, 1f));
        Text progress = NewText("AbsorbProgress", info.transform, _selected != null ? "星级： " + _selected.AbsorbCount + "/40" : "星级：0/40", 18, TextAnchor.MiddleRight, new Color(0.62f, 0.18f, 0.18f, 1f));
        SetRect(progress.rectTransform, new Vector2(-95f, -28f), new Vector2(180f, 26f), new Vector2(1f, 1f));
        BuildTalentRow(info.transform, manager, _selected, new Vector2(180f, -86f), 70f, 54f);

        GameObject hintGO = NewUI("Hint", _body);
        SetRect(hintGO.GetComponent<RectTransform>(), new Vector2(760f, -70f), new Vector2(300f, 42f), new Vector2(0f, 1f));
        Image hintBg = hintGO.AddComponent<Image>();
        hintBg.color = new Color(0.88f, 0.88f, 0.78f, 0.7f);
        Text hint = NewText("HintText", hintGO.transform, "只可吞噬未上阵同名灵兽", 18, TextAnchor.MiddleCenter, new Color(0.70f, 0.38f, 0.12f, 1f));
        Stretch(hint.rectTransform);

        List<PetInstance> materials = _selected != null ? manager.GetAbsorbMaterials(_selected) : new List<PetInstance>();
        if (_selectedMaterial == null && materials.Count > 0)
            _selectedMaterial = materials[0];
        BuildPetGrid(_body, materials, new Vector2(760f, -190f), 4, 2, true);

        GameObject benefit = NewPaper("Benefit", _body, new Vector2(760f, -405f), new Vector2(330f, 92f));
        Text benefitText = NewText("BenefitText", benefit.transform, "吞噬收益\n星级 +1\n随机提升1个技能", 18, TextAnchor.MiddleCenter, MutedInk);
        Stretch(benefitText.rectTransform);

        Button confirm = NewPlainButton("ConfirmAbsorb", _body, "吞噬", new Vector2(760f, -510f), new Vector2(190f, 62f), new Vector2(0f, 1f), GreenButton, Color.white);
        confirm.interactable = _selected != null && _selectedMaterial != null;
        confirm.onClick.AddListener(OnAbsorbClicked);

        Button back = NewPaperButton("BackButton", _body, "返回", new Vector2(545f, -510f), new Vector2(116f, 42f));
        back.onClick.AddListener(() =>
        {
            _mode = PanelMode.Detail;
            _selectedMaterial = null;
            Refresh();
        });
    }

    void DrawPetPortraitBlock(Transform parent, PetInstance pet, Vector2 center, bool showQuality)
    {
        PetConfig config = pet != null ? PetSystemManager.Instance.GetPetConfig(pet.PetId) : null;

        Image portrait = NewUI("Portrait", parent).AddComponent<Image>();
        portrait.sprite = LoadPetSprite(config);
        portrait.preserveAspect = true;
        SetRect(portrait.rectTransform, center, new Vector2(210f, 210f), new Vector2(0f, 1f));

        if (showQuality)
        {
            GameObject qualityGO = NewUI("Quality", parent);
            SetRect(qualityGO.GetComponent<RectTransform>(), center + new Vector2(-106f, 80f), new Vector2(66f, 66f), new Vector2(0f, 1f));
            qualityGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
            Text quality = NewText("QualityText", qualityGO.transform, "品质", 16, TextAnchor.MiddleCenter, InkColor);
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
            img.sprite = LoadSprite("UI/Pets/Button_Back");
            img.type = Image.Type.Sliced;
            img.color = i < count && pet.Talents[i].Rarity >= 2 ? BlueTalent : GoldTalent;

            string label = "?";
            if (i < count)
            {
                PetTalentInstance talent = pet.Talents[i];
                TalentSkillConfig talentConfig = manager.GetTalentConfig(talent.Attr);
                float value = talentConfig != null ? talentConfig.GetValue(talent.Rarity, talent.Level) : 0f;
                label = AttrName(talent.Attr) + "\n" + FormatTalentValue(talent.Attr, value);
            }

            Text text = NewText("Label", slot.transform, label, 16, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, 4f, 4f, 3f, 3f);
        }
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
            bg.sprite = LoadSprite("UI/Pets/Card_Back");
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.42f, 0.39f, 0.32f, 0.95f);

            if (i >= pets.Count)
            {
                Text plus = NewText("Empty", slot.transform, materialMode ? "+" : "", 42, TextAnchor.MiddleCenter, new Color(0.82f, 0.78f, 0.68f, 0.7f));
                Stretch(plus.rectTransform);
                continue;
            }

            PetInstance pet = pets[i];
            PetConfig config = PetSystemManager.Instance.GetPetConfig(pet.PetId);
            Image icon = NewUI("Icon", slot.transform).AddComponent<Image>();
            icon.sprite = LoadPetSprite(config);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform, 4f, 4f, 4f, 4f);

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
                SetRect(deployedGO.GetComponent<RectTransform>(), new Vector2(-26f, -12f), new Vector2(45f, 24f), new Vector2(0f, 1f));
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
        for (int i = _body.childCount - 1; i >= 0; i--)
            Destroy(_body.GetChild(i).gameObject);
        _spawned.Clear();
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
            case "AttackRate": return "强攻";
            case "DefenceRate": return "抵抗";
            case "HpRate": return "吸血";
            case "Agility": return "敏捷";
            case "CritRate": return "暴伤";
            case "CounterRate": return "反击";
            case "ComboRate": return "连击";
            case "DodgeRate": return "闪避";
            case "StunRate": return "击晕";
            case "LifeStealRate": return "吸血";
            default: return key;
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

    Sprite LoadSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    GameObject NewPaper(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = NewUI(name, parent);
        SetRect(go.GetComponent<RectTransform>(), pos, size, new Vector2(0f, 1f));
        Image image = go.AddComponent<Image>();
        image.sprite = LoadSprite("UI/Pets/Card_Back");
        image.type = Image.Type.Sliced;
        image.color = PaperSoft;
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
        image.sprite = LoadSprite("UI/Pets/Button_Back");
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
}
