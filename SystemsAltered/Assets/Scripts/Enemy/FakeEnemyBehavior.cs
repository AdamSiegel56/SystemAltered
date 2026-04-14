using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Behavior for hallucination enemies. Mimics real enemy movement
/// and has a fake health bar that always shows full.
///
/// Setup:
/// 1. Duplicate your real Enemy prefab
/// 2. Remove: EnemyAI, EnemyHealth
/// 3. Keep: NavMeshAgent, Collider (set Is Trigger = true), all visual children
///    INCLUDING the HealthBar child canvas
/// 4. Add: FakeEnemyBehavior
/// 5. Set Tag to "FakeEnemy"
/// 6. Set modelPivotHeight to match the real enemy
/// 7. Drag the HealthBar child into the fakeHealthBar slot
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class FakeEnemyBehavior : MonoBehaviour
{
    [Header("Movement")]
    public float wanderRadius = 12f;
    public float wanderInterval = 3f;
    public float moveSpeed = 3f;

    [Header("Fake Charge")]
    [Tooltip("Chance per wander cycle to charge at the player instead")]
    public float chargeChance = 0.25f;
    public float chargeSpeed = 6f;
    public float chargeStopDistance = 2f;

    [Header("Ground Alignment")]
    [Tooltip("Same as real enemy — half capsule height")]
    public float modelPivotHeight = 1f;

    [Header("Fake Health Bar")]
    public HealthBar fakeHealthBar;

    private NavMeshAgent agent;
    private Transform player;
    private float nextWanderTime;
    private bool isCharging;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.baseOffset = modelPivotHeight;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Fake health bar always shows full — sells the illusion
        if (fakeHealthBar != null)
            fakeHealthBar.SetFill(1f);

        PickNewWanderTarget();
    }

    void Update()
    {
        if (agent.pathPending) return;

        if (isCharging)
        {
            if (player != null)
            {
                agent.SetDestination(player.position);

                float dist = Vector3.Distance(transform.position, player.position);
                if (dist < chargeStopDistance)
                {
                    isCharging = false;
                    agent.speed = moveSpeed;
                    nextWanderTime = Time.time + 1f;
                }
            }
            return;
        }

        if (agent.remainingDistance < 0.5f || Time.time >= nextWanderTime)
        {
            if (player != null && Random.value < chargeChance)
            {
                isCharging = true;
                agent.speed = chargeSpeed;
                agent.SetDestination(player.position);
            }
            else
            {
                PickNewWanderTarget();
            }
        }
    }

    void PickNewWanderTarget()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;
        randomDir.y = transform.position.y;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        nextWanderTime = Time.time + wanderInterval + Random.Range(-1f, 1f);
    }
}