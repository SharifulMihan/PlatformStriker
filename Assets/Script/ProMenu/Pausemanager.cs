using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseManager : MonoBehaviour
{
    [Header("Your existing pause panel")]
    [Tooltip("Drag the pause panel you already built here. Should be inactive by default in the scene.")]
    public GameObject pausePanel;

    [Header("Controls")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Scenes")]
    [Tooltip("Exact name of the main menu scene, as it appears in Build Settings. Leave blank if you don't have a Main Menu button.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused;

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        // Un-pause BEFORE reloading — otherwise the reloaded scene starts
        // with timeScale still stuck at 0 and everything looks frozen.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}