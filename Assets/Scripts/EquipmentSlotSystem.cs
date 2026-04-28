using System;
using UnityEngine;

public class EquipmentSlotSystem : MonoBehaviour
{
    public static EquipmentSlotSystem Instance { get; private set; }

    // 12 个槽位，索引与 SlotNames 一一对应
    readonly EquipmentResult[] _slots = new EquipmentResult[12];

    // 槽位变化时通知外部（传入槽位索引）
    public event Action<int> OnSlotChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 公共接口 ──────────────────────────────────────────────────────

    public EquipmentResult GetSlot(int index) =>
        (index >= 0 && index < _slots.Length) ? _slots[index] : null;

    /// <summary>将装备放入对应槽位，替换旧装备并刷新角色属性。</summary>
    public void Equip(EquipmentResult item)
    {
        int index = SlotIndex(item.slotName);
        if (index < 0)
        {
            Debug.LogWarning($"[EquipmentSlotSystem] 未知槽位：{item.slotName}");
            return;
        }
        _slots[index] = item;
        RefreshPlayerAttr();
        OnSlotChanged?.Invoke(index);
        Debug.Log($"[EquipmentSlotSystem] 装备成功：{item.itemName} → {item.slotName}");
    }

    /// <summary>分解装备，按等级查表给予大树经验。</summary>
    public void Decompose(EquipmentResult item)
    {
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

    static int SlotIndex(string slotName)
    {
        for (int i = 0; i < SlotNames.Length; i++)
            if (SlotNames[i] == slotName) return i;
        return -1;
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
