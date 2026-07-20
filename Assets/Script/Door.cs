using UnityEngine;

/// <summary>
/// Attach to the Door GameObject. Its Collider2D must have "Is Trigger" enabled.
/// When the Player and the Ball are both inside the door's trigger at the same
/// time, the level is complete: control locks, and LevelTransitionManager plays
/// the "Level Complete" -> wipe -> "Level 2" -> reveal sequence before loading
/// the next scene.
/// </summary>
public class Door : MonoBehaviour
{
    public PlayerController player;

    [Header("Level Complete")]
    public string completeMessage = "Level 1 Complete";
    public string nextSceneName = "Level2";
    public string nextLevelMessage = "Level 2";

    private bool playerInZone;
    private bool ballInZone;
    private bool completed;   // guards against triggering more than once

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInZone = true;
        if (other.CompareTag("Ball")) ballInZone = true;

        TryComplete();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInZone = false;
        if (other.CompareTag("Ball")) ballInZone = false;
    }

    private void TryComplete()
    {
        if (completed || !playerInZone || !ballInZone) return;
        completed = true;

        if (player != null)
        {
            player.movementLocked = true;
            player.SnapToPosition(player.transform.position);   // kills any leftover velocity immediately
        }

        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.CompleteLevel(completeMessage, nextSceneName, nextLevelMessage);
        }
    }
}