using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Meth hallucination escalation system. Reads intensity from DrugStateController
/// and drives fake enemy spawn rate, HUD corruption, geometry fracture, and
/// floor crack decals at random positions around the player.
/// </summary>
public class HallucinationEscalation : MonoBehaviour
{
    [Header("References")]
    public DrugStateController stateController;
    public HUDController hudController;
    public FakeEnemySpawner fakeEnemySpawner;

    [Header("Geometry Fracture")]
    public Material fractureMaterial;
    private static readonly int FractureIntensity = Shader.PropertyToID("_FractureIntensity");

    [Header("Floor Crack Decals")]
    public GameObject floorCrackDecalPrefab;
    [Tooltip("Decals spawn randomly within this radius around the player")]
    public float decalSpawnRadius = 8f;
    [Tooltip("Max active decals at once")]
    public int maxDecals = 6;
    public LayerMask floorMask;

    private DrugStateData currentState;
    private bool isEscalating;
    private float lastDecalSpawnIntensity;
    private List<GameObject> activeDecals = new List<GameObject>();

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
            if (hudController != null) hudController.SetCorruptionLevel(0f);
            if (fractureMaterial != null) fractureMaterial.SetFloat(FractureIntensity, 0f);
            if (fakeEnemySpawner != null) fakeEnemySpawner.SetSpawnMultiplier(1f);

            // Clean up decals when drug wears off
            foreach (var decal in activeDecals)
            {
                if (decal != null) Destroy(decal);
            }
            activeDecals.Clear();
        }
    }

    void Update()
    {
        if (!isEscalating || currentState == null || stateController == null) return;

        float intensity = stateController.HallucinationIntensity;

        if (fakeEnemySpawner != null)
        {
            fakeEnemySpawner.SetSpawnMultiplier(1f + intensity * 3f);
        }

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

        // Spawn floor crack decals at random positions around the player
        activeDecals.RemoveAll(d => d == null);

        if (intensity > currentState.geometryFractureThreshold
            && intensity - lastDecalSpawnIntensity > 0.1f
            && floorCrackDecalPrefab != null
            && activeDecals.Count < maxDecals)
        {
            SpawnFloorCrack();
            lastDecalSpawnIntensity = intensity;
        }
    }

    void SpawnFloorCrack()
    {
        // Pick a random point on the floor near the player
        Vector2 randomCircle = Random.insideUnitCircle * decalSpawnRadius;
        Vector3 origin = transform.position + new Vector3(randomCircle.x, 5f, randomCircle.y);

        // Raycast down to find the floor
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, floorMask))
        {
            // Spawn slightly above floor to avoid z-fighting
            Vector3 spawnPos = hit.point + hit.normal * 0.01f;
            Quaternion spawnRot = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

            GameObject crack = Instantiate(floorCrackDecalPrefab, spawnPos, spawnRot);
            activeDecals.Add(crack);

            // Auto-destroy when drug wears off
            float remaining = currentState.duration * (1f - stateController.NormalizedProgress) + 1f;
            Destroy(crack, remaining);
        }
    }
}