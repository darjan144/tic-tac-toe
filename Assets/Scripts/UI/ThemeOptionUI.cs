using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThemeOptionUI : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Image _xImage;
    [SerializeField] UnityEngine.UI.Image _oImage;
    [SerializeField] GameObject _selectionBorder;
    [SerializeField] Button _selectButton;

    public Button SelectButton => _selectButton;

    public void Setup(Sprite xSprite, Sprite oSprite, bool selected)
    {
        _xImage.sprite = xSprite;
        _oImage.sprite = oSprite;
        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        _selectionBorder.SetActive(selected);
    }

    /// <summary>Dims the marks and shows a "listen to unlock" label for premium themes.</summary>
    public void SetLocked(bool locked)
    {
        if (!_colorsCached)
        {
            _xColor = _xImage.color;
            _oColor = _oImage.color;
            _colorsCached = true;
        }

        const float lockedAlpha = 0.3f;
        _xImage.color = locked ? new Color(_xColor.r, _xColor.g, _xColor.b, _xColor.a * lockedAlpha) : _xColor;
        _oImage.color = locked ? new Color(_oColor.r, _oColor.g, _oColor.b, _oColor.a * lockedAlpha) : _oColor;

        if (locked && _lockLabel == null) _lockLabel = CreateLockLabel();
        if (_lockLabel != null) _lockLabel.gameObject.SetActive(locked);
    }

    TMP_Text CreateLockLabel()
    {
        var go = new GameObject("LockLabel", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = "LISTEN TO\nUNLOCK";
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 48;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.85f, 0.25f);
        label.raycastTarget = false;
        return label;
    }

    TMP_Text _lockLabel;
    Color _xColor;
    Color _oColor;
    bool _colorsCached;
}
