using UnityEngine;
using UnityEngine.UI;

public class PooledMarker : MonoBehaviour
{
    [SerializeField] Image _image;
    [SerializeField] DrawAnimation _drawAnimation;

    public Image Image => _image;
    public DrawAnimation DrawAnimation => _drawAnimation;
}
