using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Transform orientation;
    Rigidbody rb;

    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
        


    private float horizontalInput;
    private float verticalInput;
    Vector3 moveDirection;
    private Vector2 inputDirection;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool jumpReady;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask layer;
    bool isGrounded;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb= GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        jumpReady = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MovePlayer();
    }

    private void Update()
    {
        GroundCheck();
        SpeedControl();
    }

    public void MovePlayer()
    {
        moveDirection = orientation.forward * inputDirection.y + orientation.right * inputDirection.x;

        if (isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    public void GroundCheck()
    {
        //Checks for ground and applies drag if you are grounded.

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, layer);

        if (isGrounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        //if you go faster than your movement speed, you don't now.
        if(flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    public void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    public void ResetJump()
    {
        jumpReady = true;
    }

    //GettingInputs:
    public void OnMove(InputAction.CallbackContext ctx)
    {
        inputDirection = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if(ctx.action.triggered)
        {
            Debug.Log("jump");
        }

        if(jumpReady && isGrounded && ctx.action.triggered)
        {
            Jump();
            jumpReady = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

}
