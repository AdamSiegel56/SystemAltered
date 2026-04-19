using UnityEngine;

/// <summary>
/// THC wall breathing effect. Drives a _BreathingIntensity property
/// on a shared material to make walls pulse via vertex displacement.
/// </summary>
public class BreathingGeometry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugState;
    [Tooltip("Shared material on walls that has the breathing shader")]
    [SerializeField] private Material breathingMaterial;

    [Header("Animation")]
    [SerializeField] private float breathingSpeed = 1.5f;

    private static readonly int BreathingIntensityId = Shader.PropertyToID("_BreathingIntensity");

    private DrugStateData State => drugState != null ? drugState.CurrentState : null;
    private bool IsActive => State != null && State.enableBreathingGeometry;
    private float Amplitude => State != null ? State.breathingAmplitude : 0f;

    private void Update()
    {
        if (breathingMaterial == null) return;

        if (!IsActive)
        {
            breathingMaterial.SetFloat(BreathingIntensityId, 0f);
            return;
        }

        // Sine wave pulsing — smooth in and out, remapped to 0..1
        var breath = Mathf.Sin(Time.time * breathingSpeed) * 0.5f + 0.5f;
        breathingMaterial.SetFloat(BreathingIntensityId, breath * Amplitude);
    }

    private void OnDisable()
    {
        if (breathingMaterial != null)
            breathingMaterial.SetFloat(BreathingIntensityId, 0f);
    }
}