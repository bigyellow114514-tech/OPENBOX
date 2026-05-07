using UnityEngine;

public class TreeExpManager : MonoBehaviour
{
    public static TreeExpManager Instance { get; private set; }

    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; } = 0f;
    public float ExpToNextLevel => GetLvUpCost(Level);

    public int UpgradeTier { get; private set; } = 0;
    // Tier 0 = Tree Lv.1 (base), Tier 35 = Tree Lv.36 (max)
    public const int MaxUpgradeTier = 35;
    public int UpgradeLevel => UpgradeTier + 1;

    public event System.Action OnExpChanged;
    public event System.Action OnUpgradeTierChanged;

    const int MaxLevel = 36;
    const string KeyLevel       = "TreeLevel";
    const string KeyExp         = "TreeExp";
    const string KeyUpgradeTier = "TreeUpgradeTier";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    public void AddExp(float amount)
    {
        if (Level >= MaxLevel) return;

        CurrentExp += amount;

        while (Level < MaxLevel && CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;
        }

        if (Level >= MaxLevel)
            CurrentExp = 0f;

        Save();
        OnExpChanged?.Invoke();
    }

    // Returns gold cost to upgrade from current UpgradeTier to next.
    // Matches Tree.xlsx LvUpCost: level n costs n*1000 gold (1→2=1000, 2→3=2000 …)
    // Returns 0 if already at max tier.
    public int GetNextUpgradeCost()
    {
        if (UpgradeTier >= MaxUpgradeTier) return 0;
        return (UpgradeTier + 1) * 1000;
    }

    // Spends gold and advances UpgradeTier by 1. Returns true on success.
    public bool TryUpgrade()
    {
        if (UpgradeTier >= MaxUpgradeTier) return false;

        int cost = GetNextUpgradeCost();
        var res  = PlayerResourceManager.Instance;
        if (res == null || !res.SpendGold(cost)) return false;

        UpgradeTier++;
        Save();
        OnUpgradeTierChanged?.Invoke();
        return true;
    }

    void Save()
    {
        PlayerPrefs.SetInt(KeyLevel, Level);
        PlayerPrefs.SetFloat(KeyExp, CurrentExp);
        PlayerPrefs.SetInt(KeyUpgradeTier, UpgradeTier);
        PlayerPrefs.Save();
    }

    void Load()
    {
        Level       = PlayerPrefs.GetInt(KeyLevel, 1);
        CurrentExp  = PlayerPrefs.GetFloat(KeyExp, 0f);
        UpgradeTier = Mathf.Clamp(PlayerPrefs.GetInt(KeyUpgradeTier, 0), 0, MaxUpgradeTier);
    }

    public void ResetToDefault()
    {
        Level       = 1;
        CurrentExp  = 0f;
        UpgradeTier = 0;
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyExp);
        PlayerPrefs.DeleteKey(KeyUpgradeTier);
        PlayerPrefs.Save();
        OnExpChanged?.Invoke();
        OnUpgradeTierChanged?.Invoke();
    }

    // EXP needed to level up: level n requires n*1000 exp (matches Tree.xlsx LvUpCost reuse)
    static float GetLvUpCost(int level) => level * 1000f;

    // Rare1-Rare6 weights per UpgradeTier (0-35 = Tree Lv.1-36); all rows sum to 100.
    // Designed from Tree.xlsx placeholder data: early levels are Rare1-heavy,
    // high levels unlock Rare4-6 progressively.
    static readonly int[,] TierBoxWeights = {
        // Rare1  Rare2  Rare3  Rare4  Rare5  Rare6
        {  80,    10,    10,    0,     0,     0  }, // Lv 1  (Tier  0)
        {  79,    10,    10,    1,     0,     0  }, // Lv 2  (Tier  1)
        {  77,    10,    10,    2,     1,     0  }, // Lv 3  (Tier  2)
        {  75,    11,    10,    3,     1,     0  }, // Lv 4  (Tier  3)
        {  73,    11,    10,    4,     2,     0  }, // Lv 5  (Tier  4)
        {  71,    11,    10,    5,     2,     1  }, // Lv 6  (Tier  5)
        {  69,    11,    11,    5,     3,     1  }, // Lv 7  (Tier  6)
        {  67,    12,    11,    6,     3,     1  }, // Lv 8  (Tier  7)
        {  65,    12,    11,    7,     4,     1  }, // Lv 9  (Tier  8)
        {  63,    12,    11,    8,     4,     2  }, // Lv10  (Tier  9)
        {  61,    12,    11,    8,     5,     3  }, // Lv11  (Tier 10)
        {  59,    12,    12,    9,     5,     3  }, // Lv12  (Tier 11)
        {  57,    12,    12,   10,     6,     3  }, // Lv13  (Tier 12)
        {  55,    13,    11,   11,     6,     4  }, // Lv14  (Tier 13)
        {  52,    13,    11,   12,     7,     5  }, // Lv15  (Tier 14)
        {  49,    13,    11,   13,     8,     6  }, // Lv16  (Tier 15)
        {  46,    13,    12,   14,     9,     6  }, // Lv17  (Tier 16)
        {  43,    13,    12,   14,    11,     7  }, // Lv18  (Tier 17)
        {  40,    13,    12,   15,    12,     8  }, // Lv19  (Tier 18)
        {  37,    13,    12,   16,    13,     9  }, // Lv20  (Tier 19)
        {  34,    13,    12,   17,    14,    10  }, // Lv21  (Tier 20)
        {  31,    12,    12,   18,    16,    11  }, // Lv22  (Tier 21)
        {  28,    12,    12,   19,    17,    12  }, // Lv23  (Tier 22)
        {  26,    12,    11,   19,    19,    13  }, // Lv24  (Tier 23)
        {  24,    11,    11,   20,    20,    14  }, // Lv25  (Tier 24)
        {  22,    11,    11,   20,    22,    14  }, // Lv26  (Tier 25)
        {  20,    10,    11,   20,    24,    15  }, // Lv27  (Tier 26)
        {  18,    10,    11,   20,    25,    16  }, // Lv28  (Tier 27)
        {  15,     9,    10,   21,    27,    18  }, // Lv29  (Tier 28)
        {  13,     8,    10,   21,    28,    20  }, // Lv30  (Tier 29)
        {  11,     8,     9,   21,    29,    22  }, // Lv31  (Tier 30)
        {   9,     7,     9,   21,    30,    24  }, // Lv32  (Tier 31)
        {   7,     7,     8,   21,    31,    26  }, // Lv33  (Tier 32)
        {   6,     6,     8,   20,    32,    28  }, // Lv34  (Tier 33)
        {   4,     5,     7,   20,    33,    31  }, // Lv35  (Tier 34)
        {   3,     5,     7,   20,    34,    31  }, // Lv36  (Tier 35)
    };

    // Returns Rare1-Rare6 weights based on current UpgradeTier (treeLevel retained for API compat)
    public static void GetBoxWeights(int treeLevel, int[] result)
    {
        int tier = Instance != null ? Instance.UpgradeTier : 0;
        tier = Mathf.Clamp(tier, 0, MaxUpgradeTier);
        for (int i = 0; i < 6; i++)
            result[i] = TierBoxWeights[tier, i];
    }
}
