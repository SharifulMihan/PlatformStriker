using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Switch : MonoBehaviour
{
    [Tooltip("Tag on the Ball GameObject.")]
    public string ballTag = "Ball";

    [Tooltip("Minimum seconds between activations. Prevents a resting/bouncing ball " +
             "from triggering multiple toggles from a single hit.")]
    public float cooldown = 0.5f;

    [Header("Feedback (all optional)")]
    public ParticleSystem hitEffect;
    public AudioSource hitSound;
    [Tooltip("Animator with a trigger parameter named 'Hit', for a switch flip/press animation.")]
    public Animator animator;

    [Header("Destroy On Hit")]
    [Tooltip("Seconds to wait after activation before destroying the switch. Keep this " +
             "above 0 if you've set a hitEffect/hitSound so they get a chance to actually " +
             "play — destroying the GameObject immediately (0s) would cut them off instantly.")]
    public float destroyDelay = 0f;

    private float lastTriggerTime = -999f;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryActivate(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only fires if you instead choose a solid (non-trigger) collider on the switch.
        TryActivate(collision.collider);
    }

    private void TryActivate(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;
        if (Time.time - lastTriggerTime < cooldown) return;

        lastTriggerTime = Time.time;

        if (PlatformManager.Instance != null)
        {
            PlatformManager.Instance.Toggle();
        }
        else
        {
            Debug.LogWarning("Switch hit but no PlatformManager.Instance found in the scene.");
        }

        if (hitEffect != null) hitEffect.Play();
        if (hitSound != null) hitSound.Play();
        if (animator != null) animator.SetTrigger("Hit");
        Destroy(gameObject, destroyDelay);
    }
}