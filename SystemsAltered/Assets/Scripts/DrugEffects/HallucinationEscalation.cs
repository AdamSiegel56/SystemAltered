using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Meth hallucination escalation system. Reads intensity from DrugStateController
/// and drives fake enemy spawn rate, HUD corruption, geometry fracture, and
/// floor crack decals at random positions around the player.
/// </summary>
public class HallucinationEscalation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugState;
    [SerializeField] private HUDController hudController;
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Geometry Fracture")]
    [SerializeField] private Material fractureMaterial;

    [Header("Floor Crack Decals")]
    [SerializeField] private GameObject floorCrackDecalPrefab;
    [Tooltip("Decals spawn randomly within this radius around the player")]
    [SerializeField] private float decalSpawnRadius = 8f;
    [Tooltip("Max active decals at once")]
    [SerializeField] private int maxDecals = 6;
    [Tooltip("How far from the random point to search the NavMesh for a valid spawn")]
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Tuning")]
    [Tooltip("Fake enemy spawn rate scales by (1 + intensity * this)")]
    [SerializeField] private float spawnRateScale = 3f;
    [Tooltip("Minimum intensity delta between decal spawns")]
    [SerializeField] private float decalSpawnCooldown = 0.1f;

    private static readonly int FractureIntensityId = Shader.PropertyToID("_FractureIntensity");

    private float _lastDecalSpawnIntensity;
    private readonly List<GameObject> _activeDecals = new();
    private DrugStateData _lastAppliedState;

    private DrugStateData State => drugState != null ? drugState.CurrentState : null;
    private bool IsEscalating => State != null && State.escalatingHallucinations;
    private float Intensity => drugState != null ? drugState.HallucinationIntensity : 0f;

    // --- Lifecycle ---

    private void Update()
    {
        HandleStateChange();
        if (!IsEscalating) return;

        var intensity = Intensity;
        ApplyFakeEnemyRate(intensity);
        ApplyHudCorruption(intensity);
        ApplyGeometryFracture(intensity);
        TrySpawnDecal(intensity);
    }

    private void HandleStateChange()
    {
        var current = State;
        if (current == _lastAppliedState) return;

        _lastAppliedState = current;
        _lastDecalSpawnIntensity = 0f;

        if (!IsEscalating)
            ResetEffects();
    }

    // --- Reset ---

    private void ResetEffects()
    {
        if (hudController != null) hudController.SetCorruptionLevel(0f);
        if (fractureMaterial != null) fractureMaterial.SetFloat(FractureIntensityId, 0f);
        if (enemySpawner != null) enemySpawner.SetSpawnMultiplier(1f);

        foreach (var decal in _activeDecals)
            if (decal != null) Destroy(decal);
        _activeDecals.Clear();
    }

    // --- Effects ---

    private void ApplyFakeEnemyRate(float intensity)
    {
        if (enemySpawner == null) return;
        enemySpawner.SetSpawnMultiplier(1f + intensity * spawnRateScale);
    }

    private void ApplyHudCorruption(float intensity)
    {
        if (hudController == null) return;
        hudController.SetCorruptionLevel(RemapAboveThreshold(intensity, State.hudCorruptionThreshold));
    }

    private void ApplyGeometryFracture(float intensity)
    {
        if (fractureMaterial == null) return;
        fractureMaterial.SetFloat(
            FractureIntensityId,
            RemapAboveThreshold(intensity, State.geometryFractureThreshold)
        );
    }

    private static float RemapAboveThreshold(float intensity, float threshold)
    {
        if (intensity <= threshold) return 0f;
        return Mathf.Clamp01((intensity - threshold) / (1f - threshold));
    }

    // --- Decals ---

    private void TrySpawnDecal(float intensity)
    {
        _activeDecals.RemoveAll(d => d == null);

        if (floorCrackDecalPrefab == null) return;
        if (_activeDecals.Count >= maxDecals) return;
        if (intensity <= State.geometryFractureThreshold) return;
        if (intensity - _lastDecalSpawnIntensity < decalSpawnCooldown) return;

        SpawnFloorCrack();
        _lastDecalSpawnIntensity = intensity;
    }

    private void SpawnFloorCrack()
    {
        // Pick a random point in a circle around the player, snap to NavMesh
        var randomCircle = Random.insideUnitCircle * decalSpawnRadius;
        var candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            return;

        var spawnPos = hit.position + Vector3.up * 0.01f;
        var spawnRot = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

        var crack = Instantiate(floorCrackDecalPrefab, spawnPos, spawnRot);
        _activeDecals.Add(crack);

        // Auto-destroy when the drug would wear off
        var remaining = State.duration * (1f - drugState.NormalizedProgress) + 1f;
        Destroy(crack, remaining);
    }
}