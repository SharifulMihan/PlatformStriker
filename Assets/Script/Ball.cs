using UnityEngine;

/// <summary>
/// Wraps the ball's Rigidbody2D and exposes a simple "held" state.
/// While held, the ball is kinematic so it can be positioned exactly
/// at the throw origin without physics interference. On release it
/// becomes dynamic and obeys AddForce / gravity as normal.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
    [HideInInspector] public Rigidbody2D rb;
    public bool IsHeld { get; private set; }

    [Header("Sound")]
    [Tooltip("Plays when the player picks the ball up (aim mode starts).")]
    [SerializeField] private AudioClip pickupSound;
    [Tooltip("Plays when the ball is thrown (space bar released).")]
    [SerializeField] private AudioClip throwSound;
    [Tooltip("Volume for both pickup and throw sounds.")]
    [Range(0f, 1f)][SerializeField] private float soundVolume = 1f;

    private AudioSource audioSource;

    [Header("Ground Contact")]
    [Tooltip("Tag that counts as \"the ball is resting on the ground\" for ThrowController's aiming gate.")]
    [SerializeField] private string groundTag = "Ground";
    [Tooltip("Second tag (alongside groundTag) that also counts as the ball resting on solid ground — e.g. \"Platform\".")]
    [SerializeField] private string platformTag = "Platform";

    // Counts overlapping ground colliders rather than using a single bool, so a
    // stray OnCollisionExit2D from a secondary/child collider can't falsely flip
    // this off while the ball is still actually resting on the ground — same
    // reasoning as NoThrowPlatform.ballContactCount.
    private int groundContactCount;

    /// <summary>True while the ball is resting on an object tagged "Ground" or "Platform".
    /// ThrowController requires this (alongside the player's own ground check) before aiming
    /// can start — so throwing can't be triggered while the ball is mid-flight or mid-air.</summary>
    public bool IsOnGround => groundContactCount > 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-add an AudioSource if one isn't already on the ball, so this
        // works out of the box without extra setup in the Inspector.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // Discrete detection only checks for overlaps at the start/end of a physics
        // step, not along the path between them. A ball falling fast onto the
        // (now-moving) FloatingPlatform can end up deeply overlapping it within a
        // single step instead of touching cleanly; the solver then has to shove
        // it back out hard to resolve that overlap, which looks like the ball
        // getting violently launched/spun instead of just landing. Continuous
        // sweeps the motion instead, so it catches the collision before the
        // overlap gets that deep.
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    /// <summary>
    /// Switches the ball between "carried/aimed" (kinematic) and
    /// "in flight" (dynamic) physics states.
    /// </summary>
    public void SetHeld(bool held)
    {
        IsHeld = held;
        rb.bodyType = held ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;

        if (held)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    /// <summary>Plays the pickup sound. Called by ThrowController when aim mode starts.</summary>
    public void PlayPickupSound()
    {
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound, soundVolume);
        }
    }

    /// <summary>Plays the throw sound. Called by ThrowController when the ball is released.</summary>
    public void PlayThrowSound()
    {
        if (throwSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(throwSound, soundVolume);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag) || collision.gameObject.CompareTag(platformTag))
            groundContactCount++;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag) || collision.gameObject.CompareTag(platformTag))
            groundContactCount = Mathf.Max(0, groundContactCount - 1);
    }
}