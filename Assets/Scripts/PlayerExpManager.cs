using UnityEngine;

public class PlayerExpManager : MonoBehaviour
{
    public static PlayerExpManager Instance { get; private set; }

    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; } = 0f;
    public float ExpToNextLevel => GetLvUpExp(Level);

    public event System.Action OnExpChanged;

    const int MaxLevel = 100;
    const string KeyLevel = "PlayerLevel";
    const string KeyExp   = "PlayerExp";

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

    // Matches RoleLevel.xlsx: Level n requires 100 + (n-1)*50 exp
    static float GetLvUpExp(int level) => 100f + (level - 1) * 50f;
}
