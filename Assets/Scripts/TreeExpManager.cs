using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class TreeExpManager : MonoBehaviour
{
    public static TreeExpManager Instance { get; private set; }

    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; } = 0f;
    public float ExpToNextLevel => GetLvUpCost(Level);

    public int UpgradeTier { get; private set; } = 0;
    // Tier 0 = Tree Lv.1 (base), Tier 35 = Tree Lv.36 (max)
    public const int MaxUpgradeTier = 35;
    public int UpgradeLevel => UpgradeTier + 1;
    public bool IsUpgradeInProgress { get; private set; }
    public int UpgradingTargetLevel => IsUpgradeInProgress ? Mathf.Min(UpgradeLevel + 1, MaxLevel) : UpgradeLevel;
    public float UpgradeRemainingSeconds => IsUpgradeInProgress ? Mathf.Max(0f, (float)(_upgradeEndUtc - System.DateTime.UtcNow).TotalSeconds) : 0f;

    public event System.Action OnExpChanged;
    public event System.Action OnUpgradeTierChanged;

    const int MaxLevel = 36;
    const string KeyLevel       = "TreeLevel";
    const string KeyExp         = "TreeExp";
    const string KeyUpgradeTier = "TreeUpgradeTier";
    const string KeyUpgradeInProgress = "TreeUpgradeInProgress";
    const string KeyUpgradeEndUtcTicks = "TreeUpgradeEndUtcTicks";
    const string KeyUpgradeStartTier = "TreeUpgradeStartTier";
    const string TreeXlsxRelativePath = "Excel/Tree.xlsx";
    const string ItemXlsxRelativePath = "Excel/Item.xlsx";

    static readonly List<TreeLevelConfig> _configs = new();
    static readonly Dictionary<int, ItemConfig> _items = new();
    System.DateTime _upgradeEndUtc;
    int _upgradeStartTier;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        LoadTreeConfig();
        LoadItemConfig();
        Load();
        RefreshUpgradeProgress();
    }

    void Update()
    {
        RefreshUpgradeProgress();
    }

    public void AddExp(float amount)
    {
        if (Level >= MaxLevel) return;

        CurrentExp += amount;

        while (Level < MaxLevel && CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;
        }

        if (Level >= MaxLevel)
            CurrentExp = 0f;

        Save();
        OnExpChanged?.Invoke();
    }

    // Returns gold cost to upgrade from current UpgradeTier to next.
    // Matches Tree.xlsx LvUpCost: level n costs n*1000 gold (1→2=1000, 2→3=2000 …)
    // Returns 0 if already at max tier.
    public int GetNextUpgradeCost()
    {
        if (UpgradeTier >= MaxUpgradeTier) return 0;
        return GetConfigForTier(UpgradeTier).LvUpCost;
    }

    public int GetNextUpgradeTimeSeconds()
    {
        if (UpgradeTier >= MaxUpgradeTier) return 0;
        return GetConfigForTier(UpgradeTier).LvUpTime;
    }

    // Spends gold and starts the upgrade countdown. Returns true on success.
    public bool TryUpgrade()
    {
        return TryStartUpgrade();
    }

    public bool TryStartUpgrade()
    {
        if (UpgradeTier >= MaxUpgradeTier) return false;
        if (IsUpgradeInProgress) return false;

        int cost = GetNextUpgradeCost();
        var res  = PlayerResourceManager.Instance;
        if (res == null || !res.SpendGold(cost)) return false;

        _upgradeStartTier = UpgradeTier;
        int duration = GetNextUpgradeTimeSeconds();
        if (duration <= 0)
        {
            CompleteUpgrade();
            return true;
        }

        _upgradeEndUtc = System.DateTime.UtcNow.AddSeconds(duration);
        IsUpgradeInProgress = true;
        Save();
        OnUpgradeTierChanged?.Invoke();
        return true;
    }

    public bool TrySpeedUpUpgrade(int bottleCount = 1)
    {
        if (!IsUpgradeInProgress || bottleCount <= 0) return false;

        var res = PlayerResourceManager.Instance;
        if (res == null || !res.SpendMagicBottles(bottleCount)) return false;

        int reduceSeconds = Mathf.RoundToInt(GetMagicBottleMinutes() * 60f * bottleCount);
        _upgradeEndUtc = _upgradeEndUtc.AddSeconds(-reduceSeconds);
        RefreshUpgradeProgress();
        Save();
        OnUpgradeTierChanged?.Invoke();
        return true;
    }

    void Save()
    {
        PlayerPrefs.SetInt(KeyLevel, Level);
        PlayerPrefs.SetFloat(KeyExp, CurrentExp);
        PlayerPrefs.SetInt(KeyUpgradeTier, UpgradeTier);
        PlayerPrefs.SetInt(KeyUpgradeInProgress, IsUpgradeInProgress ? 1 : 0);
        PlayerPrefs.SetString(KeyUpgradeEndUtcTicks, _upgradeEndUtc.Ticks.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(KeyUpgradeStartTier, _upgradeStartTier);
        PlayerPrefs.Save();
    }

    void Load()
    {
        Level       = PlayerPrefs.GetInt(KeyLevel, 1);
        CurrentExp  = PlayerPrefs.GetFloat(KeyExp, 0f);
        UpgradeTier = Mathf.Clamp(PlayerPrefs.GetInt(KeyUpgradeTier, 0), 0, MaxUpgradeTier);
        IsUpgradeInProgress = PlayerPrefs.GetInt(KeyUpgradeInProgress, 0) != 0;
        _upgradeStartTier = Mathf.Clamp(PlayerPrefs.GetInt(KeyUpgradeStartTier, UpgradeTier), 0, MaxUpgradeTier);
        string ticksRaw = PlayerPrefs.GetString(KeyUpgradeEndUtcTicks, "0");
        _upgradeEndUtc = long.TryParse(ticksRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) && ticks > 0
            ? new System.DateTime(ticks, System.DateTimeKind.Utc)
            : System.DateTime.UtcNow;
    }

    public void ResetToDefault()
    {
        Level       = 1;
        CurrentExp  = 0f;
        UpgradeTier = 0;
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyExp);
        PlayerPrefs.DeleteKey(KeyUpgradeTier);
        PlayerPrefs.DeleteKey(KeyUpgradeInProgress);
        PlayerPrefs.DeleteKey(KeyUpgradeEndUtcTicks);
        PlayerPrefs.DeleteKey(KeyUpgradeStartTier);
        IsUpgradeInProgress = false;
        _upgradeStartTier = 0;
        _upgradeEndUtc = System.DateTime.UtcNow;
        PlayerPrefs.Save();
        OnExpChanged?.Invoke();
        OnUpgradeTierChanged?.Invoke();
    }

    void RefreshUpgradeProgress()
    {
        if (!IsUpgradeInProgress) return;
        if (System.DateTime.UtcNow < _upgradeEndUtc) return;

        CompleteUpgrade();
    }

    void CompleteUpgrade()
    {
        UpgradeTier = Mathf.Clamp(_upgradeStartTier + 1, 0, MaxUpgradeTier);
        IsUpgradeInProgress = false;
        _upgradeStartTier = UpgradeTier;
        _upgradeEndUtc = System.DateTime.UtcNow;
        Save();
        OnUpgradeTierChanged?.Invoke();
    }

    static float GetLvUpCost(int level)
    {
        return GetConfigForTreeLevel(level).LvUpCost;
    }

    // Returns Rare1-Rare6 weights based on current UpgradeTier (treeLevel retained for API compat)
    public static void GetBoxWeights(int treeLevel, int[] result)
    {
        TreeLevelConfig config = Instance != null
            ? GetConfigForTier(Instance.UpgradeTier)
            : GetConfigForTreeLevel(treeLevel);
        for (int i = 0; i < 6; i++)
            result[i] = config.RareWeights[i];
    }

    public static List<TreeItemDrop> RollItemDrops(int treeLevel)
    {
        TreeLevelConfig config = GetConfigForTreeLevel(treeLevel);
        var drops = new List<TreeItemDrop>();

        for (int i = 0; i < config.ItemDrops.Count; i++)
        {
            TreeItemDropConfig drop = config.ItemDrops[i];
            float chance = drop.DropRate > 1f ? drop.DropRate / 100f : drop.DropRate;
            if (drop.ItemId > 0 && drop.DropNum > 0 && Random.value < Mathf.Clamp01(chance))
                drops.Add(new TreeItemDrop(drop.ItemId, drop.DropNum));
        }

        return drops;
    }

    public static TreeLevelView GetLevelView(int treeLevel)
    {
        TreeLevelConfig config = GetConfigForTreeLevel(treeLevel);
        return new TreeLevelView(config);
    }

    public static TreeLevelView GetCurrentLevelView()
    {
        int level = Instance != null ? Instance.UpgradeLevel : 1;
        return GetLevelView(level);
    }

    public static TreeLevelView GetNextLevelView()
    {
        int level = Instance != null ? Mathf.Min(Instance.UpgradeLevel + 1, MaxLevel) : 2;
        return GetLevelView(level);
    }

    public static ItemView GetItemView(int itemId)
    {
        if (_items.Count == 0)
            LoadItemConfig();

        return _items.TryGetValue(itemId, out ItemConfig item)
            ? new ItemView(item)
            : new ItemView(itemId, "", "", 0f, "");
    }

    public static float GetMagicBottleMinutes()
    {
        ItemView item = GetItemView(PlayerResourceManager.ItemIdMagicBottle);
        return item.Value > 0f ? item.Value : 5f;
    }

    static void LoadTreeConfig()
    {
        _configs.Clear();
        string path = Path.Combine(Application.dataPath, TreeXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[TreeExpManager] Tree.xlsx not found, using fallback config: " + path);
            BuildFallbackConfig();
            return;
        }

        try
        {
            ExcelTable table = ExcelTable.Load(path);
            Dictionary<string, int> columns = table.ReadHeader(2);
            for (int i = 3; i < table.Rows.Count; i++)
            {
                ExcelRow row = table.Rows[i];
                int treeLevel = row.GetInt(columns, "TreeLevel", -1);
                if (treeLevel <= 0) continue;

                var config = new TreeLevelConfig
                {
                    TreeLevel = treeLevel,
                    LvUpCost = Mathf.Max(0, row.GetInt(columns, "LvUpCost", treeLevel * 1000)),
                    LvUpTime = Mathf.Max(0, row.GetInt(columns, "LvUpTime", 0)),
                    RareWeights = new[]
                    {
                        Mathf.Max(0, row.GetInt(columns, "Rare1Weight")),
                        Mathf.Max(0, row.GetInt(columns, "Rare2Weight")),
                        Mathf.Max(0, row.GetInt(columns, "Rare3Weight")),
                        Mathf.Max(0, row.GetInt(columns, "Rare4Weight")),
                        Mathf.Max(0, row.GetInt(columns, "Rare5Weight")),
                        Mathf.Max(0, row.GetInt(columns, "Rare6Weight")),
                    },
                    RareRates = new[]
                    {
                        NormalizeRate(row.GetFloat(columns, "Rare1Rate")),
                        NormalizeRate(row.GetFloat(columns, "Rare2Rate")),
                        NormalizeRate(row.GetFloat(columns, "Rare3Rate")),
                        NormalizeRate(row.GetFloat(columns, "Rare4Rate")),
                        NormalizeRate(row.GetFloat(columns, "Rare5Rate")),
                        NormalizeRate(row.GetFloat(columns, "Rare6Rate")),
                    },
                };
                config.ItemDrops.AddRange(ParseItemDrops(row, columns));
                _configs.Add(config);
            }

            _configs.Sort((a, b) => a.TreeLevel.CompareTo(b.TreeLevel));
            if (_configs.Count == 0)
                BuildFallbackConfig();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TreeExpManager] Failed to read Tree.xlsx, using fallback config: " + e.Message);
            BuildFallbackConfig();
        }
    }

    static List<TreeItemDropConfig> ParseItemDrops(ExcelRow row, Dictionary<string, int> columns)
    {
        string[] ids = SplitList(row.Get(columns, "UnlockDropItemID"));
        string[] rates = SplitList(row.Get(columns, "DropRate"));
        string[] nums = SplitList(row.Get(columns, "DropNum"));
        int count = Mathf.Min(ids.Length, Mathf.Min(rates.Length, nums.Length));

        if (ids.Length != rates.Length || ids.Length != nums.Length)
            Debug.LogWarning("[TreeExpManager] Drop config length mismatch at TreeLevel " + row.Get(columns, "TreeLevel"));

        var drops = new List<TreeItemDropConfig>();
        for (int i = 0; i < count; i++)
        {
            if (!int.TryParse(ids[i], out int itemId)) continue;
            if (!float.TryParse(rates[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float rate)) continue;
            if (!int.TryParse(nums[i], out int num)) continue;

            drops.Add(new TreeItemDropConfig
            {
                ItemId = itemId,
                DropRate = Mathf.Max(0f, rate),
                DropNum = Mathf.Max(0, num),
            });
        }

        return drops;
    }

    static string[] SplitList(string raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? System.Array.Empty<string>()
            : raw.Split(';').Select(v => v.Trim()).Where(v => v.Length > 0).ToArray();
    }

    static float NormalizeRate(float raw)
    {
        return raw > 1f ? raw / 100f : Mathf.Max(0f, raw);
    }

    static void LoadItemConfig()
    {
        _items.Clear();
        string path = Path.Combine(Application.dataPath, ItemXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[TreeExpManager] Item.xlsx not found: " + path);
            return;
        }

        try
        {
            ExcelTable table = ExcelTable.Load(path);
            Dictionary<string, int> columns = table.ReadHeader(2);
            for (int i = 3; i < table.Rows.Count; i++)
            {
                ExcelRow row = table.Rows[i];
                int itemId = row.GetInt(columns, "ItemID", -1);
                if (itemId <= 0) continue;

                _items[itemId] = new ItemConfig
                {
                    ItemId = itemId,
                    ItemName = row.Get(columns, "ItemName"),
                    IconName = row.Get(columns, "iconName"),
                    Value = row.GetFloat(columns, "Value"),
                    ValueMeaning = row.Get(columns, "ValueMeaning"),
                };
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TreeExpManager] Failed to read Item.xlsx: " + e.Message);
        }
    }

    static TreeLevelConfig GetConfigForTier(int tier)
    {
        int treeLevel = Mathf.Clamp(tier, 0, MaxUpgradeTier) + 1;
        return GetConfigForTreeLevel(treeLevel);
    }

    static TreeLevelConfig GetConfigForTreeLevel(int treeLevel)
    {
        if (_configs.Count == 0)
            BuildFallbackConfig();

        treeLevel = Mathf.Clamp(treeLevel, 1, MaxLevel);
        TreeLevelConfig exact = _configs.FirstOrDefault(c => c.TreeLevel == treeLevel);
        if (exact != null) return exact;

        return _configs
            .OrderBy(c => Mathf.Abs(c.TreeLevel - treeLevel))
            .FirstOrDefault() ?? FallbackConfig(treeLevel);
    }

    static void BuildFallbackConfig()
    {
        _configs.Clear();
        for (int level = 1; level <= MaxLevel; level++)
            _configs.Add(FallbackConfig(level));
    }

    static TreeLevelConfig FallbackConfig(int level)
    {
        return new TreeLevelConfig
        {
            TreeLevel = level,
            LvUpCost = level * 1000,
            LvUpTime = level * 100,
            RareWeights = new[] { 80, 10, 10, 0, 0, 0 },
            RareRates = new[] { 0.8f, 0.1f, 0.1f, 0f, 0f, 0f },
        };
    }
}

