using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Cell : MonoBehaviour
{
    [SerializeField] Image _image;
    [SerializeField] Button _button;
    [SerializeField] DrawAnimation _drawAnimation;

    public enum CellState { Empty, X, O }

    public CellState State { get; private set; }
    public Button Button => _button;
    public RectTransform RectTransform => (RectTransform)transform;

    public Tween SetMark(CellState state, Sprite sprite)
    {
        State = state;
        _image.sprite = sprite;
        _image.enabled = true;

        if (state == CellState.X)
        {
            _image.type = Image.Type.Filled;
            _image.fillMethod = Image.FillMethod.Radial180;
            _image.fillOrigin = (int)Image.Origin180.Top;
        }
        else if (state == CellState.O)
        {
            _image.type = Image.Type.Filled;
            _image.fillMethod = Image.FillMethod.Radial360;
            _image.fillOrigin = (int)Image.Origin360.Bottom;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPopSFX();

        return _drawAnimation.Play();
    }

    public void Clear()
    {
        State = CellState.Empty;
        _image.enabled = false;
        _drawAnimation.ResetFill();
    }
}
