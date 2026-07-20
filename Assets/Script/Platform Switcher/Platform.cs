using System.Collections;
using UnityEngine;

/// <summary>
/// Sits on an individual platform piece (e.g. each "Square" under WhitePlatform/BlackPlatform).
/// Handles its own visual fade and collider toggling so PlatformManager doesn't need to know
/// anything about rendering — it just calls SetState(true/false).
///
/// "Inactive" does NOT mean SetActive(false) on the GameObject. The platform stays visible
/// (faded to inactiveAlpha) with its collider disabled, so players can see the swap coming
/// instead of platforms popping in/out of existence.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Platform : MonoBehaviour
{
    private SpriteRenderer sr;
    private Collider2D[] colliders;
    private Coroutine fadeRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();
    }

    /// <summary>
    /// Enables/disables collision immediately, and fades opacity toward the target
    /// (1 = fully active, inactiveAlpha = ghosted) over `duration` seconds.
    /// </summary>
    public void SetState(bool active, float inactiveAlpha, float duration, bool instant = false)
    {
        // Collision toggles instantly — no "half-solid" window during the fade.
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = active;
        }

        float targetAlpha = active ? 1f : inactiveAlpha;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (instant || duration <= 0f)
        {
            SetAlpha(targetAlpha);
        }
        else
        {
            fadeRoutine = StartCoroutine(FadeTo(targetAlpha, duration));
        }
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        Color c = sr.color;
        float start = c.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, target, t / duration);
            sr.color = c;
            yield return null;
        }

        c.a = target;
        sr.color = c;
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}