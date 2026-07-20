using UnityEngine;

// A Rigidbody2D is required (and must be set to Kinematic in the Inspector).
// Without it, this collider is treated as STATIC by the physics engine. Moving
// a static collider via transform.position forces Unity to re-insert it into
// the physics broadphase from scratch every frame instead of sweeping it
// smoothly, and any dynamic body resting on it (the player) gets shoved out
// by the sudden overlap each step — that's what causes the bounce/jitter when
// the platform moves, especially on the way down. Kinematic bodies moved with
// MovePosition are swept properly by the solver, so resting bodies get carried
// via friction instead of popping.
[RequireComponent(typeof(Rigidbody2D))]
public class FloatingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float range = 2f; // Distance above and below the starting point
    [SerializeField] private float speed = 2f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Vector3 previousPosition;

    /// <summary>
    /// World-space movement this platform made during the last physics step.
    /// Anything riding the platform (e.g. a player that's being pinned in place
    /// during aiming) can add this to its own position every FixedUpdate so it
    /// travels along with the platform instead of being left hanging in the air.
    /// </summary>
    public Vector3 DeltaMovement { get; private set; }

    /// <summary>
    /// The platform's current velocity (DeltaMovement / fixedDeltaTime). Used by
    /// PlayerController's jump check: a player riding the platform inherits its
    /// vertical velocity through friction, so "am I moving up" has to be judged
    /// relative to the platform's velocity, not relative to zero.
    /// </summary>
    public Vector2 Velocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Enforce Kinematic here rather than trusting the Inspector setting:
        // if this were left Dynamic, gravity/collisions would fight the sine
        // motion below; if it had no Rigidbody2D at all we'd be back to the
        // static-collider bounce bug this script exists to avoid.
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Start()
    {
        startPosition = rb.position;
        previousPosition = startPosition;
    }

    // Runs on the physics tick (same as the player's position-lock during
    // aiming — see ThrowController.FixedUpdate / PlayerController.PinPosition)
    // so the delta below always reflects exactly one physics step, not a
    // frame-rate-dependent slice of one.
    void FixedUpdate()
    {
        float offset = Mathf.Sin(Time.time * speed) * range;
        Vector3 newPosition = startPosition + Vector3.up * offset;

        // MovePosition sweeps the kinematic body through the solver so
        // resting dynamic bodies get carried smoothly via friction, instead
        // of the collider being teleported and everything on it bouncing.
        rb.MovePosition(newPosition);

        DeltaMovement = newPosition - previousPosition;
        previousPosition = newPosition;

        // Kinematic bodies don't get their velocity auto-computed from
        // MovePosition — it stays whatever we set it to (zero, by default).
        // Setting it explicitly lets the solver factor real platform velocity
        // into friction/contact resolution, and lets PlayerController read it
        // to judge the player's velocity *relative to the platform* rather
        // than relative to a stationary world.
        Velocity = DeltaMovement / Time.fixedDeltaTime;
        rb.linearVelocity = Velocity;
    }
}