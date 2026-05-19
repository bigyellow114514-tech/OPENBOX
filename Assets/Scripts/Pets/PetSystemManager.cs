using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class PetSystemManager : MonoBehaviour
{
    public static PetSystemManager Instance { get; private set; }

    const string SaveKey = "PetSystem.Save";
    const string PetsXlsxRelativePath = "Excel/Pets.xlsx";
    const int MaxPetCount = 30;
    const int GachaTicketCost = 1;

    readonly List<PetInstance> _pets = new List<PetInstance>();
    readonly Dictionary<int, PetConfig> _petConfigs = new Dictionary<int, PetConfig>();
    readonly Dictionary<int, TalentSkillNumConfig> _talentNums = new Dictionary<int, TalentSkillNumConfig>();
    readonly Dictionary<string, List<PetLevelConfig>> _levelConfigs = new Dictionary<string, List<PetLevelConfig>>();
    readonly List<TalentSkillConfig> _talentPool = new List<TalentSkillConfig>();
    readonly List<StarConfig> _stars = new List<StarConfig>();

    bool _tablesLoaded;
    bool _saveLoaded;

    public event Action OnPetsChanged;

    public IReadOnlyList<PetInstance> Pets
    {
        get
        {
            EnsureLoaded();
            return _pets;
        }
    }

    public int PetCount
    {
        get
        {
            EnsureLoaded();
            return _pets.Count;
        }
    }

    public int MaxCount => MaxPetCount;

    public PetInstance DeployedPet
    {
        get
        {
            EnsureLoaded();
            return _pets.Find(p => p.IsDeployed);
        }
    }

    public static PetSystemManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("PetSystemManager");
        return go.AddComponent<PetSystemManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureLoaded();
    }

    public PetConfig GetPetConfig(int petId)
    {
        EnsureLoaded();
        _petConfigs.TryGetValue(petId, out PetConfig config);
        return config;
    }

    public PetLevelConfig GetLevelConfig(int rarity, int level)
    {
        EnsureLoaded();
        string key = rarity.ToString();
        if (!_levelConfigs.TryGetValue(key, out List<PetLevelConfig> list)) return null;
        return list.Find(c => c.Level == level);
    }

    public TalentSkillConfig GetTalentConfig(string attr)
    {
        EnsureLoaded();
        return _talentPool.Find(t => string.Equals(t.Attr, attr, StringComparison.OrdinalIgnoreCase));
    }

    public int GetTalentMaxLevel(int petRarity)
    {
        EnsureLoaded();
        return _talentNums.TryGetValue(petRarity, out TalentSkillNumConfig config)
            ? Mathf.Max(1, config.TalentSkillMaxLevel)
            : 10;
    }

    public string GetStarColor(int absorbCount)
    {
        EnsureLoaded();
        if (absorbCount <= 0) return "empty";

        StarConfig star = _stars.Find(s => s.Star == absorbCount);
        if (star != null && !string.IsNullOrEmpty(star.Color)) return star.Color;

        int layer = Mathf.Max(0, (absorbCount - 1) / 5);
        switch (layer % 4)
        {
            case 0: return "yellow";
            case 1: return "purple";
            case 2: return "blue";
            default: return "red";
        }
    }

    public bool CanGacha(out string reason)
    {
        EnsureLoaded();
        if (_pets.Count >= MaxPetCount)
        {
            reason = "宠物已满，请先分解宠物";
            return false;
        }

        if (PlayerResourceManager.Instance == null || PlayerResourceManager.Instance.PetTickets < GachaTicketCost)
        {
            reason = "宠物券不足";
            return false;
        }

        if (_petConfigs.Count == 0 || GetTotalGachaWeight() <= 0f)
        {
            reason = "没有可抽取宠物配置";
            return false;
        }

        reason = "";
        return true;
    }

    public PetInstance Gacha()
    {
        if (!CanGacha(out _)) return null;
        PetConfig config = PickPetConfig();
        if (config == null) return null;
        if (!PlayerResourceManager.Instance.SpendPetTickets(GachaTicketCost)) return null;

        var pet = CreatePetInstance(config);
        _pets.Add(pet);
        if (_pets.Count == 1) pet.IsDeployed = true;
        SaveAndNotify();
        return pet;
    }

    public bool Deploy(string instanceId)
    {
        EnsureLoaded();
        PetInstance target = Find(instanceId);
        if (target == null) return false;

        foreach (PetInstance pet in _pets)
            pet.IsDeployed = pet == target;

        SaveAndNotify();
        return true;
    }

    public bool Upgrade(string instanceId, out string reason)
    {
        EnsureLoaded();
        PetInstance pet = Find(instanceId);
        if (pet == null)
        {
            reason = "未选择宠物";
            return false;
        }

        if (pet.Level >= 100)
        {
            reason = "宠物已满级";
            return false;
        }

        PetConfig config = GetPetConfig(pet.PetId);
        PetLevelConfig level = config != null ? GetLevelConfig(config.Rarity, pet.Level) : null;
        int cost = level != null ? Mathf.Max(0, level.FoodCost) : pet.Level * 100;
        if (cost <= 0)
        {
            reason = "没有升级消耗配置";
            return false;
        }

        if (PlayerResourceManager.Instance == null || !PlayerResourceManager.Instance.SpendPetFood(cost))
        {
            reason = "饲料不足";
            return false;
        }

        pet.Level++;
        reason = "";
        SaveAndNotify();
        return true;
    }

    public bool Decompose(string instanceId, out string reason)
    {
        EnsureLoaded();
        PetInstance pet = Find(instanceId);
        if (pet == null)
        {
            reason = "未选择宠物";
            return false;
        }

        if (pet.IsDeployed)
        {
            reason = "上阵宠物不能分解";
            return false;
        }

        int food = CalculateReturnFood(pet);
        _pets.Remove(pet);
        PlayerResourceManager.Instance?.AddPetFood(food);
        reason = "";
        SaveAndNotify();
        return true;
    }

    public bool Absorb(string targetId, string materialId, out string reason)
    {
        EnsureLoaded();
        PetInstance target = Find(targetId);
        PetInstance material = Find(materialId);
        if (target == null || material == null)
        {
            reason = "请选择目标宠物和素材宠物";
            return false;
        }

        if (target == material || target.PetId != material.PetId || material.IsDeployed)
        {
            reason = "只能吞噬未上阵的同名宠物";
            return false;
        }

        PetConfig config = GetPetConfig(target.PetId);
        int rarity = config != null ? config.Rarity : 1;
        int maxLevel = GetTalentMaxLevel(rarity);
        if (target.Talents == null || target.Talents.Count == 0 || target.Talents.TrueForAll(t => t.Level >= maxLevel))
        {
            reason = "天赋已满，不能继续吞噬";
            return false;
        }

        List<PetTalentInstance> candidates = target.Talents.FindAll(t => t.Level < maxLevel);
        PetTalentInstance talent = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        talent.Level++;
        target.AbsorbCount++;

        int food = CalculateReturnFood(material);
        _pets.Remove(material);
        PlayerResourceManager.Instance?.AddPetFood(food);

        reason = "";
        SaveAndNotify();
        return true;
    }

    public List<PetInstance> GetAbsorbMaterials(PetInstance target)
    {
        EnsureLoaded();
        if (target == null) return new List<PetInstance>();
        return _pets.FindAll(p => p != target && p.PetId == target.PetId && !p.IsDeployed);
    }

    public void ResetToDefault()
    {
        EnsureLoaded();
        _pets.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        SaveAndNotify();
    }

    public RoleAttr GetDeployedPetBonus(RoleAttr baseAttr)
    {
        EnsureLoaded();
        PetInstance pet = DeployedPet;
        if (pet == null) return default;

        RoleAttr bonus = default;
        PetConfig config = GetPetConfig(pet.PetId);
        if (config != null)
        {
            PetLevelConfig level = GetLevelConfig(config.Rarity, pet.Level);
            if (level != null)
            {
                AddRateBonus(ref bonus, baseAttr, "AttackRate", level.AttackRate);
                AddRateBonus(ref bonus, baseAttr, "DefenceRate", level.DefenceRate);
                AddRateBonus(ref bonus, baseAttr, "HpRate", level.HpRate);
            }
        }

        if (pet.Talents != null)
        {
            foreach (PetTalentInstance talent in pet.Talents)
            {
                TalentSkillConfig talentConfig = GetTalentConfig(talent.Attr);
                if (talentConfig == null) continue;
                float value = talentConfig.GetValue(talent.Rarity, talent.Level);
                AddRateBonus(ref bonus, baseAttr, talent.Attr, value);
            }
        }

        return bonus;
    }

    public int CalculateReturnFood(PetInstance pet)
    {
        if (pet == null) return 0;
        PetConfig config = GetPetConfig(pet.PetId);
        PetLevelConfig level = config != null ? GetLevelConfig(config.Rarity, pet.Level) : null;
        return level != null ? Mathf.Max(0, level.FoodReturn) : Mathf.Max(0, (pet.Level - 1) * 80);
    }

    public int GetUpgradeCost(PetInstance pet)
    {
        if (pet == null) return 0;
        PetConfig config = GetPetConfig(pet.PetId);
        PetLevelConfig level = config != null ? GetLevelConfig(config.Rarity, pet.Level) : null;
        return level != null ? Mathf.Max(0, level.FoodCost) : pet.Level * 100;
    }

    void EnsureLoaded()
    {
        LoadTables();
        LoadSave();
    }

    void LoadTables()
    {
        if (_tablesLoaded) return;
        _tablesLoaded = true;

        string path = Path.Combine(Application.dataPath, PetsXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[PetSystem] Pets.xlsx not found: " + path);
            CreateFallbackTables();
            return;
        }

        try
        {
            LoadPetConfigs(path);
            LoadLevelConfigs(path);
            LoadTalentNumConfigs(path);
            LoadTalentPool(path);
            LoadStars(path);
        }
        catch (Exception e)
        {
            Debug.LogError("[PetSystem] Failed to read Pets.xlsx: " + e.Message);
            CreateFallbackTables();
        }
    }

    void LoadPetConfigs(string path)
    {
        ExcelTable table = ExcelTable.LoadSheet(path, "Pets");
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            int petId = row.GetInt(columns, "PetID");
            if (petId <= 0) continue;

            _petConfigs[petId] = new PetConfig
            {
                PetId = petId,
                GachaWeight = Mathf.Max(0f, row.GetFloat(columns, "GachaWeight")),
                PetResource = row.Get(columns, "PetResource"),
                PetName = row.Get(columns, "PetName"),
                Rarity = Mathf.Max(1, row.GetInt(columns, "Rarity", 1)),
                SkillType = row.Get(columns, "SkillType"),
                Description = row.Get(columns, "Description"),
                CastInterval = Mathf.Max(1, row.GetInt(columns, "CastInterval", 1)),
                DamagePercent = row.GetFloat(columns, "DamagePercent"),
                HealPercent = row.GetFloat(columns, "HealPercent"),
                HasBuff = row.GetInt(columns, "HasBuff") != 0,
                AttrName = row.Get(columns, "AttrName"),
                AttrValue = row.GetFloat(columns, "AttrValue"),
                MaxStack = row.GetInt(columns, "MaxStack"),
                DurationRounds = row.GetInt(columns, "DurationRounds"),
            };
        }
    }

    void LoadLevelConfigs(string path)
    {
        ExcelTable table = ExcelTable.LoadSheet(path, "PetLevelup");
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            int rarity = row.GetInt(columns, "PetRarity");
            int level = row.GetInt(columns, "Level");
            if (rarity <= 0 || level <= 0) continue;

            string key = rarity.ToString();
            if (!_levelConfigs.TryGetValue(key, out List<PetLevelConfig> list))
            {
                list = new List<PetLevelConfig>();
                _levelConfigs[key] = list;
            }

            list.Add(new PetLevelConfig
            {
                PetRarity = rarity,
                Level = level,
                FoodCost = Mathf.RoundToInt(row.GetFloat(columns, "FoodCost")),
                AttackRate = row.GetFloat(columns, "AttackRate"),
                DefenceRate = row.GetFloat(columns, "DefenceRate"),
                HpRate = row.GetFloat(columns, "HpRate"),
                FoodReturn = Mathf.RoundToInt(row.GetFloat(columns, "FoodReturn")),
            });
        }
    }

    void LoadTalentNumConfigs(string path)
    {
        ExcelTable table = ExcelTable.LoadSheet(path, "TalentSkillNum");
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            int rarity = row.GetInt(columns, "Rarity");
            if (rarity <= 0) continue;

            _talentNums[rarity] = new TalentSkillNumConfig
            {
                Rarity = rarity,
                TalentSkillNum = Mathf.Max(1, row.GetInt(columns, "TalentSkillNum", 1)),
                TalentSkillMaxLevel = Mathf.Max(1, row.GetInt(columns, "TalentSkillMaxLevel", 10)),
                AbsorbLimit = Mathf.Max(0, row.GetInt(columns, "AbsorbLimit")),
            };
        }
    }

    void LoadTalentPool(string path)
    {
        ExcelTable table = ExcelTable.LoadSheet(path, "TalentSkill");
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            string attr = row.Get(columns, "Attr");
            if (string.IsNullOrWhiteSpace(attr)) continue;

            _talentPool.Add(new TalentSkillConfig
            {
                Attr = attr.Trim(),
                Name = row.Get(columns, "Name"),
                AttrWeight = row.GetFloat(columns, "AttrWeight"),
                Rarity1Weight = row.GetFloat(columns, "Rarity1Weight"),
                Rarity1Value = row.GetFloat(columns, "Rarity1Value"),
                Rarity1Delta = row.GetFloat(columns, "Rarity1Delta"),
                Rarity2Weight = row.GetFloat(columns, "Rarity2Weight"),
                Rarity2Value = row.GetFloat(columns, "Rarity2Value"),
                Rarity2Delta = row.GetFloat(columns, "Rarity2Delta"),
                Rarity3Weight = row.GetFloat(columns, "Rarity3Weight"),
                Rarity3Value = row.GetFloat(columns, "Rarity3Value"),
                Rarity3Delta = row.GetFloat(columns, "Rarity3Delta"),
                Rarity4Weight = row.GetFloat(columns, "Rarity4Weight"),
                Rarity4Value = row.GetFloat(columns, "Rarity4Value"),
                Rarity4Delta = row.GetFloat(columns, "Rarity4Delta"),
            });
        }
    }

    void LoadStars(string path)
    {
        ExcelTable table = ExcelTable.LoadSheet(path, "Stars");
        var columns = table.ReadHeader(2);
        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            int star = row.GetInt(columns, "Star");
            if (star <= 0) continue;
            _stars.Add(new StarConfig { Star = star, Color = row.Get(columns, "Color") });
        }
    }

    void LoadSave()
    {
        if (_saveLoaded) return;
        _saveLoaded = true;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var save = JsonUtility.FromJson<PetSaveData>(json);
            if (save?.Pets != null)
                _pets.AddRange(save.Pets);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PetSystem] Failed to load pet save: " + e.Message);
        }
    }

    void SaveAndNotify()
    {
        var save = new PetSaveData { Pets = _pets };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();

        PlayerCharacter.Instance?.RefreshFromPetSystem();
        OnPetsChanged?.Invoke();
    }

    PetInstance Find(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return null;
        return _pets.Find(p => p.InstanceId == instanceId);
    }

    PetConfig PickPetConfig()
    {
        float totalWeight = GetTotalGachaWeight();
        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (PetConfig config in _petConfigs.Values)
        {
            roll -= Mathf.Max(0f, config.GachaWeight);
            if (roll <= 0f)
                return config;
        }

        return null;
    }

    float GetTotalGachaWeight()
    {
        float totalWeight = 0f;
        foreach (PetConfig config in _petConfigs.Values)
            totalWeight += Mathf.Max(0f, config.GachaWeight);
        return totalWeight;
    }

    PetInstance CreatePetInstance(PetConfig config)
    {
        int talentNum = _talentNums.TryGetValue(config.Rarity, out TalentSkillNumConfig numConfig)
            ? numConfig.TalentSkillNum
            : Mathf.Min(2, _talentPool.Count);

        var instance = new PetInstance
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            PetId = config.PetId,
            Level = 1,
            AbsorbCount = 0,
            IsDeployed = false,
            Talents = new List<PetTalentInstance>(),
        };

        var usedAttrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < talentNum && usedAttrs.Count < _talentPool.Count; i++)
        {
            TalentSkillConfig talent = PickTalent(usedAttrs);
            if (talent == null) break;
            usedAttrs.Add(talent.Attr);
            instance.Talents.Add(new PetTalentInstance
            {
                Attr = talent.Attr,
                Rarity = PickTalentRarity(talent),
                Level = 1,
            });
        }

        return instance;
    }

    TalentSkillConfig PickTalent(HashSet<string> usedAttrs)
    {
        float total = 0f;
        foreach (TalentSkillConfig talent in _talentPool)
        {
            if (usedAttrs.Contains(talent.Attr)) continue;
            total += Mathf.Max(0f, talent.AttrWeight);
        }

        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);
        foreach (TalentSkillConfig talent in _talentPool)
        {
            if (usedAttrs.Contains(talent.Attr)) continue;
            roll -= Mathf.Max(0f, talent.AttrWeight);
            if (roll <= 0f) return talent;
        }

        return null;
    }

    int PickTalentRarity(TalentSkillConfig talent)
    {
        float w1 = Mathf.Max(0f, talent.Rarity1Weight);
        float w2 = Mathf.Max(0f, talent.Rarity2Weight);
        float w3 = Mathf.Max(0f, talent.Rarity3Weight);
        float total = w1 + w2 + w3;
        if (total <= 0f) return 1;

        float roll = UnityEngine.Random.Range(0f, total);
        if (roll < w1) return 1;
        if (roll < w1 + w2) return 2;
        return 3;
    }

    void AddRateBonus(ref RoleAttr bonus, RoleAttr baseAttr, string key, float value)
    {
        switch (key)
        {
            case "AttackRate":
                bonus.Attack += baseAttr.Attack * value / 100f;
                break;
            case "DefenceRate":
                bonus.Defence += baseAttr.Defence * value / 100f;
                break;
            case "HpRate":
                bonus.Hp += baseAttr.Hp * value / 100f;
                break;
            default:
                bonus.SetByKey(key, bonus.GetByKey(key) + value);
                break;
        }
    }

    void CreateFallbackTables()
    {
        if (_petConfigs.Count == 0)
        {
            _petConfigs[101] = new PetConfig { PetId = 101, GachaWeight = 1f, PetName = "输出宝宝", PetResource = "Pet_101", Rarity = 1, Description = "攻击型宠物" };
            _petConfigs[102] = new PetConfig { PetId = 102, GachaWeight = 1f, PetName = "奶妈宝宝", PetResource = "Pet_102", Rarity = 2, Description = "治疗型宠物" };
            _petConfigs[103] = new PetConfig { PetId = 103, GachaWeight = 1f, PetName = "暴击宝宝", PetResource = "Pet_103", Rarity = 3, Description = "增益型宠物" };
        }

        if (_talentPool.Count == 0)
        {
            _talentPool.Add(new TalentSkillConfig { Attr = "AttackRate", AttrWeight = 10, Rarity1Weight = 60, Rarity1Value = 5, Rarity1Delta = 5, Rarity2Weight = 25, Rarity2Value = 8, Rarity2Delta = 8, Rarity3Weight = 10, Rarity3Value = 12, Rarity3Delta = 12 });
            _talentPool.Add(new TalentSkillConfig { Attr = "HpRate", AttrWeight = 10, Rarity1Weight = 60, Rarity1Value = 5, Rarity1Delta = 5, Rarity2Weight = 25, Rarity2Value = 8, Rarity2Delta = 8, Rarity3Weight = 10, Rarity3Value = 12, Rarity3Delta = 12 });
        }

        for (int rarity = 1; rarity <= 3; rarity++)
        {
            if (!_talentNums.ContainsKey(rarity))
                _talentNums[rarity] = new TalentSkillNumConfig { Rarity = rarity, TalentSkillNum = rarity + 1, TalentSkillMaxLevel = 10, AbsorbLimit = (rarity + 1) * 9 };

            string key = rarity.ToString();
            if (!_levelConfigs.ContainsKey(key))
            {
                var list = new List<PetLevelConfig>();
                for (int level = 1; level <= 100; level++)
                    list.Add(new PetLevelConfig { PetRarity = rarity, Level = level, FoodCost = level * 100, AttackRate = level * 0.5f * rarity, DefenceRate = level * 0.5f * rarity, HpRate = level * 0.5f * rarity, FoodReturn = Mathf.Max(0, (level - 1) * 80) });
                _levelConfigs[key] = list;
            }
        }
    }
}

