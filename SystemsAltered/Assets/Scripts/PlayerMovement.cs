using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Transform orientation;
    private Rigidbody rb;

    private DrugStateData currentState;

    [Header("Base")]
    public float baseMoveSpeed = 6f;
    public float groundDrag = 5f;
    public float airMultiplier = 0.4f;

    [Header("Jump")]
    public float baseJumpForce = 5f;
    public float jumpCooldown = 0.25f;
    private bool jumpReady = true;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask layer;
    private bool isGrounded;

    private Vector2 inputDirection;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += ApplyState;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= ApplyState;
    }

    void Update()
    {
        GroundCheck();
        SpeedControl();
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyGravity();
    }

    void ApplyState(DrugStateData state)
    {
        currentState = state;
    }

    void MovePlayer()
    {
        float speed = currentState != null ? currentState.moveSpeed : baseMoveSpeed;

        moveDirection = orientation.forward * inputDirection.y + orientation.right * inputDirection.x;

        if (isGrounded)
            rb.AddForce(moveDirection.normalized * speed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * speed * 10f * airMultiplier, ForceMode.Force);
    }

    void ApplyGravity()
    {
        if (currentState == null) return;

        rb.AddForce(Physics.gravity * (currentState.gravityScale - 1f), ForceMode.Acceleration);
    }

    void GroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, layer);
        rb.linearDamping = isGrounded ? groundDrag : 0;
    }

    void SpeedControl()
    {
        float speed = currentState != null ? currentState.moveSpeed : baseMoveSpeed;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > speed)
        {
            Vector3 limited = flatVel.normalized * speed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        inputDirection = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.action.triggered || !jumpReady || !isGrounded) return;

        float jumpForce = currentState != null ? currentState.jumpForce : baseJumpForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        jumpReady = false;
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    void ResetJump()
    {
        jumpReady = true;
    }
}