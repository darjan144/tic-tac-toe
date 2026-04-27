using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class OrientationManager : UIBehaviour
{
    public enum Orientation { Landscape, Portrait }

    public static event Action<Orientation> OnOrientationChanged;
    public static Orientation CurrentOrientation { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CurrentOrientation = Screen.width >= Screen.height ? Orientation.Landscape : Orientation.Portrait;
    }

    protected override void OnRectTransformDimensionsChange()
    {
        var newOrientation = Screen.width >= Screen.height ? Orientation.Landscape : Orientation.Portrait;
        if (newOrientation == CurrentOrientation) return;

        CurrentOrientation = newOrientation;
        OnOrientationChanged?.Invoke(CurrentOrientation);
    }
}
