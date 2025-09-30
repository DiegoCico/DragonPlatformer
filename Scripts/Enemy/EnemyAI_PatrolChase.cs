using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyAI_PatrolChase : MonoBehaviour
{
    [Header("Target")]
    public Transform player;                 // if left null, found by tag "Player"

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float accel = 12f;
    public float decel = 14f;
    public float stopDistance = 0.2f;        // stop jittering when basically on top of player

    [Header("Patrol")]
    public float patrolTurnCheckDist = 0.4f; // wall check distance
    public LayerMask groundMask;
    public LayerMask wallMask;
    public bool startFacingRight = true;

    [Header("Chase")]
    public float aggroRadius = 6f;
    public float deaggroRadius = 8f;
    public bool avoidCliffsWhileChasing = true;

    // --- internals ---
    Rigidbody2D rb;
    Vector2 velRef;           // SmoothDamp ref for x-velocity
    int dir = 1;              // +1 right, -1 left
    bool chasing = false;
    Vector3 baseScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        baseScale = transform.localScale;
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

        if (chasing) ChaseStep(dist);
        else PatrolStep();
    }

    void PatrolStep()
    {
        // flip on wall or at ledge
        if (WallAhead() || NoGroundAhead())
            dir = -dir;

        float targetX = dir * moveSpeed;
        float smoothIn = 1f / Mathf.Max(0.0001f, accel);
        float smoothOut = 1f / Mathf.Max(0.0001f, decel);
        float vx = Mathf.SmoothDamp(
            rb.linearVelocity.x,
            targetX,
            ref velRef.x,
            (Mathf.Abs(targetX) > 0.01f ? smoothIn : smoothOut)
        );

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // face move direction
        if (Mathf.Abs(vx) > 0.02f)
            transform.localScale = new Vector3(Mathf.Sign(vx) * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void ChaseStep(float distToPlayer)
    {
        float dx = player.position.x - transform.position.x;

        // close enough; brake to stop jitter
        if (Mathf.Abs(dx) <= stopDistance)
        {
            float stopVx = Mathf.SmoothDamp(rb.linearVelocity.x, 0f, ref velRef.x, 1f / Mathf.Max(0.0001f, decel));
            rb.linearVelocity = new Vector2(stopVx, rb.linearVelocity.y);
            return;
        }

        dir = dx > 0 ? 1 : -1;

        // optionally avoid running off cliffs while chasing
        if (avoidCliffsWhileChasing && NoGroundAhead())
        {
            float brakeVx = Mathf.SmoothDamp(rb.linearVelocity.x, 0f, ref velRef.x, 1f / Mathf.Max(0.0001f, decel));
            rb.linearVelocity = new Vector2(brakeVx, rb.linearVelocity.y);
            return;
        }

        float targetX = dir * moveSpeed;
        float vx = Mathf.SmoothDamp(rb.linearVelocity.x, targetX, ref velRef.x, 1f / Mathf.Max(0.0001f, accel));
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        if (Mathf.Abs(vx) > 0.02f)
            transform.localScale = new Vector3(Mathf.Sign(vx) * Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    bool WallAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(0.2f * dir, 0.1f);
        return Physics2D.Raycast(origin, Vector2.right * dir, patrolTurnCheckDist, wallMask);
    }

    bool NoGroundAhead()
    {
        // cast slightly ahead and down
        Vector2 origin = (Vector2)transform.position + new Vector2(0.25f * dir, 0.05f);
        return !Physics2D.Raycast(origin, Vector2.down, 0.7f, groundMask);
    }

    void OnDrawGizmosSelected()
    {
        // aggro/deaggro radii
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, deaggroRadius);

        // preview rays (uses current dir; safe in edit mode)
        Gizmos.color = Color.red;
        Vector3 wallOrigin = transform.position + new Vector3(0.2f * dir, 0.1f, 0f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector3(patrolTurnCheckDist * dir, 0f, 0f));

        Gizmos.color = Color.cyan;
        Vector3 groundOrigin = transform.position + new Vector3(0.25f * dir, 0.05f, 0f);
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector3.down * 0.7f);
    }
}
