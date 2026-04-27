[System.Serializable]
public struct RoleAttr
{
    public float Attack;        // 攻击力
    public float Defence;       // 防御力
    public float Hp;            // 生命值
    public float Agility;       // 敏捷
    public float CritRate;      // 暴击率   (5 = 5%)
    public float CritDmg;       // 爆伤     (100 = 100%)
    public float CounterRate;   // 反击     (% value)
    public float ComboRate;     // 连击     (% value)
    public float DodgeRate;     // 闪避     (% value)
    public float StunRate;      // 击晕     (% value)
    public float LifeStealRate; // 吸血     (% value)

    public static RoleAttr operator +(RoleAttr a, RoleAttr b) => new RoleAttr
    {
        Attack        = a.Attack        + b.Attack,
        Defence       = a.Defence       + b.Defence,
        Hp            = a.Hp            + b.Hp,
        Agility       = a.Agility       + b.Agility,
        CritRate      = a.CritRate      + b.CritRate,
        CritDmg       = a.CritDmg       + b.CritDmg,
        CounterRate   = a.CounterRate   + b.CounterRate,
        ComboRate     = a.ComboRate     + b.ComboRate,
        DodgeRate     = a.DodgeRate     + b.DodgeRate,
        StunRate      = a.StunRate      + b.StunRate,
        LifeStealRate = a.LifeStealRate + b.LifeStealRate,
    };

    public static RoleAttr operator -(RoleAttr a, RoleAttr b) => new RoleAttr
    {
        Attack        = a.Attack        - b.Attack,
        Defence       = a.Defence       - b.Defence,
        Hp            = a.Hp            - b.Hp,
        Agility       = a.Agility       - b.Agility,
        CritRate      = a.CritRate      - b.CritRate,
        CritDmg       = a.CritDmg       - b.CritDmg,
        CounterRate   = a.CounterRate   - b.CounterRate,
        ComboRate     = a.ComboRate     - b.ComboRate,
        DodgeRate     = a.DodgeRate     - b.DodgeRate,
        StunRate      = a.StunRate      - b.StunRate,
        LifeStealRate = a.LifeStealRate - b.LifeStealRate,
    };
}
