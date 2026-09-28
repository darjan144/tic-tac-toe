using UnityEngine;

public class OrientationLayoutSwitcher : MonoBehaviour
{
    [SerializeField] GameObject _landscapeLayout;
    [SerializeField] GameObject _portraitLayout;

    void OnEnable()
    {
        Apply(OrientationManager.CurrentOrientation);
        OrientationManager.OnOrientationChanged += Apply;
        Debug.Log("[Observer] OrientationLayoutSwitcher: subscribing to OnOrientationChanged");
    }

    void OnDisable()
    {
        OrientationManager.OnOrientationChanged -= Apply;
        Debug.Log("[Observer] OrientationLayoutSwitcher: unsubscribing from OnOrientationChanged");
    }

    void Apply(OrientationManager.Orientation orientation)
    {
        Debug.Log($"[Observer] OrientationLayoutSwitcher: received orientation change -> {orientation}");
        bool landscape = orientation == OrientationManager.Orientation.Landscape;
        if (_landscapeLayout) _landscapeLayout.SetActive(landscape);
        if (_portraitLayout) _portraitLayout.SetActive(!landscape);
    }
}
