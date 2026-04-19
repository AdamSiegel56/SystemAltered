using UnityEngine;

/// <summary>
/// THC wall breathing effect. Drives a _BreathingIntensity property
/// on a shared material to make walls pulse via vertex displacement.
/// Attach to any object in the scene (e.g. an empty "EffectsManager").
/// 
/// The material needs a shader with a _BreathingIntensity float property
/// that controls vertex displacement along normals. The script handles
/// the sine wave animation and drug state activation.
/// </summary>
public class BreathingGeometry : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Shared material on walls that has the breathing shader")]
    public Material breathingMaterial;

    [Header("Animation")]
    public float breathingSpeed = 1.5f;

    private static readonly int BreathingIntensity = Shader.PropertyToID("_BreathingIntensity");
    private bool isActive;
    private float amplitude;

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += ApplyState;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= ApplyState;

        // Reset material when disabled
        if (breathingMaterial != null)
            breathingMaterial.SetFloat(BreathingIntensity, 0f);
    }

    void ApplyState(DrugStateData state)
    {
        isActive = state.enableBreathingGeometry;
        amplitude = state.breathingAmplitude;

        if (!isActive && breathingMaterial != null)
            breathingMaterial.SetFloat(BreathingIntensity, 0f);
    }

    void Update()
    {
        if (!isActive || breathingMaterial == null) return;

        // Sine wave pulsing — smooth in and out
        float breath = Mathf.Sin(Time.time * breathingSpeed) * 0.5f + 0.5f;
        breathingMaterial.SetFloat(BreathingIntensity, breath * amplitude);
    }
}