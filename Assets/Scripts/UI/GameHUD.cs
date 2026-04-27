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

    void OnEnable()
    {
        _settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    void OnDisable()
    {
        _settingsButton.onClick.RemoveListener(OnSettingsClicked);
    }

    public void UpdateTimer(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        _timerText.text = $"{minutes:00}:{secs:00}";
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
}
