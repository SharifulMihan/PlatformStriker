using UnityEngine;

/// <summary>
/// Attach to the "Head" child object (the small red hitbox shown above the player
/// in the Scene view). While the Ball is touching it, the player's jump force is
/// boosted; the moment the ball leaves, jump force goes back to whatever it was
/// before the boost — it doesn't stay maxed out.
///
/// Works whether the Head's Collider2D is a trigger or a solid collider — but a
/// trigger is recommended, so the ball doesn't physically bounce off the player's
/// head on the way through. Reset() below flips that on automatically if you add
/// this script fresh.
/// </summary>
public class HeadBallBoost : MonoBehaviour
{
    [Tooltip("The PlayerController whose jumpForce gets boosted. Auto-found on a parent if left empty.")]
    public PlayerController player;

    [Header("Boost")]
    [Tooltip("How much jumpForce increases while the ball is touching the head.")]
    public float jumpForceIncrease = 1f;
    [Tooltip("Boosted jumpForce is clamped so it can never climb past this.")]
    public float maxJumpForce = 20f;

    private float baseJumpForce;   // player's jumpForce before any boost, restored on exit
    private bool boosted;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            baseJumpForce = player.jumpForce;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball")) ApplyBoost();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ball")) RemoveBoost();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ball")) ApplyBoost();
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ball")) RemoveBoost();
    }

    private void ApplyBoost()
    {
        if (player == null || boosted) return;

        boosted = true;
        player.jumpForce = Mathf.Min(baseJumpForce + jumpForceIncrease, maxJumpForce);
    }

    private void RemoveBoost()
    {
        if (player == null || !boosted) return;

        boosted = false;
        player.jumpForce = baseJumpForce;
    }
}