using System;
using UnityEngine;

public class ThemeManager : SingletonMonoBehaviour<ThemeManager>
{
    [Serializable]
    public class ThemeData
    {
        public Sprite XSprite;
        public Sprite OSprite;
    }

    [SerializeField] ThemeData[] _availableThemes;

    [Header("Ads")]
    [Tooltip("The last theme is premium: it is unlocked by listening to a rewarded audio ad.")]
    [SerializeField] bool _lastThemeIsPremium = true;

    public bool IsPremium(int index) =>
        _lastThemeIsPremium && _availableThemes.Length > 1 && index == _availableThemes.Length - 1;

    public bool IsLocked(int index) =>
        IsPremium(index) && (SaveManager.Instance == null || !SaveManager.Instance.IsThemeUnlocked(index));

    public ThemeData SelectedTheme { get; private set; }
    public int SelectedThemeIndex { get; private set; }
    public int ThemeCount => _availableThemes.Length;

    protected override void Awake()
    {
        base.Awake();
        if (_availableThemes != null && _availableThemes.Length > 0)
            SelectedTheme = _availableThemes[0];
    }

    public void SelectTheme(int index)
    {
        if (index < 0 || index >= _availableThemes.Length) return;
        SelectedThemeIndex = index;
        SelectedTheme = _availableThemes[index];
    }

    public ThemeData GetTheme(int index)
    {
        if (index < 0 || index >= _availableThemes.Length) return null;
        return _availableThemes[index];
    }
}
