using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyAI_PatrolChase : MonoBehaviour
{
    public Transform player; // drag Player here (or we find by tag)
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float accel = 12f;
    public float decel = 14f;

    [Header("Patrol")]
    public float patrolTurnCheckDist = 0.4f; // edge/wall check
    public LayerMask groundMask;
    public LayerMask wallMask;
    public bool startFacingRight = true;

    [Header("Chase")]
    public float aggroRadius = 6f;
    public float deaggroRadius = 8f;

    Rigidbody2D rb;
    Vector2 velRef;
    int dir = 1; // +1 right, -1 left
    bool chasing = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        dir = startFacingRight ? 1 : -1;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (!player)
        {
            PatrolStep();
            return;
        }

        float dist = Vector2.Distance(player.position, transform.position);
        if (!chasing && dist <= aggroRadius) chasing = true;
        if (chasing && dist >= deaggroRadius) chasing = false;

        if (chasing) ChaseStep();
        else PatrolStep();
    }

    void PatrolStep()
    {
        // flip on wall or ledge
        if (WallAhead() || NoGroundAhead()) dir = -dir;

        float targetX = dir * moveSpeed;
        float vx = Mathf.SmoothDamp(rb.linearVelocity.x, targetX, ref velRef.x, (Mathf.Abs(targetX) > 0.01f ? 1f/accel : 1f/decel));
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // face move direction
        if (Mathf.Abs(vx) > 0.02f) transform.localScale = new Vector3(Mathf.Sign(vx), 1f, 1f);
    }

    void ChaseStep()
    {
        float dx = Mathf.Sign(player.position.x - transform.position.x);
        dir = (int)dx;

        float targetX = dx * moveSpeed;
        float vx = Mathf.SmoothDamp(rb.linearVelocity.x, targetX, ref velRef.x, 1f/accel);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        if (Mathf.Abs(vx) > 0.02f) transform.localScale = new Vector3(Mathf.Sign(vx), 1f, 1f);

        // optional: avoid walking off cliffs while chasing
        if (NoGroundAhead()) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    bool WallAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(0.2f * dir, 0.1f);
        return Physics2D.Raycast(origin, Vector2.right * dir, patrolTurnCheckDist, wallMask);
    }

    bool NoGroundAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(0.25f * dir, 0f);
        return !Physics2D.Raycast(origin, Vector2.down, 0.7f, groundMask);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, deaggroRadius);
    }
}
