using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns fake (hallucination) enemies during meth state.
/// Fake enemies look and move like real enemies but deal no damage
/// and bullets pass through them with a visual pop.
/// </summary>
public class FakeEnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject fakeEnemyPrefab;
    public Transform[] spawnPoints;
    public float baseSpawnInterval = 4f;
    public int maxFakeEnemies = 6;

    private float spawnMultiplier = 1f;
    private float nextSpawnTime;
    private bool active;

    private List<GameObject> activeFakes = new List<GameObject>();

    private DrugStateData currentState;

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
        active = state.spawnFakeEnemies;

        if (!active)
        {
            ClearAllFakes();
        }
    }

    void Update()
    {
        if (!active || fakeEnemyPrefab == null || spawnPoints == null) return;

        // Clean up destroyed entries
        activeFakes.RemoveAll(f => f == null);

        if (activeFakes.Count >= maxFakeEnemies) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnFake();
            nextSpawnTime = Time.time + (baseSpawnInterval / spawnMultiplier);
        }
    }

    public void SetSpawnMultiplier(float multiplier)
    {
        spawnMultiplier = Mathf.Max(0.1f, multiplier);
    }

    void SpawnFake()
    {
        if (spawnPoints.Length == 0) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject fake = Instantiate(fakeEnemyPrefab, point.position, point.rotation);
        activeFakes.Add(fake);
    }

    void ClearAllFakes()
    {
        foreach (var fake in activeFakes)
        {
            if (fake != null) Destroy(fake);
        }
        activeFakes.Clear();
    }
}