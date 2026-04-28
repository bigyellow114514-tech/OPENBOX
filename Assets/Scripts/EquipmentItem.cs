using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "OpenBox/创建装备数据")]
public class EquipmentItem : ScriptableObject
{
    public string itemName;
    public string slotType;   // 武器 / 头盔 / 胸甲 / 腿甲 / 戒指 ...
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;
    public RoleAttr bonusAttr;
}
