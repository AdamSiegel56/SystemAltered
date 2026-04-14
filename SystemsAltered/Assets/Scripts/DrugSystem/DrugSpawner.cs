using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and respawns drug pickups around the arena.
/// Each drug entry pairs a state with its material so the right
/// visual always matches the right drug regardless of pool weighting.
/// </summary>
public class DrugSpawner : MonoBehaviour
{
    [System.Serializable]
    public class DrugEntry
    {
        public DrugStateData state;
        public Material material;
        [Tooltip("Higher = more likely to spawn. 1 = normal, 2 = twice as likely.")]
        public int weight = 1;
    }

    [Header("Spawning")]
    public GameObject drugPickupPrefab;
    public Transform[] spawnPoints;

    [Header("Drug Pool")]
    public DrugEntry[] drugs;

    [Header("Timing")]
    public float initialDelay = 1f;
    public float respawnDelay = 8f;
    public int maxActivePickups = 4;

    [Header("Overlap Check")]
    [Tooltip("Radius to check for existing pickups before spawning")]
    public float overlapCheckRadius = 1.5f;

    private Dictionary<Transform, GameObject> pointToPickup = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, float> cooldowns = new Dictionary<Transform, float>();
    private DrugEntry[] weightedPool;
    private float startTime;

    void Start()
    {
        startTime = Time.time + initialDelay;

        foreach (var point in spawnPoints)
        {
            cooldowns[point] = startTime;
            pointToPickup[point] = null;
        }

        BuildWeightedPool();
    }

    void BuildWeightedPool()
    {
        List<DrugEntry> pool = new List<DrugEntry>();
        foreach (var entry in drugs)
        {
            for (int i = 0; i < Mathf.Max(1, entry.weight); i++)
                pool.Add(entry);
        }
        weightedPool = pool.ToArray();
    }

    void Update()
    {
        if (Time.time < startTime) return;

        int activeCount = 0;
        foreach (var point in spawnPoints)
        {
            if (pointToPickup[point] != null)
                activeCount++;
            else
                pointToPickup[point] = null;
        }

        if (activeCount >= maxActivePickups) return;

        foreach (var point in spawnPoints)
        {
            if (activeCount >= maxActivePickups) break;
            if (pointToPickup[point] != null) continue;
            if (Time.time < cooldowns[point]) continue;
            if (IsPointOccupied(point)) continue;

            SpawnPickup(point);
            cooldowns[point] = Time.time + respawnDelay;
            activeCount++;
        }
    }

    void SpawnPickup(Transform point)
    {
        if (drugPickupPrefab == null || weightedPool.Length == 0) return;

        DrugEntry entry = weightedPool[Random.Range(0, weightedPool.Length -1)];

        GameObject pickup = Instantiate(drugPickupPrefab, point.position, point.rotation);

        var drugPickup = pickup.GetComponent<DrugPickup>();
        if (drugPickup != null)
        {
            drugPickup.state = entry.state;
        }

        if (entry.material != null)
        {
            var renderer = pickup.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = entry.material;
            }
        }

        pointToPickup[point] = pickup;
    }

    bool IsPointOccupied(Transform point)
    {
        Collider[] hits = Physics.OverlapSphere(point.position, overlapCheckRadius);

        foreach (var hit in hits)
        {
            if (hit.GetComponent<DrugPickup>() != null)
                return true;
        }

        return false;
    }
}