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
    public Sprite CurrentSprite { get; private set; }
    public Button Button => _button;
    public RectTransform RectTransform => (RectTransform)transform;

    void Awake()
    {
        if (_image != null) _image.enabled = false;
    }

    public void SetMark(CellState state, Sprite sprite)
    {
        State = state;
        CurrentSprite = sprite;
        Debug.Log($"[Observer] Cell {gameObject.name}: firing OnMarkSet({state})");
        OnMarkSet?.Invoke(state, sprite);
    }

    public void Clear()
    {
        State = CellState.Empty;
        CurrentSprite = null;
        Debug.Log($"[Observer] Cell {gameObject.name}: firing OnCleared");
        OnCleared?.Invoke();
    }
}
