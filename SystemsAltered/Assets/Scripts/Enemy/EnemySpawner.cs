using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Spawns both real and fake enemies in the arena. Real enemies deal damage
/// and persist; fake enemies are hallucinations that only appear when the
/// player is on a drug with spawnFakeEnemies enabled (e.g. meth), can't
/// damage the player, and pop instantly when shot.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugState;

    [Header("Prefabs")]
    [SerializeField] private GameObject realEnemyPrefab;
    [SerializeField] private GameObject fakeEnemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("How far from a spawn point to search the NavMesh for a valid spawn")]
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Real Enemies")]
    [SerializeField] private int maxRealEnemies = 8;
    [SerializeField] private float realSpawnInterval = 6f;
    [SerializeField] private float initialDelay = 2f;

    [Header("Fake Enemies")]
    [SerializeField] private int maxFakeEnemies = 6;
    [SerializeField] private float fakeSpawnInterval = 3f;
    [Tooltip("Base fake-spawn chance when drug allows it (before drug's fakeEnemyChance is applied)")]
    [Range(0f, 1f)] [SerializeField] private float baseFakeSpawnChance = 1f;

    private readonly List<GameObject> _realEnemies = new();
    private readonly List<GameObject> _fakeEnemies = new();

    private float _nextRealSpawnTime;
    private float _nextFakeSpawnTime;
    private float _spawnMultiplier = 1f;

    private DrugStateData State => drugState != null ? drugState.CurrentState : null;
    private bool FakeSpawnsAllowed => State != null && State.spawnFakeEnemies;

    // --- Lifecycle ---

    private void Start()
    {
        _nextRealSpawnTime = Time.time + initialDelay;
        _nextFakeSpawnTime = Time.time + initialDelay;
    }

    private void Update()
    {
        PruneDestroyed(_realEnemies);
        PruneDestroyed(_fakeEnemies);

        TrySpawnReal();
        TrySpawnFake();
        CullFakesIfDrugEnded();
    }

    // --- Public API ---

    /// <summary>
    /// Called by HallucinationEscalation to ramp fake spawn rate with intensity.
    /// </summary>
    public void SetSpawnMultiplier(float multiplier)
    {
        _spawnMultiplier = Mathf.Max(multiplier, 0.01f);
    }

    // --- Real enemies ---

    private void TrySpawnReal()
    {
        if (realEnemyPrefab == null) return;
        if (_realEnemies.Count >= maxRealEnemies) return;
        if (Time.time < _nextRealSpawnTime) return;

        SpawnAt(realEnemyPrefab, _realEnemies);
        _nextRealSpawnTime = Time.time + realSpawnInterval;
    }

    // --- Fake enemies ---

    private void TrySpawnFake()
    {
        if (fakeEnemyPrefab == null) return;
        if (!FakeSpawnsAllowed) return;
        if (_fakeEnemies.Count >= maxFakeEnemies) return;
        if (Time.time < _nextFakeSpawnTime) return;

        var chance = baseFakeSpawnChance * State.fakeEnemyChance;
        if (Random.value <= chance)
            SpawnAt(fakeEnemyPrefab, _fakeEnemies);

        // Spawn interval shrinks as multiplier grows (meth ramp-up)
        _nextFakeSpawnTime = Time.time + fakeSpawnInterval / _spawnMultiplier;
    }

    private void CullFakesIfDrugEnded()
    {
        if (FakeSpawnsAllowed) return;
        if (_fakeEnemies.Count == 0) return;

        foreach (var fake in _fakeEnemies)
            if (fake != null) Destroy(fake);
        _fakeEnemies.Clear();
    }

    // --- Shared spawning ---

    private void SpawnAt(GameObject prefab, List<GameObject> trackingList)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (point == null) return;

        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            return;

        var enemy = Instantiate(prefab, hit.position, point.rotation);
        trackingList.Add(enemy);
    }

    private static void PruneDestroyed(List<GameObject> list)
    {
        list.RemoveAll(e => e == null);
    }
}