public readonly struct TreeItemDrop
{
    public readonly int ItemId;
    public readonly int Count;

    public TreeItemDrop(int itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }
}

public readonly struct TreeLevelView
{
    public readonly int TreeLevel;
    public readonly int LvUpCost;
    public readonly int LvUpTime;
    public readonly int[] RareWeights;
    public readonly float[] RareRates;
    public readonly List<TreeItemDropView> ItemDrops;

    internal TreeLevelView(TreeLevelConfig config)
    {
        TreeLevel = config.TreeLevel;
        LvUpCost = config.LvUpCost;
        LvUpTime = config.LvUpTime;
        RareWeights = (int[])config.RareWeights.Clone();
        RareRates = config.RareRates != null && config.RareRates.Length >= 6
            ? (float[])config.RareRates.Clone()
            : BuildRates(config.RareWeights);
        ItemDrops = new List<TreeItemDropView>();
        for (int i = 0; i < config.ItemDrops.Count; i++)
            ItemDrops.Add(new TreeItemDropView(config.ItemDrops[i]));
    }

    static float[] BuildRates(int[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += Mathf.Max(0, weights[i]);

        var rates = new float[6];
        if (total <= 0f) return rates;
        for (int i = 0; i < rates.Length && i < weights.Length; i++)
            rates[i] = Mathf.Max(0, weights[i]) / total;
        return rates;
    }
}

public readonly struct TreeItemDropView
{
    public readonly int ItemId;
    public readonly float DropRate;
    public readonly int DropNum;

    internal TreeItemDropView(TreeItemDropConfig config)
    {
        ItemId = config.ItemId;
        DropRate = config.DropRate;
        DropNum = config.DropNum;
    }
}

public readonly struct ItemView
{
    public readonly int ItemId;
    public readonly string ItemName;
    public readonly string IconName;
    public readonly float Value;
    public readonly string ValueMeaning;

    internal ItemView(ItemConfig config)
    {
        ItemId = config.ItemId;
        ItemName = config.ItemName;
        IconName = config.IconName;
        Value = config.Value;
        ValueMeaning = config.ValueMeaning;
    }

    public ItemView(int itemId, string itemName, string iconName, float value, string valueMeaning)
    {
        ItemId = itemId;
        ItemName = itemName;
        IconName = iconName;
        Value = value;
        ValueMeaning = valueMeaning;
    }
}

sealed class TreeLevelConfig
{
    public int TreeLevel;
    public int LvUpCost;
    public int LvUpTime;
    public int[] RareWeights = new int[6];
    public float[] RareRates = new float[6];
    public readonly List<TreeItemDropConfig> ItemDrops = new();
}

sealed class TreeItemDropConfig
{
    public int ItemId;
    public float DropRate;
    public int DropNum;
}

sealed class ItemConfig
{
    public int ItemId;
    public string ItemName;
    public string IconName;
    public float Value;
    public string ValueMeaning;
}
