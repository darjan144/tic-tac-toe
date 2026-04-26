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
