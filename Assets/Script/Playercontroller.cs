using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("Tag that ground objects must have for the player to be allowed to start aiming a throw.")]
    public string groundTag = "Ground";
    [Tooltip("Second tag (alongside groundTag) that's allowed to jump from — e.g. moving/floating platforms tagged \"Platform\".")]
    public string platformTag = "Platform";

    [Header("Throw Origin")]
    [Tooltip("The child transform marking where the ball rests before a throw. Its X offset gets mirrored automatically based on facing direction.")]
    public Transform throwOrigin;

    [Header("Animation References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    /// True while the player is facing right. Used by ThrowController
    /// to decide which way to launch the ball
    public bool FacingRight { get; private set; } = true;

    /// True while the player is touching the ground. Used by ThrowController so
    /// aiming can only start while grounded, never mid-air/mid-jump.
    public bool IsGrounded => isGrounded;

    /// <summary>
    /// True only while the player is standing on an object tagged "Ground" (not
    /// just anything included in groundLayer, which may also cover things like a
    /// FloatingPlatform). ThrowController uses this to gate ball-throwing so
    /// aiming can only start while standing on a "Ground"-tagged object.
    /// </summary>
    public bool IsOnGroundTag { get; private set; }

    /// <summary>
    /// True while the player is standing on an object tagged either "Ground" or
    /// "Platform" (the two tags set in groundTag/platformTag). Jumping requires this —
    /// so it works from both, but not from some other tagged surface that happens to
    /// be in groundLayer for other reasons (e.g. a NoThrowPlatform).
    /// </summary>
    public bool IsOnJumpableTag { get; private set; }

    /// <summary>
    /// The FloatingPlatform the player is currently standing on, or null if grounded
    /// on static geometry (or airborne). ThrowController reads this every FixedUpdate
    /// while aiming so it can carry the pinned player along with the platform.
    /// </summary>
    public FloatingPlatform CurrentPlatform { get; private set; }

    /// <summary>
    /// The NoThrowPlatform the player is currently standing on, or null if not on
    /// one. Set the same way as CurrentPlatform (via the ground check). ThrowController
    /// combines this with NoThrowPlatform.IsBallPresent to decide whether aiming is
    /// allowed to start — both the player AND the ball have to be on the same one.
    /// </summary>
    public NoThrowPlatform CurrentNoThrowPlatform { get; private set; }

    /// <summary>Set true by ThrowController to freeze movement during aim/charge.</summary>
    [HideInInspector] public bool movementLocked = false;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool hasJumped;    // becomes true the instant we jump, only clears on landing
    private float throwOriginOffsetX;  // absolute distance from player pivot, sign gets flipped per facing

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-fetch animation components if not assigned in Inspector
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Prevents the player from tipping/rotating when its collider clips a platform edge
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Continuous sweeps motion to prevent falling/landing overlaps on moving platforms
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (throwOrigin != null)
        {
            throwOriginOffsetX = Mathf.Abs(throwOrigin.localPosition.x);
            UpdateThrowOriginSide();
        }
    }

    void Update()
    {
        Collider2D groundHit = null;
        if (groundCheck != null)
        {
            Vector2 checkPos = groundCheck.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, groundCheckRadius, groundLayer);
            foreach (Collider2D hit in hits)
            {
                Vector2 closest = hit.ClosestPoint(checkPos);
                Vector2 toCheck = checkPos - closest;

                if (toCheck.y > 0f && toCheck.y >= Mathf.Abs(toCheck.x))
                {
                    groundHit = hit;
                    break;
                }
            }
        }

        isGrounded = groundHit != null;
        CurrentPlatform = groundHit != null ? groundHit.GetComponentInParent<FloatingPlatform>() : null;
        CurrentNoThrowPlatform = groundHit != null ? groundHit.GetComponentInParent<NoThrowPlatform>() : null;
        IsOnGroundTag = groundHit != null && groundHit.CompareTag(groundTag);
        IsOnJumpableTag = groundHit != null &&
            (groundHit.CompareTag(groundTag) || groundHit.CompareTag(platformTag));

        // Landing resets the jump lock
        if (isGrounded)
        {
            hasJumped = false;
        }

        if (movementLocked)
        {
            moveInput = 0f;
            UpdateAnimations();
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        bool wasFacingRight = FacingRight;
        if (moveInput > 0f) FacingRight = true;
        else if (moveInput < 0f) FacingRight = false;

        if (FacingRight != wasFacingRight)
        {
            UpdateThrowOriginSide();
        }

        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W)) && isGrounded && IsOnJumpableTag && !hasJumped)
        {
            float platformVelY = CurrentPlatform != null ? CurrentPlatform.Velocity.y : 0f;
            float relativeVelY = rb.linearVelocity.y - platformVelY;

            if (relativeVelY <= 0.05f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, platformVelY + jumpForce);
                hasJumped = true;
            }
        }

        UpdateAnimations();
    }

    /// <summary>
    /// Directly sets facing and mirrors the throw origin/sprite facing.
    /// Called by ThrowController during aim mode.
    /// </summary>
    public void SetFacing(bool right)
    {
        if (FacingRight == right) return;
        FacingRight = right;
        UpdateThrowOriginSide();
    }

    /// <summary>
    /// Mirrors ThrowOrigin's local X offset and flips SpriteRenderer to match facing.
    /// Assumes default sprite asset faces Right.
    /// </summary>
    public void UpdateThrowOriginSide()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !FacingRight;
        }

        if (throwOrigin == null) return;

        Vector3 pos = throwOrigin.localPosition;
        pos.x = FacingRight ? throwOriginOffsetX : -throwOriginOffsetX;
        throwOrigin.localPosition = pos;
    }

    /// <summary>
    /// Sends physics metrics to Unity Animator parameters.
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("YVelocity", rb.linearVelocity.y);
        animator.SetBool("IsGrounded", isGrounded);
    }

    /// <summary>
    /// Triggers kick animation state. Called by ThrowController on release.
    /// </summary>
    public void PlayKickAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Kick");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    void FixedUpdate()
    {
        if (movementLocked)
        {
            // Kill horizontal drift while locked, keep gravity acting normally
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    public void PinPosition(Vector2 worldPosition)
    {
        rb.position = worldPosition;
        transform.position = worldPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void SnapToPosition(Vector2 worldPosition)
    {
        rb.position = worldPosition;
        transform.position = worldPosition;
        rb.linearVelocity = Vector2.zero;
    }
}