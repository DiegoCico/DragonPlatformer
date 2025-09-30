using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;
    public float damageCooldown = 0.6f;

    [Header("Filter (optional)")]
    public string targetTag = "Player";          // set your Player tag
    public LayerMask targetLayers = ~0;          // or narrow to Player layer

    float nextDamageTime;

    void OnTriggerEnter2D(Collider2D other) { TryDamage(other); }
    void OnTriggerStay2D(Collider2D other)  { TryDamage(other); }

    void TryDamage(Collider2D other)
    {
        if (Time.time < nextDamageTime) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

        // Look on this object, its parents, or its children for PlayerHealth
        var health = other.GetComponent<PlayerHealth>()
                  ?? other.GetComponentInParent<PlayerHealth>()
                  ?? other.GetComponentInChildren<PlayerHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);
            nextDamageTime = Time.time + damageCooldown;
            Debug.Log($"[EnemyDamage] Hit {health.name}. New HP: {health.currentHealth}");
        }
    }
}
