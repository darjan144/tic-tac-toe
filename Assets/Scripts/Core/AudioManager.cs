using UnityEngine;

public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    [Header("Music")]
    [SerializeField] AudioClip _bgmClip;

    [Header("SFX")]
    [SerializeField] AudioClip _clickSFX1;
    [SerializeField] AudioClip _clickSFX2;
    [SerializeField] AudioClip _popSFX;
    [SerializeField] AudioClip _wooshSFX;

    AudioSource _bgmSource;
    AudioSource[] _sfxPool;
    int _sfxPoolIndex;

    const int SFX_POOL_SIZE = 4;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _bgmSource = CreateAudioSource("BGM");
        _bgmSource.loop = true;
        _bgmSource.clip = _bgmClip;

        _sfxPool = new AudioSource[SFX_POOL_SIZE];
        for (int i = 0; i < SFX_POOL_SIZE; i++)
            _sfxPool[i] = CreateAudioSource($"SFX_{i}");
    }

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsBGMEnabled)
            PlayBGM();
    }

    AudioSource CreateAudioSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);
        return go.AddComponent<AudioSource>();
    }

    public void PlayBGM()
    {
        if (_bgmSource.isPlaying) return;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (SaveManager.Instance != null && !SaveManager.Instance.IsSFXEnabled) return;

        _sfxPool[_sfxPoolIndex].PlayOneShot(clip);
        _sfxPoolIndex = (_sfxPoolIndex + 1) % SFX_POOL_SIZE;
    }

    public void PlayButtonClick()
    {
        PlaySFX(Random.value > 0.5f ? _clickSFX1 : _clickSFX2);
    }

    public void PlayPopSFX()
    {
        PlaySFX(_popSFX);
    }

    public void PlayWooshSFX()
    {
        PlaySFX(_wooshSFX);
    }

    public void SetBGMEnabled(bool enabled)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IsBGMEnabled = enabled;

        if (enabled)
            PlayBGM();
        else
            StopBGM();
    }

    public void SetSFXEnabled(bool enabled)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IsSFXEnabled = enabled;
    }
}
