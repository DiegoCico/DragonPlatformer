using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Slash : MonoBehaviour
{
    [Header("Lifetime / Motion")]
    public float lifetime = 0.12f;      // how long the hitbox exists
    public float forwardSpeed = 0.0f;   // small push forward (0 = stays put)

    [Header("Damage")]
    public int damage = 1;
    public LayerMask damageLayers;      // which layers can be hit (e.g., Enemy)
    public bool destroyOnHit = true;

    [Header("Meta")]
    public Transform owner;             // who spawned this slash (optional)
    public System.Action<Collider2D> onHit; // callback fired when we hit something

    // Internal
    Vector2 dir = Vector2.right;
    float timer;

    public void SetDirection(Vector2 d)
    {
        if (d.sqrMagnitude > 0.0001f)
            dir = d.normalized;
    }

    void Awake()
    {
        // Ensure trigger collider
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        timer = lifetime;
    }

    void Update()
    {
        // Optional forward lunge
        if (forwardSpeed > 0f)
            transform.position += (Vector3)(dir * forwardSpeed * Time.deltaTime);

        // Expire
        timer -= Time.deltaTime;
        if (timer <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) => TryDamage(other);
    void OnTriggerStay2D(Collider2D other)  => TryDamage(other);

    void TryDamage(Collider2D other)
    {
        // Respect layer mask
        if (((1 << other.gameObject.layer) & damageLayers) == 0) return;

        // EnemyHealth with knockback
        EnemyHealth eh = other.GetComponent<EnemyHealth>() ??
                         other.GetComponentInParent<EnemyHealth>() ??
                         other.GetComponentInChildren<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamageWithKnockback(damage, transform.position);
            onHit?.Invoke(other); // notify player
            if (destroyOnHit) Destroy(gameObject);
            return;
        }

        // Generic IDamageable fallback
        IDamageable dmg = other.GetComponent<IDamageable>() ??
                          other.GetComponentInParent<IDamageable>() ??
                          other.GetComponentInChildren<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            onHit?.Invoke(other); // notify player
            if (destroyOnHit) Destroy(gameObject);
        }
    }
}

// Example interface you can use in your enemies:
public interface IDamageable
{
    void TakeDamage(int amount);
}
