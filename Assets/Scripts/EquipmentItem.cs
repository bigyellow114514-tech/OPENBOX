using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "OpenBox/Equipment Item")]
public class EquipmentItem : ScriptableObject
{
    public string itemName;
    public string slotType;   // 武器 / 头盔 / 胸甲 / 腿甲 / 戒指 ...
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;
    public RoleAttr bonusAttr;
}
