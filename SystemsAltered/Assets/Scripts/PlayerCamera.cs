using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{

    public Transform orientation;

    public float sensX;
    public float sensY;

    private float xMouse;
    private float yMouse;

    private float xRotation;
    private float yRotation;

    private Vector2 lookPos;
    private Vector2 moveDir;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        xMouse = lookPos.x * Time.deltaTime * sensX;
        yMouse = lookPos.y * Time.deltaTime * sensY;

        yRotation += xMouse;
        xRotation -= yMouse;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0f);



    }


    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookPos = ctx.ReadValue<Vector2>();
    }

}
