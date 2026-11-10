using SoundAds;
using UnityEngine;
using UnityEngine.UI;

public class ThemeSelectionPopup : PopupBase
{
    [SerializeField] ThemeOptionUI[] _themeOptions;
    [SerializeField] Button _startButton;
    [SerializeField] Button _closeButton;

    int _selectedIndex;
    bool _rewardedAdInProgress;

    void OnEnable()
    {
        _startButton.onClick.AddListener(OnStartClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnDisable()
    {
        _startButton.onClick.RemoveListener(OnStartClicked);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    public override void Open()
    {
        PopulateThemes();
        base.Open();
    }

    void PopulateThemes()
    {
        var tm = ThemeManager.Instance;
        if (tm == null) return;

        _selectedIndex = 0;
        bool anyLocked = false;

        for (int i = 0; i < _themeOptions.Length; i++)
        {
            var theme = tm.GetTheme(i);
            if (theme == null) continue;

            _themeOptions[i].Setup(theme.XSprite, theme.OSprite, i == _selectedIndex);
            _themeOptions[i].SetLocked(tm.IsLocked(i));
            anyLocked |= tm.IsLocked(i);

            int index = i;
            _themeOptions[i].SelectButton.onClick.RemoveAllListeners();
            _themeOptions[i].SelectButton.onClick.AddListener(() => SelectTheme(index));
        }

        // Pre-buffer the unlock ad so it starts instantly when tapped.
        if (anyLocked)
            GameAds.Preload(AdPlacements.ThemeUnlock, AdFormat.Rewarded, AdMoment.Reward);
    }

    void SelectTheme(int index)
    {
        AudioManager.Instance?.PlayButtonClick();

        if (ThemeManager.Instance != null && ThemeManager.Instance.IsLocked(index))
        {
            OfferUnlock(index);
            return;
        }

        _selectedIndex = index;
        for (int i = 0; i < _themeOptions.Length; i++)
            _themeOptions[i].SetSelected(i == _selectedIndex);
    }

    /// <summary>Tapping a locked theme plays a rewarded audio ad; listening to the end unlocks it for good.</summary>
    void OfferUnlock(int index)
    {
        if (_rewardedAdInProgress) return;
        _rewardedAdInProgress = true;
        _startButton.interactable = false;

        GameAds.Show(AdPlacements.ThemeUnlock, AdFormat.Rewarded, AdMoment.Reward,
            onFinished: _ =>
            {
                _rewardedAdInProgress = false;
                _startButton.interactable = true;
            },
            onReward: () =>
            {
                SaveManager.Instance?.UnlockTheme(index);
                _themeOptions[index].SetLocked(false);
                SelectTheme(index);
            });
    }

    void OnStartClicked()
    {
        if (ThemeManager.Instance != null && ThemeManager.Instance.IsLocked(_selectedIndex)) return;

        AudioManager.Instance?.PlayButtonClick();
        ThemeManager.Instance?.SelectTheme(_selectedIndex);
        GameFlowManager.Instance?.LoadGame();
    }

    void OnCloseClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Close();
    }
}
