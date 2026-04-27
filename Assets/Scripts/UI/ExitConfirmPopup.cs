using UnityEngine;
using UnityEngine.UI;

public class ExitConfirmPopup : PopupBase
{
    [SerializeField] Button _yesButton;
    [SerializeField] Button _noButton;

    void OnEnable()
    {
        _yesButton.onClick.AddListener(OnYesClicked);
        _noButton.onClick.AddListener(OnNoClicked);
    }

    void OnDisable()
    {
        _yesButton.onClick.RemoveListener(OnYesClicked);
        _noButton.onClick.RemoveListener(OnNoClicked);
    }

    void OnYesClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameFlowManager.Instance?.QuitGame();
    }

    void OnNoClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Close();
    }
}
