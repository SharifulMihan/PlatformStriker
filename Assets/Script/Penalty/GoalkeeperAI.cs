using UnityEngine;

/// <summary>
/// A deliberately weak goalkeeper: reacts late, moves slowly, and only
/// guesses the correct side part of the time. Tune the values in the
/// Inspector to adjust difficulty (keep them low for an easy game).
/// Attach this to the Keeper GameObject.
/// </summary>
public class GoalkeeperAI : MonoBehaviour
{
    public Transform goalLeft;
    public Transform goalRight;
    public Transform homePosition;   // center of the goal, keeper's resting spot

    [Header("Weak AI Tuning")]
    [Range(0f, 1f)] public float diveAccuracy = 0.45f;  // chance the keeper dives toward the real target
    public float reactionDelay = 0.25f;                 // seconds before the keeper starts moving
    public float moveSpeed = 3f;                         // low speed = easy to beat
    public float maxReachFromCenter = 1.5f;               // keeper physically can't cover the full goal width

    private Vector2 diveTarget;
    private bool diving;
    private float diveTimer;

    void Awake()
    {
        ResetKeeper();
    }

    void Update()
    {
        if (!diving) return;

        diveTimer += Time.deltaTime;
        if (diveTimer < reactionDelay) return; // keeper hasn't "reacted" yet

        transform.position = Vector2.MoveTowards(transform.position, diveTarget, moveSpeed * Time.deltaTime);
    }

    public void ReactToShot(Vector2 ballTarget)
    {
        diveTimer = 0f;
        diving = true;

        bool guessesCorrectly = Random.value <= diveAccuracy;

        float chosenX;
        if (guessesCorrectly)
        {
            chosenX = ballTarget.x;
        }
        else
        {
            // Dive to a random spot, possibly the wrong side entirely
            chosenX = Random.Range(goalLeft.position.x, goalRight.position.x);
        }

        // Clamp how far the keeper can actually reach in time — keeps corners scoreable
        float clampedX = Mathf.Clamp(
            chosenX,
            homePosition.position.x - maxReachFromCenter,
            homePosition.position.x + maxReachFromCenter);

        diveTarget = new Vector2(clampedX, homePosition.position.y);
    }

    public void ResetKeeper()
    {
        diving = false;
        diveTimer = 0f;
        transform.position = homePosition.position;
    }
}
