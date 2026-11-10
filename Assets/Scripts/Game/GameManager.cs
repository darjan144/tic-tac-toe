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
    [SerializeField] MarkerPool _markerPool;

    [Header("Ads")]
    [Tooltip("Free undos per game. After these are used, an extra undo costs one rewarded audio ad.")]
    [SerializeField, Min(0)] int _freeUndosPerGame = 1;

    int _undosRemaining;

    /// <summary>True when an undo is possible but the free undos are spent (a rewarded ad unlocks one).</summary>
    public bool UndoRequiresAd => _gameActive && _commandManager.CanUndo && _undosRemaining <= 0;

    BoardState _board;
    ThemeManager.ThemeData _theme;
    Player _currentPlayer;
    int _p1Moves;
    int _p2Moves;
    float _gameTimer;
    bool _gameActive;
    CommandManager _commandManager;

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
        _commandManager = new CommandManager();
        _undosRemaining = _freeUndosPerGame;

        if (_markerPool != null) _markerPool.ReturnAll();

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
        _hud.UpdateUndo(false);
    }

    void Update()
    {
        if (!_gameActive) return;
        if (SoundAds.SoundAdsSdk.IsShowingAd) return; // ad time doesn't count towards game duration

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

        var cmd = new PlaceMarkCommand(index, state, sprite, _board, _cells[index], _markerPool);
        _commandManager.ExecuteCommand(cmd);

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
        _hud.UpdateUndo(UndoRequiresAd);
    }

    /// <summary>Reward callback: one more undo for this game.</summary>
    public void GrantExtraUndo()
    {
        _undosRemaining++;
        Debug.Log($"[Ads] Extra undo granted ({_undosRemaining} available)");
    }

    public void OnUndo()
    {
        if (!_gameActive || !_commandManager.CanUndo) return;
        if (_undosRemaining <= 0) return;

        _undosRemaining--;
        _commandManager.Undo();

        _currentPlayer = _currentPlayer == Player.P1 ? Player.P2 : Player.P1;

        if (_currentPlayer == Player.P1) _p1Moves--;
        else _p2Moves--;

        _hud.UpdateMoves(_p1Moves, _p2Moves);
        _hud.UpdateTurn(_currentPlayer);
        _hud.UpdateUndo(UndoRequiresAd);
    }

    public void OnRedo()
    {
        if (!_gameActive || !_commandManager.CanRedo) return;

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

        _commandManager.Redo();

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
        _hud.UpdateUndo(UndoRequiresAd);
    }

    void EndGame(Player winner, int winLineIndex)
    {
        _gameActive = false;
        GameFlowManager.Instance?.SetGameOver();

        SaveManager.Instance?.RecordGameResult(winner, _gameTimer);

        // Pre-buffer the between-games ad while the strike animation / result popup plays,
        // tagged with the moment so advertisers can target wins vs draws.
        GameAds.Preload(AdPlacements.BetweenGames, SoundAds.AdFormat.Standard, GameAds.MomentFor(winner));

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
