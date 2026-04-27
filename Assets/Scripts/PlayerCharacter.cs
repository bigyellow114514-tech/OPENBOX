using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public static PlayerCharacter Instance { get; private set; }

    // Level is owned by PlayerExpManager; expose here for convenience
    public int Level => PlayerExpManager.Instance != null ? PlayerExpManager.Instance.Level : 1;

    // Base attributes from RoleAttr.xlsx — leveling up does NOT change these
    static readonly RoleAttr BaseAttr = new RoleAttr
    {
        Attack        = 10,
        Defence       = 1,
        Hp            = 100,
        Agility       = 5,
        CritRate      = 5,    // 5%
        CritDmg       = 100,  // 100%
        CounterRate   = 0,
        ComboRate     = 0,
        DodgeRate     = 0,
        StunRate      = 0,
        LifeStealRate = 0,
    };

    RoleAttr _equipAttr;

    // Final panel value = base + equipment
    public RoleAttr FinalAttr => BaseAttr + _equipAttr;

    public event System.Action OnAttrChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddEquipAttr(RoleAttr attr)
    {
        _equipAttr += attr;
        OnAttrChanged?.Invoke();
    }

    public void RemoveEquipAttr(RoleAttr attr)
    {
        _equipAttr -= attr;
        OnAttrChanged?.Invoke();
    }

    public void ResetEquipAttr()
    {
        _equipAttr = default;
        OnAttrChanged?.Invoke();
    }
}
