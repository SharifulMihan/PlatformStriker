using UnityEngine;

// This platform can be a plain static collider (no Rigidbody2D needed) —
// same reasoning as BallResetPlatform. It only needs to answer one question:
// "is the ball currently resting on me?" PlayerController already tracks
// which platform the player is standing on (see CurrentNoThrowPlatform,
// same pattern as CurrentPlatform for FloatingPlatform), so ThrowController
// combines "player is standing on this platform" with "this platform says
// the ball is on it" to decide whether aiming is allowed to start.
public class NoThrowPlatform : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private string ballTag = "Ball";

    // Counts overlapping ball colliders rather than using a single bool, so a
    // stray OnCollisionExit2D from a secondary/child collider on the ball
    // can't falsely flip this off while the ball is still actually resting
    // on the platform.
    private int ballContactCount;

    /// <summary>True while at least one object tagged "Ball" is resting on this platform.</summary>
    public bool IsBallPresent => ballContactCount > 0;

    // Uses OnCollisionEnter2D/Exit2D (not triggers) because the ball's
    // Rigidbody2D is a normal solid collider while in flight/at rest — same
    // as BallResetPlatform, this expects a non-trigger Collider2D so contact
    // is detected the same way the ball actually lands and settles here.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(ballTag))
            ballContactCount++;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(ballTag))
            ballContactCount = Mathf.Max(0, ballContactCount - 1);
    }
}