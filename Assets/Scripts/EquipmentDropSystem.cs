using UnityEngine;

public class EquipmentDropSystem : MonoBehaviour
{
    public static EquipmentDropSystem Instance { get; private set; }

    [SerializeField] Sprite[] itemIcons;     // Equip1001 ~ Equip1008，由 Builder 自动注入
    // 装备名称表：[slot * NumBands + band]，由 EquipmentNameBuilder 从 EuipmentIcon.xlsx 写入
    [SerializeField] string[] itemNameTable;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 公共接口 ──────────────────────────────────────────────────────

    public EquipmentResult GenerateDrop(int rarityIndex, int playerLevel)
    {
        // 步骤2：随机部位 (0-based)
        int slot = Random.Range(0, SlotNames.Length);

        // 步骤3：装备等级 = playerLevel ± 3，夹在 [1, MaxLevel]
        int equipLevel = Mathf.Clamp(playerLevel + Random.Range(-3, 4), 1, BaseStats.Length);

        // 步骤4：查表
        var baseAttr = GetBaseAttr(equipLevel);
        var rarCoef  = RarityRatios[Mathf.Clamp(rarityIndex, 0, RarityRatios.Length - 1)];
        var sltCoef  = SlotRatios[Mathf.Clamp(slot, 0, SlotRatios.Length - 1)];

        // 最终属性 = 基础值 × (1 + 品质系数) × (1 + 部位系数)
        return new EquipmentResult
        {
            slotIndex  = slot,
            itemName   = GetItemName(slot, equipLevel),
            slotName   = SlotNames[slot],
            icon       = GetIcon(equipLevel),
            rarity     = rarityIndex + 1,
            equipLevel = equipLevel,
            bonusAttr = new RoleAttr
            {
                Attack  = Mathf.Round(baseAttr.Attack  * (1f + rarCoef.Attack)  * (1f + sltCoef.Attack)),
                Defence = Mathf.Round(baseAttr.Defence * (1f + rarCoef.Defence) * (1f + sltCoef.Defence)),
                Hp      = Mathf.Round(baseAttr.Hp      * (1f + rarCoef.Hp)      * (1f + sltCoef.Hp)),
                Agility = Mathf.Round(baseAttr.Agility * (1f + rarCoef.Agility) * (1f + sltCoef.Agility)),
            }
        };
    }

    // ── 图标 & 名称（EuipmentIcon.xlsx）────────────────────────────────

    static readonly int[] LevelLimits = { 9, 19, 29, 39, 49, 59, 69, 79 };

    Sprite GetIcon(int playerLevel)
    {
        if (itemIcons == null || itemIcons.Length == 0) return null;
        int idx = GetLevelBand(playerLevel);
        return itemIcons[Mathf.Clamp(idx, 0, itemIcons.Length - 1)];
    }

    const int NumBands = 8;

    string GetItemName(int slot, int playerLevel)
    {
        int band  = GetLevelBand(playerLevel);
        int index = slot * NumBands + band;
        if (itemNameTable != null && index < itemNameTable.Length)
        {
            string name = itemNameTable[index];
            if (!string.IsNullOrEmpty(name)) return name;
        }
        return SlotNames[slot]; // 表格未填充时回退到槽位名
    }

    static int GetLevelBand(int playerLevel)
    {
        for (int i = 0; i < LevelLimits.Length; i++)
            if (playerLevel <= LevelLimits[i]) return i;
        return LevelLimits.Length - 1;
    }

    // ── 槽位名称 ──────────────────────────────────────────────────────

    static readonly string[] SlotNames =
    {
        "武器", "副手", "头盔", "胸甲", "腿甲", "靴子",
        "护手", "腰带", "项链", "主戒", "副戒", "圣物"
    };

    // ── Equipment.xlsx — 基础属性（1-36 级）────────────────────────────

