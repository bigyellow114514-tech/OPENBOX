using UnityEngine;

public class TreeExpManager : MonoBehaviour
{
    public static TreeExpManager Instance { get; private set; }

    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; } = 0f;
    public float ExpToNextLevel => GetLvUpCost(Level);

    public event System.Action OnExpChanged;

    const int MaxLevel = 36;
    const string KeyLevel = "TreeLevel";
    const string KeyExp   = "TreeExp";

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

    void Save()
    {
        PlayerPrefs.SetInt(KeyLevel, Level);
        PlayerPrefs.SetFloat(KeyExp, CurrentExp);
        PlayerPrefs.Save();
    }

    void Load()
    {
        Level      = PlayerPrefs.GetInt(KeyLevel, 1);
        CurrentExp = PlayerPrefs.GetFloat(KeyExp, 0f);
    }

    public void ResetToDefault()
    {
        Level      = 1;
        CurrentExp = 0f;
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyExp);
        PlayerPrefs.Save();
        OnExpChanged?.Invoke();
    }

    // Matches Tree.xlsx: Level n requires n * 1000 exp to level up (LvUpCost column)
    static float GetLvUpCost(int level) => level * 1000f;

    // Tree.xlsx: Rare1~Rare6 weights indexed [0..5], rows = level 1..36
    static readonly int[,] BoxWeights = {
        // Rare1  Rare2  Rare3  Rare4  Rare5  Rare6
        {  80,    10,    10,    0,     0,     0  }, // Lv1
        {  80,    10,    10,    0,     0,     0  }, // Lv2
        {  80,    10,    10,    0,     0,     0  }, // Lv3
        {  80,    10,    10,    0,     0,     0  }, // Lv4
        {  80,    10,    10,    0,     0,     0  }, // Lv5
        {  80,    10,    10,    0,     0,     0  }, // Lv6
        {  80,    10,    10,    0,     0,     0  }, // Lv7
        {  80,    10,    10,    0,     0,     0  }, // Lv8
        {  80,    10,    10,    0,     0,     0  }, // Lv9
        {  80,    10,    10,    0,     0,     0  }, // Lv10
        {  80,    10,    10,    0,     0,     0  }, // Lv11
        {  80,    10,    10,    0,     0,     0  }, // Lv12
        {  80,    10,    10,    0,     0,     0  }, // Lv13
        {  80,    10,    10,    0,     0,     0  }, // Lv14
        {  80,    10,    10,    0,     0,     0  }, // Lv15
        {  80,    10,    10,    0,     0,     0  }, // Lv16
        {  80,    10,    10,    0,     0,     0  }, // Lv17
        {  80,    10,    10,    0,     0,     0  }, // Lv18
        {  80,    10,    10,    0,     0,     0  }, // Lv19
        {  80,    10,    10,    0,     0,     0  }, // Lv20
        {  80,    10,    10,    0,     0,     0  }, // Lv21
        {  80,    10,    10,    0,     0,     0  }, // Lv22
        {  80,    10,    10,    0,     0,     0  }, // Lv23
        {  80,    10,    10,    0,     0,     0  }, // Lv24
        {  80,    10,    10,    0,     0,     0  }, // Lv25
        {  80,    10,    10,    0,     0,     0  }, // Lv26
        {  80,    10,    10,    0,     0,     0  }, // Lv27
        {  80,    10,    10,    0,     0,     0  }, // Lv28
        {  80,    10,    10,    0,     0,     0  }, // Lv29
        {  80,    10,    10,    0,     0,     0  }, // Lv30
        {  80,    10,    10,    0,     0,     0  }, // Lv31
        {  80,    10,    10,    0,     0,     0  }, // Lv32
        {  80,    10,    10,    0,     0,     0  }, // Lv33
        {  80,    10,    10,    0,     0,     0  }, // Lv34
        {  80,    10,    10,    0,     0,     0  }, // Lv35
        {  80,    10,    10,    0,     0,     0  }, // Lv36
    };

    // Returns weights[0..5] for the given tree level (1-based, clamped)
    public static void GetBoxWeights(int treeLevel, int[] result)
    {
        int row = Mathf.Clamp(treeLevel, 1, MaxLevel) - 1;
        for (int i = 0; i < 6; i++)
            result[i] = BoxWeights[row, i];
    }
}
