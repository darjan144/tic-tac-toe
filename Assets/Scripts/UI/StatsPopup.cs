using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPopup : PopupBase
{
    [SerializeField] TMP_Text _totalGamesText;
    [SerializeField] TMP_Text _p1WinsText;
    [SerializeField] TMP_Text _p2WinsText;
    [SerializeField] TMP_Text _drawsText;
    [SerializeField] TMP_Text _avgDurationText;
    [SerializeField] Button _closeButton;

    void OnEnable()
    {
        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnDisable()
    {
        _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    public override void Open()
    {
        RefreshStats();
        base.Open();
    }

    void RefreshStats()
    {
        var sm = SaveManager.Instance;
        if (sm == null) return;

        _totalGamesText.text = sm.TotalGames.ToString();
        _p1WinsText.text = sm.P1Wins.ToString();
        _p2WinsText.text = sm.P2Wins.ToString();
        _drawsText.text = sm.Draws.ToString();

        float avg = sm.AverageDuration;
        int minutes = Mathf.FloorToInt(avg / 60f);
        int seconds = Mathf.FloorToInt(avg % 60f);
        _avgDurationText.text = $"{minutes:00}:{seconds:00}";
    }

    void OnCloseClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        Close();
    }
}
