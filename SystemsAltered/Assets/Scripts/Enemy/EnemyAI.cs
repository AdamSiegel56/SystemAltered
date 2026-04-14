using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrol,
    Chase,
    Attack
}

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
    public GameObject hitbox;
    
    public GameObject disguiseModel;

    [Header("Aggression Scaling")]
    public float baseSpeed = 3f;
    public float chaseSpeedMultiplier = 1.5f;

    [Header("Ground Alignment")]
    public float groundRayOriginHeight = 3f;
    public float groundRayDistance = 6f;
    public float groundYOffset = 0f;
    public LayerMask groundMask;

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
    private float lostPlayerTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = baseSpeed;
        spawnPosition = transform.position;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRagePull = playerObj.GetComponent<RagePullSystem>();
        }

        if (disguiseModel != null)
            SetDisguised(true);

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
            SetDisguised(!playerOnDrugs);

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

        if (isDisguised)
        {
            agent.isStopped = true;
            SnapModelToGround();
            return;
        }

        agent.isStopped = false;
        attackTimer -= Time.deltaTime;

        SnapModelToGround();

        switch (currentState)
        {
            case EnemyState.Patrol:  UpdatePatrol(); break;
            case EnemyState.Chase:   UpdateChase();  break;
            case EnemyState.Attack:  UpdateAttack(); break;
        }
    }

    // --- PATROL ---

    void UpdatePatrol()
    {
        agent.speed = baseSpeed;

        if (PlayerInRange(detectRange) && HasLineOfSight())
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
                PickRandomPatrolTarget();
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

        agent.SetDestination(spawnPosition);
        patrolWaitTimer = patrolWaitTime;
    }

    // --- CHASE ---

    void UpdateChase()
    {
        agent.speed = baseSpeed * chaseSpeedMultiplier * aggressionMultiplier;
        agent.SetDestination(player.position);

        float dist = DistToPlayer();

        if (dist <= attackRange)
        {
            TransitionTo(EnemyState.Attack);
            return;
        }

        if (dist > detectRange * 1.5f)
        {
            TransitionTo(EnemyState.Patrol);
        }
    }

    // --- ATTACK ---

    void UpdateAttack()
    {
        float dist = DistToPlayer();

        // Face the player
        Vector3 lookDir = (player.position - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir.normalized),
                Time.deltaTime * 8f
            );

        // Stop moving while attacking
        agent.SetDestination(transform.position);

        // Player moved too far away — chase again
        if (dist > attackRange * 1.5f)
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        // Shoot on cooldown — no line of sight check here,
        // just range. If they're close enough, fire.
        if (attackTimer <= 0f)
        {
            Shoot();
            attackTimer = attackCooldown / aggressionMultiplier;
        }
    }

    void Shoot()
    {
        Transform origin = firePoint != null ? firePoint : transform;

        // Aim at player center mass
        Vector3 targetPos = player.position + Vector3.up * 0.8f;
        Vector3 dirToPlayer = (targetPos - origin.position).normalized;

        dirToPlayer += origin.right * Random.Range(-aimSpread, aimSpread);
        dirToPlayer += origin.up * Random.Range(-aimSpread, aimSpread);
        dirToPlayer.Normalize();

        // Spawn bullet ahead of fire point to clear enemy collider
        Vector3 spawnPos = origin.position + dirToPlayer * 1f;

        if (enemyBulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(enemyBulletPrefab, spawnPos, Quaternion.identity);
            var bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.speed = bulletSpeed;
                bullet.damage = attackDamage;
                bullet.Init(dirToPlayer);
            }
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, origin.position, origin.rotation);
            Destroy(flash, 0.1f);
        }
    }

    // --- DETECTION ---

    float DistToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    bool PlayerInRange(float range)
    {
        return DistToPlayer() <= range;
    }

    bool HasLineOfSight()
    {
        // Raycast from eye height toward player center mass
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 0.8f;
        Vector3 dir = (targetPos - eyePos).normalized;
        float dist = Vector3.Distance(eyePos, targetPos);

        // Only check obstacles — NOT the ground, NOT enemies
        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist, obstacleMask))
        {
            // Hit an obstacle before reaching the player — blocked
            return false;
        }

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

        var health = GetComponent<EnemyHealth>();
        if (health != null)
            health.invulnerable = disguised;
    }

    // --- GROUND ALIGNMENT ---

    void SnapModelToGround()
    {
        // Only move child models, not the agent transform itself
        // This avoids fighting with NavMeshAgent
        if (realModel == null && disguiseModel == null) return;

        Vector3 rayOrigin = transform.position + Vector3.up * groundRayOriginHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask))
        {
            float targetY = hit.point.y + groundYOffset - transform.position.y;

            if (realModel != null)
            {
                Vector3 pos = realModel.transform.localPosition;
                pos.y = targetY;
                realModel.transform.localPosition = pos;
                hitbox.transform.localPosition = pos;
            }

            if (disguiseModel != null)
            {
                Vector3 pos = disguiseModel.transform.localPosition;
                pos.y = targetY;
                disguiseModel.transform.localPosition = pos;
                hitbox.transform.localPosition = pos;
            }
        }
    }

    // --- TRANSITIONS ---

    void TransitionTo(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Patrol)
            PickRandomPatrolTarget();
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
