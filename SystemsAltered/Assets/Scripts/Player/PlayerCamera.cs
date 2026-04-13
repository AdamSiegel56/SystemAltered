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

    // Paranoia camera pull
    private float paranoiaPullStrength;
    private float paranoiaPullYaw;   // target yaw offset from phantom audio
    private float currentPullYaw;

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

        // Apply paranoia pull — subtly drags yaw toward phantom sound direction
        currentPullYaw = Mathf.Lerp(currentPullYaw, paranoiaPullYaw, Time.deltaTime * 2f);
        float pullOffset = currentPullYaw * paranoiaPullStrength * Time.deltaTime;
        yRotation += pullOffset;

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

        // Paranoia settings
        paranoiaPullStrength = state.enableParanoia ? state.paranoiaCameraPull : 0f;
        if (!state.enableParanoia)
        {
            paranoiaPullYaw = 0f;
            currentPullYaw = 0f;
        }

        // Steroid height offset
        targetHeightOffset = state.cameraHeightOffset;
    }

    /// <summary>
    /// Called by ParanoiaSystem to set which direction a phantom sound came from.
    /// yawOffset is in degrees: negative = left, positive = right.
    /// </summary>
    public void SetParanoiaPullTarget(float yawOffset)
    {
        paranoiaPullYaw = yawOffset;
    }

    /// <summary>
    /// Called by RagePullSystem to forcibly rotate camera toward a damage source.
    /// </summary>
    public void ApplyRagePull(Vector3 damageSourceWorld)
    {
        Vector3 dir = (damageSourceWorld - transform.position).normalized;
        float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        yRotation = Mathf.LerpAngle(yRotation, targetYaw, 0.5f);
    }
}