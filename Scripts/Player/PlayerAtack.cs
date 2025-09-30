using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Input (Input System)")]
    public InputActionReference attackAction; // Button
    public InputActionReference moveAction;   // Vector2 (fallback facing)
    public InputActionReference aimAction;    // Vector2 (Right Stick). Optional.

    [Header("Slash")]
    public GameObject slashPrefab;
    public float slashOffset = 0.6f;
    public float cooldown = 0.15f;

    [Header("Aim Snapping")]
    [Tooltip("Stick/mouse vertical dominates when |y| >= this.")]
    public float verticalSnap = 0.6f;         // 0.55–0.7 feels good
    [Tooltip("If not vertical, snap to pure left/right when |x| >= this.")]
    public float horizontalSnap = 0.2f;

    [Header("Down-Attack Pogo")]
    public bool allowAirDownAttack = true;    // enable pogo style
    public float pogoUpVelocity = 12f;        // upward velocity to apply on hit
    public float pogoMinReplaceY = 6f;        // if current vy < this, replace with pogoUpVelocity
    public bool lockHorizontalOnPogo = true;  // optional: reduce drift when pogoing

    [Header("Optional")]
    public SpriteRenderer spriteToFlip;
    public Camera worldCamera;

    [Header("Ground Check (for formatting)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    float lastAttackTime = -999f;
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void OnEnable()
    {
        if (attackAction) attackAction.action.Enable();
        if (moveAction)   moveAction.action.Enable();
        if (aimAction)    aimAction.action.Enable();
    }
    void OnDisable()
    {
        if (attackAction) attackAction.action.Disable();
        if (moveAction)   moveAction.action.Disable();
        if (aimAction)    aimAction.action.Disable();
    }

    void Update()
    {
        if (!(attackAction && attackAction.action.WasPressedThisFrame())) return;
        if (Time.time < lastAttackTime + cooldown) return;
        lastAttackTime = Time.time;

        Vector2 dir = GetAimDirSnapped();

        // If aiming down while grounded, format to horizontal (prevents awkward ground-stab)
        if (dir.y < -0.5f && IsGrounded())
            dir = new Vector2(Mathf.Sign(dir.x == 0 ? 1f : dir.x), 0f);

        // If aiming down in air is not allowed, snap to horizontal
        if (dir.y < -0.5f && !allowAirDownAttack && !IsGrounded())
            dir = new Vector2(Mathf.Sign(dir.x == 0 ? 1f : dir.x), 0f);

        SpawnSlash(dir);
    }

    Vector2 GetAimDirSnapped()
    {
        // 1) aim from right stick if present
        Vector2 dir = Vector2.zero;
        if (aimAction != null)
        {
            var v = aimAction.action.ReadValue<Vector2>();
            if (v.sqrMagnitude > 0.04f) dir = v.normalized;
        }

        // 2) mouse fallback
        if (dir == Vector2.zero && Mouse.current != null)
        {
            var cam = worldCamera ? worldCamera : Camera.main;
            if (cam != null)
            {
                Vector2 m = Mouse.current.position.ReadValue();
                Vector3 mw = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, -cam.transform.position.z));
                Vector2 toMouse = (Vector2)(mw - transform.position);
                if (toMouse.sqrMagnitude > 0.001f) dir = toMouse.normalized;
            }
        }

        // 3) movement-facing fallback
        if (dir == Vector2.zero)
        {
            float x = moveAction ? moveAction.action.ReadValue<Vector2>().x : 1f;
            if (spriteToFlip != null && Mathf.Abs(x) < 0.01f) x = spriteToFlip.flipX ? -1f : 1f;
            dir = new Vector2(Mathf.Sign(Mathf.Abs(x) < 0.01f ? 1f : x), 0f);
        }

        // --- Snap formatting ---
        // If strong vertical, snap to pure up/down
        if (Mathf.Abs(dir.y) >= verticalSnap)
            return new Vector2(0f, Mathf.Sign(dir.y));

        // Else snap to pure left/right when x is meaningful
        if (Mathf.Abs(dir.x) >= horizontalSnap)
            return new Vector2(Mathf.Sign(dir.x), 0f);

        // Tiny aim: keep previous facing from sprite
        if (spriteToFlip != null)
            return new Vector2(spriteToFlip.flipX ? -1f : 1f, 0f);

        return Vector2.right;
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void SpawnSlash(Vector2 dir)
    {
        if (!slashPrefab) return;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        Vector3 spawnPos = transform.position + (Vector3)(dir * slashOffset);
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        var slashGO = Instantiate(slashPrefab, spawnPos, rot);

        if (slashGO.TryGetComponent<Slash>(out var slash))
        {
            slash.SetDirection(dir);
            slash.owner = transform;

            // Subscribe to hit callback for pogo
            slash.onHit += _ =>
            {
                if (dir.y < -0.5f && !IsGrounded()) // only pogo when actually doing a down-air attack
                {
                    var v = rb.linearVelocity;
                    if (v.y < pogoMinReplaceY) v.y = pogoUpVelocity; // minimum bounce
                    else v.y = Mathf.Max(v.y, pogoUpVelocity);       // ensure at least this much
                    if (lockHorizontalOnPogo) v.x = 0f;
                    rb.linearVelocity = v;
                }
            };
        }

        if (spriteToFlip != null && Mathf.Abs(dir.x) > 0.01f)
            spriteToFlip.flipX = dir.x < 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
