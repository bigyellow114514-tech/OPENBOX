using UnityEngine;

public static class StageFlowBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeObjects()
    {
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
