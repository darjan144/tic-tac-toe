using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button _playButton;
    [SerializeField] Button _statsButton;
    [SerializeField] Button _settingsButton;
    [SerializeField] Button _exitButton;

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

#if UNITY_WEBGL && !UNITY_EDITOR
        _exitButton.gameObject.SetActive(false);
#endif

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM();
    }

    void OnPlayClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _themeSelectionPopup.Open();
    }

    void OnStatsClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _statsPopup.Open();
    }

    void OnSettingsClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _settingsPopup.Open();
    }

    void OnExitClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _exitConfirmPopup.Open();
    }
}
