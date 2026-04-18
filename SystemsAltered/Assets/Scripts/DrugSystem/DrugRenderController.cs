using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Single authority for URP renderer index swapping.
/// Handles drug state changes AND damage flashes. No other script
/// should call cameraData.SetRenderer directly.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DrugRenderController : MonoBehaviour
{
    [Header("Damage Flash")]
    [Tooltip("URP Renderer List index for the damage indicator shader")]
    public int damageRendererIndex = 3;
    public float damageFlashDuration = 0.2f;
    public float falseDamageFlashDuration = 0.15f;

    private UniversalAdditionalCameraData cameraData;
    private int currentDrugRendererIndex;
    private Coroutine flashRoutine;

    void Awake()
    {
        cameraData = GetComponent<UniversalAdditionalCameraData>();
    }

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += ApplyDrugState;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= ApplyDrugState;
    }

    void ApplyDrugState(DrugStateData state)
    {
        currentDrugRendererIndex = state.rendererIndex;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        
        if (cameraData != null)
        {
            cameraData.SetRenderer(currentDrugRendererIndex);
            Debug.Log($"[DrugRenderController] Set renderer to index {currentDrugRendererIndex} for state {state.stateType} ({state.name})");
        }
    }

    /// <summary>
    /// Called by PlayerHealth when the player takes real damage.
    /// </summary>
    public void FlashDamage()
    {
        if (cameraData == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(damageFlashDuration));
    }

    /// <summary>
    /// Called by HUDController for false meth damage flashes.
    /// </summary>
    public void FlashFalseDamage()
    {
        if (cameraData == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(falseDamageFlashDuration));
    }

    IEnumerator FlashRoutine(float duration)
    {
        cameraData.SetRenderer(damageRendererIndex);
        yield return new WaitForSeconds(duration);
        // Always restore to the CURRENT drug renderer, not a cached value
        cameraData.SetRenderer(currentDrugRendererIndex);
        flashRoutine = null;
    }
}