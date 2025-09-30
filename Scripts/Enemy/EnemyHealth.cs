using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHP = 3;
    public float invulnTime = 0.15f;

    [Header("On Hit")]
    public float knockbackForce = 6f;
    public Vector2 knockbackDir = new(1f, 0.4f); // relative to attacker→enemy
    public Color flashColor = Color.red;

    int hp;
    float invulnTimer = 0f;
    SpriteRenderer sr;
    Rigidbody2D rb;

    void Awake()
    {
        hp = maxHP;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (invulnTimer > 0f) return;

        hp -= Mathf.Max(1, amount);
        invulnTimer = invulnTime;

        // Small visual flash (optional)
        if (sr) StartCoroutine(HitFlash());

        if (hp <= 0)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void TakeDamageWithKnockback(int amount, Vector2 fromWorldPos)
    {
        TakeDamage(amount);
        if (rb == null) return;

        // Direction away from the hit source
        Vector2 dir = ( (Vector2)transform.position - fromWorldPos ).normalized;
        Vector2 kb = new Vector2(
            Mathf.Sign(dir.x) * Mathf.Abs(knockbackDir.x),
            Mathf.Sign(dir.y) * Mathf.Abs(knockbackDir.y)
        ).normalized * knockbackForce;

        rb.linearVelocity = new Vector2(kb.x, Mathf.Max(rb.linearVelocity.y, kb.y));
    }

    System.Collections.IEnumerator HitFlash()
    {
        if (!sr) yield break;
        var original = sr.color;
        sr.color = flashColor;
        yield return new WaitForSeconds(0.06f);
        sr.color = original;
    }
}
