using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageFlowBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeObjects()
    {
        if (SceneManager.GetActiveScene().name == "StartScene") return;

        if (Object.FindObjectOfType<StageEntryUI>() != null) return;

        var go = new GameObject("StageFlowRuntime");
        Object.DontDestroyOnLoad(go);

        if (Object.FindObjectOfType<StageManager>() == null)
            go.AddComponent<StageManager>();

        if (Object.FindObjectOfType<PlayerResourceManager>() == null)
            go.AddComponent<PlayerResourceManager>();

        go.AddComponent<CombatWindowUI>();
        go.AddComponent<StageEntryUI>();
    }
}
