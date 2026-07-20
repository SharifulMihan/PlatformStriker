using UnityEngine;

public class ThrowController : MonoBehaviour
{
    public enum State { Idle, Aiming, Thrown }
    public State CurrentState { get; private set; } = State.Idle;

    [Header("References")]
    public PlayerController player;
    public Ball ball;
    public Transform throwOrigin;      // Empty child on the Player marking where the ball sits pre-throw
    public LineRenderer trajectoryLine;

    [Header("Throw Range")]
    [Tooltip("How close the ball needs to be to the player (world units) for the K key to start aiming.")]
    public float throwRange = 3f;
    public KeyCode throwKey = KeyCode.K;

    [Header("Aiming")]
    [Tooltip("Angle range in degrees, measured from horizontal.")]
    public float minAngle = 10f;
    public float maxAngle = 80f;
    public float aimRotateSpeed = 60f;   // degrees/sec for keyboard aiming

    [Header("Power")]
    public float minForce = 5f;
    public float maxForce = 20f;
    public float chargeRate = 15f;       // force units gained per second while charging

    [Header("Trajectory Preview")]
    public int trajectoryPoints = 30;
    public float trajectoryTimeStep = 0.08f;
    public float lineWidth = 0.12f;
    public Color lowChargeColor = Color.yellow;
    public Color highChargeColor = Color.red;

    private float currentAngle = 45f;
    private float currentPower;
    private bool isCharging;
    private Vector2 aimAnchor;   // player's exact position when aiming started; re-applied every FixedUpdate

    void Update()
    {
        if (CurrentState == State.Idle)
        {
            HandleIdleInput();
            return;
        }

        if (CurrentState != State.Aiming) return;

        if (Input.GetKeyDown(throwKey))
        {
            CancelAimMode();
            return;
        }

        HandleFacingInput();
        HandleAimInput();
        HandleChargeInput();
        UpdateTrajectoryPreview();
    }

    /// <summary>
    /// Lets A/D (or Left/Right arrows) flip the player's facing while aiming.
    /// </summary>
    private void HandleFacingInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            player.SetFacing(false);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            player.SetFacing(true);
        }

        ball.transform.position = throwOrigin.position;
    }

    private void HandleIdleInput()
    {
        if (player == null || ball == null || ball.IsHeld) return;
        if (!player.IsGrounded) return;

        if (!player.IsOnJumpableTag) return;

        if (!ball.IsOnGround) return;

        if (player.CurrentNoThrowPlatform != null && player.CurrentNoThrowPlatform.IsBallPresent)
        {
            return;
        }

        if (Input.GetKeyDown(throwKey))
        {
            float distanceX = Mathf.Abs(player.transform.position.x - ball.transform.position.x);
            if (distanceX <= throwRange)
            {
                EnterAimMode(ball.transform.position);
            }
        }
    }

    void FixedUpdate()
    {
        if (CurrentState == State.Aiming && player != null)
        {
            if (player.CurrentPlatform != null)
            {
                aimAnchor += (Vector2)player.CurrentPlatform.DeltaMovement;
            }

            player.PinPosition(aimAnchor);

            if (ball != null && throwOrigin != null)
            {
                ball.transform.position = throwOrigin.position;
            }
        }
    }

    public void EnterAimMode(Vector2 snapPosition)
    {
        if (player == null || !player.IsGrounded || !player.IsOnJumpableTag) return;
        if (ball == null || !ball.IsOnGround) return;

        CurrentState = State.Aiming;
        currentAngle = 45f;
        currentPower = minForce;
        isCharging = false;

        player.movementLocked = true;

        player.SnapToPosition(player.transform.position);
        player.UpdateThrowOriginSide();
        aimAnchor = player.transform.position;

        ball.transform.position = throwOrigin.position;
        ball.SetHeld(true);
        ball.PlayPickupSound();

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = trajectoryPoints;
            trajectoryLine.startWidth = lineWidth;
            trajectoryLine.endWidth = lineWidth;
        }
    }

    private void CancelAimMode()
    {
        isCharging = false;

        if (trajectoryLine != null) trajectoryLine.enabled = false;

        ball.SetHeld(false);
        ball.rb.linearVelocity = Vector2.zero;

        player.movementLocked = false;
        CurrentState = State.Idle;
    }

    private void HandleAimInput()
    {
        float vertical = 0f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) vertical = 1f;
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) vertical = -1f;

        currentAngle += vertical * aimRotateSpeed * Time.deltaTime;
        currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
    }

    private void HandleChargeInput()
    {
        bool pressedThisFrame = Input.GetKeyDown(KeyCode.Space);
        bool heldThisFrame = Input.GetKey(KeyCode.Space);
        bool releasedThisFrame = Input.GetKeyUp(KeyCode.Space);

        if (pressedThisFrame)
        {
            isCharging = true;
            currentPower = minForce;
        }
        else if (isCharging && heldThisFrame)
        {
            currentPower = Mathf.Clamp(currentPower + chargeRate * Time.deltaTime, minForce, maxForce);
        }
        else if (isCharging && releasedThisFrame)
        {
            isCharging = false;
            ReleaseThrow();
        }
    }

    private Vector2 GetLaunchDirection()
    {
        float facing = player.FacingRight ? 1f : -1f;
        float rad = currentAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * facing, Mathf.Sin(rad));
    }

    private void UpdateTrajectoryPreview()
    {
        if (trajectoryLine == null) return;

        Vector2 origin = throwOrigin.position;
        Vector2 velocity = GetLaunchDirection() * currentPower;
        Vector2 gravity = Physics2D.gravity * ball.rb.gravityScale;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i * trajectoryTimeStep;
            Vector2 point = origin + velocity * t + 0.5f * gravity * t * t;
            trajectoryLine.SetPosition(i, point);
        }

        float chargeFraction = Mathf.InverseLerp(minForce, maxForce, currentPower);
        Color c = Color.Lerp(lowChargeColor, highChargeColor, chargeFraction);
        trajectoryLine.startColor = c;
        trajectoryLine.endColor = c;
    }

    private void ReleaseThrow()
    {
        CurrentState = State.Thrown;

        if (trajectoryLine != null) trajectoryLine.enabled = false;

        ball.SetHeld(false);

        Vector2 launchVelocity = GetLaunchDirection() * currentPower;
        ball.rb.linearVelocity = Vector2.zero;
        ball.rb.AddForce(launchVelocity, ForceMode2D.Impulse);
        ball.PlayThrowSound();

        // Triggers the player's kick animation when launching the ball
        if (player != null)
        {
            player.PlayKickAnimation();
        }

        player.movementLocked = false;
        CurrentState = State.Idle;
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.cyan;
        Vector3 pos = player.transform.position;
        Vector3 top = pos + Vector3.up * 50f;
        Vector3 bottom = pos + Vector3.down * 50f;
        Gizmos.DrawLine(bottom + Vector3.left * throwRange, top + Vector3.left * throwRange);
        Gizmos.DrawLine(bottom + Vector3.right * throwRange, top + Vector3.right * throwRange);
    }
}