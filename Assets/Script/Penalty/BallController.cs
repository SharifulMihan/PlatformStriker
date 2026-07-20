using UnityEngine;

/// <summary>
/// Moves the ball from the penalty spot to the clicked target over time.
/// Shrinks the ball slightly as it travels to fake depth/perspective.
/// Attach this to the Ball GameObject.
/// </summary>
public class BallController : MonoBehaviour
{
    public Transform penaltySpot;
    public float kickDuration = 0.6f;
    public float startScale = 1f;
    public float endScale = 0.5f;

    [Header("Sound")]
    [Tooltip("Plays when the ball is shot (mouse click).")]
    [SerializeField] private AudioClip kickSound;
    [Range(0f, 1f)][SerializeField] private float soundVolume = 1f;

    private AudioSource audioSource;
    private Vector2 startPos;
    private Vector2 targetPos;
    private float timer;
    private bool moving;

    void Awake()
    {
        // Auto-add an AudioSource if one isn't already on the ball, so this
        // works out of the box without extra setup in the Inspector.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        ResetBall();
    }

    void Update()
    {
        if (!moving) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / kickDuration);

        transform.position = Vector2.Lerp(startPos, targetPos, t);
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (t >= 1f) moving = false;
    }

    public void Shoot(Vector2 target)
    {
        startPos = penaltySpot.position;
        targetPos = target;
        timer = 0f;
        moving = true;

        if (kickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(kickSound, soundVolume);
        }
    }

    public void ResetBall()
    {
        moving = false;
        timer = 0f;
        transform.position = penaltySpot.position;
        transform.localScale = new Vector3(startScale, startScale, 1f);
    }
}