using UnityEngine;

/// <summary>
/// THC environmental deception: objects slowly drift from their original
/// positions when outside the player's view frustum, creating the feeling
/// that the room rearranges itself when you're not looking.
/// Attach to any driftable object in the arena.
/// </summary>
public class ObjectDrift : MonoBehaviour
{
    private Vector3 originalPosition;
    private Vector3 driftTarget;
    private float driftSpeed;
    private bool driftEnabled;
    private float maxDriftRadius = 1.5f;

    private Camera playerCam;

    void Start()
    {
        originalPosition = transform.position;
        driftTarget = originalPosition;
        playerCam = Camera.main;
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
            // Snap back to original position when drug wears off
            transform.position = originalPosition;
            driftTarget = originalPosition;
        }
    }

    void Update()
    {
        if (!driftEnabled || playerCam == null) return;

        // Check if this object is visible to the player
        Vector3 viewportPos = playerCam.WorldToViewportPoint(transform.position);
        bool isVisible = viewportPos.z > 0
                         && viewportPos.x > -0.1f && viewportPos.x < 1.1f
                         && viewportPos.y > -0.1f && viewportPos.y < 1.1f;

        if (isVisible)
        {
            // Player is watching — freeze in place
            return;
        }

        // Not visible — drift toward target
        if (Vector3.Distance(transform.position, driftTarget) < 0.1f)
        {
            // Pick new drift target within radius of original position
            Vector2 offset = Random.insideUnitCircle * maxDriftRadius;
            driftTarget = originalPosition + new Vector3(offset.x, 0, offset.y);
        }

        transform.position = Vector3.MoveTowards(
            transform.position, driftTarget, driftSpeed * Time.deltaTime
        );
    }
}