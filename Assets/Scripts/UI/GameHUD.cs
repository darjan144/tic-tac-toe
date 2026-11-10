using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] TMP_Text _timerText;
    [SerializeField] TMP_Text _p1MovesText;
    [SerializeField] TMP_Text _p2MovesText;
    [SerializeField] TMP_Text _currentTurnText;
    [SerializeField] Button _settingsButton;
    [SerializeField] SettingsPopup _settingsPopup;

    [Header("Command Pattern")]
    [SerializeField] Button _undoButton;
    [SerializeField] Button _redoButton;
    [SerializeField] GameManager _gameManager;

    TMP_Text _undoLabel;
    string _undoDefaultText = "UNDO";
    bool _rewardedAdInProgress;

    void OnEnable()
    {
        _settingsButton.onClick.AddListener(OnSettingsClicked);
        if (_undoButton != null) _undoButton.onClick.AddListener(OnUndoClicked);
        if (_redoButton != null) _redoButton.onClick.AddListener(OnRedoClicked);
    }

    void OnDisable()
    {
        _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (_undoButton != null) _undoButton.onClick.RemoveListener(OnUndoClicked);
        if (_redoButton != null) _redoButton.onClick.RemoveListener(OnRedoClicked);
    }

    public void UpdateTimer(float seconds)
    {
        _timerText.text = TimeFormatUtil.FormatDuration(seconds);
    }

    public void UpdateMoves(int p1Moves, int p2Moves)
    {
        _p1MovesText.text = p1Moves.ToString();
        _p2MovesText.text = p2Moves.ToString();
    }

    public void UpdateTurn(Player player)
    {
        _currentTurnText.text = player == Player.P1 ? "PLAYER ONE'S TURN (X)" : "PLAYER TWO'S TURN (O)";
    }

    void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        AudioManager.Instance?.PlayWooshSFX();
        _settingsPopup.Open();
    }

    public void UpdateUndo(bool requiresAd)
    {
        if (_undoLabel == null && _undoButton != null)
        {
            _undoLabel = _undoButton.GetComponentInChildren<TMP_Text>(true);
            if (_undoLabel != null) _undoDefaultText = _undoLabel.text;
        }

        if (_undoLabel != null)
            _undoLabel.text = requiresAd ? "+1 UNDO (AD)" : _undoDefaultText;
    }

    void OnUndoClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (!_gameManager.UndoRequiresAd)
        {
            _gameManager.OnUndo();
            return;
        }

        if (_rewardedAdInProgress) return;
        _rewardedAdInProgress = true;
        _undoButton.interactable = false;

        // Rewarded audio ad: listen to the end, and the undo happens straight away.
        GameAds.Show(AdPlacements.ExtraUndo, SoundAds.AdFormat.Rewarded, SoundAds.AdMoment.Reward,
            onFinished: _ =>
            {
                _rewardedAdInProgress = false;
                if (_undoButton != null) _undoButton.interactable = true;
                GameAds.Preload(AdPlacements.ExtraUndo, SoundAds.AdFormat.Rewarded, SoundAds.AdMoment.Reward);
            },
            onReward: () =>
            {
                _gameManager.GrantExtraUndo();
                _gameManager.OnUndo();
            });
    }

    void OnRedoClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        _gameManager.OnRedo();
    }
}
