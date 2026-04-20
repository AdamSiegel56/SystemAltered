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

    private Vector3 eulerAngles;
    
    private float SensitivityMult =>
        drugState?.CurrentState?.lookSensitivityMultiplier ?? 1f;
    
    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CamInput input)
    {
        var delta = new Vector3(-input.Look.y, input.Look.x, 0f);
        eulerAngles += delta * sensitivity * SensitivityMult;
        transform.eulerAngles = eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
