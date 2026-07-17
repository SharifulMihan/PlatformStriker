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

    [Header("Aiming")]
    [Tooltip("Angle range in degrees, measured from horizontal.")]
    public float minAngle = 10f;
    public float maxAngle = 80f;
    public float aimRotateSpeed = 60f;   // degrees/sec for keyboard aiming
    public bool useMouseAim = false;

    [Header("Power")]
    public float minForce = 5f;
    public float maxForce = 20f;
    public float chargeRate = 15f;       // force units gained per second held

    [Header("Trajectory Preview")]
    public int trajectoryPoints = 30;
    public float trajectoryTimeStep = 0.08f;
    public float lineWidth = 0.12f;
    public Color lowChargeColor = Color.yellow;
    public Color highChargeColor = Color.red;

    private float currentAngle = 45f;
    private float currentPower;
    private bool isCharging;

    void Update()
    {
        if (CurrentState != State.Aiming) return;

        HandleAimInput();
        HandleChargeInput();
        UpdateTrajectoryPreview();
    }

    /// Called by FlagZone once player + ball are both in range.
    /// snapPosition is the Flag's exact world position — the player is
    /// teleported there so the throw always starts from the same spot.
    public void EnterAimMode(Vector2 snapPosition)
    {
        CurrentState = State.Aiming;
        currentAngle = 45f;
        currentPower = minForce;
        isCharging = false;

        player.movementLocked = true;
        player.SnapToPosition(snapPosition);
        player.UpdateThrowOriginSide();

        // throwOrigin is a child of the player, so it moved along with the snap above.
        ball.transform.position = throwOrigin.position;
        ball.SetHeld(true);

        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = trajectoryPoints;
            // Force a sane width here rather than relying on the Inspector's
            // width curve being dragged to the right value by hand.
            trajectoryLine.startWidth = lineWidth;
            trajectoryLine.endWidth = lineWidth;
        }
    }

    private void HandleAimInput()
    {
        if (useMouseAim)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (Vector2)mouseWorld - (Vector2)throwOrigin.position;
            // Use the magnitude of the angle regardless of which side the mouse is on;
            // actual throw direction still comes from the player's facing.
            float angle = Mathf.Atan2(Mathf.Abs(dir.y), Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
            currentAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        }
        else
        {
            float vertical = 0f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) vertical = 1f;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) vertical = -1f;

            currentAngle += vertical * aimRotateSpeed * Time.deltaTime;
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
        }
    }

    private void HandleChargeInput()
    {
        bool pressedThisFrame = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        bool heldThisFrame = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        bool releasedThisFrame = Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0);

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

    /// <summary>Unit direction vector for the current aim angle, respecting player facing.</summary>
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

        // Visual charge feedback: line shifts from low-charge to high-charge color.
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

        // Setting velocity directly (rather than only AddForce) guarantees the
        // ball follows exactly the arc that was just previewed, regardless of mass.
        Vector2 launchVelocity = GetLaunchDirection() * currentPower;
        ball.rb.linearVelocity = Vector2.zero;
        ball.rb.AddForce(launchVelocity, ForceMode2D.Impulse);

        player.movementLocked = false;
        CurrentState = State.Idle;
    }
}