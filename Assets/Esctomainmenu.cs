using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drop this on any GameObject in a level scene. Pressing Escape immediately loads
/// the main menu scene. No Canvas, no buttons, no EventSystem needed — this only
/// reads keyboard input directly, so nothing about your UI setup can break it.
/// </summary>
public class EscToMainMenu : MonoBehaviour
{
    [Tooltip("Exact name of the main menu scene, as it appears in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    public KeyCode key = KeyCode.Escape;

    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            // Reset in case anything upstream left the game paused/slowed.
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}