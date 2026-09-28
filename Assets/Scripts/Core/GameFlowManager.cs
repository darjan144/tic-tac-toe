using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : SingletonMonoBehaviour<GameFlowManager>
{
    readonly GameStateMachine _stateMachine = new();

    public GameState CurrentState => _stateMachine.CurrentState;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        _stateMachine.ChangeState(GameState.MainMenu);
    }

    public void LoadMainMenu()
    {
        _stateMachine.ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGame()
    {
        _stateMachine.ChangeState(GameState.Playing);
        SceneManager.LoadScene("Game");
    }

    public void SetGameOver()
    {
        _stateMachine.ChangeState(GameState.GameOver);
    }

    public void ResumePlay()
    {
        _stateMachine.ChangeState(GameState.Playing);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Application.ExternalEval("location.reload();");
#else
        Application.Quit();
#endif
    }
}
