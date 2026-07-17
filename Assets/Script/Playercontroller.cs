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

    [Header("Throw Origin")]
    [Tooltip("The child transform marking where the ball rests before a throw. Its X offset gets mirrored automatically based on facing direction.")]
    public Transform throwOrigin;

    ///True while the player is facing right. Used by ThrowController
    /// to decide which way to launch the ball
    public bool FacingRight { get; private set; } = true;

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

        if (throwOrigin != null)
        {
            throwOriginOffsetX = Mathf.Abs(throwOrigin.localPosition.x);
            UpdateThrowOriginSide();
        }
    }

    void Update()
    {
        if (movementLocked)
        {
            moveInput = 0f;
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

        
        
        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Landing resets the jump lock. Checking isGrounded here (rather than only
        // inside the jump input check) means a mid-air state can never keep hasJumped
        // stuck at false, and a single ground touch is enough to re-arm the jump.
        if (isGrounded)
        {
            hasJumped = false;
        }

        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W)) && isGrounded && !hasJumped && rb.linearVelocity.y <= 0.05f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            hasJumped = true;
        }
    }

    /// <summary>
    /// Mirrors ThrowOrigin's local X offset to match current facing, so the ball
    /// always sets up on the side the player is actually facing/approached from —
    /// left side approach + facing right keeps it in front, not behind.
    /// </summary>
    public void UpdateThrowOriginSide()
    {
        if (throwOrigin == null) return;

        Vector3 pos = throwOrigin.localPosition;
        pos.x = FacingRight ? throwOriginOffsetX : -throwOriginOffsetX;
        throwOrigin.localPosition = pos;
    }

    // Draws the ground check circle in the Scene view (yellow = airborne, green = grounded)
    // so you can visually confirm it's positioned at the feet and not overlapping the player's
    // own body collider.
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
            // Kill horizontal drift while locked, keep gravity acting normally.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    /// <summary>
    /// Teleports the player to an exact world position (e.g. the Flag's transform)
    /// and zeroes velocity so it doesn't carry over any momentum into the throw setup.
    /// Updates both Rigidbody2D and Transform so there's no one-frame lag between them.
    /// </summary>
    public void SnapToPosition(Vector2 worldPosition)
    {
        rb.position = worldPosition;
        transform.position = worldPosition;
        rb.linearVelocity = Vector2.zero;
    }
}