using UnityEngine;

/// <summary>
/// Attach to any platform the player is allowed to reposition with the mouse.
/// The presence of this component (plus a Collider2D) is what marks a platform as
/// "movable" — platforms without it can't be picked up, so only the specific ones
/// you add this to can be dragged.
///
/// Click the platform to grab it, drag to move it, release to drop it.
/// If a Kinematic Rigidbody2D is present the platform is moved through physics, so it
/// stays solid and carries/pushes the player correctly. Without one it falls back to
/// moving the Transform directly (simpler, but the player won't be carried).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DraggablePlatform : MonoBehaviour
{
    [Header("Drag Constraints (optional)")]
    [Tooltip("Allow the platform to be dragged left/right.")]
    public bool allowHorizontal = true;
    [Tooltip("Allow the platform to be dragged up/down.")]
    public bool allowVertical = true;
    [Tooltip("Farthest the platform may be moved from where this grab started. 0 = unlimited.")]
    public float maxDragDistance = 0f;

    [Header("Feedback (optional)")]
    [Tooltip("Tint shown while the platform is grabbed. Needs a SpriteRenderer to be visible.")]
    public Color grabbedTint = new Color(0.7f, 1f, 0.7f);

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Color originalColor;

    private Vector3 anchorPosition;   // where the platform sat when this grab began
    private Vector3 grabOffset;       // platform pos - mouse pos at grab, so it doesn't snap to the cursor
    private Vector3 targetPosition;   // where we want the platform this step
    private bool isGrabbed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;

        anchorPosition = transform.position;
        targetPosition = transform.position;
    }

    // OnMouseDown/Drag/Up are sent by Unity to any object with a Collider2D under the
    // cursor, using the MainCamera. No manual raycasting needed.
    void OnMouseDown()
    {
        anchorPosition = transform.position;              // this spot is the anchor for the grab
        grabOffset = transform.position - MouseWorld();   // grab from where you clicked, not the pivot
        isGrabbed = true;

        if (sprite != null) sprite.color = grabbedTint;
    }

    void OnMouseDrag()
    {
        if (!isGrabbed) return;

        Vector3 desired = MouseWorld() + grabOffset;

        // Axis locks: pin a locked axis back to where the grab started.
        if (!allowHorizontal) desired.x = anchorPosition.x;
        if (!allowVertical)   desired.y = anchorPosition.y;
        desired.z = anchorPosition.z;

        // Optional leash: don't let it travel farther than maxDragDistance from the anchor.
        if (maxDragDistance > 0f)
        {
            Vector3 delta = desired - anchorPosition;
            if (delta.magnitude > maxDragDistance)
                desired = anchorPosition + delta.normalized * maxDragDistance;
        }

        targetPosition = desired;

        // With no Rigidbody2D we move the Transform immediately. With one, FixedUpdate
        // drives it through physics so collisions with the player stay correct.
        if (rb == null) transform.position = targetPosition;
    }

    void OnMouseUp()
    {
        isGrabbed = false;
        if (sprite != null) sprite.color = originalColor;
    }

    void FixedUpdate()
    {
        // Move a (kinematic) body through physics so it pushes/carries the player instead
        // of tunnelling through them.
        if (rb != null && isGrabbed) rb.MovePosition(targetPosition);
    }

    private Vector3 MouseWorld()
    {
        Vector3 screen = Input.mousePosition;
        // Push the point out to the platform's plane so ScreenToWorldPoint lands on it
        // (works for both orthographic and perspective cameras).
        screen.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 world = Camera.main.ScreenToWorldPoint(screen);
        world.z = transform.position.z;
        return world;
    }

    // Draws the allowed drag radius in the Scene view when the platform is selected.
    void OnDrawGizmosSelected()
    {
        if (maxDragDistance <= 0f) return;
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? anchorPosition : transform.position;
        Gizmos.DrawWireSphere(center, maxDragDistance);
    }
}