    static RoleAttr GetBaseAttr(int level)
    {
        int idx = Mathf.Clamp(level, 1, BaseStats.Length) - 1;
        var b   = BaseStats[idx];
        return new RoleAttr { Attack = b.a, Defence = b.d, Hp = b.h, Agility = b.g };
    }

    static readonly (float a, float d, float h, float g)[] BaseStats =
    {
        // Lv  Atk   Def    Hp   Agi
        (100,  20,  1000,  10), // 1
        (100,  20,  1000,  10), // 2
        (100,  20,  1000,  10), // 3
        (100,  20,  1000,  10), // 4
        (100,  20,  1000,  10), // 5
        (100,  20,  1000,  10), // 6
        (100,  20,  1000,  10), // 7
        (100,  20,  1000,  10), // 8
        (100,  20,  1000,  10), // 9
        (100,  20,  1000,  10), // 10
        (100,  20,  1000,  10), // 11
        (100,  20,  1000,  10), // 12
        (140,  35,  1350,  15), // 13
        (140,  35,  1700,  20), // 14
        (140,  35,  2050,  25), // 15
        (140,  35,  2400,  30), // 16
        (140,  35,  2750,  35), // 17
        (140,  35,  3100,  40), // 18
        (140,  35,  3450,  45), // 19
        (140,  35,  3800,  50), // 20
        (140,  35,  4150,  55), // 21
        (140,  35,  4500,  60), // 22
        (140,  35,  4850,  65), // 23
        (140,  35,  5200,  70), // 24
        (180,  50,  5550,  75), // 25
        (180,  50,  5900,  80), // 26
        (180,  50,  6250,  85), // 27
        (180,  50,  6600,  90), // 28
        (180,  50,  6950,  95), // 29
        (180,  50,  7300, 100), // 30
        (180,  50,  7650, 105), // 31
        (180,  50,  8000, 110), // 32
        (180,  50,  8350, 115), // 33
        (180,  50,  8700, 120), // 34
        (180,  50,  9050, 125), // 35
        (180,  50,  9400, 130), // 36
    };

    // ── EquipmentRarityRatio.xlsx ─────────────────────────────────────

    static readonly (float Attack, float Defence, float Hp, float Agility)[] RarityRatios =
    {
        (1.0f, 1.0f, 1.0f, 1.0f), // Rarity 1
        (1.2f, 1.2f, 1.2f, 1.2f), // Rarity 2
        (1.4f, 1.4f, 1.4f, 1.4f), // Rarity 3
        (1.6f, 1.6f, 1.6f, 1.6f), // Rarity 4
        (1.8f, 1.8f, 1.8f, 1.8f), // Rarity 5
        (2.0f, 2.0f, 2.0f, 2.0f), // Rarity 6
    };

    // ── EquipmentSlotRatio.xlsx ───────────────────────────────────────

    static readonly (float Attack, float Defence, float Hp, float Agility)[] SlotRatios =
    {
        (1.00f, 1.00f, 1.00f, 1.00f), // Slot  1 武器
        (0.80f, 0.80f, 0.80f, 0.80f), // Slot  2 副手
        (0.75f, 0.75f, 0.75f, 0.75f), // Slot  3 头盔
        (0.90f, 0.90f, 0.90f, 0.90f), // Slot  4 胸甲
        (0.80f, 0.80f, 0.80f, 0.80f), // Slot  5 腿甲
        (0.75f, 0.75f, 0.75f, 0.75f), // Slot  6 靴子
        (0.90f, 0.90f, 0.90f, 0.90f), // Slot  7 护手
        (0.80f, 0.80f, 0.80f, 0.80f), // Slot  8 腰带
        (0.75f, 0.75f, 0.75f, 0.75f), // Slot  9 项链
        (0.80f, 0.80f, 0.80f, 0.80f), // Slot 10 主戒
        (0.75f, 0.75f, 0.75f, 0.75f), // Slot 11 副戒
        (0.90f, 0.90f, 0.90f, 0.90f), // Slot 12 圣物
    };
}
