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

    // Steroid camera height
    private float baseCameraY;
    private float targetHeightOffset;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        baseCameraY = transform.localPosition.y;
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

        // Steroid camera height offset (lower = feels heavier/bigger)
        Vector3 localPos = transform.localPosition;
        localPos.y = Mathf.Lerp(localPos.y, baseCameraY + targetHeightOffset, Time.deltaTime * 4f);
        transform.localPosition = localPos;
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

        targetHeightOffset = state.cameraHeightOffset;
    }

    /// <summary>
    /// Called by RagePullSystem to forcibly rotate camera toward a damage source.
    /// Uses a strong lerp so the snap is violent and disorienting.
    /// </summary>
    public void ApplyRagePull(Vector3 damageSourceWorld)
    {
        Vector3 dir = (damageSourceWorld - transform.position).normalized;
        float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        yRotation = Mathf.LerpAngle(yRotation, targetYaw, 0.8f);

        // Also pull pitch slightly toward the source
        float targetPitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        xRotation = Mathf.Lerp(xRotation, Mathf.Clamp(targetPitch, -90f, 90f), 0.3f);
    }
}
