using UnityEngine;
using static Cell;

public class PlaceMarkCommand : ICommand
{
    readonly int _index;
    readonly CellState _state;
    readonly Sprite _sprite;
    readonly BoardState _board;
    readonly Cell _cell;
    readonly MarkerPool _pool;

    PooledMarker _marker;

    public PlaceMarkCommand(int index, CellState state, Sprite sprite,
        BoardState board, Cell cell, MarkerPool pool)
    {
        _index = index;
        _state = state;
        _sprite = sprite;
        _board = board;
        _cell = cell;
        _pool = pool;
    }

    public void Execute()
    {
        _board.Place(_index, _state);

        _marker = _pool.Get(_cell.transform);
        if (_marker != null)
        {
            _marker.Image.sprite = _sprite;
            _marker.Image.enabled = true;
            CellVisuals.ApplyFill(_marker.Image, _state);
            _marker.DrawAnimation.Play();
        }

        _cell.SetMark(_state, _sprite);
        Debug.Log($"[Command] Execute: Place {_state} at cell {_index}");
    }

    public void Undo()
    {
        _board.ClearCell(_index);
        _pool.Return(_marker);
        _marker = null;
        _cell.Clear();
        Debug.Log($"[Command] Undo: Remove {_state} from cell {_index}");
    }
}
