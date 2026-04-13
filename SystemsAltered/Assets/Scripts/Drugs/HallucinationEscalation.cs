using UnityEngine;

/// <summary>
/// Meth hallucination escalation system. Reads intensity from DrugStateController
/// and drives fake enemy spawn rate, HUD corruption, and geometry fracture activation.
/// Attach to the player alongside DrugStateController.
/// </summary>
public class HallucinationEscalation : MonoBehaviour
{
    [Header("References")]
    public DrugStateController stateController;
    public HUDController hudController;           // Reference to corrupt the HUD
    public FakeEnemySpawner fakeEnemySpawner;     // Reference to control spawn rate

    [Header("Geometry Fracture")]
    public Material fractureMaterial;              // Material with vertex displacement shader
    private static readonly int FractureIntensity = Shader.PropertyToID("_FractureIntensity");

    [Header("Decals")]
    public GameObject floorCrackDecalPrefab;
    public Transform[] decalSpawnPoints;

    private DrugStateData currentState;
    private bool isEscalating;
    private float lastDecalSpawnIntensity;

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
        currentState = state;
        isEscalating = state.escalatingHallucinations;
        lastDecalSpawnIntensity = 0f;

        if (!isEscalating)
        {
            // Reset all escalation effects
            if (hudController != null) hudController.SetCorruptionLevel(0f);
            if (fractureMaterial != null) fractureMaterial.SetFloat(FractureIntensity, 0f);
            if (fakeEnemySpawner != null) fakeEnemySpawner.SetSpawnMultiplier(1f);
        }
    }

    void Update()
    {
        if (!isEscalating || currentState == null || stateController == null) return;

        float intensity = stateController.HallucinationIntensity;

        // Fake enemy spawn rate scales with intensity
        if (fakeEnemySpawner != null)
        {
            fakeEnemySpawner.SetSpawnMultiplier(1f + intensity * 3f);
        }

        // HUD corruption kicks in past threshold
        if (hudController != null)
        {
            float hudCorruption = 0f;
            if (intensity > currentState.hudCorruptionThreshold)
            {
                float t = (intensity - currentState.hudCorruptionThreshold)
                          / (1f - currentState.hudCorruptionThreshold);
                hudCorruption = Mathf.Clamp01(t);
            }
            hudController.SetCorruptionLevel(hudCorruption);
        }

        // Geometry fracture past threshold
        if (fractureMaterial != null)
        {
            float fracture = 0f;
            if (intensity > currentState.geometryFractureThreshold)
            {
                float t = (intensity - currentState.geometryFractureThreshold)
                          / (1f - currentState.geometryFractureThreshold);
                fracture = Mathf.Clamp01(t);
            }
            fractureMaterial.SetFloat(FractureIntensity, fracture);
        }

        // Spawn floor crack decals at high intensity
        if (intensity > currentState.geometryFractureThreshold
            && intensity - lastDecalSpawnIntensity > 0.1f
            && floorCrackDecalPrefab != null
            && decalSpawnPoints != null
            && decalSpawnPoints.Length > 0)
        {
            SpawnFloorCrack();
            lastDecalSpawnIntensity = intensity;
        }
    }

    void SpawnFloorCrack()
    {
        Transform point = decalSpawnPoints[Random.Range(0, decalSpawnPoints.Length)];
        GameObject crack = Instantiate(floorCrackDecalPrefab, point.position, point.rotation);
        // Decals auto-destroy after the drug wears off (tied to state duration)
        Destroy(crack, currentState.duration - (stateController.NormalizedProgress * currentState.duration) + 1f);
    }
}