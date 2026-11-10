using DG.Tweening;
using UnityEngine;

public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    const float AD_FADE_DURATION = 0.3f;

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
    float _bgmVolume = 1f;
    bool _adPlaying;

    const int SFX_POOL_SIZE = 4;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _bgmSource = CreateAudioSource("BGM");
        _bgmSource.loop = true;
        _bgmSource.clip = _bgmClip;
        _bgmVolume = _bgmSource.volume;

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
        if (_adPlaying) return; // never talk over an audio ad
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
        if (enabled && !_adPlaying)
            PlayBGM();
        else if (!enabled)
            StopBGM();
    }

    // ---- Audio ads: the game yields the audio channel while an ad is audible ----

    /// <summary>Fades out and pauses music, and suppresses SFX, while an audio ad plays.</summary>
    public void PauseBGMForAd()
    {
        _adPlaying = true;
        _bgmSource.DOKill();
        if (!_bgmSource.isPlaying) return;

        _bgmSource.DOFade(0f, AD_FADE_DURATION)
            .SetUpdate(true)
            .OnComplete(() => _bgmSource.Pause());
    }

    /// <summary>Resumes and fades music back in after an ad (if BGM is enabled).</summary>
    public void ResumeBGMAfterAd()
    {
        _adPlaying = false;
        _bgmSource.DOKill();

        bool bgmEnabled = SaveManager.Instance == null || SaveManager.Instance.IsBGMEnabled;
        if (!bgmEnabled)
        {
            _bgmSource.volume = _bgmVolume;
            return;
        }

        _bgmSource.volume = 0f;
        if (_bgmSource.time > 0f) _bgmSource.UnPause();
        else _bgmSource.Play();
        _bgmSource.DOFade(_bgmVolume, AD_FADE_DURATION).SetUpdate(true);
    }
}
