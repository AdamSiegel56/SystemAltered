using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public Transform orientation;
    public Camera cam;

    public float baseSensX = 100f;
    public float baseSensY = 100f;

    private float sensMultiplier = 1f;

    private float xRotation;
    private float yRotation;

    private Vector2 lookInput;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
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
        float xMouse = lookInput.x * Time.deltaTime * baseSensX * sensMultiplier;
        float yMouse = lookInput.y * Time.deltaTime * baseSensY * sensMultiplier;

        yRotation += xMouse;
        xRotation -= yMouse;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0f);
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    void ApplyState(DrugStateData state)
    {
        sensMultiplier = state.lookSensitivity;

        if (cam != null)
            cam.fieldOfView = state.fov;
    }
}