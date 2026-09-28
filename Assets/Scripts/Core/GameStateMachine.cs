using UnityEngine;

public enum GameState { None, MainMenu, Playing, GameOver }

public class GameStateMachine
{
    public GameState CurrentState { get; private set; } = GameState.None;

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        Debug.Log($"[StateMachine] {CurrentState} → {newState}");
        CurrentState = newState;
    }
}