[Serializable]
public sealed class PetInstance
{
    public string InstanceId;
    public int PetId;
    public int Level = 1;
    public int AbsorbCount;
    public bool IsDeployed;
    public List<PetTalentInstance> Talents = new List<PetTalentInstance>();
}

[Serializable]
public sealed class PetTalentInstance
{
    public string Attr;
    public int Rarity = 1;
    public int Level = 1;
}

[Serializable]
public sealed class PetSaveData
{
    public List<PetInstance> Pets = new List<PetInstance>();
}

public sealed class PetConfig
{
    public int PetId;
    public float GachaWeight;
    public string PetResource;
    public string PetName;
    public int Rarity;
    public string SkillType;
    public string Description;
    public int CastInterval;
    public float DamagePercent;
    public float HealPercent;
    public bool HasBuff;
    public string AttrName;
    public float AttrValue;
    public int MaxStack;
    public int DurationRounds;
}

public sealed class PetLevelConfig
{
    public int PetRarity;
    public int Level;
    public int FoodCost;
    public float AttackRate;
    public float DefenceRate;
    public float HpRate;
    public int FoodReturn;
}

public sealed class TalentSkillNumConfig
{
    public int Rarity;
    public int TalentSkillNum;
    public int TalentSkillMaxLevel;
    public int AbsorbLimit;
}

public sealed class TalentSkillConfig
{
    public string Attr;
    public string Name;
    public float AttrWeight;
    public float Rarity1Weight;
    public float Rarity1Value;
    public float Rarity1Delta;
    public float Rarity2Weight;
    public float Rarity2Value;
    public float Rarity2Delta;
    public float Rarity3Weight;
    public float Rarity3Value;
    public float Rarity3Delta;
    public float Rarity4Weight;
    public float Rarity4Value;
    public float Rarity4Delta;

    public float GetValue(int rarity, int level)
    {
        int safeLevel = Mathf.Max(1, level);
        switch (rarity)
        {
            case 2: return Rarity2Value + Rarity2Delta * (safeLevel - 1);
            case 3: return Rarity3Value + Rarity3Delta * (safeLevel - 1);
            case 4: return Rarity4Value + Rarity4Delta * (safeLevel - 1);
            default: return Rarity1Value + Rarity1Delta * (safeLevel - 1);
        }
    }
}

public sealed class StarConfig
{
    public int Star;
    public string Color;
}
