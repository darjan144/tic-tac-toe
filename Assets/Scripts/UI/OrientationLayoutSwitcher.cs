using UnityEngine;

public class OrientationLayoutSwitcher : MonoBehaviour
{
    [SerializeField] GameObject _landscapeLayout;
    [SerializeField] GameObject _portraitLayout;

    void OnEnable()
    {
        Apply(OrientationManager.CurrentOrientation);
        OrientationManager.OnOrientationChanged += Apply;
    }

    void OnDisable()
    {
        OrientationManager.OnOrientationChanged -= Apply;
    }

    void Apply(OrientationManager.Orientation orientation)
    {
        bool landscape = orientation == OrientationManager.Orientation.Landscape;
        if (_landscapeLayout) _landscapeLayout.SetActive(landscape);
        if (_portraitLayout) _portraitLayout.SetActive(!landscape);
    }
}
