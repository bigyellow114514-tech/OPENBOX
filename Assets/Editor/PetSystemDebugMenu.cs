using UnityEditor;
using UnityEngine;

public static class PetSystemDebugMenu
{
    [MenuItem("OpenBox/测试/打开宠物面板（仅运行时）")]
    [MenuItem("OpenBox/Test/Open Pet Panel Play Mode")]
    static void OpenPetPanel()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请先进入 Play Mode 再打开宠物面板。");
            return;
        }

        PetPanelUI.ShowOrCreate();
    }

    [MenuItem("OpenBox/测试/增加宠物测试资源（仅运行时）")]
    [MenuItem("OpenBox/Test/Add Pet Test Resources Play Mode")]
    static void AddPetTestResources()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请先进入 Play Mode 再增加宠物测试资源。");
            return;
        }

        PlayerResourceManager.Instance?.AddPetTickets(10);
        PlayerResourceManager.Instance?.AddPetFood(5000);
    }
}
