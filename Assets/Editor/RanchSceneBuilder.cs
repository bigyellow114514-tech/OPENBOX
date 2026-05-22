using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RanchSceneBuilder
{
    const string ScenePath   = "Assets/Scenes/RanchScene.unity";
    const string BgName      = "Background";
    const string FenceName   = "FenceBounds";
    const string SpritePath  = "Assets/Resources/Sprites/Main_Ranch.png";

    [MenuItem("OpenBox/场景构建/构建牧场场景")]
    public static void Build()
    {
        // Open RanchScene if it's not the active scene
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (active.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        BuildBackground();
        BuildFenceBounds();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RanchSceneBuilder] 牧场场景已构建：背景 + 栅栏碰撞盒已就位。");
    }

    // ── Background ────────────────────────────────────────────────────────────

    static void BuildBackground()
    {
        GameObject existing = GameObject.Find(BgName);
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject(BgName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        sr.sortingOrder = 0;
        go.AddComponent<BackgroundFill>();

        EditorUtility.SetDirty(go);
    }

    // ── Fence Bounds ──────────────────────────────────────────────────────────

    static void BuildFenceBounds()
    {
        // Skip if already exists — don't overwrite a manually drawn fence shape
        GameObject existing = GameObject.Find(FenceName);
        if (existing != null)
        {
            Debug.Log("[RanchSceneBuilder] FenceBounds 已存在，跳过重建（保留手动编辑的形状）。");
            return;
        }

        var go = new GameObject(FenceName);
        var col = go.AddComponent<PolygonCollider2D>();
        col.isTrigger = false; // OverlapPoint on triggers is affected by queriesHitTriggers setting

        // Default parallelogram in world-space units (camera orthographicSize = 5).
        // Matches the fence enclosure in Main_Ranch.png.
        // The user should drag these 4 points in the Scene view to refine the shape.
        col.SetPath(0, new Vector2[]
        {
            new Vector2(-8.5f, -3.0f),   // bottom-left
            new Vector2( 7.5f, -3.0f),   // bottom-right
            new Vector2( 7.5f,  1.6f),   // top-right
            new Vector2(-8.5f,  1.6f),   // top-left
        });

        EditorUtility.SetDirty(go);
    }
}
