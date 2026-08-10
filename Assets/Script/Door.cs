using UnityEngine;

/// <summary>
/// Attach to the Door GameObject. Its Collider2D must have "Is Trigger" enabled.
/// When the Player and the Ball are both inside the door's trigger at the same
/// time, the level is complete: control locks, and LevelTransitionManager plays
/// the "Level Complete" -> wipe -> "Next Level Intro" -> reveal sequence before
/// loading the next scene.
///
/// Images are per-door: assign THIS level's "Level N Complete" sprite and the
/// NEXT level's intro sprite here in the Inspector, so every scene shows its
/// own artwork instead of a shared/default image.
/// </summary>
public class Door : MonoBehaviour
{
    public PlayerController player;

    [Header("Level Complete")]
    [Tooltip("This level's 'Level N Complete' image, shown first when the door triggers.")]
    public Sprite completeSprite;
    public string nextSceneName = "Level2";
    [Tooltip("The next level's intro image, shown after the wipe.")]
    public Sprite nextLevelSprite;

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
            LevelTransitionManager.Instance.CompleteLevel(completeSprite, nextSceneName, nextLevelSprite);
        }
    }
}