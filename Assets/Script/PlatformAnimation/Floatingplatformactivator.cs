using UnityEngine;

// This object can be a plain trigger or solid collider — it isn't something
// anything rests or rides on, it's just a switch. Its only job is watching
// for the ball and then turning on movement for whichever platforms are
// wired up to it.
//
// SETUP: put a FloatingPlatform and/or RotatingPlatform component on each
// platform you want to stay still until touched, but leave that component's
// checkbox UNCHECKED (disabled) in the Inspector. Both scripts' Awake() still
// runs on scene load either way (Unity runs Awake regardless of enabled
// state), so the platform's Rigidbody2D is already forced Kinematic and sits
// perfectly still — it just never gets a Start()/FixedUpdate() tick while
// disabled, so no floating/rotating happens until this switch flips it on.
public class FloatingPlatformActivator : MonoBehaviour
{
    [Header("Floating Platforms To Activate")]
    // Drag in as many FloatingPlatform components as you want this switch to
    // turn on. Resize this array in the Inspector (the size field / +
    // button) to add or remove platforms — no code changes needed per switch.
    [SerializeField] private FloatingPlatform[] platformsToActivate;

    [Header("Rotating Platforms To Activate")]
    // Same idea as above, for RotatingPlatform instead. A single switch can
    // drive any mix of floating and rotating platforms — fill in whichever
    // arrays you need and leave the other empty.
    [SerializeField] private RotatingPlatform[] rotatingPlatformsToActivate;

    [Header("Detection Settings")]
    [SerializeField] private string ballTag = "Ball";

    [Header("Activation Settings")]
    [Tooltip("If true, this switch only fires once. If false, it re-runs activation every time the ball touches it again (harmless if the platforms are already floating, but useful if something else can disable them again later).")]
    [SerializeField] private bool activateOnce = true;

    private bool activated;

    // Uses OnCollisionEnter2D (not just a trigger) because Ball.cs sets its
    // Rigidbody2D to CollisionDetectionMode2D.Continuous specifically so
    // fast-moving balls register a clean collision instead of tunnelling
    // through — matches the same detection style as BallResetPlatform.
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryActivate(collision.gameObject);
    }

    // Also supports a trigger volume, in case you want this switch to fire
    // from a zone larger than its visible sprite rather than a solid bump.
    // Toggle "Is Trigger" on the Collider2D to use this path instead.
    void OnTriggerEnter2D(Collider2D other)
    {
        TryActivate(other.gameObject);
    }

    private void TryActivate(GameObject obj)
    {
        if (activateOnce && activated) return;

        // Tag-based check, same pattern as BallResetPlatform — only the
        // ball should be able to trip this switch.
        if (!obj.CompareTag(ballTag)) return;

        // A held ball is kinematic and being carried by the player during
        // aiming (see Ball.SetHeld); guard against it firing the switch just
        // by being swung near it before it's actually thrown.
        Ball ball = obj.GetComponent<Ball>();
        if (ball != null && ball.IsHeld) return;

        activated = true;

        foreach (FloatingPlatform platform in platformsToActivate)
        {
            if (platform == null) continue;

            // Enabling the component is what starts it floating. Since it
            // hasn't run Start() yet, enabling it now captures *this* moment's
            // position as the sine wave's center (see FloatingPlatform.Start),
            // so it begins floating from wherever it currently sits rather
            // than jumping to some other baked position.
            platform.enabled = true;
        }

        foreach (RotatingPlatform platform in rotatingPlatformsToActivate)
        {
            if (platform == null) continue;

            // Same reasoning as above: enabling now (rather than earlier)
            // means RotatingPlatform.Start() captures *this* moment's
            // rotation as previousRotation, so it begins spinning from
            // whatever angle it's currently at instead of snapping.
            platform.enabled = true;
        }
    }
}