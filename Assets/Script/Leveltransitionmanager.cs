using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent (DontDestroyOnLoad) singleton that owns the level-complete /
/// level-intro UI and drives the whole transition:
///   1) "Level 1 Complete" panel (an Image showing that level's sprite) slides in
///      from the left, holds.
///   2) That panel slides out to the right while a "Level 2" panel slides in
///      from the left AT THE SAME TIME — a wipe — while the next scene loads
///      in the background.
///   3) "Level 2" panel holds, then slides out to the right, revealing gameplay.
///   4) The new scene's player is unlocked.
///
/// Setup: put this on a Canvas GameObject in your first scene (Level 1). Because
/// of DontDestroyOnLoad it survives into Level 2, Level 3, etc., so you only ever
/// need one in the whole game — every Door just calls LevelTransitionManager.Instance.
/// Build the two panels as children of this Canvas, positioned wherever they should
/// rest on screen; Awake() reads that as home base and moves them off-screen.
/// </summary>
public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance { get; private set; }

    /// <summary>True for the whole duration of RunTransition — from the "Level Complete"
    /// slide-in through the wipe to the "Level 2" slide-out. Other systems (e.g. the pause
    /// menu) should check this before acting, so you can't pause mid-wipe and get stuck
    /// looking at a half-slid panel, or restart into a scene load that's already in flight.</summary>
    public bool IsTransitioning { get; private set; }

    [Header("Panels")]
    [Tooltip("Shown first — slides in from the left. Its Image's sprite is swapped per-door.")]
    public RectTransform outgoingPanel;
    public Image outgoingImage;
    [Tooltip("Slides in from the left as the outgoing panel slides out to the right.")]
    public RectTransform incomingPanel;
    public Image incomingImage;

    [Header("Timing")]
    public float slideDuration = 0.6f;
    public float completeHoldDuration = 1.5f;
    public float introHoldDuration = 1.5f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Main Menu")]
    [Tooltip("If a loaded scene's name matches this, this persistent manager destroys itself instead of " +
             "surviving into it — so returning to the main menu doesn't drag the previous level's transition " +
             "Canvas (and its stale panel references) along for the ride. Leave empty to never self-destruct.")]
    public string mainMenuSceneName = "";

    private Vector2 outgoingRestPos, outgoingOffLeft, outgoingOffRight;
    private Vector2 incomingRestPos, incomingOffLeft, incomingOffRight;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CachePositions(outgoingPanel, out outgoingRestPos, out outgoingOffLeft, out outgoingOffRight);
        CachePositions(incomingPanel, out incomingRestPos, out incomingOffLeft, out incomingOffRight);

        outgoingPanel.anchoredPosition = outgoingOffLeft;
        incomingPanel.anchoredPosition = incomingOffLeft;
        outgoingPanel.gameObject.SetActive(false);
        incomingPanel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Fires on every scene load, including the ones this manager itself triggers in
    // RunTransition. If the newly loaded scene IS the main menu, this persistent
    // instance has done its job for this playthrough of the game — destroy it so the
    // main menu (and whatever comes after it, e.g. starting a fresh game) doesn't
    // inherit a Canvas full of panel references pointing at the level that just ended.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(mainMenuSceneName) || scene.name != mainMenuSceneName) return;

        Instance = null;
        Destroy(gameObject);
    }

    private void CachePositions(RectTransform rect, out Vector2 rest, out Vector2 offLeft, out Vector2 offRight)
    {
        rest = rect.anchoredPosition;
        float travel = rect.rect.width + Mathf.Abs(rest.x) + 50f;
        offLeft = rest + Vector2.left * travel;
        offRight = rest + Vector2.right * travel;
    }

    /// <summary>Call from Door.cs once the level is complete. completeSprite is this level's
    /// "Level N Complete" image, nextLevelSprite is the next level's intro image — both
    /// supplied per-door so each scene shows its own artwork instead of a shared default.</summary>
    public void CompleteLevel(Sprite completeSprite, string nextSceneName, Sprite nextLevelSprite)
    {
        StartCoroutine(RunTransition(completeSprite, nextSceneName, nextLevelSprite));
    }

    private IEnumerator RunTransition(Sprite completeSprite, string nextSceneName, Sprite nextLevelSprite)
    {
        IsTransitioning = true;

        // 1) "Level 1 Complete" slides in from the left.
        SetSprite(outgoingImage, completeSprite);
        outgoingPanel.gameObject.SetActive(true);
        outgoingPanel.anchoredPosition = outgoingOffLeft;
        yield return Slide(outgoingPanel, outgoingOffLeft, outgoingRestPos);
        yield return new WaitForSecondsRealtime(completeHoldDuration);

        // 2) Start loading the next scene in the background now, so it's ready by
        //    the time the wipe reaches it — no hitch, no frozen frame mid-transition.
        AsyncOperation load = SceneManager.LoadSceneAsync(nextSceneName);
        load.allowSceneActivation = false;

        // 3) Wipe: outgoing slides out right, incoming ("Level 2") slides in from
        //    the left, both moving in the same coroutine tick so they're in sync.
        SetSprite(incomingImage, nextLevelSprite);
        incomingPanel.gameObject.SetActive(true);
        incomingPanel.anchoredPosition = incomingOffLeft;
        yield return SlideBoth(
            outgoingPanel, outgoingRestPos, outgoingOffRight,
            incomingPanel, incomingOffLeft, incomingRestPos);
        outgoingPanel.gameObject.SetActive(false);

        // The new scene is fully loaded by now (or close to it) — let it activate
        // while it's still hidden behind the incoming panel, so the swap is invisible.
        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;

        yield return new WaitForSecondsRealtime(introHoldDuration);

        // 4) "Level 2" slides out to the right, revealing gameplay.
        yield return Slide(incomingPanel, incomingRestPos, incomingOffRight);
        incomingPanel.gameObject.SetActive(false);

        // Hand control back to whatever PlayerController exists in the freshly loaded scene.
        PlayerController newPlayer = FindObjectOfType<PlayerController>();
        if (newPlayer != null) newPlayer.movementLocked = false;

        IsTransitioning = false;
    }

    // A null sprite means the Door's inspector field was never assigned for this scene —
    // warn instead of silently leaving whatever image was showing before.
    private void SetSprite(Image image, Sprite sprite)
    {
        if (image == null) return;

        if (sprite == null)
            Debug.LogWarning($"[LevelTransitionManager] No sprite assigned — check the Door's " +
                             $"Complete Sprite / Next Level Sprite fields in scene '{SceneManager.GetActiveScene().name}'.", this);

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private IEnumerator Slide(RectTransform rect, Vector2 from, Vector2 to)
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float e = ease.Evaluate(Mathf.Clamp01(t / slideDuration));
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    private IEnumerator SlideBoth(RectTransform rectA, Vector2 fromA, Vector2 toA,
                                   RectTransform rectB, Vector2 fromB, Vector2 toB)
    {
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float e = ease.Evaluate(Mathf.Clamp01(t / slideDuration));
            rectA.anchoredPosition = Vector2.LerpUnclamped(fromA, toA, e);
            rectB.anchoredPosition = Vector2.LerpUnclamped(fromB, toB, e);
            yield return null;
        }
        rectA.anchoredPosition = toA;
        rectB.anchoredPosition = toB;
    }
}