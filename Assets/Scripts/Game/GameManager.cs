using UnityEngine;
using DG.Tweening;
using static Cell;

public class GameManager : MonoBehaviour
{
    [SerializeField] Cell[] _cells;
    [SerializeField] GameHUD _hud;
    [SerializeField] GameResultPopup _resultPopup;
    [SerializeField] StrikeAnimation _strikeAnimation;

    static readonly int[,] WinLines =
    {
        { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 },
        { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 },
        { 0, 4, 8 }, { 2, 4, 6 }
    };

    CellState[] _board;
    ThemeManager.ThemeData _theme;
    int _currentPlayer;
    int _p1Moves;
    int _p2Moves;
    float _gameTimer;
    bool _gameActive;

    void Start()
    {
        _theme = ThemeManager.Instance != null ? ThemeManager.Instance.SelectedTheme : null;

        _board = new CellState[9];
        _currentPlayer = 1;
        _p1Moves = 0;
        _p2Moves = 0;
        _gameTimer = 0f;
        _gameActive = true;

        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Clear();

            int index = i;
            _cells[i].Button.onClick.AddListener(() => OnCellClicked(index));
        }

        _strikeAnimation.Reset();
        _hud.UpdateMoves(0, 0);
        _hud.UpdateTimer(0f);
        _hud.UpdateTurn(_currentPlayer);
    }

    void Update()
    {
        if (!_gameActive) return;

        _gameTimer += Time.deltaTime;
        _hud.UpdateTimer(_gameTimer);
    }

    void OnCellClicked(int index)
    {
        if (!_gameActive) return;
        if (_cells[index].State != CellState.Empty) return;

        CellState state;
        Sprite sprite;

        if (_currentPlayer == 1)
        {
            state = CellState.X;
            sprite = _theme != null ? _theme.XSprite : null;
            _p1Moves++;
        }
        else
        {
            state = CellState.O;
            sprite = _theme != null ? _theme.OSprite : null;
            _p2Moves++;
        }

        _board[index] = state;
        _cells[index].SetMark(state, sprite);
        _hud.UpdateMoves(_p1Moves, _p2Moves);

        int winLine = CheckWin();
        if (winLine >= 0)
        {
            EndGame(_currentPlayer, winLine);
            return;
        }

        if (CheckDraw())
        {
            EndGame(0, -1);
            return;
        }

        _currentPlayer = _currentPlayer == 1 ? 2 : 1;
        _hud.UpdateTurn(_currentPlayer);
    }

    int CheckWin()
    {
        for (int i = 0; i < WinLines.GetLength(0); i++)
        {
            int a = WinLines[i, 0];
            int b = WinLines[i, 1];
            int c = WinLines[i, 2];

            if (_board[a] != CellState.Empty && _board[a] == _board[b] && _board[b] == _board[c])
                return i;
        }

        return -1;
    }

    bool CheckDraw()
    {
        for (int i = 0; i < _board.Length; i++)
        {
            if (_board[i] == CellState.Empty)
                return false;
        }

        return true;
    }

    void EndGame(int winner, int winLineIndex)
    {
        _gameActive = false;

        if (SaveManager.Instance != null)
            SaveManager.Instance.RecordGameResult(winner, _gameTimer);

        string result;
        if (winner == 1)
            result = "PLAYER 1 (X) WINS!";
        else if (winner == 2)
            result = "PLAYER 2 (O) WINS!";
        else
            result = "DRAW!";

        if (winner != 0 && winLineIndex >= 0)
        {
            int middleIdx = WinLines[winLineIndex, 1];

            _strikeAnimation.Play(_cells[middleIdx].RectTransform, winLineIndex)
                .OnComplete(() => _resultPopup.Show(result, _gameTimer));
        }
        else
        {
            _resultPopup.Show(result, _gameTimer);
        }
    }
}
