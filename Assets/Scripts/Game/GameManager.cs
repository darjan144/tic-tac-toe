using UnityEngine;
using DG.Tweening;
using static Cell;

public class GameManager : MonoBehaviour
{
    [SerializeField] Cell[] _cells;
    [SerializeField] CellMirror[] _cellMirrors;
    [SerializeField] GameHUD _hud;
    [SerializeField] GameResultPopup _resultPopup;
    [SerializeField] StrikeAnimation _landscapeStrike;
    [SerializeField] StrikeAnimation _portraitStrike;

    BoardState _board;
    ThemeManager.ThemeData _theme;
    Player _currentPlayer;
    int _p1Moves;
    int _p2Moves;
    float _gameTimer;
    bool _gameActive;

    StrikeAnimation ActiveStrike =>
        OrientationManager.CurrentOrientation == OrientationManager.Orientation.Portrait
            ? _portraitStrike : _landscapeStrike;

    RectTransform ActiveCellRect(int index) =>
        OrientationManager.CurrentOrientation == OrientationManager.Orientation.Portrait
            ? _cellMirrors[index].RectTransform : _cells[index].RectTransform;

    void Start()
    {
        _theme = ThemeManager.Instance?.SelectedTheme;

        _board = new BoardState();
        _currentPlayer = Player.P1;
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

        _landscapeStrike.Reset();
        _portraitStrike.Reset();
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
        if (!_board.IsEmpty(index)) return;

        CellState state;
        Sprite sprite;

        if (_currentPlayer == Player.P1)
        {
            state = CellState.X;
            sprite = _theme?.XSprite;
            _p1Moves++;
        }
        else
        {
            state = CellState.O;
            sprite = _theme?.OSprite;
            _p2Moves++;
        }

        _board.Place(index, state);
        _cells[index].SetMark(state, sprite);
        AudioManager.Instance?.PlayPopSFX();
        _hud.UpdateMoves(_p1Moves, _p2Moves);

        int winLine = _board.CheckWin();
        if (winLine >= 0)
        {
            EndGame(_currentPlayer, winLine);
            return;
        }

        if (_board.CheckDraw())
        {
            EndGame(Player.None, -1);
            return;
        }

        _currentPlayer = _currentPlayer == Player.P1 ? Player.P2 : Player.P1;
        _hud.UpdateTurn(_currentPlayer);
    }

    void EndGame(Player winner, int winLineIndex)
    {
        _gameActive = false;

        SaveManager.Instance?.RecordGameResult(winner, _gameTimer);

        string result;
        if (winner == Player.P1)
            result = "PLAYER 1 (X) WINS!";
        else if (winner == Player.P2)
            result = "PLAYER 2 (O) WINS!";
        else
            result = "DRAW!";

        if (winner != Player.None && winLineIndex >= 0)
        {
            int middleIdx = BoardState.WinLines[winLineIndex, 1];

            AudioManager.Instance?.PlayWooshSFX();
            ActiveStrike.Play(ActiveCellRect(middleIdx), winLineIndex)
                .OnComplete(() => _resultPopup.Show(result, _gameTimer));
        }
        else
        {
            _resultPopup.Show(result, _gameTimer);
        }
    }
}
