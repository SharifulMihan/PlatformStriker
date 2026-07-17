using UnityEngine;

/// <summary>
/// Attach to the Flag GameObject. Its Collider2D must have "Is Trigger" enabled.
/// Watches for the Player and Ball both being inside the zone at once, and
/// hands control over to the ThrowController when that happens.
///
/// While the ball is still rolling into the zone it also gently bleeds off the
/// ball's speed so it settles at the flag instead of overshooting it.
/// </summary>
public class FlagZone : MonoBehaviour
{
    public ThrowController throwController;

    [Header("Ball Settling")]
    [Tooltip("How strongly the ball is slowed each physics step while it rolls inside the " +
             "flag zone, so it eases to a stop at the flag instead of blowing past it. " +
             "0 = no braking, 1 = frozen instantly. ~0.1 is a smooth roll-to-stop over ~1s. " +
             "Give the trigger collider some width so the ball has room to settle.")]
    [Range(0f, 1f)]
    [SerializeField]public float ballSettleStrength = 0.2f;

    private bool playerInZone;
    private bool ballInZone;
    private bool consumed;   // true once this flag has handed control to the throw setup

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInZone = true;
        if (other.CompareTag("Ball")) ballInZone = true;

        TryActivate();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInZone = false;
        if (other.CompareTag("Ball")) ballInZone = false;
    }

    void FixedUpdate()
    {
        // Ease the incoming ball to a stop while it's inside the zone. This is deliberately
        // limited to a ball that is still "arriving": before the throw setup takes over
        // (consumed) and never while the ball is held or after a throw. Throws are launched
        // from inside this same zone, so braking an in-flight ball would kill every throw.
        if (consumed || !ballInZone) return;
        if (throwController == null || throwController.ball == null) return;
        if (throwController.CurrentState != ThrowController.State.Idle) return;
        if (throwController.ball.IsHeld) return;

        Rigidbody2D ballRb = throwController.ball.rb;
        ballRb.linearVelocity *= (1f - ballSettleStrength);
        ballRb.angularVelocity *= (1f - ballSettleStrength);
    }

    void TryActivate()
    {
        if (!consumed && playerInZone && ballInZone &&
            throwController.CurrentState == ThrowController.State.Idle)
        {
            consumed = true;
            throwController.EnterAimMode(transform.position);
        }
    }
}
