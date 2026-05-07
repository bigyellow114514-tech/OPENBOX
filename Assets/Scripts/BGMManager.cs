using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    const float DelayBetweenLoops = 3f;

    AudioSource _audioSource;
    AudioClip _bgmClip;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = false;
        _audioSource.playOnAwake = false;
        _bgmClip = Resources.Load<AudioClip>("BGM/BGM001");
    }

    void Start()
    {
        StartCoroutine(PlayLoop());
    }

    IEnumerator PlayLoop()
    {
        while (true)
        {
            _audioSource.clip = _bgmClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_bgmClip.length);
            yield return new WaitForSeconds(DelayBetweenLoops);
        }
    }
}
