using UnityEngine;

/// <summary>
/// Player bullet. Uses Rigidbody velocity for movement so Unity's
/// physics handles collision detection via OnTriggerEnter.
/// Passes through fake enemies with a pop effect.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Bullet : MonoBehaviour
{
    [Header("Ballistics")]
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifeTime = 3f;

    [Header("Impact FX")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Rigidbody _rb;
    private SphereCollider _col;
    private GameObject _shooter;
    private float _damage;

    public void Init(Vector3 direction, float damage, GameObject shooter = null)
    {
        _shooter = shooter;
        _damage = damage;

        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<SphereCollider>();

        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.linearVelocity = direction.normalized * speed;

        _col.isTrigger = true;

        IgnoreShooterCollisions();

        Destroy(gameObject, lifeTime);
    }

    private void IgnoreShooterCollisions()
    {
        if (_shooter == null || _col == null) return;

        var shooterColliders = _shooter.GetComponentsInChildren<Collider>();
        foreach (var col in shooterColliders)
            Physics.IgnoreCollision(_col, col, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        var hitObj = other.gameObject;

        // Ignore other enemies so bullets don't friendly-fire
        if (hitObj.CompareTag("Enemy") || hitObj.CompareTag("FakeEnemy"))
            return;

        if (hitObj.CompareTag("Player"))
        {
            var health = hitObj.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(_damage, transform.position);
        }

        SpawnHitEffect();
        Destroy(gameObject);
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;

        var normal = -_rb.linearVelocity.normalized;
        if (normal == Vector3.zero) normal = Vector3.up;

        Instantiate(hitEffectPrefab, transform.position, Quaternion.LookRotation(normal));
    }
}
