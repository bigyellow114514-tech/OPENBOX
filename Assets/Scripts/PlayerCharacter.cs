using System.IO;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public static PlayerCharacter Instance { get; private set; }

    const string RoleAttrXlsxRelativePath = "Excel/RoleAttr.xlsx";

    static bool _roleAttrLoaded;
    static RoleAttr _baseAttr;
    static RoleAttr _combatPowerPerUnit;

    RoleAttr _equipAttr;

    public int Level => PlayerExpManager.Instance != null ? PlayerExpManager.Instance.Level : 1;
    public RoleAttr BaseAttr => GetBaseAttr();
    public RoleAttr EquipAttr => _equipAttr;
    public RoleAttr PetAttr => PetSystemManager.Instance != null ? PetSystemManager.Instance.GetDeployedPetBonus(GetBaseAttr()) : default;
    public RoleAttr FinalAttr => GetBaseAttr() + _equipAttr + PetAttr;
    public int CombatPower => CalculateCombatPower(FinalAttr);

    public event System.Action OnAttrChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        PetSystemManager.EnsureInstance();
        GetBaseAttr();
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

    public void RefreshFromPetSystem()
    {
        OnAttrChanged?.Invoke();
    }

    static RoleAttr GetBaseAttr()
    {
        LoadRoleAttrTable();
        return _baseAttr;
    }

    public static int CalculateCombatPower(RoleAttr attr)
    {
        LoadRoleAttrTable();

        float total = 0f;
        foreach (string key in RoleAttrKeys)
            total += attr.GetByKey(key) * _combatPowerPerUnit.GetByKey(key);

        return Mathf.Max(0, Mathf.RoundToInt(total));
    }

    static void LoadRoleAttrTable()
    {
        if (_roleAttrLoaded) return;

        _roleAttrLoaded = true;
        _baseAttr = DefaultBaseAttr();
        _combatPowerPerUnit = default;

        string path = Path.Combine(Application.dataPath, RoleAttrXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[PlayerCharacter] RoleAttr.xlsx not found, using fallback base attributes: " + path);
            return;
        }

        try
        {
            ExcelTable table = ExcelTable.Load(path);
            if (table.Rows.Count < 4) return;

            var columns = table.ReadHeader(2);
            RoleAttr loaded = _baseAttr;
            RoleAttr loadedCombatPower = _combatPowerPerUnit;

            for (int i = 3; i < table.Rows.Count; i++)
            {
                ExcelRow row = table.Rows[i];
                string key = row.Get(columns, "Attr");
                if (string.IsNullOrEmpty(key)) continue;

                loaded.SetByKey(key, row.GetFloat(columns, "InitValue"));
                loadedCombatPower.SetByKey(key, row.GetFloat(columns, "CombatPower"));
            }

            _baseAttr = loaded;
            _combatPowerPerUnit = loadedCombatPower;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PlayerCharacter] Failed to read RoleAttr.xlsx, using fallback base attributes: " + e.Message);
        }
    }

    static RoleAttr DefaultBaseAttr()
    {
        return new RoleAttr
        {
            Attack = 10,
            Defence = 1,
            Hp = 100,
            Agility = 5,
            CritRate = 5,
            CritDmg = 100,
        };
    }

    static readonly string[] RoleAttrKeys =
    {
        "Attack",
        "Defence",
        "Hp",
        "Agility",
        "CritRate",
        "CounterRate",
        "ComboRate",
        "DodgeRate",
        "StunRate",
        "LifeStealRate",
        "AntiCritRate",
        "AntiCounterRate",
        "AntiComboRate",
        "AntiDodgeRate",
        "AntiStunRate",
        "AntiLifeStealRate",
        "CritDmg",
        "AntiCritDmg",
        "DamageIncrease",
        "DamageDecrease",
        "Healing",
        "AntiHealing",
        "PetIncrease",
        "PetDecrease",
    };
}
