using UnityEngine;

[System.Serializable]
public class StageData
{
    public int StageId;
    public string StageName;
    public int MaxRound;
    public RoleAttr EnemyAttr;
    public int PetTicketReward;

    public static StageData Create(int stageId)
    {
        stageId = Mathf.Max(1, stageId);
        float scale = stageId - 1;

        return new StageData
        {
            StageId = stageId,
            StageName = "Stage " + stageId,
            MaxRound = 15,
            EnemyAttr = new RoleAttr
            {
                Hp = 70f + scale * 18f,
                Attack = 8f + scale * 2.5f,
                Defence = 1f + scale * 0.8f,
                Agility = 4f + scale * 0.5f,
                CritRate = Mathf.Min(5f + scale * 0.4f, 25f),
                CritDmg = 80f,
                CounterRate = Mathf.Min(scale * 0.3f, 15f),
                ComboRate = Mathf.Min(scale * 0.35f, 18f),
                DodgeRate = Mathf.Min(scale * 0.25f, 12f),
                StunRate = Mathf.Min(scale * 0.2f, 10f),
                LifeStealRate = Mathf.Min(scale * 0.15f, 8f),
            },
            PetTicketReward = 1,
        };
    }
}
