using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EquipmentSlotSystem : MonoBehaviour
{
    public static EquipmentSlotSystem Instance { get; private set; }

    // 12 个槽位，索引与 SlotNames 一一对应
    readonly EquipmentResult[] _slots = new EquipmentResult[12];

    // 槽位变化时通知外部（传入槽位索引）
    public event Action<int> OnSlotChanged;

    const string KeySlots = "EquipmentSlots";

    [Serializable] class SaveData { public SlotSave[] slots = new SlotSave[12]; }
    [Serializable] class SlotSave
    {
        public bool   occupied;
        public int    slotIndex;
        public string itemName;
        public string slotName;
        public int    rarity, equipLevel;
        public float  attack, defence, hp, agility;
        public float  critRate, critDmg, counterRate, comboRate;
        public float  dodgeRate, stunRate, lifeStealRate;
        public float  antiCritRate, antiCounterRate, antiComboRate;
        public float  antiDodgeRate, antiStunRate, antiLifeStealRate;
        public float  antiCritDmg, damageIncrease, damageDecrease;
        public float  healing, antiHealing, petIncrease, petDecrease;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        LoadRaw();
    }

    void Start()
    {
        ResolveIcons();
        RefreshPlayerAttr();
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] != null) OnSlotChanged?.Invoke(i);
    }

    // 读档（不含 Sprite，等 Start 时再从 EquipmentDropSystem 补全）
    void LoadRaw()
    {
        string json = PlayerPrefs.GetString(KeySlots, "");
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonUtility.FromJson<SaveData>(json);
        if (data?.slots == null) return;
        for (int i = 0; i < data.slots.Length && i < _slots.Length; i++)
        {
            var s = data.slots[i];
            if (!s.occupied) continue;
            _slots[i] = new EquipmentResult
            {
                slotIndex  = s.slotIndex,
                itemName   = s.itemName,
                slotName   = s.slotName,
                rarity     = s.rarity,
                equipLevel = s.equipLevel,
                bonusAttr  = new RoleAttr
                {
                    Attack        = s.attack,
                    Defence       = s.defence,
                    Hp            = s.hp,
                    Agility       = s.agility,
                    CritRate      = s.critRate,
                    CritDmg       = s.critDmg,
                    CounterRate   = s.counterRate,
                    ComboRate     = s.comboRate,
                    DodgeRate     = s.dodgeRate,
                    StunRate      = s.stunRate,
                    LifeStealRate = s.lifeStealRate,
                    AntiCritRate = s.antiCritRate,
                    AntiCounterRate = s.antiCounterRate,
                    AntiComboRate = s.antiComboRate,
                    AntiDodgeRate = s.antiDodgeRate,
                    AntiStunRate = s.antiStunRate,
                    AntiLifeStealRate = s.antiLifeStealRate,
                    AntiCritDmg = s.antiCritDmg,
                    DamageIncrease = s.damageIncrease,
                    DamageDecrease = s.damageDecrease,
                    Healing = s.healing,
                    AntiHealing = s.antiHealing,
                    PetIncrease = s.petIncrease,
                    PetDecrease = s.petDecrease,
                }
            };
        }
    }

    void ResolveIcons()
    {
        var drop = EquipmentDropSystem.Instance;
        if (drop == null) return;
        foreach (var slot in _slots)
            if (slot != null) slot.icon = drop.GetIconForLevel(slot.equipLevel);
    }

    void Save()
    {
        var data = new SaveData();
        for (int i = 0; i < _slots.Length; i++)
        {
            var s = _slots[i];
            if (s == null) { data.slots[i] = new SlotSave(); continue; }
            data.slots[i] = new SlotSave
            {
                occupied      = true,
                slotIndex     = s.slotIndex,
                itemName      = s.itemName,
                slotName      = s.slotName,
                rarity        = s.rarity,
                equipLevel    = s.equipLevel,
                attack        = s.bonusAttr.Attack,
                defence       = s.bonusAttr.Defence,
                hp            = s.bonusAttr.Hp,
                agility       = s.bonusAttr.Agility,
                critRate      = s.bonusAttr.CritRate,
                critDmg       = s.bonusAttr.CritDmg,
                counterRate   = s.bonusAttr.CounterRate,
                comboRate     = s.bonusAttr.ComboRate,
                dodgeRate     = s.bonusAttr.DodgeRate,
                stunRate      = s.bonusAttr.StunRate,
                lifeStealRate = s.bonusAttr.LifeStealRate,
                antiCritRate = s.bonusAttr.AntiCritRate,
                antiCounterRate = s.bonusAttr.AntiCounterRate,
                antiComboRate = s.bonusAttr.AntiComboRate,
                antiDodgeRate = s.bonusAttr.AntiDodgeRate,
                antiStunRate = s.bonusAttr.AntiStunRate,
                antiLifeStealRate = s.bonusAttr.AntiLifeStealRate,
                antiCritDmg = s.bonusAttr.AntiCritDmg,
                damageIncrease = s.bonusAttr.DamageIncrease,
                damageDecrease = s.bonusAttr.DamageDecrease,
                healing = s.bonusAttr.Healing,
                antiHealing = s.bonusAttr.AntiHealing,
                petIncrease = s.bonusAttr.PetIncrease,
                petDecrease = s.bonusAttr.PetDecrease,
            };
        }
        PlayerPrefs.SetString(KeySlots, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void ResetToDefault()
    {
        for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
        PlayerPrefs.DeleteKey(KeySlots);
        PlayerPrefs.Save();
        RefreshPlayerAttr();
        for (int i = 0; i < _slots.Length; i++) OnSlotChanged?.Invoke(i);
    }

    // ── 公共接口 ──────────────────────────────────────────────────────

    public EquipmentResult GetSlot(int index) =>
        (index >= 0 && index < _slots.Length) ? _slots[index] : null;

    public bool IsEquipped(EquipmentResult item)
    {
        if (item == null) return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (ReferenceEquals(_slots[i], item))
                return true;
        }

        return false;
    }

    /// <summary>将装备放入对应槽位，替换旧装备并刷新角色属性。</summary>
    public void Equip(EquipmentResult item)
    {
        int index = item.slotIndex;
        if (index < 0 || index >= _slots.Length)
        {
            Debug.LogWarning($"[EquipmentSlotSystem] 槽位越界：{item.slotName}（index={index}）");
            return;
        }

        EquipmentResult replacedItem = _slots[index];
        _slots[index] = item;
        Save();
        RefreshPlayerAttr();
        OnSlotChanged?.Invoke(index);
        Debug.Log($"[EquipmentSlotSystem] 装备成功：{item.itemName} → slot {index}（{item.slotName}）");

        if (replacedItem != null && !ReferenceEquals(replacedItem, item))
            AwardDecomposeReward(replacedItem, "替换自动分解");
    }

    /// <summary>分解装备，按等级查表给予大树经验。</summary>
    public void Decompose(EquipmentResult item)
    {
        if (item == null) return;
        if (IsEquipped(item))
        {
            Debug.LogWarning($"[EquipmentSlotSystem] Cannot decompose equipped item: {item.itemName}");
            return;
        }

        AwardDecomposeReward(item, "分解");
    }

    // ── 内部逻辑 ──────────────────────────────────────────────────────

    void RefreshPlayerAttr()
    {
        if (PlayerCharacter.Instance == null) return;
        PlayerCharacter.Instance.ResetEquipAttr();
        foreach (var slot in _slots)
            if (slot != null)
                PlayerCharacter.Instance.AddEquipAttr(slot.bonusAttr);
    }

    static void AwardDecomposeReward(EquipmentResult item, string reason)
    {
        if (item == null) return;

        SellReward reward = GetDecomposeReward(item.equipLevel);
        EnsureResourceManager()?.AddGold(reward.gold);
        EnsurePlayerExpManager()?.AddExp(reward.exp);
        Debug.Log($"[EquipmentSlotSystem] {reason}：{item.itemName}，获得金币 {reward.gold}，经验 {reward.exp}");
    }

    static SellReward GetDecomposeReward(int equipLevel)
    {
        EnsureSellRewardsLoaded();
        if (_sellRewards.TryGetValue(equipLevel, out SellReward reward))
            return reward;

        int idx = Mathf.Clamp(equipLevel - 1, 0, FallbackExpTable.Length - 1);
        return new SellReward(0, FallbackExpTable[idx]);
    }

    static void EnsureSellRewardsLoaded()
    {
        if (_sellRewardsLoaded) return;
        _sellRewardsLoaded = true;

        string path = FindSellEquipmentPath();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[EquipmentSlotSystem] 找不到 SellEquipment.xlsx，分解奖励将使用旧经验兜底且金币为 0");
            return;
        }

        try
        {
            var table = ExcelTable.Load(path);
            var columns = table.ReadHeader(2);
            for (int i = 3; i < table.Rows.Count; i++)
            {
                ExcelRow row = table.Rows[i];
                int level = FirstInt(row, columns, 0, "EquipmentLevel", "Level");
                if (level <= 0) continue;

                int gold = FirstInt(row, columns, 0, "GoldReward", "Gold", "CoinReward", "Coins");
                int exp = FirstInt(row, columns, 0, "ExpReward", "Exp", "ExperienceReward", "PlayerExp");
                _sellRewards[level] = new SellReward(gold, exp);
            }
        }
        catch (Exception ex)
        {
            _sellRewardsLoaded = false;
            _sellRewards.Clear();
            Debug.LogWarning($"[EquipmentSlotSystem] 读取分解奖励表失败：{path}，将使用旧经验兜底且金币为 0。{ex.Message}");
        }
    }

    static string FindSellEquipmentPath()
    {
        string dir = Path.Combine(Application.dataPath, "Excel");
        string normal = Path.Combine(dir, "SellEquipment.xlsx");
        if (File.Exists(normal)) return normal;

        string legacyTypo = Path.Combine(dir, "SellEuipment.xlsx");
        if (File.Exists(legacyTypo)) return legacyTypo;

        return null;
    }

    static int FirstInt(ExcelRow row, Dictionary<string, int> columns, int defaultValue, params string[] keys)
    {
        foreach (string key in keys)
        {
            foreach (var column in columns)
            {
                if (!string.Equals(column.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                return Mathf.RoundToInt(row.GetFloat(columns, column.Key, defaultValue));
            }
        }
        return defaultValue;
    }

    static PlayerResourceManager EnsureResourceManager()
    {
        if (PlayerResourceManager.Instance != null)
            return PlayerResourceManager.Instance;

        var go = new GameObject("PlayerResourceManager");
        DontDestroyOnLoad(go);
        return go.AddComponent<PlayerResourceManager>();
    }

    static PlayerExpManager EnsurePlayerExpManager()
    {
        if (PlayerExpManager.Instance != null)
            return PlayerExpManager.Instance;

        var go = new GameObject("PlayerExpManager");
        DontDestroyOnLoad(go);
        return go.AddComponent<PlayerExpManager>();
    }

    readonly struct SellReward
    {
        public readonly int gold;
        public readonly int exp;

        public SellReward(int gold, int exp)
        {
            this.gold = Mathf.Max(0, gold);
            this.exp = Mathf.Max(0, exp);
        }
    }

    static bool _sellRewardsLoaded;
    static readonly Dictionary<int, SellReward> _sellRewards = new();

    // ── SellEuipment.xlsx（Level 1-36 的 exp 列）────────────────────────
    static readonly int[] FallbackExpTable =
    {
         5,  5,  5,  5,  5,  // Lv 1-5
         6,  6,  6,  6,  6,  // Lv 6-10
         7,  7,  7,  7,  7,  // Lv 11-15
         8,  8,  8,  8,  8,  // Lv 16-20
         9,  9,  9,  9,  9,  // Lv 21-25
        10, 10, 10, 10, 10,  // Lv 26-30
        11, 11, 11, 11, 11,  // Lv 31-35
        12,                  // Lv 36
    };

    // 与 EquipmentDropSystem.SlotNames 保持一致
    static readonly string[] SlotNames =
    {
        "武器","副手","头盔","胸甲","腿甲","靴子",
        "护手","腰带","项链","主戒","副戒","圣物"
    };
}
