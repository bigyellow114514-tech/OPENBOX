using UnityEngine;
using UnityEditor;

public static class EquipmentItemCreator
{
    [MenuItem("OpenBox/装备系统/创建示例装备数据")]
    static void CreateSampleItems()
    {
        EnsureAllIconsAreSprites();

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Equipment"))
            AssetDatabase.CreateFolder("Assets/Data", "Equipment");

        CreateItem("铁剑",   "武器", "Equip1001", new RoleAttr { Attack = 15, CritRate = 3 },
            "一把普通的铁剑，锋利却不起眼。");

        CreateItem("精钢盾", "胸甲", "Equip1002", new RoleAttr { Defence = 20, Hp = 50 },
            "用精钢锻造的盾牌，坚不可摧。");

        CreateItem("猎手弓", "武器", "Equip1003", new RoleAttr { Attack = 12, Agility = 8, DodgeRate = 5 },
            "轻盈的猎弓，射速极快。");

        CreateItem("暗影披风", "腿甲", "Equip1004", new RoleAttr { DodgeRate = 10, Agility = 6 },
            "融入黑暗的披风，令敌人难以捉摸。");

        CreateItem("战狂戒指", "戒指", "Equip1005", new RoleAttr { CritRate = 8, CritDmg = 50, ComboRate = 5 },
            "佩戴此戒指，战意将燃烧至极限。");

        CreateItem("龙鳞头盔", "头盔", "Equip1006", new RoleAttr { Defence = 15, Hp = 80, StunRate = 5 },
            "以远古龙鳞制成，散发着神秘光泽。");

        CreateItem("吸血匕首", "武器", "Equip1007", new RoleAttr { Attack = 10, LifeStealRate = 8, CritRate = 5 },
            "每一次斩击都能汲取敌人的生命力。");

        CreateItem("反骨护手", "头盔", "Equip1008", new RoleAttr { CounterRate = 12, Defence = 8 },
            "内嵌反骨符文，受击时有机会反弹伤害。");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EquipmentItemCreator] 已生成 8 件装备，位于 Assets/Data/Equipment/");

        // 在 Project 窗口中高亮显示该文件夹
        var folder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Data/Equipment");
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }

    static void CreateItem(string name, string slot, string iconName, RoleAttr attr, string desc)
    {
        string path = $"Assets/Data/Equipment/{name}.asset";

        // 已存在则跳过，避免重复创建
        if (AssetDatabase.LoadAssetAtPath<EquipmentItem>(path) != null) return;

        var item = ScriptableObject.CreateInstance<EquipmentItem>();
        item.itemName    = name;
        item.slotType    = slot;
        item.description = desc;
        item.bonusAttr   = attr;
        item.icon        = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Picture/Item/{iconName}.png");

        AssetDatabase.CreateAsset(item, path);
    }

    static void EnsureAllIconsAreSprites()
    {
        for (int i = 1001; i <= 1008; i++)
        {
            string path = $"Assets/Picture/Item/Equip{i}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;

            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }
}
