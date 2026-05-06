using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EquipmentDropSystemBuilder
{
    const int NumSlots = 12;
    const int NumBands = 8;

    static int ItemIconCol(int band) => 3 + band * 3;

    [MenuItem("OpenBox/装备系统/配置掉落系统")]
    static void Setup()
    {
        string xlsxPath = "Assets/Excel/EuipmentIcon.xlsx";
        string fullPath = Path.GetFullPath(xlsxPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[DropSystemBuilder] 找不到 {xlsxPath}");
            return;
        }

        string[] iconNames = ParseIconNames(fullPath);
        if (iconNames == null) return;

        foreach (string name in iconNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            EnsureSprite($"Assets/Picture/Item/{name}.png");
        }

        var sprites = new Sprite[NumSlots * NumBands];
        for (int i = 0; i < iconNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(iconNames[i])) continue;
            string spritePath = $"Assets/Picture/Item/{iconNames[i]}.png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
                Debug.LogWarning($"[DropSystemBuilder] 找不到 {spritePath}");
        }

        var go = GetOrCreateGO();
        var sys = go.GetComponent<EquipmentDropSystem>() ?? go.AddComponent<EquipmentDropSystem>();

        var so = new SerializedObject(sys);
        var prop = so.FindProperty("itemIcons");
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[DropSystemBuilder] 完成！已配置 {NumSlots} 槽位 × {NumBands} 等级段共 {sprites.Length} 个图标，请按 Ctrl+S 保存。");
        Selection.activeGameObject = go;
    }

    static string[] ParseIconNames(string path)
    {
        var result = new string[NumSlots * NumBands];
        try
        {
            var table = ExcelTable.Load(path);
            for (int rowIdx = 3; rowIdx < table.Rows.Count; rowIdx++)
            {
                int slot = rowIdx - 3;
                if (slot >= NumSlots) break;
                var row = table.Rows[rowIdx];
                for (int band = 0; band < NumBands; band++)
                {
                    string name = row.Get(ItemIconCol(band)).Trim();
                    result[slot * NumBands + band] = name;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DropSystemBuilder] 解析 EuipmentIcon.xlsx 出错：{e.Message}");
            return null;
        }
        return result;
    }

    static GameObject GetOrCreateGO()
    {
        var existing = UnityEngine.Object.FindObjectOfType<EquipmentDropSystem>();
        if (existing != null) return existing.gameObject;
        return new GameObject("EquipmentDropSystem");
    }

    static void EnsureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }
}
