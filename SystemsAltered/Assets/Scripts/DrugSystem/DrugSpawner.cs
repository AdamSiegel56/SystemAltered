using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and respawns drug pickups around the arena.
/// Each drug entry pairs a state with its material so the right
/// visual always matches the right drug regardless of pool weighting.
/// </summary>
public class DrugSpawner : MonoBehaviour
{

    [Header("Spawning")]
    public Transform[] spawnPoints;

    [Header("Drug Pool")]
    public GameObject[] drugs;

    [Header("Timing")]
    public float initialDelay = 1f;
    public float respawnDelay = 8f;
    public int maxActivePickups = 4;

    [Header("Overlap Check")]
    [Tooltip("Radius to check for existing pickups before spawning")]
    public float overlapCheckRadius = 1.5f;

    private Dictionary<Transform, GameObject> pointToPickup = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, float> cooldowns = new Dictionary<Transform, float>();
    private float startTime;

    void Start()
    {
        startTime = Time.time + initialDelay;

        foreach (var point in spawnPoints)
        {
            cooldowns[point] = startTime;
            pointToPickup[point] = null;
        }
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
        GameObject spawnedDrug = drugs[Random.Range(0, drugs.Length)];
        
        Instantiate(spawnedDrug.gameObject, point.position, point.rotation);

        pointToPickup[point] = spawnedDrug;
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