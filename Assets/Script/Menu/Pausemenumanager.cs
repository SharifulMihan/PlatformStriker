using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent (DontDestroyOnLoad) singleton that tracks pause state and handles the
/// scene-load edge cases around it. Has NO buttons or panel of its own — wire your
/// own pause panel and buttons to the public methods below (Pause, Resume,
/// TogglePause, RestartLevel, QuitToMainMenu) via their OnClick() events.
///
/// Setup: put this on any GameObject in your first scene (Level 1). Because of
/// DontDestroyOnLoad it survives into Level 2, Level 3, etc., so you only need one
/// instance for the whole game — do NOT add a copy of this to every scene.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Your existing pause panel")]
    [Tooltip("Drag your PauseMenu panel GameObject here. This guarantees it always " +
             "gets hidden on Resume/Restart/Quit, even though this Canvas persists " +
             "across scene reloads via DontDestroyOnLoad.")]
    public GameObject pausePanel;

    [Header("Input")]
    [Tooltip("Lets Escape toggle pause in addition to whatever button calls TogglePause().")]
    public bool allowEscapeKey = true;

    [Header("Scenes")]
    [Tooltip("Optional — sends the player back to a main menu scene via QuitToMainMenu(). " +
             "Also used to auto-destroy this manager once the player reaches the menu.")]
    public string mainMenuSceneName = "";

    public bool IsPaused { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // This fires if a scene has its own copy of this manager AND a persistent
            // instance already survived here from an earlier scene (e.g. via
            // DontDestroyOnLoad from Level 1). Only one instance is meant to exist for
            // the whole game, so this makes the self-destruction visible instead of
            // leaving a silently dead duplicate sitting in the scene.
            Debug.LogWarning($"[PauseMenuManager] Duplicate instance on '{gameObject.name}' in scene " +
                              $"'{gameObject.scene.name}' destroyed — a persistent instance from an earlier " +
                              $"scene already exists. Only one PauseMenuManager should exist across the whole " +
                              $"game; remove this duplicate from the scene instead of leaving it here.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // A pause that was active going into a scene load (e.g. Restart, or a level's own
    // scene change) must never carry over — otherwise the freshly loaded level opens
    // frozen at Time.timeScale == 0 with no obvious way to unpause.
    //
    // Separately: if the scene that just loaded IS the main menu, this persistent
    // manager has done its job for this playthrough — destroy it instead of letting
    // it survive into the menu (and whatever fresh game session starts after it).
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName) && scene.name == mainMenuSceneName)
        {
            Time.timeScale = 1f; // never leave the main menu accidentally frozen
            Instance = null;
            Destroy(gameObject);
            return;
        }

        if (IsPaused) Resume();
    }

    void Update()
    {
        if (allowEscapeKey && Input.GetKeyDown(KeyCode.Escape) && CanTogglePause())
        {
            TogglePause();
        }
    }

    /// <summary>Blocks pausing during a level-complete/level-intro transition (see
    /// LevelTransitionManager.IsTransitioning) — pausing mid-transition would freeze
    /// things halfway through with no clean way to recover.</summary>
    private bool CanTogglePause()
    {
        return LevelTransitionManager.Instance == null || !LevelTransitionManager.Instance.IsTransitioning;
    }

    public void TogglePause()
    {
        if (!CanTogglePause()) return;
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || !CanTogglePause()) return;
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    /// <summary>Wire this to your Restart button. Reloads the current scene — always
    /// guaranteed to leave timeScale back at normal speed first.</summary>
    public void RestartLevel()
    {
        if (!CanTogglePause()) return; // don't restart out from under an in-progress scene load

        Time.timeScale = 1f;
        IsPaused = false;
        // This is the fix: this Canvas survives the reload via DontDestroyOnLoad, so
        // nothing else will ever hide the panel unless we do it here explicitly.
        if (pausePanel != null) pausePanel.SetActive(false);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>Wire this to your Quit/Main Menu button. No-op if mainMenuSceneName is unset.</summary>
    public void QuitToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName)) return;

        Time.timeScale = 1f;
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}