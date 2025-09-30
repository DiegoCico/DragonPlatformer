using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SmoothPlatformerMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float accelerationTime = 0.08f;
    public float decelerationTime = 0.12f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    [Tooltip("Small grace period after a valid ground contact (sec).")]
    public float groundCoyote = 0.08f;

    [Header("Input System")]
    public InputActionReference moveAction;  // Vector2 (use X)
    public InputActionReference jumpAction;  // Button (Space)

    [Header("Layers")]
    public LayerMask groundLayer;            // set to your Ground layer(s)

    // Internal
    Rigidbody2D rb;
    Vector2 velRef;
    float groundedTimer = 0f;   // >0 means grounded recently
    bool readyToJump = true;    // re-armed when groundedTimer > 0
    bool jumpHeld = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnEnable()
    {
        if (moveAction) moveAction.action.Enable();
        if (jumpAction) jumpAction.action.Enable();
    }
    void OnDisable()
    {
        if (moveAction) moveAction.action.Disable();
        if (jumpAction) jumpAction.action.Disable();
    }

    void Update()
    {
        // Track button state (also works if you add Gamepad South)
        if (jumpAction)
        {
            jumpHeld = jumpAction.action.IsPressed();
        }
        else
        {
            var kb = Keyboard.current;
            jumpHeld = kb != null && kb.spaceKey.isPressed;
        }

        // Re-arm jump whenever we have ground contact recently
        if (groundedTimer > 0f) readyToJump = true;

        // Queue jump on press if grounded and re-armed
        bool pressedThisFrame = jumpAction
            ? jumpAction.action.WasPressedThisFrame()
            : (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false);

        if (pressedThisFrame && groundedTimer > 0f && readyToJump)
        {
            // Apply immediately in Update so physics doesn’t overwrite before FixedUpdate
            var v = rb.linearVelocity;
            v.y = Mathf.Max(v.y, jumpForce);
            rb.linearVelocity = v;

            readyToJump = false;     // lock until we touch ground again
        }

        // Decay ground timer
        groundedTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Horizontal smoothing
        float inputX = 0f;
        if (moveAction) inputX = moveAction.action.ReadValue<Vector2>().x;
        else
        {
            var kb = Keyboard.current;
            if (kb != null)
                inputX = (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? -1f : 0f) +
                         (kb.dKey.isPressed || kb.rightArrowKey.isPressed ?  1f : 0f);
        }

        bool moving = Mathf.Abs(inputX) > 0.01f;
        float t = moving ? accelerationTime : decelerationTime;

        float targetX = inputX * moveSpeed;
        float vx = Mathf.SmoothDamp(rb.linearVelocity.x, targetX, ref velRef.x, t);
        float vy = rb.linearVelocity.y;

        // Better gravity / variable jump height
        if (rb.linearVelocity.y < 0f)
            vy += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            vy += Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(vx, vy);
    }

    // ------- Robust grounding via contacts on Ground layer -------
    void OnCollisionStay2D(Collision2D col)
    {
        if (((1 << col.collider.gameObject.layer) & groundLayer) == 0) return;

        // Check any contact that looks like "standing on"
        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f) // upward facing surface
            {
                groundedTimer = groundCoyote; // refresh
                break;
            }
        }
    }
    void OnCollisionEnter2D(Collision2D col) => OnCollisionStay2D(col);
}
