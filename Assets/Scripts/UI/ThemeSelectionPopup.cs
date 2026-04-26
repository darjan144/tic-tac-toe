using UnityEngine;
using UnityEngine.UI;

public class ThemeSelectionPopup : PopupBase
{
    [SerializeField] ThemeOptionUI[] _themeOptions;
    [SerializeField] Button _startButton;
    [SerializeField] Button _closeButton;

    int _selectedIndex;

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

        for (int i = 0; i < _themeOptions.Length; i++)
        {
            var theme = tm.GetTheme(i);
            if (theme == null) continue;

            _themeOptions[i].Setup(theme.XSprite, theme.OSprite, i == _selectedIndex);

            int index = i;
            _themeOptions[i].SelectButton.onClick.RemoveAllListeners();
            _themeOptions[i].SelectButton.onClick.AddListener(() => SelectTheme(index));
        }
    }

    void SelectTheme(int index)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _selectedIndex = index;
        for (int i = 0; i < _themeOptions.Length; i++)
            _themeOptions[i].SetSelected(i == _selectedIndex);
    }

    void OnStartClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (ThemeManager.Instance != null)
            ThemeManager.Instance.SelectTheme(_selectedIndex);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.LoadGame();
    }

    void OnCloseClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        Close();
    }
}
