using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns real enemies in the arena. Works alongside FakeEnemySpawner —
/// real enemies deal damage, fake ones don't. The player has to figure
/// out which is which (on meth, they look identical).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int maxEnemies = 8;
    public float spawnInterval = 6f;
    public float initialDelay = 2f;

    [Header("Patrol")]
    [Tooltip("Shared patrol points that spawned enemies will cycle through")]
    public Transform[] sharedPatrolPoints;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private float nextSpawnTime;

    void Start()
    {
        nextSpawnTime = Time.time + initialDelay;
    }

    void Update()
    {
        // Clean up destroyed entries
        activeEnemies.RemoveAll(e => e == null);

        if (activeEnemies.Count >= maxEnemies) return;
        if (Time.time < nextSpawnTime) return;

        SpawnEnemy();
        nextSpawnTime = Time.time + spawnInterval;
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;

        // Pick a random spawn point
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        activeEnemies.Add(enemy);

        // Assign patrol points to the enemy AI
        var ai = enemy.GetComponent<EnemyAI>();
        if (ai != null && sharedPatrolPoints.Length > 0)
        {
            // Give each enemy a randomized subset of patrol points
            // so they don't all walk the same path
            ai.patrolPoints = GetRandomPatrolRoute(3);
        }
    }

    Transform[] GetRandomPatrolRoute(int count)
    {
        if (sharedPatrolPoints.Length <= count)
            return sharedPatrolPoints;

        List<Transform> pool = new List<Transform>(sharedPatrolPoints);
        Transform[] route = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            route[i] = pool[index];
            pool.RemoveAt(index);
        }

        return route;
    }
}