using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Landscape Buttons")]
    [SerializeField] Button _playButton;
    [SerializeField] Button _statsButton;
    [SerializeField] Button _settingsButton;
    [SerializeField] Button _exitButton;

    [Header("Portrait Buttons")]
    [SerializeField] Button _portraitPlayButton;
    [SerializeField] Button _portraitStatsButton;
    [SerializeField] Button _portraitSettingsButton;
    [SerializeField] Button _portraitExitButton;

    [Header("Popups")]
    [SerializeField] ThemeSelectionPopup _themeSelectionPopup;
    [SerializeField] StatsPopup _statsPopup;
    [SerializeField] SettingsPopup _settingsPopup;
    [SerializeField] ExitConfirmPopup _exitConfirmPopup;

    void Start()
    {
        _playButton.onClick.AddListener(OnPlayClicked);
        _statsButton.onClick.AddListener(OnStatsClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
        _exitButton.onClick.AddListener(OnExitClicked);

        _portraitPlayButton.onClick.AddListener(OnPlayClicked);
        _portraitStatsButton.onClick.AddListener(OnStatsClicked);
        _portraitSettingsButton.onClick.AddListener(OnSettingsClicked);
        _portraitExitButton.onClick.AddListener(OnExitClicked);

    }

    void OnPlayClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        AudioManager.Instance?.PlayWooshSFX();
        _themeSelectionPopup.Open();
    }

    void OnStatsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        AudioManager.Instance?.PlayWooshSFX();
        _statsPopup.Open();
    }

    void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        AudioManager.Instance?.PlayWooshSFX();
        _settingsPopup.Open();
    }

    void OnExitClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        AudioManager.Instance?.PlayWooshSFX();
        _exitConfirmPopup.Open();
    }
}
