using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Owns the two platform groups (WhitePlatform / BlackPlatform) and flips which one is
/// solid whenever Toggle() is called. Auto-collects every Platform component under each
/// group in Awake, so you never have to manually wire up individual platforms — just add
/// a Platform.cs to each piece and drop it under the correct parent in the Hierarchy.
///
/// Singleton so Switch.cs (and anything else) can call PlatformManager.Instance.Toggle()
/// without needing a scene reference wired up in the Inspector.
/// </summary>
public class PlatformManager : MonoBehaviour
{
    public static PlatformManager Instance { get; private set; }

    [Header("Platform Groups")]
    [Tooltip("Parent transform containing all white platforms as children (e.g. WhitePlatform).")]
    public Transform whiteGroup;
    [Tooltip("Parent transform containing all black platforms as children (e.g. BlackPlatform).")]
    public Transform blackGroup;

    [Header("Starting State")]
    public bool whiteStartsActive = true;

    [Header("Visuals")]
    [Range(0f, 1f)]
    [Tooltip("Opacity of a platform while it's inactive/non-solid.")]
    public float inactiveAlpha = 0.25f;
    [Tooltip("How long the fade between active/inactive takes.")]
    public float fadeDuration = 0.25f;

    [Header("Events")]
    [Tooltip("Fires every time the groups swap. Hook up sound/screenshake/particles here.")]
    public UnityEvent onToggled;

    private Platform[] whitePlatforms;
    private Platform[] blackPlatforms;
    private bool whiteActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlatformManager instances found — destroying the extra one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        whitePlatforms = whiteGroup != null ? whiteGroup.GetComponentsInChildren<Platform>(true) : new Platform[0];
        blackPlatforms = blackGroup != null ? blackGroup.GetComponentsInChildren<Platform>(true) : new Platform[0];

        whiteActive = whiteStartsActive;
    }

    void Start()
    {
        // Apply instantly on scene start — no fade-in on load.
        ApplyState(instant: true);
    }

    /// <summary>
    /// Swaps which group is currently solid. Called by Switch when the ball hits it.
    /// </summary>
    public void Toggle()
    {
        whiteActive = !whiteActive;
        ApplyState(instant: false);
        onToggled?.Invoke();
    }

    private void ApplyState(bool instant)
    {
        foreach (var p in whitePlatforms)
        {
            if (p != null) p.SetState(whiteActive, inactiveAlpha, fadeDuration, instant);
        }
        foreach (var p in blackPlatforms)
        {
            if (p != null) p.SetState(!whiteActive, inactiveAlpha, fadeDuration, instant);
        }
    }
}