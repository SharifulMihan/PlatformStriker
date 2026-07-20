using UnityEngine;

/// <summary>
/// Smooth platformer camera. Attach to Main Camera and assign `target` (the Player).
///
/// Follows using SmoothDamp with independent X/Y smoothing (X tracks tighter than Y by
/// default, since snapping the camera up/down on every small hop/land feels jarring).
/// Optionally leans the camera ahead in the direction the player is facing, and can be
/// clamped to level bounds so it never shows past the edge of the map.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // usually the Player
    public PlayerController player;          // optional, only needed for look-ahead (reads FacingRight)
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Smoothing")]
    [Tooltip("Lower = snappier, higher = laggier/smoother. Horizontal is usually tighter than vertical.")]
    public float smoothTimeX = 0.12f;
    public float smoothTimeY = 0.22f;

    [Header("Look Ahead")]
    public bool useLookAhead = true;
    [Tooltip("How far the camera leans toward the direction the player is facing.")]
    public float lookAheadDistance = 2f;
    public float lookAheadSmoothTime = 0.35f;

    [Header("Bounds (optional)")]
    [Tooltip("Clamp the camera so it never shows past the edges of the level.")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private float velocityX;
    private float velocityY;
    private float lookAheadVelocity;
    private float currentLookAheadX;
    private float startZ;

    void Awake()
    {
        startZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Look-ahead ---
        float targetLookAheadX = 0f;
        if (useLookAhead && player != null)
        {
            targetLookAheadX = player.FacingRight ? lookAheadDistance : -lookAheadDistance;
        }
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref lookAheadVelocity, lookAheadSmoothTime);

        // --- Desired position ---
        Vector2 desired = (Vector2)target.position + offset;
        desired.x += currentLookAheadX;

        // --- Smooth follow, X and Y independently ---
        float newX = Mathf.SmoothDamp(transform.position.x, desired.x, ref velocityX, smoothTimeX);
        float newY = Mathf.SmoothDamp(transform.position.y, desired.y, ref velocityY, smoothTimeY);

        // --- Optional level bounds clamp ---
        if (useBounds)
        {
            newX = Mathf.Clamp(newX, minBounds.x, maxBounds.x);
            newY = Mathf.Clamp(newY, minBounds.y, maxBounds.y);
        }

        transform.position = new Vector3(newX, newY, startZ);
    }

    // Visualizes the bounds box in the Scene view so it's easy to line up with your level edges.
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.magenta;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}