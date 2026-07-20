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
///
/// Three safety rules layered on top of the base drag behaviour:
///   1) While the platform is being held, its Collider2D is disabled. This stops it
///      from shoving/carrying the player or the ball around mid-drag as you fling it
///      across the screen — it becomes purely visual for normal physics purposes.
///   2) The platform can't be grabbed at all while the Ball or Player is currently
///      resting on it. This prevents yanking a platform out from under something
///      that's standing on it (which would otherwise drop them through empty air
///      the instant the collider switched off in rule 1).
///   3) Even with the collider disabled, the platform still can't be dragged INTO the
///      Ball or Player. Every drag step is cast ahead along the mouse's movement
///      direction; if the Ball/Player is in the way, movement is clamped to stop right
///      at their edge instead of overlapping them. This is what stops the "freeze" you
///      get from shoving a kinematic body deep inside another collider and then having
///      the physics solver fight to separate them the instant collision re-enables —
///      the platform simply can't get inside them in the first place, and glides back
///      into your cursor's control the moment you drag away from the obstruction.
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
    [Tooltip("Tint shown when the platform is occupied and therefore can't be grabbed right now. " +
             "Leave alpha at 0 / same as normal if you don't want a visual cue for this.")]
    public Color occupiedTint = new Color(1f, 0.6f, 0.6f);
    [Tooltip("Show occupiedTint whenever the Ball or Player is standing on this platform, even before you try to grab it.")]
    public bool showOccupiedFeedback = true;

    [Header("Occupancy Lock")]
    [Tooltip("Any GameObject with one of these tags, while touching this platform, blocks it from being grabbed, " +
             "and also blocks the platform from being dragged INTO them while held.")]
    [SerializeField] private string[] blockingTags = { "Ball", "Player" };
    [Tooltip("Small gap kept between the platform and a blocked object so they don't end up sitting flush " +
             "(which could immediately re-trigger the block every subsequent frame).")]
    [SerializeField] private float dragBlockSkin = 0.02f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Collider2D col;
    private Color originalColor;

    private Vector3 anchorPosition;   // where the platform sat when this grab began
    private Vector3 grabOffset;       // platform pos - mouse pos at grab, so it doesn't snap to the cursor
    private Vector3 targetPosition;   // where we want the platform this step
    private bool isGrabbed;

    // Reused every drag step so casting ahead for the Ball/Player doesn't allocate garbage each frame.
    private readonly RaycastHit2D[] dragCastHitBuffer = new RaycastHit2D[8];

    // Reused every occupancy check so it doesn't allocate garbage each frame.
    private readonly Collider2D[] occupancyOverlapBuffer = new Collider2D[8];

    // Occupancy used to be tracked with an Enter/Exit counter, but Unity does NOT
    // reliably send OnCollisionExit2D when a collider is disabled while still in
    // contact — and this platform disables its own collider the moment it's grabbed
    // (see OnMouseDown below). That meant if the player was standing on the platform
    // at the wrong instant, the Enter could fire without a matching Exit ever
    // arriving, permanently stuck-locking the platform as "occupied" and unable to
    // be dragged again. Instead, occupancy is now actively re-queried every frame via
    // a physics overlap check — there's no persistent counter to desync, so it can't
    // get stuck either true or false.
    [Tooltip("Small margin added around the platform's bounds when checking who's standing on it, " +
             "so something resting exactly flush on the surface still counts as touching.")]
    [SerializeField] private float occupancyCheckSkin = 0.05f;
    private bool cachedOccupied;
    private bool IsOccupied => cachedOccupied;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sprite != null) originalColor = sprite.color;

        anchorPosition = transform.position;
        targetPosition = transform.position;
    }

    // OnMouseDown/Drag/Up are sent by Unity to any object with a Collider2D under the
    // cursor, using the MainCamera. No manual raycasting needed.
    void OnMouseDown()
    {
        // Refuse the grab entirely while the ball or player is resting on this
        // platform — dragging it out from under them would otherwise drop them
        // the instant the collider disables below.
        if (IsOccupied) return;

        anchorPosition = transform.position;              // this spot is the anchor for the grab
        grabOffset = transform.position - MouseWorld();   // grab from where you clicked, not the pivot
        isGrabbed = true;

        if (sprite != null) sprite.color = grabbedTint;

        // Disable collision while held. The platform is now purely visual until
        // dropped, so it can't push or carry anything while being dragged around.
        if (col != null) col.enabled = false;
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

        // Even though the collider is disabled while held (so it doesn't shove/carry
        // anything on normal contact), we still don't want it able to slide INTO the
        // ball or player. Clamp this step's movement to stop right at their edge.
        desired = ClampAgainstBlockingTags(transform.position, desired);

        targetPosition = desired;

        // With no Rigidbody2D we move the Transform immediately. With one, FixedUpdate
        // drives it through physics so collisions with the player stay correct.
        if (rb == null) transform.position = targetPosition;
    }

    /// <summary>
    /// Casts the platform's own footprint from <paramref name="from"/> toward
    /// <paramref name="to"/> and, if a Ball/Player-tagged collider is hit along the way,
    /// returns a point just short of it instead of the full requested destination.
    /// Uses a box cast (not a simple overlap check) so a fast mouse flick can't skip
    /// past a thin obstruction in a single frame.
    /// </summary>
    private Vector3 ClampAgainstBlockingTags(Vector3 from, Vector3 to)
    {
        if (col == null) return to;

        Vector2 origin = from;
        Vector2 delta = (Vector2)to - origin;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return to;

        Vector2 direction = delta / distance;
        Vector2 castSize = col.bounds.size;

        // ~0 (all layers) here because we're filtering by tag below, not by layer —
        // the ball/player might live on any layer depending on how the project is set up.
        int hitCount = Physics2D.BoxCastNonAlloc(origin, castSize, 0f, direction, dragCastHitBuffer, distance, ~0);

        float closestBlockedDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = dragCastHitBuffer[i].collider;
            if (hitCollider == null || hitCollider == col) continue;
            if (!HasBlockingTag(hitCollider.gameObject)) continue;

            if (dragCastHitBuffer[i].distance < closestBlockedDistance)
                closestBlockedDistance = dragCastHitBuffer[i].distance;
        }

        if (closestBlockedDistance >= distance) return to; // nothing blocking within this step

        float allowedDistance = Mathf.Max(0f, closestBlockedDistance - dragBlockSkin);
        return from + (Vector3)(direction * allowedDistance);
    }

    void OnMouseUp()
    {
        if (!isGrabbed) return; // grab may never have started (e.g. blocked by IsOccupied)

        isGrabbed = false;

        // Re-enable collision now that the platform has been dropped, so it's solid
        // again and can be landed/stood on.
        if (col != null) col.enabled = true;

        RefreshTint();
    }

    void Update()
    {
        // Skip while grabbed: the collider is disabled during a drag anyway (see
        // OnMouseDown), so an overlap check against it would always read empty —
        // and occupancy doesn't matter again until after the platform is dropped.
        if (isGrabbed) return;

        bool wasOccupied = cachedOccupied;
        cachedOccupied = CheckOccupied();

        if (cachedOccupied != wasOccupied) RefreshTint();
    }

    void FixedUpdate()
    {
        // Move a (kinematic) body through physics so it pushes/carries the player instead
        // of tunnelling through them.
        if (rb != null && isGrabbed) rb.MovePosition(targetPosition);
    }

    /// <summary>
    /// Queries physics directly for anything blocking-tagged currently overlapping the
    /// platform's own bounds (expanded slightly by occupancyCheckSkin so something
    /// resting exactly flush on top still counts). Re-run fresh every frame instead of
    /// relying on Enter/Exit callbacks, so there's no persistent counter that can get
    /// stuck out of sync with reality.
    /// </summary>
    private bool CheckOccupied()
    {
        if (col == null) return false;

        Bounds b = col.bounds;
        Vector2 center = b.center;
        Vector2 size = (Vector2)b.size + Vector2.one * (occupancyCheckSkin * 2f);

        int hitCount = Physics2D.OverlapBoxNonAlloc(center, size, 0f, occupancyOverlapBuffer, ~0);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = occupancyOverlapBuffer[i];
            if (hit == null || hit == col) continue;
            if (HasBlockingTag(hit.gameObject)) return true;
        }
        return false;
    }

    private bool HasBlockingTag(GameObject obj)
    {
        foreach (string tag in blockingTags)
        {
            if (!string.IsNullOrEmpty(tag) && obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    /// <summary>Keeps the sprite tint in sync with grabbed/occupied/idle state without
    /// stepping on whichever state currently "owns" the color.</summary>
    private void RefreshTint()
    {
        if (sprite == null || isGrabbed) return; // grabbedTint takes priority while held

        sprite.color = (showOccupiedFeedback && IsOccupied) ? occupiedTint : originalColor;
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