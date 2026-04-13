using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns and respawns drug pickups around the arena.
/// Picks randomly from available drug states, respects max active count,
/// and respawns at empty points after a delay.
/// </summary>
public class DrugSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject drugPickupPrefab;
    public Transform[] spawnPoints;

    [Header("Drug Pool")]
    [Tooltip("Which drug states can spawn. Weight each by adding duplicates.")]
    public DrugStateData[] drugPool;

    [Header("Timing")]
    public float initialDelay = 1f;
    public float respawnDelay = 8f;
    public int maxActivePickups = 4;

    [Header("Visuals")]
    [Tooltip("Optional: per-drug materials for pickup glow. Index must match drugPool order.")]
    public Material[] drugMaterials;

    private Dictionary<Transform, float> cooldowns = new Dictionary<Transform, float>();
    private List<GameObject> activePickups = new List<GameObject>();
    private float startTime;

    void Start()
    {
        startTime = Time.time + initialDelay;

        foreach (var point in spawnPoints)
        {
            cooldowns[point] = startTime;
        }
    }

    void Update()
    {
        if (Time.time < startTime) return;

        activePickups.RemoveAll(p => p == null);

        if (activePickups.Count >= maxActivePickups) return;

        foreach (var point in spawnPoints)
        {
            if (activePickups.Count >= maxActivePickups) break;

            if (Time.time < cooldowns[point]) continue;

            if (IsPointOccupied(point)) continue;

            SpawnPickup(point);
            cooldowns[point] = Time.time + respawnDelay;
        }
    }

    void SpawnPickup(Transform point)
    {
        if (drugPickupPrefab == null || drugPool.Length == 0) return;

        GameObject pickup = Instantiate(drugPickupPrefab, point.position, point.rotation);

        // Assign random drug from pool
        int index = Random.Range(0, drugPool.Length);
        var drugPickup = pickup.GetComponent<DrugPickup>();
        if (drugPickup != null)
        {
            drugPickup.state = drugPool[index];
        }

        // Apply per-drug material if available
        if (drugMaterials != null && index < drugMaterials.Length && drugMaterials[index] != null)
        {
            var renderer = pickup.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = drugMaterials[index];
            }
        }

        activePickups.Add(pickup);
    }

    bool IsPointOccupied(Transform point)
    {
        foreach (var pickup in activePickups)
        {
            if (pickup != null && Vector3.Distance(pickup.transform.position, point.position) < 1f)
                return true;
        }
        return false;
    }
}