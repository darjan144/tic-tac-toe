using System.Collections.Generic;
using UnityEngine;

public class CommandManager
{
    readonly Stack<ICommand> _undoStack = new();
    readonly Stack<ICommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int HistoryCount => _undoStack.Count;

    public void ExecuteCommand(ICommand cmd)
    {
        cmd.Execute();
        _undoStack.Push(cmd);
        _redoStack.Clear();
        Debug.Log($"[Command] Command executed — history size: {_undoStack.Count}");
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
        Debug.Log($"[Command] Undo — remaining: {_undoStack.Count}, redo available: {_redoStack.Count}");
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.Push(cmd);
        Debug.Log($"[Command] Redo — history size: {_undoStack.Count}, redo available: {_redoStack.Count}");
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
