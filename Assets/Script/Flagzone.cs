using UnityEngine;

/// <summary>
/// Attach to the Flag GameObject. Its Collider2D must have "Is Trigger" enabled.
/// Watches for the Player and Ball both being inside the zone at once, and
/// hands control over to the ThrowController when that happens.
/// </summary>
public class FlagZone : MonoBehaviour
{
    public ThrowController throwController;

    private bool playerInZone;
    private bool ballInZone;

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

    void TryActivate()
    {
        if (playerInZone && ballInZone && throwController.CurrentState == ThrowController.State.Idle)
        {
            throwController.EnterAimMode(transform.position);
        }
    }
}