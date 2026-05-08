using UnityEngine;

[CreateAssetMenu(
    fileName = "PetHealVFXSettings",
    menuName = "OpenBox/VFX/Pet Heal VFX Settings")]
public sealed class CombatHealVFXSettings : ScriptableObject
{
    public const string DefaultResourcePath = "VFX/PetHealVFXSettings";

    [Header("Timing")]
    [Min(0f)] public float startDelayAfterImpact = 0f;
    [Min(0.05f)] public float duration = 0.64f;
    [Range(0.01f, 0.5f)] public float fadeInPortion = 0.18f;
    [Range(0.01f, 0.8f)] public float fadeOutStart = 0.68f;

    [Header("Glow")]
    public Color outerGlowColor = new Color(0.38f, 1f, 0.48f, 0.26f);
    public Color innerGlowColor = new Color(0.76f, 1f, 0.58f, 0.18f);
    [Min(0f)] public float outerGlowWidth = 0.9f;
    [Min(0f)] public float outerGlowHeight = 0.72f;
    [Min(0f)] public float innerGlowWidth = 0.54f;
    [Min(0f)] public float innerGlowHeight = 1.1f;
    public float innerGlowYOffset = -0.08f;
    [Range(0f, 0.3f)] public float pulseStrength = 0.08f;
    [Min(0f)] public float pulseFrequency = 4f;

    [Header("Rising Particles")]
    [Range(0, 48)] public int risingParticleCount = 12;
    public Color risingParticleColor = new Color(0.64f, 1f, 0.42f, 0.72f);
    [Range(0f, 1f)] public float risingMinSideOffset = 0.13f;
    [Range(0f, 1f)] public float risingSideOffsetRange = 0.25f;
    [Range(0f, 1f)] public float risingBottomOffset = 0.08f;
    [Range(0f, 1f)] public float risingTravelHeight = 0.82f;
    [Min(0f)] public float risingMinSize = 0.035f;
    [Min(0f)] public float risingMaxSize = 0.085f;
    [Range(1, 8)] public int crossEvery = 3;

    [Header("Orbit Sparks")]
    [Range(0, 24)] public int orbitSparkCount = 5;
    public Color orbitSparkColor = new Color(0.9f, 1f, 0.62f, 0.82f);
    [Range(0f, 1f)] public float orbitRadiusX = 0.18f;
    [Range(0f, 1f)] public float orbitRadiusXPulse = 0.05f;
    [Range(0f, 1f)] public float orbitRadiusY = 0.18f;
    [Min(0f)] public float orbitSparkSize = 0.045f;
    [Min(0f)] public float orbitSpeed = 1f;

    [Header("Placement")]
    [Range(0f, 1f)] public float centerY = 0.48f;

    public static CombatHealVFXSettings LoadDefault()
    {
        CombatHealVFXSettings settings = Resources.Load<CombatHealVFXSettings>(DefaultResourcePath);
        return settings != null ? settings : CreateInstance<CombatHealVFXSettings>();
    }

    void OnValidate()
    {
        duration = Mathf.Max(0.05f, duration);
        fadeInPortion = Mathf.Clamp(fadeInPortion, 0.01f, 0.5f);
        fadeOutStart = Mathf.Clamp(fadeOutStart, fadeInPortion, 0.98f);
        crossEvery = Mathf.Max(1, crossEvery);
    }
}
