using UnityEngine;

public class OrientationLayoutSwitcher : MonoBehaviour
{
    [SerializeField] GameObject landscapeLayout;
    [SerializeField] GameObject portraitLayout;

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
        if (landscapeLayout) landscapeLayout.SetActive(landscape);
        if (portraitLayout) portraitLayout.SetActive(!landscape);
    }
}
