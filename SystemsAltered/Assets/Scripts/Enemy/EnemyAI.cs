using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private DrugStateController drugState;
    private Transform player;

    [Header("Combat")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private float forwardImpulse = 32f;
    [Tooltip("Spawn point for projectiles. If null, falls back to transform.position + spawnHeightOffset")]
    [SerializeField] private Transform muzzlePoint;
    [Tooltip("Fallback vertical offset from pivot when no muzzle point is assigned")]
    [SerializeField] private float spawnHeightOffset = 1f;
    [Tooltip("Vertical offset applied to the player position when computing aim direction (targets center-mass)")]
    [SerializeField] private float playerAimHeightOffset = 1f;

    public LayerMask whatIsPlayer;
    public LayerMask whatIsGround;

    [Header("Detection")]
    [SerializeField] private float sightRange = 15f;
    [SerializeField] private float attackRange = 8f;

    [Header("Patrol")]
    [SerializeField] private float walkPointRange = 10f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private float walkPointArrivalDistance = 1f;
    [Tooltip("How many times to attempt finding a reachable walk point before giving up")]
    [SerializeField] private int walkPointSearchAttempts = 6;

    [Header("Fake Enemy")]
    [SerializeField] private bool isFakeEnemy;
    [SerializeField] private GameObject fakeDeathPopPrefab;

    // Runtime state
    private float currentHealth;
    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool alreadyAttacked;
    private bool playerInSightRange;
    private bool playerInAttackRange;
    private bool isDead;

    // Cached base appearance
    private float _baseSpeed;
    private Vector3 _baseScale;
    private Mesh _baseMesh;
    private Material _baseMaterial;
    private DrugStateData _lastAppliedState;

    // Reusable path object — avoids allocating one every search
    private NavMeshPath _navPath;

    // --- Drug state injection ---

    public DrugStateController DrugState
    {
        set => drugState = value;
    }

    // --- Multiplier accessors ---

    private float SpeedMult =>
        drugState?.CurrentState?.enemySpeedMultiplier ?? 1f;

    private float DamageMult =>
        drugState?.CurrentState?.enemyDamageMultiplier ?? 1f;

    private float AttackRateMult =>
        drugState?.CurrentState?.enemyAttackRateMultiplier ?? 1f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float NormalizedHealth => currentHealth / maxHealth;
    public bool IsFake => isFakeEnemy;

    public static event System.Action EnemyKilled;

    // --- Lifecycle ---

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentHealth = maxHealth;
        _navPath = new NavMeshPath();
        CacheBaseAppearance();
    }

    private void Update()
    {
        if (isDead) return;

        ApplyDrugStateIfChanged();
        UpdateDetection();
        UpdateBehaviour();
    }

    // --- Appearance / stats ---

    private void CacheBaseAppearance()
    {
        _baseSpeed = agent != null ? agent.speed : 0f;
        _baseScale = transform.localScale;
        _baseMesh = meshFilter != null ? meshFilter.sharedMesh : null;
        _baseMaterial = meshRenderer != null ? meshRenderer.sharedMaterial : null;
    }

    private void ApplyDrugStateIfChanged()
    {
        var state = drugState != null ? drugState.CurrentState : null;
        if (state == _lastAppliedState) return;

        _lastAppliedState = state;
        ApplyAppearance(state);
        ApplyStats();
    }

    private void ApplyAppearance(DrugStateData state)
    {
        if (state != null && state.overrideEnemyAppearance)
        {
            SetMesh(state.enemyMesh != null ? state.enemyMesh : _baseMesh);
            SetMaterial(state.enemyMaterial != null ? state.enemyMaterial : _baseMaterial);
            transform.localScale = Vector3.Scale(_baseScale, state.enemyScale);
        }
        else
        {
            SetMesh(_baseMesh);
            SetMaterial(_baseMaterial);
            transform.localScale = _baseScale;
        }
    }

    private void SetMesh(Mesh mesh)
    {
        if (meshFilter != null) meshFilter.mesh = mesh;
    }

    private void SetMaterial(Material material)
    {
        if (meshRenderer != null) meshRenderer.material = material;
    }

    private void ApplyStats()
    {
        if (agent != null)
            agent.speed = _baseSpeed * SpeedMult;
    }

    // --- Detection ---

    private void UpdateDetection()
    {
        if (player == null)
        {
            playerInSightRange = false;
            playerInAttackRange = false;
            return;
        }

        var distance = Vector3.Distance(transform.position, player.position);
        playerInSightRange  = distance <= sightRange;
        playerInAttackRange = distance <= attackRange;
    }

    // --- Behaviour ---

    private void UpdateBehaviour()
    {
        if (!playerInSightRange && !playerInAttackRange) Patrol();
        else if (playerInSightRange && !playerInAttackRange) Chase();
        else if (playerInAttackRange && playerInSightRange) Attack();
    }

    // --- Patrol ---

    private void Patrol()
    {
        if (!walkPointSet) SearchWalkPoint();
        if (!walkPointSet) return;

        agent.SetDestination(walkPoint);

        if (Vector3.Distance(transform.position, walkPoint) < walkPointArrivalDistance)
            walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        for (int i = 0; i < walkPointSearchAttempts; i++)
        {
            var randomX = Random.Range(-walkPointRange, walkPointRange);
            var randomZ = Random.Range(-walkPointRange, walkPointRange);

            var candidate = new Vector3(
                transform.position.x + randomX,
                transform.position.y,
                transform.position.z + randomZ
            );

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                continue;

            if (!IsReachable(hit.position))
                continue;

            walkPoint = hit.position;
            walkPointSet = true;
            return;
        }
    }

    private bool IsReachable(Vector3 target)
    {
        _navPath.ClearCorners();
        if (!agent.CalculatePath(target, _navPath)) return false;
        return _navPath.status == NavMeshPathStatus.PathComplete;
    }

    // --- Chase ---

    private void Chase()
    {
        if (player != null)
            agent.SetDestination(player.position);
    }

    // --- Attack ---

    private void Attack()
    {
        if (player == null) return;

        agent.SetDestination(transform.position);

        var dir = player.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (alreadyAttacked) return;

        FireProjectile();

        alreadyAttacked = true;
        var cooldown = timeBetweenAttacks / Mathf.Max(AttackRateMult, 0.01f);
        Invoke(nameof(ResetAttack), cooldown);
    }

    private void FireProjectile()
    {
        if (isFakeEnemy) return;
        if (projectile == null || player == null) return;

        var spawnPos = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * spawnHeightOffset;

        var targetPos = player.position + Vector3.up * playerAimHeightOffset;
        var aimDir = (targetPos - spawnPos).normalized;

        var bulletObj = Instantiate(projectile, spawnPos, Quaternion.LookRotation(aimDir));
        var bullet = bulletObj.GetComponent<Bullet>();
        var finalDamage = attackDamage * DamageMult;

        if (bullet != null)
        {
            bullet.Init(aimDir, finalDamage, gameObject);
        }
        else
        {
            var rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(aimDir * forwardImpulse, ForceMode.Impulse);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    // --- Damage / Death ---

    private bool hasntEvent;

    public void TakeDamage(float damage, Vector3 hitPos, Vector3 hitNormal)
    {
        if (hitEffect != null)
            Instantiate(hitEffect, hitPos, Quaternion.LookRotation(hitNormal));

        if (isFakeEnemy)
        {
            PopFakeEnemy();
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            if (!hasntEvent)
            {
                EnemyKilled?.Invoke();
                hasntEvent = true;
            }

            isDead = true;
            agent.ResetPath();
            Invoke(nameof(DestroyEnemy), 0.5f);
        }
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    private void PopFakeEnemy()
    {
        if (fakeDeathPopPrefab != null)
        {
            var pop = Instantiate(fakeDeathPopPrefab, transform.position, Quaternion.identity);
            Destroy(pop, 0.5f);
        }

        Destroy(gameObject);
    }

    // --- Gizmos ---

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}

