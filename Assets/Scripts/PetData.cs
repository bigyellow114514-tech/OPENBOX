using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public sealed class PetData
{
    const string PetsXlsxRelativePath = "Excel/Pets.xlsx";

    static Dictionary<int, PetData> _petTable;
    static bool _petTableLoaded;

    public int PetId;
    public string PetResource;
    public string PetName;
    public string SkillType;
    public string Description;
    public int CastInterval;
    public float DamagePercent;
    public float HealPercent;
    public bool HasBuff;
    public string AttrName;
    public float AttrValue;
    public int MaxStack;
    public int DurationRounds;

    public static PetData Get(int petId)
    {
        EnsurePetTableLoaded();

        if (_petTable != null && _petTable.TryGetValue(petId, out PetData pet))
            return Clone(pet);

        return null;
    }

    static void EnsurePetTableLoaded()
    {
        if (_petTableLoaded) return;

        _petTableLoaded = true;
        _petTable = new Dictionary<int, PetData>();

        string path = Path.Combine(Application.dataPath, PetsXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[PetData] Pets.xlsx not found: " + path);
            return;
        }

        try
        {
            LoadPetTable(path);
        }
        catch (System.Exception e)
        {
            _petTable.Clear();
            Debug.LogError("[PetData] Failed to read Pets.xlsx: " + e.Message);
        }
    }

    static void LoadPetTable(string path)
    {
        ExcelTable table = ExcelTable.Load(path);
        if (table.Rows.Count < 4) return;

        Dictionary<string, int> columns = table.ReadHeader(2);

        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            int petId = row.GetInt(columns, "PetID");
            if (petId <= 0) continue;

            _petTable[petId] = new PetData
            {
                PetId = petId,
                PetResource = row.Get(columns, "PetResource"),
                PetName = row.Get(columns, "PetName"),
                SkillType = row.Get(columns, "SkillType"),
                Description = row.Get(columns, "Description"),
                CastInterval = Mathf.Max(1, row.GetInt(columns, "CastInterval", 1)),
                DamagePercent = row.GetFloat(columns, "DamagePercent"),
                HealPercent = row.GetFloat(columns, "HealPercent"),
                HasBuff = row.GetInt(columns, "HasBuff") != 0,
                AttrName = row.Get(columns, "AttrName"),
                AttrValue = row.GetFloat(columns, "AttrValue"),
                MaxStack = Mathf.Max(0, row.GetInt(columns, "MaxStack")),
                DurationRounds = Mathf.Max(0, row.GetInt(columns, "DurationRounds")),
            };
        }
    }

    static PetData Clone(PetData source)
    {
        return new PetData
        {
            PetId = source.PetId,
            PetResource = source.PetResource,
            PetName = source.PetName,
            SkillType = source.SkillType,
            Description = source.Description,
            CastInterval = source.CastInterval,
            DamagePercent = source.DamagePercent,
            HealPercent = source.HealPercent,
            HasBuff = source.HasBuff,
            AttrName = source.AttrName,
            AttrValue = source.AttrValue,
            MaxStack = source.MaxStack,
            DurationRounds = source.DurationRounds,
        };
    }
}
