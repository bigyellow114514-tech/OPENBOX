using UnityEngine;

public class EquipmentResult
{
    public int      slotIndex;  // 0-11，与 EquipmentSlotSystem._slots 直接对应
    public string   itemName;
    public string   slotName;   // 仅用于显示文字，不再参与路由
    public Sprite   icon;
    public RoleAttr bonusAttr;
    public int      rarity;    // 1-6
    public int      equipLevel; // 装备等级（1-36），供分解经验查表
}
