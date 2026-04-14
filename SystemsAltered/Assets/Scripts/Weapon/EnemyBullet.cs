using UnityEngine;

/// <summary>
/// Bullet fired by enemies. Uses Rigidbody velocity for movement
/// so Unity's physics handles collision detection via OnTriggerEnter.
/// Continuous Dynamic collision detection prevents tunneling.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeTime = 3f;
    public float damage = 8f;

    private Vector3 originPosition;
    private Rigidbody rb;

    public void Init(Vector3 dir)
    {
        originPosition = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = dir.normalized * speed;

        // Ignore all enemy colliders so the bullet doesn't hit the shooter
        IgnoreEnemyColliders();

        Destroy(gameObject, lifeTime);
    }

    void IgnoreEnemyColliders()
    {
        // Ignore everything on the Enemy layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, enemyLayer, true);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        // Skip enemies and fake enemies
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("FakeEnemy"))
            return;

        // Find PlayerHealth anywhere in hierarchy
        var playerHealth = other.gameObject.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, originPosition);
            Destroy(gameObject);
            return;
        }

        // Wall or obstacle
        Destroy(gameObject);
    }
}