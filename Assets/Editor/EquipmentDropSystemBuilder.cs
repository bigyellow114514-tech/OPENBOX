using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EquipmentDropSystemBuilder
{
    [MenuItem("OpenBox/Setup Equipment Drop System")]
    static void Setup()
    {
        EnsureIconsAreSprites();

        var go = GetOrCreateGO();
        var sys = go.GetComponent<EquipmentDropSystem>()
                  ?? go.AddComponent<EquipmentDropSystem>();

        var sprites = new Sprite[8];
        for (int i = 0; i < 8; i++)
        {
            string path = $"Assets/Picture/Item/Equip{1001 + i}.png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null)
                Debug.LogWarning($"[DropSystemBuilder] 找不到 {path}");
        }

        var so = new SerializedObject(sys);
        var prop = so.FindProperty("itemIcons");
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[DropSystemBuilder] 完成！EquipmentDropSystem 已配置，请按 Ctrl+S 保存。");
        Selection.activeGameObject = go;
    }

    static GameObject GetOrCreateGO()
    {
        var existing = Object.FindObjectOfType<EquipmentDropSystem>();
        if (existing != null) return existing.gameObject;

        var go = new GameObject("EquipmentDropSystem");
        return go;
    }

    static void EnsureIconsAreSprites()
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
