using UnityEngine;

[CreateAssetMenu(
    fileName = "CombatBattleLayoutSettings",
    menuName = "OpenBox/Combat/Battle Layout Settings")]
public sealed class CombatBattleLayoutSettings : ScriptableObject
{
    public const string DefaultResourcePath = "Combat/CombatBattleLayoutSettings";

    [Header("Scene Popup")]
    public Vector2 panelSize = new Vector2(9.2f, 5.9f);
    public Vector2 arenaPadding = new Vector2(0.34f, 0.86f);
    public float arenaBottomPadding = 0.72f;

    [Header("Units")]
    [Range(0.1f, 0.6f)] public float unitWidthRatio = 0.24f;
    [Range(0.1f, 0.9f)] public float unitHeightRatio = 0.68f;
    [Range(0.5f, 2f)] public float playerUnitSizeScale = 1f;
    [Range(0.5f, 2f)] public float enemyUnitSizeScale = 1.25f;
    [Range(0.1f, 0.8f)] public float playerXRatio = 0.22f;
    [Range(0.2f, 0.9f)] public float enemyXRatio = 0.72f;
    [Range(0f, 0.5f)] public float unitBottomRatio = 0.08f;
    [Range(0.1f, 0.8f)] public float unitPivotHeight = 0.46f;

    [Header("Pets")]
    [Range(0.1f, 0.8f)] public float petSizeRatio = 0.42f;
    public Vector2 playerPetOffset = new Vector2(-0.38f, 0.2f);
    public Vector2 enemyPetOffset = new Vector2(0.38f, 0.2f);

    [Header("Canvas Panel")]
    public Vector2 canvasPanelSize = new Vector2(900f, 560f);
    public Vector2 titlePosition = new Vector2(0f, 226f);
    public Vector2 roundPosition = new Vector2(0f, 198f);
    public Vector2 speedStartPosition = new Vector2(-362f, 196f);
    public Vector2 speedStep = new Vector2(64f, 0f);
    public Vector2 speedButtonSize = new Vector2(58f, 30f);
    public Vector2 playerHpTextPosition = new Vector2(-230f, 158f);
    public Vector2 playerHpBarPosition = new Vector2(-205f, 132f);
    public Vector2 enemyHpTextPosition = new Vector2(205f, 158f);
    public Vector2 enemyHpBarPosition = new Vector2(230f, 132f);
    public Vector2 hpBarSize = new Vector2(285f, 20f);
    public Vector2 actionButtonPosition = new Vector2(378f, -230f);
    public Vector2 resultTextPosition = new Vector2(-160f, -230f);

    [Header("Canvas Fonts")]
    [Min(1)] public int titleFontSize = 30;
    [Min(1)] public int roundFontSize = 20;
    [Min(1)] public int hpFontSize = 19;
    [Min(1)] public int speedFontSize = 17;
    [Min(1)] public int actionFontSize = 19;
    [Min(1)] public int resultFontSize = 20;
    [Min(1)] public int floatingFontSize = 34;

    public static CombatBattleLayoutSettings LoadDefault()
    {
        CombatBattleLayoutSettings settings = Resources.Load<CombatBattleLayoutSettings>(DefaultResourcePath);
        return settings != null ? settings : CreateInstance<CombatBattleLayoutSettings>();
    }
}
