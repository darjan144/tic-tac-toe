using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : PopupBase
{
    [SerializeField] Toggle _bgmToggle;
    [SerializeField] Toggle _sfxToggle;
    [SerializeField] Toggle _fullscreenToggle;
    [SerializeField] Button _closeButton;

    Toggle _adsConsentToggle;

    void Awake()
    {
        _adsConsentToggle = CreateRowLike(_sfxToggle, _bgmToggle, "ADS CONSENT", "PERSONALISED ADS");
    }

    /// <summary>
    /// Builds an extra settings row at runtime by cloning the SFX row, placed one row-spacing
    /// below it, so no prefab edit is needed for the consent toggle.
    /// </summary>
    static Toggle CreateRowLike(Toggle template, Toggle rowAbove, string objectName, string label)
    {
        // Find the row object: the ancestor of the template that is a sibling of the other toggle's row.
        Transform row = template.transform;
        while (row.parent != null && !rowAbove.transform.IsChildOf(row.parent))
            row = row.parent;
        Transform rowAboveRoot = rowAbove.transform;
        while (rowAboveRoot.parent != row.parent && rowAboveRoot.parent != null)
            rowAboveRoot = rowAboveRoot.parent;

        var clone = Instantiate(row.gameObject, row.parent);
        clone.name = objectName;
        clone.transform.SetSiblingIndex(row.GetSiblingIndex() + 1);

        // Without a layout group, offset by the same spacing as between the BGM and SFX rows.
        if (row.parent.GetComponent<LayoutGroup>() == null &&
            clone.transform is RectTransform cloneRect && row is RectTransform rowRect &&
            rowAboveRoot is RectTransform aboveRect)
        {
            cloneRect.anchoredPosition = rowRect.anchoredPosition + (rowRect.anchoredPosition - aboveRect.anchoredPosition);
        }

        foreach (var text in clone.GetComponentsInChildren<TMPro.TMP_Text>(true))
            text.text = label;

        var toggle = clone.GetComponentInChildren<Toggle>(true);
        toggle.onValueChanged.RemoveAllListeners();
        return toggle;
    }

    void OnEnable()
    {
        _adsConsentToggle.onValueChanged.AddListener(OnAdsConsentToggleChanged);
        _bgmToggle.onValueChanged.AddListener(OnBGMToggleChanged);
        _sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        _fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnDisable()
    {
        _adsConsentToggle.onValueChanged.RemoveListener(OnAdsConsentToggleChanged);
        _bgmToggle.onValueChanged.RemoveListener(OnBGMToggleChanged);
        _sfxToggle.onValueChanged.RemoveListener(OnSFXToggleChanged);
        _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);
        _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    public override void Open()
    {
        if (SaveManager.Instance != null)
        {
            _bgmToggle.SetIsOnWithoutNotify(SaveManager.Instance.IsBGMEnabled);
            _sfxToggle.SetIsOnWithoutNotify(SaveManager.Instance.IsSFXEnabled);
            _adsConsentToggle.SetIsOnWithoutNotify(SaveManager.Instance.AdsConsent);
        }

        _fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        base.Open();
    }

    void OnBGMToggleChanged(bool value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IsBGMEnabled = value;

        AudioManager.Instance?.SetBGMEnabled(value);
    }

    void OnSFXToggleChanged(bool value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.IsSFXEnabled = value;
    }

    void OnAdsConsentToggleChanged(bool value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.AdsConsent = value;

        SoundAds.SoundAdsSdk.SetConsent(value);
    }

    void OnFullscreenToggleChanged(bool value)
    {
        Screen.fullScreen = value;
    }

    void OnCloseClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Close();
    }
}
