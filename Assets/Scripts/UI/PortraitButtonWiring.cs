using UnityEngine;
using UnityEngine.UI;

public class PortraitButtonWiring : MonoBehaviour
{
    [SerializeField] Button[] _portraitButtons;
    [SerializeField] Cell[] _landscapeCells;

    void Start()
    {
        int count = Mathf.Min(_portraitButtons.Length, _landscapeCells.Length);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            _portraitButtons[i].onClick.AddListener(() => _landscapeCells[index].Button.onClick.Invoke());
        }
    }
}
