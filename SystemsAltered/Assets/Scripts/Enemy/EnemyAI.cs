using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrol,
    Chase,
    Attack
}

/// <summary>
/// Core enemy AI. When disguised (player sober), stands completely still.
/// When revealed, patrols by picking random points within a radius (no waypoints
/// needed), chases the player on detection, and shoots bullets from range.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectRange = 15f;
    public float attackRange = 10f;
    public float fieldOfView = 120f;
    public LayerMask obstacleMask;

    [Header("Shooting")]
    public float attackDamage = 8f;
    public float attackCooldown = 1.2f;
    public GameObject enemyBulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 30f;
    public float aimSpread = 0.04f;

    [Header("Attack Visuals")]
    public GameObject muzzleFlashPrefab;

    [Header("Patrol (Random Radius)")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 2f;

    [Header("Disguise (Sober State)")]
    public GameObject realModel;
    public GameObject disguiseModel;

    [Header("Aggression Scaling")]
    public float baseSpeed = 3f;
    public float chaseSpeedMultiplier = 1.5f;

    [Header("Ground Alignment")]
    [Tooltip("Half the capsule height — fixes sinking into floor")]
    public float modelPivotHeight = 1f;

    private NavMeshAgent agent;
    private EnemyState currentState = EnemyState.Patrol;
    private Transform player;
    private RagePullSystem playerRagePull;

    private float patrolWaitTimer;
    private float attackTimer;
    private bool isDisguised;
    private bool playerOnDrugs;
    private Vector3 spawnPosition;

    private float aggressionMultiplier = 1f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = baseSpeed;
        agent.baseOffset = modelPivotHeight;
        spawnPosition = transform.position;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRagePull = playerObj.GetComponent<RagePullSystem>();
        }

        if (disguiseModel != null)
        {
            SetDisguised(true);
        }

        PickRandomPatrolTarget();
    }

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += OnDrugStateChanged;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= OnDrugStateChanged;
    }

    void OnDrugStateChanged(DrugStateData state)
    {
        playerOnDrugs = state.stateType != DrugState.Sober && state.stateType != DrugState.Crash;

        if (disguiseModel != null)
        {
            SetDisguised(!playerOnDrugs);
        }

        switch (state.stateType)
        {
            case DrugState.Cocaine:  aggressionMultiplier = 1.3f; break;
            case DrugState.Meth:     aggressionMultiplier = 1.5f; break;
            case DrugState.Steroids: aggressionMultiplier = 1.2f; break;
            case DrugState.THC:      aggressionMultiplier = 0.8f; break;
            case DrugState.Crash:    aggressionMultiplier = 1.6f; break;
            default:                 aggressionMultiplier = 1f;   break;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Disguised enemies are completely frozen
        if (isDisguised)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrol:  UpdatePatrol(); break;
            case EnemyState.Chase:   UpdateChase();  break;
            case EnemyState.Attack:  UpdateAttack(); break;
        }
    }

    // --- PATROL (Random Radius) ---

    void UpdatePatrol()
    {
        agent.speed = baseSpeed;

        if (CanSeePlayer())
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            patrolWaitTimer -= Time.deltaTime;

            if (patrolWaitTimer <= 0f)
            {
                PickRandomPatrolTarget();
            }
        }
    }

    void PickRandomPatrolTarget()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir.y = 0;
            Vector3 candidate = spawnPosition + randomDir;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                patrolWaitTimer = patrolWaitTime + Random.Range(-0.5f, 1f);
                return;
            }
        }

        // Fallback: go back to spawn
        agent.SetDestination(spawnPosition);
        patrolWaitTimer = patrolWaitTime;
    }

    // --- CHASE ---

    void UpdateChase()
    {
        agent.speed = baseSpeed * chaseSpeedMultiplier * aggressionMultiplier;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange && CanSeePlayer())
        {
            TransitionTo(EnemyState.Attack);
            return;
        }

        if (dist > detectRange * 1.5f || !CanSeePlayer())
        {
            TransitionTo(EnemyState.Patrol);
        }
    }

    // --- ATTACK (Ranged Shooting) ---

    void UpdateAttack()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // Face the player
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 8f
            );

        // Keep some distance — stop moving when in range, chase if too far
        if (dist > attackRange * 1.2f || !CanSeePlayer())
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        // Stand still while shooting
        agent.SetDestination(transform.position);

        if (attackTimer <= 0f)
        {
            Shoot();
            attackTimer = attackCooldown / aggressionMultiplier;
        }
    }

    void Shoot()
    {
        Transform origin = firePoint != null ? firePoint : transform;

        Vector3 dirToPlayer = (player.position + Vector3.up * 0.8f - origin.position).normalized;

        // Add spread
        dirToPlayer += origin.right * Random.Range(-aimSpread, aimSpread);
        dirToPlayer += origin.up * Random.Range(-aimSpread, aimSpread);
        dirToPlayer.Normalize();

        // Spawn bullet
        if (enemyBulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(enemyBulletPrefab, origin.position, Quaternion.identity);
            var bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.speed = bulletSpeed;
                bullet.damage = attackDamage;
                bullet.Init(dirToPlayer);
            }
        }

        // Muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, origin.position, origin.rotation);
            Destroy(flash, 0.1f);
        }
    }

    // --- DETECTION ---

    bool CanSeePlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dist, obstacleMask))
            return false;

        return true;
    }

    // --- DISGUISE ---

    void SetDisguised(bool disguised)
    {
        isDisguised = disguised;

        if (realModel != null)
            realModel.SetActive(!disguised);

        if (disguiseModel != null)
            disguiseModel.SetActive(disguised);
    }

    // --- TRANSITIONS ---

    void TransitionTo(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Patrol)
        {
            PickRandomPatrolTarget();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, patrolRadius);

        Vector3 leftBound = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftBound * detectRange);
        Gizmos.DrawRay(transform.position, rightBound * detectRange);
    }
}