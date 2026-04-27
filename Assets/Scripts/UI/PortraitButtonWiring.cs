using UnityEngine;
using UnityEngine.UI;

public class PortraitButtonWiring : MonoBehaviour
{
    [SerializeField] Button[] portraitButtons;
    [SerializeField] Cell[] landscapeCells;

    void Start()
    {
        for (int i = 0; i < portraitButtons.Length; i++)
        {
            int index = i;
            portraitButtons[i].onClick.AddListener(() => landscapeCells[index].Button.onClick.Invoke());
        }
    }
}
