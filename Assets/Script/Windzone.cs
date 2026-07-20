using UnityEngine;

/// <summary>
/// A rectangular wind volume. Any Rigidbody2D with a matching tag gets pushed along
/// windDirection while inside. Built around AddForce (continuous, mass-aware) rather
/// than overwriting velocity, so it blends with gravity/throw arcs instead of overriding
/// them — that's what keeps it feeling smooth rather than like a sudden snap.
///
/// Range = resize the BoxCollider2D like any other collider (drag it in the Scene view).
/// Direction = windDirection field, any angle, not just straight up.
///
/// Visual: assign a ParticleSystem (set up as stretched-billboard streaks, shape matching
/// this zone's box, aligned to windDirection) to windVisualEffect. Its emission speed is
/// kept in sync with windStrength, and toggling windEnabled starts/stops it, so the visual
/// always matches what the physics is actually doing.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class WindZone : MonoBehaviour
{
    [Header("Wind Direction")]
    [Tooltip("Direction wind blows. Gets normalized automatically. (0,1) = straight up.")]
    public Vector2 windDirection = Vector2.up;

    [Header("Wind Strength")]
    [Tooltip("Force applied per second along windDirection.")]
    public float windStrength = 10f;
    [Tooltip("Caps speed along the wind axis so a tall zone can't accelerate the ball forever.")]
    public float maxWindSpeed = 15f;

    [Header("Edge Falloff")]
    [Tooltip("Fades the force in/out near the entry and exit edges instead of an instant hit.")]
    public bool useEdgeFalloff = true;
    [Range(0.01f, 1f)]
    [Tooltip("Fraction of the zone's length (along wind direction) used for the fade in/out.")]
    public float falloffPortion = 0.25f;

    [Header("Turbulence (optional)")]
    [Tooltip("Gentle side-to-side variation so the push doesn't feel like a flat conveyor belt.")]
    public bool useTurbulence = true;
    public float turbulenceStrength = 1.5f;
    public float turbulenceFrequency = 0.5f;

    [Header("What It Affects")]
    public string affectedTag = "Ball";

    [Header("Obstruction")]
    [Tooltip("Tag of objects that physically block wind (e.g. your platform pieces). If one " +
             "of these sits between the zone's entry edge and the ball, the ball gets no wind " +
             "force that frame — as if the object is standing in the ball's 'shadow'. Leave " +
             "empty to disable blocking entirely.")]
    public string obstructionTag = "Platform";

    [Header("Enable / Disable")]
    public bool windEnabled = true;
    [Tooltip("Optional particle system that plays/stops in sync with windEnabled (e.g. rising wind streaks).")]
    public ParticleSystem windVisualEffect;

    [Header("Visual Tuning")]
    [Tooltip("How strongly windStrength affects the particle system's start speed. " +
             "0 = visual speed never changes with windStrength. Typical: 0.08-0.15.")]
    public float visualSpeedPerWindStrength = 0.1f;
    [Tooltip("Clamp on the multiplier above so a huge windStrength doesn't send particles flying " +
             "absurdly fast off-screen.")]
    public float maxVisualSpeedMultiplier = 3f;

    private BoxCollider2D zoneCollider;
    private float noiseSeed;
    private readonly RaycastHit2D[] obstructionHitBuffer = new RaycastHit2D[8];

    void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
        noiseSeed = Random.value * 1000f;
    }

    void Start()
    {
        ApplyVisualState();
        ApplyVisualSpeed();
    }

    void OnValidate()
    {
        if (windDirection.sqrMagnitude < 0.0001f) windDirection = Vector2.up;

        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;

        // Lets you scrub windStrength in the Inspector (even outside Play mode) and see
        // the particle system's speed react immediately, instead of only updating on Start.
        ApplyVisualSpeed();
    }

    /// <summary>Lets a lever/switch/timeline turn this zone on or off at runtime.</summary>
    public void SetWindEnabled(bool value)
    {
        windEnabled = value;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (windVisualEffect == null) return;

        if (windEnabled)
        {
            windVisualEffect.Play();
        }
        else
        {
            // StopEmitting (not StopEmittingAndClear) lets particles already in flight
            // finish their lifetime naturally instead of vanishing instantly — much less
            // jarring when a switch/lever turns the wind off mid-gust.
            windVisualEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// Keeps the particle system's start speed roughly proportional to windStrength, so a
    /// gentle breeze zone looks gentle and a strong gust zone looks strong, without having
    /// to hand-tune every wind zone's particle system separately.
    /// </summary>
    private void ApplyVisualSpeed()
    {
        if (windVisualEffect == null) return;

        var main = windVisualEffect.main;
        float multiplier = 1f + windStrength * visualSpeedPerWindStrength;
        multiplier = Mathf.Clamp(multiplier, 0.1f, maxVisualSpeedMultiplier);
        main.startSpeedMultiplier = multiplier;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!windEnabled) return;
        if (!other.CompareTag(affectedTag)) return;

        Rigidbody2D rb = other.attachedRigidbody;
        // Skip kinematic bodies (e.g. the ball while held during aim) — wind should only
        // ever affect the ball once it's actually in flight and obeying physics.
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        Vector2 dir = windDirection.normalized;
        Vector2 pos = other.transform.position;

        GetAxisProjection(pos, dir, out float t, out float distanceFromEntry);

        // If something solid sits between the entry edge and the ball, the ball is in
        // the obstacle's "wind shadow" — no force this frame. Re-checked every physics
        // step, so as soon as the obstacle moves/deactivates/toggles off, wind resumes
        // instantly with no extra bookkeeping needed.
        if (IsPathBlocked(pos, -dir, distanceFromEntry))
        {
            return;
        }

        float strength = windStrength;

        if (useEdgeFalloff)
        {
            strength *= GetEdgeFalloffFromT(t);
        }

        Vector2 force = dir * strength;

        if (useTurbulence)
        {
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * turbulenceFrequency) * 2f - 1f;
            force += perpendicular * noise * turbulenceStrength;
        }

        rb.AddForce(force, ForceMode2D.Force);

        // Clamp speed along the wind axis only — perpendicular/gravity motion is untouched.
        float speedAlongWind = Vector2.Dot(rb.linearVelocity, dir);
        if (speedAlongWind > maxWindSpeed)
        {
            Vector2 excess = dir * (speedAlongWind - maxWindSpeed);
            rb.linearVelocity -= excess;
        }
    }

    /// <summary>
    /// Projects worldPos onto the wind axis relative to the zone's bounds. Works at any wind
    /// angle, not just vertical, by projecting all 4 box corners onto the axis first.
    /// t: 0 at the entry edge, 1 at the exit edge.
    /// distanceFromEntry: world-unit distance from worldPos back to the entry edge — used as
    /// the raycast length for the obstruction check.
    /// </summary>
    private void GetAxisProjection(Vector2 worldPos, Vector2 dir, out float t, out float distanceFromEntry)
    {
        Bounds b = zoneCollider.bounds;
        Vector2 center = b.center;

        Vector2[] corners =
        {
            new Vector2(b.min.x, b.min.y),
            new Vector2(b.min.x, b.max.y),
            new Vector2(b.max.x, b.min.y),
            new Vector2(b.max.x, b.max.y)
        };

        float minProj = float.MaxValue, maxProj = float.MinValue;
        foreach (var c in corners)
        {
            float p = Vector2.Dot(c - center, dir);
            if (p < minProj) minProj = p;
            if (p > maxProj) maxProj = p;
        }

        float projected = Vector2.Dot(worldPos - center, dir);
        t = Mathf.InverseLerp(minProj, maxProj, projected);
        distanceFromEntry = Mathf.Max(0f, projected - minProj);
    }

    /// <summary>
    /// 0 at the entry edge, ramps to 1 across falloffPortion of the zone, stays at 1 through
    /// the middle, then ramps back to 0 across the last falloffPortion at the exit edge.
    /// </summary>
    private float GetEdgeFalloffFromT(float t)
    {
        float fadeZone = Mathf.Clamp01(falloffPortion);
        float fadeIn = Mathf.InverseLerp(0f, fadeZone, t);
        float fadeOut = Mathf.InverseLerp(1f, 1f - fadeZone, t);
        return Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
    }

    /// <summary>
    /// Casts a ray from the ball back toward the zone's entry edge (opposite the wind
    /// direction). If any collider tagged obstructionTag sits along that ray before it
    /// reaches the edge, the wind's path to the ball is physically blocked.
    /// </summary>
    private bool IsPathBlocked(Vector2 fromPos, Vector2 towardEntryDir, float distanceToEntry)
    {
        if (string.IsNullOrEmpty(obstructionTag) || distanceToEntry <= 0f) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.NoFilter(); // check every layer — we filter by tag below instead

        int hitCount = Physics2D.Raycast(fromPos, towardEntryDir, filter, obstructionHitBuffer, distanceToEntry);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = obstructionHitBuffer[i].collider;
            if (hitCollider != null && hitCollider.CompareTag(obstructionTag))
            {
                return true;
            }
        }
        return false;
    }

    // Draws the zone bounds plus a small grid of arrows showing wind direction,
    // so you can see range + direction at a glance without entering Play mode.
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Bounds b = col.bounds;

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.18f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(b.center, b.size);

        Vector2 dir = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector2.up;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        Gizmos.color = Color.cyan;

        int cols = 3, rows = 3;
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float fx = (x + 0.5f) / cols;
                float fy = (y + 0.5f) / rows;
                Vector3 pos = new Vector3(
                    Mathf.Lerp(b.min.x, b.max.x, fx),
                    Mathf.Lerp(b.min.y, b.max.y, fy),
                    0f);
                Vector3 tip = pos + (Vector3)(dir * 0.4f);
                Gizmos.DrawLine(pos, tip);
                Gizmos.DrawLine(tip, tip - (Vector3)(dir * 0.15f) + (Vector3)(perp * 0.1f));
                Gizmos.DrawLine(tip, tip - (Vector3)(dir * 0.15f) - (Vector3)(perp * 0.1f));
            }
        }
    }
}