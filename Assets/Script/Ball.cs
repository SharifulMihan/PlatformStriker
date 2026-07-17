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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
}