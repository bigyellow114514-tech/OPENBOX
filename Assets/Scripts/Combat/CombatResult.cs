using System.Collections.Generic;

public class CombatResult
{
    public bool PlayerWon;
    public bool TimedOut;
    public int LastRound;
    public float PlayerMaxHp;
    public float EnemyMaxHp;
    public float PlayerHp;
    public float EnemyHp;
    public int PlayerPetId;
    public string PlayerPetResource;
    public int EnemyPetId;
    public string EnemyPetResource;
    public List<CombatLogEntry> Logs = new List<CombatLogEntry>();
}
