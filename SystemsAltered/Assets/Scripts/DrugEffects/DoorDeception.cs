using UnityEngine;

/// <summary>
/// THC door deception: when active, displays an overlay mesh that makes
/// open doors appear closed and closed doors appear open.
/// Attach to each door in the arena.
/// </summary>
public class DoorDeception : MonoBehaviour
{
    [Header("Visual Override")]
    [Tooltip("A fake door mesh that blocks the doorway visually (no collider)")]
    public GameObject fakeClosedOverlay;
    [Tooltip("The real door visual (disabled when door appears 'open' on THC)")]
    public GameObject realDoorVisual;

    [Header("State")]
    public bool isActuallyOpen = false;  // Set by door logic / triggers

    private bool deceptionActive;

    void Start()
    {
        if (fakeClosedOverlay != null)
            fakeClosedOverlay.SetActive(false);

        UpdateVisuals();
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
        deceptionActive = state.invertDoorVisuals;
        UpdateVisuals();
    }

    /// <summary>
    /// Call when door actually opens or closes (gameplay logic).
    /// </summary>
    public void SetOpen(bool open)
    {
        isActuallyOpen = open;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (!deceptionActive)
        {
            // Normal: show real state
            if (realDoorVisual != null) realDoorVisual.SetActive(!isActuallyOpen);
            if (fakeClosedOverlay != null) fakeClosedOverlay.SetActive(false);
        }
        else
        {
            // THC: invert visual state
            // If door is actually open, show fake closed overlay
            // If door is actually closed, hide real visual (looks open)
            if (isActuallyOpen)
            {
                if (realDoorVisual != null) realDoorVisual.SetActive(false);
                if (fakeClosedOverlay != null) fakeClosedOverlay.SetActive(true);
            }
            else
            {
                if (realDoorVisual != null) realDoorVisual.SetActive(false);
                if (fakeClosedOverlay != null) fakeClosedOverlay.SetActive(false);
            }
        }
    }
}