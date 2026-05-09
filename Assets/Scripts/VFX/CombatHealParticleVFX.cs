using UnityEngine;

public sealed class CombatHealParticleVFX : MonoBehaviour
{
    [SerializeField] float destroyAfter = 1.2f;

    ParticleSystem[] _systems;

    void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        if (_systems == null || _systems.Length == 0)
            _systems = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < _systems.Length; i++)
        {
            ParticleSystem ps = _systems[i];
            if (ps == null) continue;
            NormalizeVelocityCurveModes(ps);
            ps.Clear(true);
            ps.Play(true);
        }

        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);
    }

    static void NormalizeVelocityCurveModes(ParticleSystem ps)
    {
        var velocity = ps.velocityOverLifetime;
        if (!velocity.enabled) return;

        velocity.x = ToTwoConstantCurve(velocity.x);
        velocity.y = ToTwoConstantCurve(velocity.y);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalZ = ToTwoConstantCurve(velocity.orbitalZ);
    }

    static ParticleSystem.MinMaxCurve ToTwoConstantCurve(ParticleSystem.MinMaxCurve source)
    {
        switch (source.mode)
        {
            case ParticleSystemCurveMode.TwoConstants:
                return source;
            case ParticleSystemCurveMode.Curve:
                float curveValue = source.curve != null ? source.curve.Evaluate(0f) * source.curveMultiplier : source.constant;
                return new ParticleSystem.MinMaxCurve(curveValue, curveValue);
            case ParticleSystemCurveMode.TwoCurves:
                float minCurveValue = source.curveMin != null ? source.curveMin.Evaluate(0f) * source.curveMultiplier : source.constantMin;
                float maxCurveValue = source.curveMax != null ? source.curveMax.Evaluate(0f) * source.curveMultiplier : source.constantMax;
                return new ParticleSystem.MinMaxCurve(minCurveValue, maxCurveValue);
            default:
                return new ParticleSystem.MinMaxCurve(source.constant, source.constant);
        }
    }
}
