using System.IO;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public static PlayerCharacter Instance { get; private set; }

    const string RoleAttrXlsxRelativePath = "Excel/RoleAttr.xlsx";

    static bool _baseAttrLoaded;
    static RoleAttr _baseAttr;

    RoleAttr _equipAttr;

    public int Level => PlayerExpManager.Instance != null ? PlayerExpManager.Instance.Level : 1;
    public RoleAttr BaseAttr => GetBaseAttr();
    public RoleAttr EquipAttr => _equipAttr;
    public RoleAttr FinalAttr => GetBaseAttr() + _equipAttr;

    public event System.Action OnAttrChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
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

    static RoleAttr GetBaseAttr()
    {
        if (_baseAttrLoaded) return _baseAttr;

        _baseAttrLoaded = true;
        _baseAttr = DefaultBaseAttr();

        string path = Path.Combine(Application.dataPath, RoleAttrXlsxRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning("[PlayerCharacter] RoleAttr.xlsx not found, using fallback base attributes: " + path);
            return _baseAttr;
        }

        try
        {
            ExcelTable table = ExcelTable.Load(path);
            if (table.Rows.Count < 4) return _baseAttr;

            var columns = table.ReadHeader(2);
            RoleAttr loaded = _baseAttr;

            for (int i = 3; i < table.Rows.Count; i++)
            {
                ExcelRow row = table.Rows[i];
                string key = row.Get(columns, "Attr");
                if (string.IsNullOrEmpty(key)) continue;

                loaded.SetByKey(key, row.GetFloat(columns, "InitValue"));
            }

            _baseAttr = loaded;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PlayerCharacter] Failed to read RoleAttr.xlsx, using fallback base attributes: " + e.Message);
        }

        return _baseAttr;
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
}
