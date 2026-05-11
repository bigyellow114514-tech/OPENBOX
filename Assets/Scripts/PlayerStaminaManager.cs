using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class PlayerStaminaManager : MonoBehaviour
{
    public static PlayerStaminaManager Instance { get; private set; }

    const string KeyStamina      = "PlayerStamina";
    const string KeyLastSaveTime = "StaminaLastSaveTime";
    const string RoleLevelPath   = "Excel/RoleLevel.xlsx";
    const float  RecoverySeconds = 300f; // 5 minutes per stamina point

    public int CurrentStamina { get; private set; }
    public int MaxStamina     { get; private set; }

    public event Action OnStaminaChanged;

    int _lastLevel;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _lastLevel     = PlayerExpManager.Instance?.Level ?? 1;
        MaxStamina     = ReadMaxStamina(_lastLevel);
        CurrentStamina = Mathf.Clamp(PlayerPrefs.GetInt(KeyStamina, MaxStamina), 0, MaxStamina);

        ApplyOfflineRecovery();

        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.OnExpChanged += HandleExpChanged;

        StartCoroutine(RecoveryCoroutine());
    }

    void OnDestroy()
    {
        if (PlayerExpManager.Instance != null)
            PlayerExpManager.Instance.OnExpChanged -= HandleExpChanged;
    }

    void ApplyOfflineRecovery()
    {
        string savedStr = PlayerPrefs.GetString(KeyLastSaveTime, "");
        if (string.IsNullOrEmpty(savedStr) || !long.TryParse(savedStr, out long ticks))
            return;

        double elapsed  = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
        int    recovered = Mathf.FloorToInt((float)elapsed / RecoverySeconds);
        if (recovered <= 0) return;

        CurrentStamina = Mathf.Min(CurrentStamina + recovered, MaxStamina);
        Save();
        OnStaminaChanged?.Invoke();
    }

    IEnumerator RecoveryCoroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(RecoverySeconds);
            if (CurrentStamina < MaxStamina)
            {
                CurrentStamina++;
                Save();
                OnStaminaChanged?.Invoke();
            }
        }
    }

    void HandleExpChanged()
    {
        int newLevel = PlayerExpManager.Instance?.Level ?? 1;
        if (newLevel == _lastLevel) return;

        MaxStamina = ReadMaxStamina(newLevel);

        if (newLevel > _lastLevel)
            CurrentStamina = MaxStamina;
        else
            CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina);

        _lastLevel = newLevel;
        Save();
        OnStaminaChanged?.Invoke();
    }

    public bool TryConsume()
    {
        if (CurrentStamina <= 0) return false;
        CurrentStamina--;
        Save();
        OnStaminaChanged?.Invoke();
        return true;
    }

    public void ResetToDefault()
    {
        _lastLevel     = PlayerExpManager.Instance?.Level ?? 1;
        MaxStamina     = ReadMaxStamina(_lastLevel);
        CurrentStamina = MaxStamina;
        Save();
        OnStaminaChanged?.Invoke();
    }

    void Save()
    {
        PlayerPrefs.SetInt(KeyStamina, CurrentStamina);
        PlayerPrefs.SetString(KeyLastSaveTime, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    static int ReadMaxStamina(int level)
    {
        string path = Path.Combine(Application.dataPath, RoleLevelPath);
        if (!File.Exists(path)) return 20;

        try
        {
            ExcelTable table = ExcelTable.Load(path);
            if (table.Rows.Count < 4) return 20;

            var columns = table.ReadHeader(2);
            for (int i = 3; i < table.Rows.Count; i++)
            {
                ExcelRow row = table.Rows[i];
                if (row.GetInt(columns, "Level", 0) == level)
                    return row.GetInt(columns, "Stamina", 20);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[PlayerStaminaManager] Failed to read RoleLevel.xlsx: " + e.Message);
        }

        return 20;
    }
}
