using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Cell;

public class CellMirror : MonoBehaviour
{
    [SerializeField] Cell _source;
    [SerializeField] Image _image;
    [SerializeField] DrawAnimation _drawAnimation;

    public RectTransform RectTransform => (RectTransform)transform;

    void OnEnable()
    {
        _source.OnMarkSet += OnMarkSet;
        _source.OnCleared += OnCleared;
        SyncToSource();
    }

    void OnDisable()
    {
        _source.OnMarkSet -= OnMarkSet;
        _source.OnCleared -= OnCleared;
    }

    void OnMarkSet(CellState state, Sprite sprite)
    {
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

        _drawAnimation.Play();
    }

    void OnCleared()
    {
        _image.enabled = false;
        _drawAnimation.ResetFill();
    }

    void SyncToSource()
    {
        if (_source.State == CellState.Empty)
        {
            _image.enabled = false;
            _drawAnimation.ResetFill();
            return;
        }

        _image.sprite = _source.Image.sprite;
        _image.type = _source.Image.type;
        _image.fillMethod = _source.Image.fillMethod;
        _image.fillOrigin = _source.Image.fillOrigin;
        _image.fillAmount = 1f;
        _image.enabled = true;
    }
}
