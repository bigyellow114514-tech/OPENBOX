using UnityEngine;

public class EquipmentResult
{
    public string   itemName;
    public string   slotName;
    public Sprite   icon;
    public RoleAttr bonusAttr;
    public int      rarity;    // 1-6
    public int      equipLevel; // 装备等级（1-36），供分解经验查表
}
