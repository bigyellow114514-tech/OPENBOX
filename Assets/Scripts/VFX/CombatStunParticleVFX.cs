using UnityEngine;

public sealed class CombatStunParticleVFX : MonoBehaviour
{
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
            ps.Clear(true);
            ps.Play(true);
        }
    }
}
