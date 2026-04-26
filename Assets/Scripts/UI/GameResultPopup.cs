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

        int minutes = Mathf.FloorToInt(duration / 60f);
        int seconds = Mathf.FloorToInt(duration % 60f);
        _durationText.text = $"GAME DURATION: {minutes:00}:{seconds:00}";

        base.Open();
    }

    void OnRetryClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadGame();
    }

    void OnExitClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadMainMenu();
    }
}
