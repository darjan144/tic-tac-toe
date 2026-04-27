using UnityEngine;
using DG.Tweening;

public class StrikeAnimation : MonoBehaviour
{
    [SerializeField] RectTransform _lineTransform;
    [SerializeField] DrawAnimation _drawAnimation;

    public Tween Play(RectTransform middleCell, int winLineIndex)
    {
        _lineTransform.localPosition = new Vector3(middleCell.localPosition.x, middleCell.localPosition.y, _lineTransform.localPosition.z);

        float rotation = 0f;
        if (winLineIndex >= 3 && winLineIndex <= 5)
            rotation = -90f;
        else if (winLineIndex == 6)
            rotation = -45f;
        else if (winLineIndex == 7)
            rotation = 45f;

        _lineTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        _lineTransform.gameObject.SetActive(true);

        return _drawAnimation.Play();
    }

    public void Reset()
    {
        _lineTransform.gameObject.SetActive(false);
        _drawAnimation.ResetFill();
    }
}
