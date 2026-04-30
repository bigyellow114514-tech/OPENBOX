using System;
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
        _slots[index] = item;
        Save();
        RefreshPlayerAttr();
        OnSlotChanged?.Invoke(index);
        Debug.Log($"[EquipmentSlotSystem] 装备成功：{item.itemName} → slot {index}（{item.slotName}）");
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

        int exp = GetDecomposeExp(item.equipLevel);
        TreeExpManager.Instance?.AddExp(exp);
        Debug.Log($"[EquipmentSlotSystem] 分解：{item.itemName}，获得 {exp} 树经验");
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

    static int GetDecomposeExp(int equipLevel)
    {
        int idx = Mathf.Clamp(equipLevel - 1, 0, DecomposeTable.Length - 1);
        return DecomposeTable[idx];
    }

    // ── SellEuipment.xlsx（Level 1-36 的 exp 列）────────────────────────
    static readonly int[] DecomposeTable =
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
