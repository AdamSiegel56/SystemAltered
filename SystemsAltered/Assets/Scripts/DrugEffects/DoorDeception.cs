using UnityEngine;

/// <summary>
/// THC door deception: when active, displays an overlay mesh that makes
/// open doors appear closed and closed doors appear open.
/// </summary>
public class DoorDeception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugState;

    [Header("Visual Override")]
    [Tooltip("A fake door mesh that blocks the doorway visually (no collider)")]
    [SerializeField] private GameObject fakeClosedOverlay;
    [Tooltip("The real door visual (disabled when door appears 'open' on THC)")]
    [SerializeField] private GameObject realDoorVisual;

    [Header("State")]
    [SerializeField] private bool isActuallyOpen;

    private DrugStateData _lastAppliedState;

    private bool DeceptionActive =>
        drugState?.CurrentState?.invertDoorVisuals ?? false;

    // --- Lifecycle ---

    private void Start()
    {
        if (fakeClosedOverlay != null) fakeClosedOverlay.SetActive(false);
        UpdateVisuals();
    }

    private void Update()
    {
        // Re-apply visuals when the drug state changes
        var current = drugState != null ? drugState.CurrentState : null;
        if (current == _lastAppliedState) return;

        _lastAppliedState = current;
        UpdateVisuals();
    }

    // --- Public API ---

    /// <summary>
    /// Call when door actually opens or closes (gameplay logic).
    /// </summary>
    public void SetOpen(bool open)
    {
        isActuallyOpen = open;
        UpdateVisuals();
    }

    // --- Visual logic ---

    private void UpdateVisuals()
    {
        if (DeceptionActive)
            ApplyDeceivedVisuals();
        else
            ApplyNormalVisuals();
    }

    private void ApplyNormalVisuals()
    {
        SetVisual(realDoorVisual, !isActuallyOpen);
        SetVisual(fakeClosedOverlay, false);
    }

    private void ApplyDeceivedVisuals()
    {
        if (isActuallyOpen)
        {
            // Door is open but appears closed — show fake overlay
            SetVisual(realDoorVisual, false);
            SetVisual(fakeClosedOverlay, true);
        }
        else
        {
            // Door is closed but appears open — hide everything
            SetVisual(realDoorVisual, false);
            SetVisual(fakeClosedOverlay, false);
        }
    }

    private static void SetVisual(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}