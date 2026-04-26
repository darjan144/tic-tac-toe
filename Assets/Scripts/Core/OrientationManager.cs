using System;
using UnityEngine;

public class OrientationManager : SingletonMonoBehaviour<OrientationManager>
{
    public enum Orientation { Landscape, Portrait }

    public static event Action<Orientation> OnOrientationChanged;
    public static Orientation CurrentOrientation { get; private set; }

    int _lastWidth;
    int _lastHeight;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        CurrentOrientation = _lastWidth >= _lastHeight ? Orientation.Landscape : Orientation.Portrait;
    }

    void Update()
    {
        if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;

        var newOrientation = _lastWidth >= _lastHeight ? Orientation.Landscape : Orientation.Portrait;
        if (newOrientation == CurrentOrientation) return;

        CurrentOrientation = newOrientation;
        OnOrientationChanged?.Invoke(CurrentOrientation);
    }
}
