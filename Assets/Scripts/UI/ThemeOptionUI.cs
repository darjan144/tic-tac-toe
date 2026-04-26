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
}
