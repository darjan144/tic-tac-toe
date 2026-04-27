using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : SingletonMonoBehaviour<GameFlowManager>
{
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("Game");
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
