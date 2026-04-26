using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class DrawAnimation : MonoBehaviour
{
    [SerializeField] float _duration = 0.5f;
    [SerializeField] Ease _ease = Ease.OutQuad;

    [SerializeField] private Image _image;

    public Tween Play()
    {
        ResetFill();
        return _image.DOFillAmount(1f, _duration).SetEase(_ease);
    }

    public void ResetFill()
    {
        _image.fillAmount = 0f;
    }

    void OnDisable()
    {
        if (_image != null)
            DOTween.Kill(_image);
    }

    [ContextMenu("Preview Draw Animation")]
    void PreviewDraw()
    {
        ResetFill();
        DOTween.Kill(_image);
        _image.DOFillAmount(1f, _duration).SetEase(_ease).SetUpdate(true);
    }
}
