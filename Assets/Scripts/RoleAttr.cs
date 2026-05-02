[System.Serializable]
public struct RoleAttr
{
    public float Attack;
    public float Defence;
    public float Hp;
    public float Agility;

    public float CritRate;
    public float CounterRate;
    public float ComboRate;
    public float DodgeRate;
    public float StunRate;
    public float LifeStealRate;

    public float AntiCritRate;
    public float AntiCounterRate;
    public float AntiComboRate;
    public float AntiDodgeRate;
    public float AntiStunRate;
    public float AntiLifeStealRate;

    public float CritDmg;
    public float AntiCritDmg;
    public float DamageIncrease;
    public float DamageDecrease;
    public float Healing;
    public float AntiHealing;
    public float PetIncrease;
    public float PetDecrease;

    public static RoleAttr operator +(RoleAttr a, RoleAttr b) => new RoleAttr
    {
        Attack = a.Attack + b.Attack,
        Defence = a.Defence + b.Defence,
        Hp = a.Hp + b.Hp,
        Agility = a.Agility + b.Agility,
        CritRate = a.CritRate + b.CritRate,
        CounterRate = a.CounterRate + b.CounterRate,
        ComboRate = a.ComboRate + b.ComboRate,
        DodgeRate = a.DodgeRate + b.DodgeRate,
        StunRate = a.StunRate + b.StunRate,
        LifeStealRate = a.LifeStealRate + b.LifeStealRate,
        AntiCritRate = a.AntiCritRate + b.AntiCritRate,
        AntiCounterRate = a.AntiCounterRate + b.AntiCounterRate,
        AntiComboRate = a.AntiComboRate + b.AntiComboRate,
        AntiDodgeRate = a.AntiDodgeRate + b.AntiDodgeRate,
        AntiStunRate = a.AntiStunRate + b.AntiStunRate,
        AntiLifeStealRate = a.AntiLifeStealRate + b.AntiLifeStealRate,
        CritDmg = a.CritDmg + b.CritDmg,
        AntiCritDmg = a.AntiCritDmg + b.AntiCritDmg,
        DamageIncrease = a.DamageIncrease + b.DamageIncrease,
        DamageDecrease = a.DamageDecrease + b.DamageDecrease,
        Healing = a.Healing + b.Healing,
        AntiHealing = a.AntiHealing + b.AntiHealing,
        PetIncrease = a.PetIncrease + b.PetIncrease,
        PetDecrease = a.PetDecrease + b.PetDecrease,
    };

    public static RoleAttr operator -(RoleAttr a, RoleAttr b) => new RoleAttr
    {
        Attack = a.Attack - b.Attack,
        Defence = a.Defence - b.Defence,
        Hp = a.Hp - b.Hp,
        Agility = a.Agility - b.Agility,
        CritRate = a.CritRate - b.CritRate,
        CounterRate = a.CounterRate - b.CounterRate,
        ComboRate = a.ComboRate - b.ComboRate,
        DodgeRate = a.DodgeRate - b.DodgeRate,
        StunRate = a.StunRate - b.StunRate,
        LifeStealRate = a.LifeStealRate - b.LifeStealRate,
        AntiCritRate = a.AntiCritRate - b.AntiCritRate,
        AntiCounterRate = a.AntiCounterRate - b.AntiCounterRate,
        AntiComboRate = a.AntiComboRate - b.AntiComboRate,
        AntiDodgeRate = a.AntiDodgeRate - b.AntiDodgeRate,
        AntiStunRate = a.AntiStunRate - b.AntiStunRate,
        AntiLifeStealRate = a.AntiLifeStealRate - b.AntiLifeStealRate,
        CritDmg = a.CritDmg - b.CritDmg,
        AntiCritDmg = a.AntiCritDmg - b.AntiCritDmg,
        DamageIncrease = a.DamageIncrease - b.DamageIncrease,
        DamageDecrease = a.DamageDecrease - b.DamageDecrease,
        Healing = a.Healing - b.Healing,
        AntiHealing = a.AntiHealing - b.AntiHealing,
        PetIncrease = a.PetIncrease - b.PetIncrease,
        PetDecrease = a.PetDecrease - b.PetDecrease,
    };

    public void SetByKey(string key, float value)
    {
        switch (key)
        {
            case "Attack": Attack = value; break;
            case "Defence": Defence = value; break;
            case "Hp": Hp = value; break;
            case "Agility": Agility = value; break;
            case "CritRate": CritRate = value; break;
            case "CounterRate": CounterRate = value; break;
            case "ComboRate": ComboRate = value; break;
            case "DodgeRate": DodgeRate = value; break;
            case "StunRate": StunRate = value; break;
            case "LifeStealRate": LifeStealRate = value; break;
            case "AntiCritRate": AntiCritRate = value; break;
            case "AntiCounterRate": AntiCounterRate = value; break;
            case "AntiComboRate": AntiComboRate = value; break;
            case "AntiDodgeRate": AntiDodgeRate = value; break;
            case "AntiStunRate": AntiStunRate = value; break;
            case "AntiLifeStealRate": AntiLifeStealRate = value; break;
            case "CritDmg": CritDmg = value; break;
            case "AntiCritDmg": AntiCritDmg = value; break;
            case "DamageIncrease": DamageIncrease = value; break;
            case "DamageDecrease": DamageDecrease = value; break;
            case "Healing": Healing = value; break;
            case "AntiHealing": AntiHealing = value; break;
            case "PetIncrease": PetIncrease = value; break;
            case "PetDecrease": PetDecrease = value; break;
        }
    }

    public float GetByKey(string key)
    {
        switch (key)
        {
            case "Attack": return Attack;
            case "Defence": return Defence;
            case "Hp": return Hp;
            case "Agility": return Agility;
            case "CritRate": return CritRate;
            case "CounterRate": return CounterRate;
            case "ComboRate": return ComboRate;
            case "DodgeRate": return DodgeRate;
            case "StunRate": return StunRate;
            case "LifeStealRate": return LifeStealRate;
            case "AntiCritRate": return AntiCritRate;
            case "AntiCounterRate": return AntiCounterRate;
            case "AntiComboRate": return AntiComboRate;
            case "AntiDodgeRate": return AntiDodgeRate;
            case "AntiStunRate": return AntiStunRate;
            case "AntiLifeStealRate": return AntiLifeStealRate;
            case "CritDmg": return CritDmg;
            case "AntiCritDmg": return AntiCritDmg;
            case "DamageIncrease": return DamageIncrease;
            case "DamageDecrease": return DamageDecrease;
            case "Healing": return Healing;
            case "AntiHealing": return AntiHealing;
            case "PetIncrease": return PetIncrease;
            case "PetDecrease": return PetDecrease;
            default: return 0f;
        }
    }
}
