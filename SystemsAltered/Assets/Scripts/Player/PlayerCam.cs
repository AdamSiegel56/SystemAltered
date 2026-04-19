using UnityEngine;

public struct CamInput
{
    public Vector2 Look;
}

public class PlayerCam : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.1f;

    [Header("References")]
    [SerializeField] private DrugStateController drugState;

    private Vector3 _eulerAngles;

    private float SensitivityMult =>
        drugState?.CurrentState?.lookSensitivityMultiplier ?? 1f;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CamInput input)
    {
        var delta = new Vector3(-input.Look.y, input.Look.x, 0f);
        _eulerAngles += delta * sensitivity * SensitivityMult;
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }

    /// <summary>
    /// Forcibly snap the camera to look at a world position.
    /// Used by RagePullSystem during steroids rage-pull.
    /// </summary>
    public void ApplyRagePull(Vector3 worldTarget)
    {
        var direction = worldTarget - transform.position;
        if (direction.sqrMagnitude < 0.001f) return;

        var targetRotation = Quaternion.LookRotation(direction);
        var euler = targetRotation.eulerAngles;

        // Keep yaw (x input) and pitch (y input) in sync with our tracked angles
        _eulerAngles = new Vector3(euler.x, euler.y, 0f);
        transform.eulerAngles = _eulerAngles;
    }
}