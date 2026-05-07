using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    AudioSource _source;
    AudioClip _kanshu;
    AudioClip _fenjie;
    AudioClip _zhuangbei;
    AudioClip _dianji;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        _source.spatialBlend = 0f;
        _source.playOnAwake  = false;

        _kanshu    = Resources.Load<AudioClip>("BGM/SE1kanshu");
        _fenjie    = Resources.Load<AudioClip>("BGM/SE2fenjie");
        _zhuangbei = Resources.Load<AudioClip>("BGM/SE3zhuangbei");
        _dianji    = Resources.Load<AudioClip>("BGM/SE4dianji");
    }

    public static void PlayKanshu()    => Instance?.Play(Instance._kanshu);
    public static void PlayFenjie()    => Instance?.Play(Instance._fenjie);
    public static void PlayZhuangbei() => Instance?.Play(Instance._zhuangbei);
    public static void PlayDianji()    => Instance?.Play(Instance._dianji);

    void Play(AudioClip clip)
    {
        if (clip != null) _source.PlayOneShot(clip);
    }
}
