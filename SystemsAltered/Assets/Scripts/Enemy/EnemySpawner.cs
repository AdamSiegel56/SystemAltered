using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns real enemies in the arena. Works alongside FakeEnemySpawner —
/// real enemies deal damage, fake ones don't. Enemies patrol using
/// random radius around their spawn point (no waypoints needed).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int maxEnemies = 8;
    public float spawnInterval = 6f;
    public float initialDelay = 2f;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private float nextSpawnTime;

    void Start()
    {
        nextSpawnTime = Time.time + initialDelay;
    }

    void Update()
    {
        activeEnemies.RemoveAll(e => e == null);

        if (activeEnemies.Count >= maxEnemies) return;
        if (Time.time < nextSpawnTime) return;

        SpawnEnemy();
        nextSpawnTime = Time.time + spawnInterval;
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        activeEnemies.Add(enemy);
    }
}