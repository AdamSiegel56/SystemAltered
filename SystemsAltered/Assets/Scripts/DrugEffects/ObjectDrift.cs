using UnityEngine;

/// <summary>
/// THC environmental deception: objects slowly drift from their original
/// positions when outside the player's view frustum.
/// Attach to any driftable object in the arena.
/// </summary>
public class ObjectDrift : MonoBehaviour
{
    [Header("Drift Settings")]
    public float maxDriftRadius = 1.5f;

    private Vector3 originalPosition;
    private Vector3 driftTarget;
    private float driftSpeed;
    private bool driftEnabled;

    private Camera playerCam;

    void Start()
    {
        originalPosition = transform.position;
        driftTarget = originalPosition;
    }

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += ApplyState;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= ApplyState;
    }

    void ApplyState(DrugStateData state)
    {
        driftEnabled = state.enableObjectDrift;
        driftSpeed = state.objectDriftSpeed;

        if (!driftEnabled)
        {
            transform.position = originalPosition;
            driftTarget = originalPosition;
        }
    }

    void Update()
    {
        if (!driftEnabled) return;

        // Lazy-find camera (Camera.main can be null on first frame)
        if (playerCam == null)
        {
            playerCam = Camera.main;
            if (playerCam == null) return;
        }

        // Check visibility using viewport + distance
        Vector3 viewportPos = playerCam.WorldToViewportPoint(transform.position);
        bool isVisible = viewportPos.z > 0f
                         && viewportPos.x > 0.05f && viewportPos.x < 0.95f
                         && viewportPos.y > 0.05f && viewportPos.y < 0.95f;

        if (isVisible) return;

        // Not visible — drift toward target
        if (Vector3.Distance(transform.position, driftTarget) < 0.1f)
        {
            Vector2 offset = Random.insideUnitCircle * maxDriftRadius;
            driftTarget = originalPosition + new Vector3(offset.x, 0, offset.y);
        }

        transform.position = Vector3.MoveTowards(
            transform.position, driftTarget, driftSpeed * Time.deltaTime
        );
    }
}
