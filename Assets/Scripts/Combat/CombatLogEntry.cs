public enum CombatEventType
{
    Attack,
    Popup,
    PetSkill,
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
    public int PetId;
    public string PetResource;
    public bool PetActorIsPlayer;
    public float Damage;
    public float Heal;
    public float BuffValue;
    public string BuffAttrName;
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
