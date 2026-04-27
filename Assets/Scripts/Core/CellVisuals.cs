using UnityEngine.UI;
using static Cell;

public static class CellVisuals
{
    public static void ApplyFill(Image image, CellState state)
    {
        if (state == CellState.X)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial180;
            image.fillOrigin = (int)Image.Origin180.Top;
        }
        else if (state == CellState.O)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Bottom;
        }
    }
}
