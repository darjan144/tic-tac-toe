using UnityEngine;
using DG.Tweening;

public abstract class PopupBase : MonoBehaviour
{
    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected RectTransform _popupPanel;
    [SerializeField] float _animDuration = 0.3f;

    public virtual void Open()
    {
        gameObject.SetActive(true);

        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_popupPanel);

        _canvasGroup.alpha = 0f;
        _popupPanel.localScale = Vector3.one * 0.8f;

        _canvasGroup.DOFade(1f, _animDuration);
        _popupPanel.DOScale(Vector3.one, _animDuration).SetEase(Ease.OutBack);
    }

    public virtual void Close()
    {
        DOTween.Kill(_canvasGroup);
        DOTween.Kill(_popupPanel);

        _canvasGroup.DOFade(0f, _animDuration);
        _popupPanel.DOScale(Vector3.one * 0.8f, _animDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
