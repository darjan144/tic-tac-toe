using static Cell;

public enum Player { None, P1, P2 }

public class BoardState
{
    public static readonly int[,] WinLines =
    {
        { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 },
        { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 },
        { 0, 4, 8 }, { 2, 4, 6 }
    };

    readonly CellState[] _board = new CellState[9];

    public void Place(int index, CellState state)
    {
        _board[index] = state;
    }

    public bool IsEmpty(int index) => _board[index] == CellState.Empty;

    public int CheckWin()
    {
        for (int i = 0; i < WinLines.GetLength(0); i++)
        {
            int a = WinLines[i, 0], b = WinLines[i, 1], c = WinLines[i, 2];
            if (_board[a] != CellState.Empty && _board[a] == _board[b] && _board[b] == _board[c])
                return i;
        }

        return -1;
    }

    public bool CheckDraw()
    {
        for (int i = 0; i < _board.Length; i++)
        {
            if (_board[i] == CellState.Empty)
                return false;
        }

        return true;
    }

    public void Reset()
    {
        for (int i = 0; i < _board.Length; i++)
            _board[i] = CellState.Empty;
    }
}
