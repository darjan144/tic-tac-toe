using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : PopupBase
{
    [SerializeField] Toggle _bgmToggle;
    [SerializeField] Toggle _sfxToggle;
    [SerializeField] Toggle _fullscreenToggle;
    [SerializeField] Button _closeButton;

    void OnEnable()
    {
        _bgmToggle.onValueChanged.AddListener(OnBGMToggleChanged);
        _sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        _fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnDisable()
    {
        _bgmToggle.onValueChanged.RemoveListener(OnBGMToggleChanged);
        _sfxToggle.onValueChanged.RemoveListener(OnSFXToggleChanged);
        _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    public override void Open()
    {
        if (SaveManager.Instance != null)
        {
            _bgmToggle.SetIsOnWithoutNotify(SaveManager.Instance.IsBGMEnabled);
            _sfxToggle.SetIsOnWithoutNotify(SaveManager.Instance.IsSFXEnabled);
        }

        _fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        base.Open();
    }

    void OnBGMToggleChanged(bool value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IsBGMEnabled = value;

        AudioManager.Instance?.SetBGMEnabled(value);
    }

    void OnSFXToggleChanged(bool value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IsSFXEnabled = value;
    }

    void OnFullscreenToggleChanged(bool value)
    {
        Screen.fullScreen = value;
    }

    void OnCloseClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Close();
    }
}
