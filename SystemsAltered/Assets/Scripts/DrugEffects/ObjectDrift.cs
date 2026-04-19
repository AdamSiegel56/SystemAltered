using UnityEngine;

/// <summary>
/// THC environmental deception: objects slowly drift from their original
/// positions when outside the player's view frustum.
/// </summary>
public class ObjectDrift : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugState;

    [Header("Drift Settings")]
    [SerializeField] private float maxDriftRadius = 1.5f;
    [Tooltip("How close to the target counts as 'arrived' and a new target is chosen")]
    [SerializeField] private float targetReachedDistance = 0.1f;
    [Tooltip("Viewport margin for visibility test (0 = edge, 0.5 = center)")]
    [SerializeField] private float viewportMargin = 0.05f;

    private Vector3 _originalPosition;
    private Vector3 _driftTarget;
    private Camera _playerCam;
    private DrugStateData _lastAppliedState;

    private DrugStateData State => drugState != null ? drugState.CurrentState : null;
    private bool DriftEnabled => State != null && State.enableObjectDrift;
    private float DriftSpeed => State != null ? State.objectDriftSpeed : 0f;

    // --- Lifecycle ---

    private void Start()
    {
        _originalPosition = transform.position;
        _driftTarget = _originalPosition;
    }

    private void Update()
    {
        HandleStateChange();

        if (!DriftEnabled) return;
        if (!EnsureCamera()) return;
        if (IsVisibleToPlayer()) return;

        DriftTowardTarget();
    }

    // --- State reactions ---

    private void HandleStateChange()
    {
        var current = State;
        if (current == _lastAppliedState) return;

        _lastAppliedState = current;

        // Reset position when drift turns off
        if (!DriftEnabled)
        {
            transform.position = _originalPosition;
            _driftTarget = _originalPosition;
        }
    }

    private bool EnsureCamera()
    {
        if (_playerCam != null) return true;

        _playerCam = Camera.main;
        return _playerCam != null;
    }

    // --- Visibility ---

    private bool IsVisibleToPlayer()
    {
        var viewportPos = _playerCam.WorldToViewportPoint(transform.position);

        return viewportPos.z > 0f
            && viewportPos.x > viewportMargin && viewportPos.x < 1f - viewportMargin
            && viewportPos.y > viewportMargin && viewportPos.y < 1f - viewportMargin;
    }

    // --- Drift ---

    private void DriftTowardTarget()
    {
        if (Vector3.Distance(transform.position, _driftTarget) < targetReachedDistance)
            PickNewDriftTarget();

        transform.position = Vector3.MoveTowards(
            transform.position,
            _driftTarget,
            DriftSpeed * Time.deltaTime
        );
    }

    private void PickNewDriftTarget()
    {
        var offset = Random.insideUnitCircle * maxDriftRadius;
        _driftTarget = _originalPosition + new Vector3(offset.x, 0f, offset.y);
    }
}