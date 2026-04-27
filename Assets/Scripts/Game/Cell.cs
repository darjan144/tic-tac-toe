using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Cell : MonoBehaviour
{
    [SerializeField] Image _image;
    [SerializeField] Button _button;
    [SerializeField] DrawAnimation _drawAnimation;

    public enum CellState { Empty, X, O }

    public event Action<CellState, Sprite> OnMarkSet;
    public event Action OnCleared;

    public CellState State { get; private set; }
    public Button Button => _button;
    public Image Image => _image;
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

        OnMarkSet?.Invoke(state, sprite);

        return _drawAnimation.Play();
    }

    public void Clear()
    {
        State = CellState.Empty;
        _image.enabled = false;
        _drawAnimation.ResetFill();
        OnCleared?.Invoke();
    }
}
