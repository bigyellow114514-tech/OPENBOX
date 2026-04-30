using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class StageData
{
    const string StageXlsxRelativePath = "Excel/Stage.xlsx";
    const int DefaultMaxRound = 25;

    static Dictionary<int, StageData> _stageTable;
    static bool _stageTableLoaded;

    public int StageId;
    public string StageName;
    public string MonsterAvatar;
    public int MaxRound;
    public RoleAttr EnemyAttr;
    public int PetTicketReward;

    public static StageData Create(int stageId)
    {
        stageId = Mathf.Max(1, stageId);

        StageData tableData = TryGetFromTable(stageId);
        if (tableData != null)
            return tableData;

        return CreateFallback(stageId);
    }

    static StageData CreateFallback(int stageId)
    {
        float scale = stageId - 1;

        return new StageData
        {
            StageId = stageId,
            StageName = "Stage " + stageId,
            MonsterAvatar = "1001",
            MaxRound = DefaultMaxRound,
            EnemyAttr = new RoleAttr
            {
                Hp = 70f + scale * 18f,
                Attack = 8f + scale * 2.5f,
                Defence = 1f + scale * 0.8f,
                Agility = 4f + scale * 0.5f,
                CritRate = Mathf.Min(5f + scale * 0.4f, 25f),
                CounterRate = Mathf.Min(scale * 0.3f, 15f),
                ComboRate = Mathf.Min(scale * 0.35f, 18f),
                DodgeRate = Mathf.Min(scale * 0.25f, 12f),
                StunRate = Mathf.Min(scale * 0.2f, 10f),
                LifeStealRate = Mathf.Min(scale * 0.15f, 8f),
                CritDmg = 100f,
            },
            PetTicketReward = 1,
        };
    }

    static StageData TryGetFromTable(int stageId)
    {
        EnsureStageTableLoaded();

        if (_stageTable != null && _stageTable.TryGetValue(stageId, out StageData stage))
            return Clone(stage);

        return null;
    }

    static void EnsureStageTableLoaded()
    {
        if (_stageTableLoaded) return;

        _stageTableLoaded = true;
        _stageTable = new Dictionary<int, StageData>();

        string path = Path.Combine(Application.dataPath, StageXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[StageData] Stage.xlsx not found, using fallback stage data: " + path);
            return;
        }

        try
        {
            LoadStageTable(path);
        }
        catch (System.Exception e)
        {
            _stageTable.Clear();
            Debug.LogError("[StageData] Failed to read Stage.xlsx, using fallback stage data: " + e.Message);
        }
    }

    static void LoadStageTable(string path)
    {
        ExcelTable table = ExcelTable.Load(path);
        if (table.Rows.Count < 4) return;

        Dictionary<string, int> columns = table.ReadHeader(2);

        for (int i = 3; i < table.Rows.Count; i++)
        {
            ExcelRow row = table.Rows[i];
            int stageId = row.GetInt(columns, "StageID");
            if (stageId <= 0) continue;

            _stageTable[stageId] = new StageData
            {
                StageId = stageId,
                StageName = "Stage " + stageId,
                MonsterAvatar = row.Get(columns, "MonsterAvatar"),
                MaxRound = DefaultMaxRound,
                EnemyAttr = new RoleAttr
                {
                    Attack = row.GetFloat(columns, "Attack"),
                    Defence = row.GetFloat(columns, "Defence"),
                    Hp = row.GetFloat(columns, "Hp"),
                    Agility = row.GetFloat(columns, "Agility"),
                    CritRate = row.GetFloat(columns, "CritRate"),
                    CritDmg = row.GetFloat(columns, "CritDmg"),
                    CounterRate = row.GetFloat(columns, "CounterRate"),
                    ComboRate = row.GetFloat(columns, "ComboRate"),
                    DodgeRate = row.GetFloat(columns, "DodgeRate"),
                    StunRate = row.GetFloat(columns, "StunRate"),
                    LifeStealRate = row.GetFloat(columns, "LifeStealRate"),
                    AntiCritRate = row.GetFloat(columns, "AntiCritRate"),
                    AntiCounterRate = row.GetFloat(columns, "AntiCounterRate"),
                    AntiComboRate = row.GetFloat(columns, "AntiComboRate"),
                    AntiDodgeRate = row.GetFloat(columns, "AntiDodgeRate"),
                    AntiStunRate = row.GetFloat(columns, "AntiStunRate"),
                    AntiLifeStealRate = row.GetFloat(columns, "AntiLifeStealRate"),
                    AntiCritDmg = row.GetFloat(columns, "AntiCritDmg"),
                    DamageIncrease = row.GetFloat(columns, "DamageIncrease"),
                    DamageDecrease = row.GetFloat(columns, "DamageDecrease"),
                    Healing = row.GetFloat(columns, "Healing"),
                    AntiHealing = row.GetFloat(columns, "AntiHealing"),
                    PetIncrease = row.GetFloat(columns, "PetIncrease"),
                    PetDecrease = row.GetFloat(columns, "PetDecrease"),
                },
                PetTicketReward = row.GetInt(columns, "PetTicketReward",
                    row.GetInt(columns, "RewardEnergy", 1)),
            };
        }
    }

    static StageData Clone(StageData source)
    {
        return new StageData
        {
            StageId = source.StageId,
            StageName = source.StageName,
            MonsterAvatar = source.MonsterAvatar,
            MaxRound = source.MaxRound,
            EnemyAttr = source.EnemyAttr,
            PetTicketReward = source.PetTicketReward,
        };
    }
}
