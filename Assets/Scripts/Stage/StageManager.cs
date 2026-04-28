using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    const string KeyCurrentStage = "CurrentStageId";

    public int CurrentStageId { get; private set; } = 1;
    public StageData CurrentStage => StageData.Create(CurrentStageId);

    public event System.Action OnStageChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CurrentStageId = Mathf.Max(1, PlayerPrefs.GetInt(KeyCurrentStage, 1));
    }

    public void CompleteCurrentStage()
    {
        CurrentStageId++;
        PlayerPrefs.SetInt(KeyCurrentStage, CurrentStageId);
        PlayerPrefs.Save();
        OnStageChanged?.Invoke();
    }

    public void ResetToDefault()
    {
        CurrentStageId = 1;
        PlayerPrefs.DeleteKey(KeyCurrentStage);
        PlayerPrefs.Save();
        OnStageChanged?.Invoke();
    }
}
