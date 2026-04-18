using UnityEngine;

public struct CamInput
{
    public Vector2 Look;
}

public class PlayerCam : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.1f;

    private Vector3 _eulerAngles;
    
    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CamInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
