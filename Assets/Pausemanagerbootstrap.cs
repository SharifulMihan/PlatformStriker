using UnityEngine;

/// <summary>
/// Drop this in EVERY level scene (Level 1, Level 2, Level 3...). On load, it checks
/// whether the persistent PauseMenuManager singleton already exists — if you played
/// through from Level 1 normally, it will, and this does nothing. If you jumped
/// straight into this level in the Editor (or any other non-Level-1 entry point),
/// no persistent instance exists yet, so this spawns one from the prefab.
///
/// SETUP (one-time):
///   1. Make sure your Canvas + PauseMenuManager (with the pause panel etc.) is
///      saved as a Prefab ASSET in your Project window (drag it there from the
///      Hierarchy once, if you haven't already).
///   2. Remove that Canvas from being manually placed in Level 2/3/4 — you don't
///      need it there anymore, this bootstrapper replaces that.
///   3. Create an empty GameObject, name it "PauseManagerBootstrap", attach this
///      script, and drag your Canvas PREFAB (from the Project window, not a scene
///      object) into the "Pause Manager Prefab" field.
///   4. Drag THIS GameObject into the Project window too, to make IT a prefab.
///   5. Now drop that same bootstrapper prefab into every level scene, including
///      Level 1. No other setup needed per-level, ever.
/// </summary>
public class PauseManagerBootstrap : MonoBehaviour
{
    [Tooltip("Drag the Canvas/PauseMenuManager PREFAB ASSET here (from the Project window).")]
    public GameObject pauseManagerPrefab;

    void Awake()
    {
        if (PauseMenuManager.Instance == null)
        {
            if (pauseManagerPrefab != null)
            {
                Instantiate(pauseManagerPrefab);
            }
            else
            {
                Debug.LogWarning("[PauseManagerBootstrap] No prefab assigned — nothing to spawn.", this);
            }
        }

        // The bootstrapper's only job was to check and spawn if needed — it doesn't
        // need to stick around itself.
        Destroy(gameObject);
    }
}