using UnityEngine;
using UnityEngine.Rendering.Universal;

// Handles switching the camera's URP Renderer
// based on the active drug state.
[RequireComponent(typeof(Camera))]
public class DrugRenderController : MonoBehaviour
{
    private UniversalAdditionalCameraData cameraData;

    void Awake()
    {
        // Get URP-specific camera data component
        cameraData = GetComponent<UniversalAdditionalCameraData>();
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
        if (cameraData == null) return;

        // Swap renderer using index defined in ScriptableObject
        cameraData.SetRenderer(state.rendererIndex);
    }
}