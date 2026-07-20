using UnityEngine;

// A Rigidbody2D is required (and must be set to Kinematic in the Inspector).
// Without it, this collider is treated as STATIC by the physics engine.
// Rotating a static collider via transform.rotation forces Unity to re-insert
// it into the physics broadphase from scratch every frame instead of sweeping
// it smoothly, and any dynamic body resting on it (the player/ball) gets
// shoved out by the sudden overlap each step instead of being carried around
// by friction. Kinematic bodies rotated with MoveRotation are swept properly
// by the solver, so resting bodies get spun/carried instead of popping.
[RequireComponent(typeof(Rigidbody2D))]
public class RotatingPlatform : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 90f; // Degrees per second
    [SerializeField] private bool clockwise = true;

    private Rigidbody2D rb;
    private float previousRotation;

    /// <summary>
    /// Signed rotation (in degrees) this platform made during the last
    /// physics step. Mirrors FloatingPlatform.DeltaMovement — anything
    /// riding the platform can use this to stay carried along instead of
    /// slipping if friction alone isn't enough (e.g. objects near the edge).
    /// </summary>
    public float DeltaRotation { get; private set; }

    /// <summary>
    /// The platform's current angular velocity in degrees/sec
    /// (DeltaRotation / fixedDeltaTime). Mirrors FloatingPlatform.Velocity —
    /// used the same way, so anything checking "am I moving" relative to the
    /// platform reads this instead of assuming a stationary world.
    /// </summary>
    public float AngularVelocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Enforce Kinematic here rather than trusting the Inspector setting:
        // if this were left Dynamic, gravity/collisions would fight the
        // rotation below; if it had no Rigidbody2D at all we'd be back to the
        // static-collider bounce bug this script exists to avoid.
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Start()
    {
        previousRotation = rb.rotation;
    }

    // Runs on the physics tick so DeltaRotation always reflects exactly one
    // physics step, not a frame-rate-dependent slice of one — same reasoning
    // as FloatingPlatform.FixedUpdate.
    void FixedUpdate()
    {
        float direction = clockwise ? -1f : 1f;
        float newRotation = rb.rotation + direction * rotationSpeed * Time.fixedDeltaTime;

        // MoveRotation sweeps the kinematic body through the solver so
        // resting dynamic bodies get carried smoothly via friction, instead
        // of the collider being teleported and everything on it bouncing.
        rb.MoveRotation(newRotation);

        DeltaRotation = newRotation - previousRotation;
        previousRotation = newRotation;

        // Kinematic bodies don't get their angular velocity auto-computed
        // from MoveRotation — it stays whatever we set it to (zero, by
        // default). Setting it explicitly lets the solver factor real
        // platform spin into friction/contact resolution, and lets other
        // scripts read it to judge motion relative to the platform rather
        // than relative to a stationary world.
        AngularVelocity = DeltaRotation / Time.fixedDeltaTime;
        rb.angularVelocity = AngularVelocity;
    }
}