using UnityEngine;
using UnityEngine.SceneManagement;

// This platform can be a plain static collider (no Rigidbody2D needed) since
// nothing is meant to rest or ride on it smoothly — it's a hazard, not
// something to be carried across. We only care about detecting the moment
// a matching object (ball, player, etc.) touches it.
public class BallResetPlatform : MonoBehaviour
{
    [Header("Detection Settings")]
    // Any GameObject whose tag appears in this list triggers a restart.
    // Set this in the Inspector, e.g. ["Ball", "Player"], so the same
    // script works for hazards that should kill the player, the ball,
    // or both — no code changes needed per platform.
    [SerializeField] private string[] detectTags = { "Ball" };

    [Header("Restart Settings")]
    [SerializeField] private float restartDelay = 0f; // Seconds before reload, 0 = instant

    private bool triggered; // Guards against restarting multiple times in one frame/step

    // Uses OnCollisionEnter2D (not a trigger) because Ball.cs sets its
    // Rigidbody2D to CollisionDetectionMode2D.Continuous specifically so
    // fast-falling balls register a clean collision instead of tunnelling
    // through — a Collider2D on this platform should be a normal (non-trigger)
    // collider so that continuous sweep actually finds it.
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandle(collision.gameObject);
    }

    // Some level layouts may prefer a trigger volume (e.g. a kill-zone that's
    // slightly larger than its visible platform sprite) instead of a solid
    // collision. Handling both means this script works either way without
    // modification — just toggle "Is Trigger" on the Collider2D.
    void OnTriggerEnter2D(Collider2D other)
    {
        TryHandle(other.gameObject);
    }

    private void TryHandle(GameObject obj)
    {
        if (triggered) return;

        // Tag-based check so this one script can watch for the ball, the
        // player, or both — just list whichever tags should trigger a
        // restart in the Inspector. Requires the relevant GameObjects to
        // actually have those tags assigned in Unity.
        if (!HasMatchingTag(obj)) return;

        // If the object is a ball, respect its held state: a held ball is
        // kinematic and being carried by the player during aiming (see
        // Ball.SetHeld), so it shouldn't trip a restart just by being swung
        // near the platform before it's even thrown. Objects without a Ball
        // component (e.g. the player) skip this check entirely.
        Ball ball = obj.GetComponent<Ball>();
        if (ball != null && ball.IsHeld) return;

        triggered = true;

        if (restartDelay <= 0f)
        {
            RestartLevel();
        }
        else
        {
            Invoke(nameof(RestartLevel), restartDelay);
        }
    }

    private bool HasMatchingTag(GameObject obj)
    {
        foreach (string tag in detectTags)
        {
            if (!string.IsNullOrEmpty(tag) && obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    private void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}