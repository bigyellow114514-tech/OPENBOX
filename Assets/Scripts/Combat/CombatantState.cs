public class CombatantState
{
    public string Name;
    public RoleAttr Attr;
    public float CurrentHp;
    public bool Stunned;

    public float MaxHp => Attr.Hp;
    public bool IsDead => CurrentHp <= 0f;

    public CombatantState(string name, RoleAttr attr)
    {
        Name = name;
        Attr = attr;
        CurrentHp = UnityEngine.Mathf.Max(1f, attr.Hp);
    }
}
