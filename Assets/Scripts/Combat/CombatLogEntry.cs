public enum CombatEventType
{
    Attack,
    Popup,
}

public class CombatLogEntry
{
    public CombatEventType Type;
    public int Round;
    public string Text;
    public bool ActorIsPlayer;
    public bool TargetIsPlayer;
    public bool Counter;
    public bool Dodged;
    public bool Crit;
    public bool Stunned;
    public bool Combo;
    public float Damage;
    public float Heal;
    public float PlayerHp;
    public float EnemyHp;
    public bool PlayerStunned;
    public bool EnemyStunned;

    public CombatLogEntry(
        CombatEventType type,
        int round,
        string text,
        bool actorIsPlayer,
        bool targetIsPlayer,
        float playerHp,
        float enemyHp,
        bool playerStunned,
        bool enemyStunned)
    {
        Type = type;
        Round = round;
        Text = text;
        ActorIsPlayer = actorIsPlayer;
        TargetIsPlayer = targetIsPlayer;
        PlayerHp = playerHp;
        EnemyHp = enemyHp;
        PlayerStunned = playerStunned;
        EnemyStunned = enemyStunned;
    }
}
