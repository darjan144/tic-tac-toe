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
        Debug.Log($"[Observer] CellMirror {gameObject.name}: subscribing to {_source.name}");
        SyncToSource();
    }

    void OnDisable()
    {
        _source.OnMarkSet -= OnMarkSet;
        _source.OnCleared -= OnCleared;
        Debug.Log($"[Observer] CellMirror {gameObject.name}: unsubscribing from {_source.name}");
    }

    void OnMarkSet(CellState state, Sprite sprite)
    {
        Debug.Log($"[Observer] CellMirror {gameObject.name}: received OnMarkSet({state}) from {_source.name}");
        _image.sprite = sprite;
        _image.enabled = true;
        CellVisuals.ApplyFill(_image, state);
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

        _image.sprite = _source.CurrentSprite;
        CellVisuals.ApplyFill(_image, _source.State);
        _image.fillAmount = 1f;
        _image.enabled = true;
    }
}
