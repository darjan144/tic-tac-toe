using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameResultPopup : PopupBase
{
    [SerializeField] TMP_Text _resultText;
    [SerializeField] TMP_Text _durationText;
    [SerializeField] Button _retryButton;
    [SerializeField] Button _exitButton;

    void OnEnable()
    {
        _retryButton.onClick.AddListener(OnRetryClicked);
        _exitButton.onClick.AddListener(OnExitClicked);
    }

    void OnDisable()
    {
        _retryButton.onClick.RemoveListener(OnRetryClicked);
        _exitButton.onClick.RemoveListener(OnExitClicked);
    }

    public void Show(string result, float duration)
    {
        _resultText.text = result;
        _durationText.text = $"GAME DURATION: {TimeFormatUtil.FormatDuration(duration)}";
        SetButtonsInteractable(true);

        base.Open();
    }

    void OnRetryClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (!GameAds.ShouldShowBetweenGamesAd())
        {
            GameFlowManager.Instance?.LoadGame();
            return;
        }

        // Between-games audio ad. OnAdFinished is guaranteed (even on failure), so the game can't get stuck.
        SetButtonsInteractable(false);
        GameAds.Show(AdPlacements.BetweenGames, SoundAds.AdFormat.Standard, SoundAds.AdMoment.None,
            onFinished: _ => GameFlowManager.Instance?.LoadGame());
    }

    void SetButtonsInteractable(bool interactable)
    {
        _retryButton.interactable = interactable;
        _exitButton.interactable = interactable;
    }

    void OnExitClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameFlowManager.Instance?.LoadMainMenu();
    }
}
