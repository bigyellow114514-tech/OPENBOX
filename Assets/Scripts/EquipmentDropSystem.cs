using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EquipmentDropSystem : MonoBehaviour
{
    public static EquipmentDropSystem Instance { get; private set; }

    [SerializeField] Sprite[] itemIcons;
    [SerializeField] string[] itemNameTable;

    const int NumSlots = 12;
    const int NumBands = 8;
    static readonly int[] LevelLimits = { 9, 19, 29, 39, 49, 59, 69, 79 };

    static readonly string[] BattleAttrKeys =
    {
        "CritRate", "CounterRate", "ComboRate", "DodgeRate", "StunRate", "LifeStealRate"
    };

    static readonly string[] AntiBattleAttrKeys =
    {
        "AntiCritRate", "AntiCounterRate", "AntiComboRate", "AntiDodgeRate", "AntiStunRate", "AntiLifeStealRate"
    };

    readonly List<RoleAttr> _levelAttrs = new();
    readonly List<RoleAttr> _rarityRatios = new();
    readonly List<RoleAttr> _slotRatios = new();
    readonly List<RoleAttr> _attrRandomRanges = new();
    readonly Dictionary<string, float> _globalConfig = new();

    int _battleAttrLevelLimit = 10;
    int _battleAttrRareLimit = 2;
    int _antiBattleAttrLevelLimit = 15;
    int _antiBattleAttrRareLimit = 3;
    float _attrRandomRange = 0.2f;
    bool _tablesLoaded;

    static readonly string[] SlotNames =
    {
        "武器", "副手", "头盔", "胸甲", "腿甲", "鞋子",
        "护手", "腰带", "项链", "主戒", "副戒", "圣物"
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        LoadTables();
    }

    public EquipmentResult GenerateDrop(int rarityIndex, int playerLevel)
    {
        LoadTables();

        int slot = Random.Range(0, SlotNames.Length);
        int maxLevel = Mathf.Max(1, _levelAttrs.Count);
        int equipLevel = Mathf.Clamp(playerLevel + Random.Range(-3, 4), 1, maxLevel);
        int rarity = Mathf.Clamp(rarityIndex + 1, 1, Mathf.Max(1, _rarityRatios.Count));

        RoleAttr baseAttr = GetLevelAttr(equipLevel);
        RoleAttr rarityRatio = GetRatio(_rarityRatios, rarity - 1);
        RoleAttr slotRatio = GetRatio(_slotRatios, slot);

        RoleAttr randomRange = GetRandomRangeAttr(equipLevel);

        RoleAttr bonusAttr = new RoleAttr
        {
            Attack = CalculateFinalAttr(baseAttr, rarityRatio, slotRatio, randomRange, "Attack", true),
            Defence = CalculateFinalAttr(baseAttr, rarityRatio, slotRatio, randomRange, "Defence", true),
            Hp = CalculateFinalAttr(baseAttr, rarityRatio, slotRatio, randomRange, "Hp", true),
            Agility = CalculateFinalAttr(baseAttr, rarityRatio, slotRatio, randomRange, "Agility", true),
        };

        if (equipLevel > _battleAttrLevelLimit && rarity > _battleAttrRareLimit)
            AddRandomExtraAttr(ref bonusAttr, baseAttr, rarityRatio, slotRatio, randomRange, BattleAttrKeys);

        if (equipLevel > _antiBattleAttrLevelLimit && rarity > _antiBattleAttrRareLimit)
            AddRandomExtraAttr(ref bonusAttr, baseAttr, rarityRatio, slotRatio, randomRange, AntiBattleAttrKeys);

        return new EquipmentResult
        {
            slotIndex = slot,
            itemName = GetItemName(slot, equipLevel),
            slotName = SlotNames[slot],
            icon = GetIcon(slot, equipLevel),
            rarity = rarity,
            equipLevel = equipLevel,
            bonusAttr = bonusAttr
        };
    }

    public Sprite GetIconForLevel(int slot, int level) => GetIcon(slot, level);

    void AddRandomExtraAttr(ref RoleAttr attr, RoleAttr baseAttr, RoleAttr rarityRatio, RoleAttr slotRatio, RoleAttr randomRange, string[] keys)
    {
        if (keys == null || keys.Length == 0) return;

        string key = keys[Random.Range(0, keys.Length)];
        attr.SetByKey(key, CalculateFinalAttr(baseAttr, rarityRatio, slotRatio, randomRange, key, false));
    }

    void LoadTables()
    {
        if (_tablesLoaded) return;
        _tablesLoaded = true;

        _levelAttrs.Clear();
        _rarityRatios.Clear();
        _slotRatios.Clear();
        _attrRandomRanges.Clear();
        _globalConfig.Clear();

        LoadAttrRows(Path.Combine(Application.dataPath, "Excel/Equipment.xlsx"), "Level", _levelAttrs);
        LoadAttrRows(Path.Combine(Application.dataPath, "Excel/EquipmentRarityRatio.xlsx"), "Rarity", _rarityRatios);
        LoadAttrRows(Path.Combine(Application.dataPath, "Excel/EquipmentSlotRatio.xlsx"), "Slot", _slotRatios);
        LoadAttrRandomRows(Path.Combine(Application.dataPath, "Excel/EquipmentAttrRandom.xlsx"), _attrRandomRanges);
        LoadGlobalConfig(Path.Combine(Application.dataPath, "Excel/GlobalConfig.xlsx"));

        _battleAttrLevelLimit = Mathf.RoundToInt(GetConfig("BattleAttrLevelLimit", _battleAttrLevelLimit));
        _battleAttrRareLimit = Mathf.RoundToInt(GetConfig("BattleAttrRareLimit", _battleAttrRareLimit));
        _antiBattleAttrLevelLimit = Mathf.RoundToInt(GetConfig("AntiBattleAttrLevelLimit", _antiBattleAttrLevelLimit));
        _antiBattleAttrRareLimit = Mathf.RoundToInt(GetConfig("AntiBattleAttrRareLimit", _antiBattleAttrRareLimit));
        float fallbackRandomRange = GetConfig("EuipmentAttrRandomRange", _attrRandomRange);
        _attrRandomRange = Mathf.Max(0f, GetConfig("EquipmentAttrRandomRange", fallbackRandomRange));
    }

    static void LoadAttrRows(string path, string idColumn, List<RoleAttr> target)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[EquipmentDropSystem] 找不到表格：{path}");
            return;
        }

        var table = ExcelTable.Load(path);
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            if (row.GetInt(columns, idColumn, -1) < 0) continue;
            target.Add(ReadAttr(row, columns));
        }
    }

    void LoadGlobalConfig(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[EquipmentDropSystem] 找不到表格：{path}");
            return;
        }

        var table = ExcelTable.Load(path);
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            string key = row.Get(columns, "ParamName");
            if (string.IsNullOrWhiteSpace(key)) continue;
            _globalConfig[key.Trim()] = row.GetFloat(columns, "Value");
        }
    }

    float GetConfig(string key, float defaultValue)
    {
        return _globalConfig.TryGetValue(key, out float value) ? value : defaultValue;
    }

    RoleAttr GetLevelAttr(int level)
    {
        if (_levelAttrs.Count == 0)
            return new RoleAttr { Attack = 100, Defence = 20, Hp = 1000, Agility = 10 };

        return _levelAttrs[Mathf.Clamp(level - 1, 0, _levelAttrs.Count - 1)];
    }

    RoleAttr GetRandomRangeAttr(int level)
    {
        if (_attrRandomRanges.Count == 0)
            return CreateUniformRandomRange(_attrRandomRange);

        return _attrRandomRanges[Mathf.Clamp(level - 1, 0, _attrRandomRanges.Count - 1)];
    }

    static RoleAttr GetRatio(List<RoleAttr> ratios, int index)
    {
        if (ratios.Count == 0)
            return new RoleAttr
            {
                Attack = 1f, Defence = 1f, Hp = 1f, Agility = 1f,
                CritRate = 1f, CounterRate = 1f, ComboRate = 1f, DodgeRate = 1f, StunRate = 1f, LifeStealRate = 1f,
                AntiCritRate = 1f, AntiCounterRate = 1f, AntiComboRate = 1f, AntiDodgeRate = 1f, AntiStunRate = 1f, AntiLifeStealRate = 1f,
            };

        return ratios[Mathf.Clamp(index, 0, ratios.Count - 1)];
    }

    static RoleAttr ReadAttr(ExcelRow row, Dictionary<string, int> columns)
    {
        return new RoleAttr
        {
            Attack = row.GetFloat(columns, "Attack"),
            Defence = row.GetFloat(columns, "Defence"),
            Hp = row.GetFloat(columns, "Hp"),
            Agility = row.GetFloat(columns, "Agility"),
            CritRate = row.GetFloat(columns, "CritRate"),
            CounterRate = row.GetFloat(columns, "CounterRate"),
            ComboRate = row.GetFloat(columns, "ComboRate"),
            DodgeRate = row.GetFloat(columns, "DodgeRate"),
            StunRate = row.GetFloat(columns, "StunRate"),
            LifeStealRate = row.GetFloat(columns, "LifeStealRate"),
            AntiCritRate = row.GetFloat(columns, "AntiCritRate"),
            AntiCounterRate = row.GetFloat(columns, "AntiCounterRate"),
            AntiComboRate = row.GetFloat(columns, "AntiComboRate"),
            AntiDodgeRate = row.GetFloat(columns, "AntiDodgeRate"),
            AntiStunRate = row.GetFloat(columns, "AntiStunRate"),
            AntiLifeStealRate = row.GetFloat(columns, "AntiLifeStealRate"),
        };
    }

    static void LoadAttrRandomRows(string path, List<RoleAttr> target)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[EquipmentDropSystem] Missing EquipmentAttrRandom table: {path}");
            return;
        }

        var table = ExcelTable.Load(path);
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            if (row.GetInt(columns, "Level", -1) < 0) continue;
            target.Add(ReadAttrRandomRange(row, columns));
        }
    }

    static RoleAttr ReadAttrRandomRange(ExcelRow row, Dictionary<string, int> columns)
    {
        return new RoleAttr
        {
            Attack = NormalizeRandomRange(row.GetFloat(columns, "AttackRandomRange")),
            Defence = NormalizeRandomRange(row.GetFloat(columns, "DefenceRandomRange")),
            Hp = NormalizeRandomRange(row.GetFloat(columns, "HpRandomRange")),
            Agility = NormalizeRandomRange(row.GetFloat(columns, "AgilityRandomRange")),
            CritRate = NormalizeRandomRange(row.GetFloat(columns, "CritRateRandomRange")),
            CounterRate = NormalizeRandomRange(row.GetFloat(columns, "CounterRateRandomRange")),
            ComboRate = NormalizeRandomRange(row.GetFloat(columns, "ComboRateRandomRange")),
            DodgeRate = NormalizeRandomRange(row.GetFloat(columns, "DodgeRateRandomRange")),
            StunRate = NormalizeRandomRange(row.GetFloat(columns, "StunRateRandomRange")),
            LifeStealRate = NormalizeRandomRange(row.GetFloat(columns, "LifeStealRateRandomRange")),
            AntiCritRate = NormalizeRandomRange(row.GetFloat(columns, "AntiCritRateRandomRange")),
            AntiCounterRate = NormalizeRandomRange(row.GetFloat(columns, "AntiCounterRateRandomRange")),
            AntiComboRate = NormalizeRandomRange(row.GetFloat(columns, "AntiComboRateRandomRange")),
            AntiDodgeRate = NormalizeRandomRange(row.GetFloat(columns, "AntiDodgeRateRandomRange")),
            AntiStunRate = NormalizeRandomRange(row.GetFloat(columns, "AntiStunRateRandomRange")),
            AntiLifeStealRate = NormalizeRandomRange(row.GetFloat(columns, "AntiLifeStealRateRandomRange")),
        };
    }

    static RoleAttr CreateUniformRandomRange(float range)
    {
        range = NormalizeRandomRange(range);
        return new RoleAttr
        {
            Attack = range,
            Defence = range,
            Hp = range,
            Agility = range,
            CritRate = range,
            CounterRate = range,
            ComboRate = range,
            DodgeRate = range,
            StunRate = range,
            LifeStealRate = range,
            AntiCritRate = range,
            AntiCounterRate = range,
            AntiComboRate = range,
            AntiDodgeRate = range,
            AntiStunRate = range,
            AntiLifeStealRate = range,
        };
    }

    static float CalculateFinalAttr(RoleAttr baseAttr, RoleAttr rarityRatio, RoleAttr slotRatio, RoleAttr randomRange, string key, bool roundToInt)
    {
        float range = GetAttrValue(randomRange, key);
        float randomFactor = Random.Range(1f - range, 1f + range);
        float value = GetAttrValue(baseAttr, key) * GetAttrValue(rarityRatio, key) * GetAttrValue(slotRatio, key) * randomFactor;
        return roundToInt ? Mathf.Round(value) : Mathf.Round(value * 10f) / 10f;
    }

    static float NormalizeRandomRange(float value)
    {
        value = Mathf.Abs(value);
        return value > 1f ? value / 100f : value;
    }

    static float GetAttrValue(RoleAttr attr, string key)
    {
        switch (key)
        {
            case "Attack": return attr.Attack;
            case "Defence": return attr.Defence;
            case "Hp": return attr.Hp;
            case "Agility": return attr.Agility;
            case "CritRate": return attr.CritRate;
            case "CounterRate": return attr.CounterRate;
            case "ComboRate": return attr.ComboRate;
            case "DodgeRate": return attr.DodgeRate;
            case "StunRate": return attr.StunRate;
            case "LifeStealRate": return attr.LifeStealRate;
            case "AntiCritRate": return attr.AntiCritRate;
            case "AntiCounterRate": return attr.AntiCounterRate;
            case "AntiComboRate": return attr.AntiComboRate;
            case "AntiDodgeRate": return attr.AntiDodgeRate;
            case "AntiStunRate": return attr.AntiStunRate;
            case "AntiLifeStealRate": return attr.AntiLifeStealRate;
            default: return 0f;
        }
    }

    Sprite GetIcon(int slot, int playerLevel)
    {
        if (itemIcons == null || itemIcons.Length == 0) return null;
        int band = GetLevelBand(playerLevel);
        int idx = slot * NumBands + band;
        return itemIcons[Mathf.Clamp(idx, 0, itemIcons.Length - 1)];
    }

    string GetItemName(int slot, int playerLevel)
    {
        int band = GetLevelBand(playerLevel);
        int index = slot * NumBands + band;
        if (itemNameTable != null && index < itemNameTable.Length)
        {
            string name = itemNameTable[index];
            if (!string.IsNullOrEmpty(name)) return name;
        }

        return SlotNames[Mathf.Clamp(slot, 0, SlotNames.Length - 1)];
    }

    static int GetLevelBand(int playerLevel)
    {
        for (int i = 0; i < LevelLimits.Length; i++)
            if (playerLevel <= LevelLimits[i]) return i;
        return LevelLimits.Length - 1;
    }
}
