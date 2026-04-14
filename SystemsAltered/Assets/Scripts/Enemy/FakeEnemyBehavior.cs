using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Behavior for hallucination enemies. Mimics real enemy movement
/// and has a fake health bar that always shows full.
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
    public float groundRayOriginHeight = 3f;
    public float groundRayDistance = 6f;
    public float groundYOffset = 0f;
    public LayerMask groundMask;

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
        SnapToGround();

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

    void SnapToGround()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundRayOriginHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + groundYOffset;
            transform.position = pos;
        }
    }
}
