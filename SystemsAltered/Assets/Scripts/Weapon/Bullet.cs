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
    public float speed = 50f;
    public float lifeTime = 3f;
    public float damage = 10f;

    [Header("Fake Enemy Hit")]
    public GameObject fakeHitPopPrefab;

    private Rigidbody rb;

    public void Init(Vector3 dir)
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = dir.normalized * speed;

        // Ignore the player who shot this bullet
        IgnoreShooter();

        Destroy(gameObject, lifeTime);
    }

    void IgnoreShooter()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Collider bulletCol = GetComponent<Collider>();
        if (bulletCol == null) return;

        // Ignore every collider on the player and its children
        Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>();
        foreach (var col in playerColliders)
        {
            Physics.IgnoreCollision(bulletCol, col, true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Fake enemies: pop effect, destroy fake, bullet continues
        if (other.CompareTag("FakeEnemy"))
        {
            if (fakeHitPopPrefab != null)
            {
                GameObject pop = Instantiate(fakeHitPopPrefab, transform.position, Quaternion.identity);
                Destroy(pop, 0.5f);
            }

            Destroy(other.gameObject);
            return;
        }

        // Ignore the player who shot it
        if (other.CompareTag("Player"))
            return;

        // Real enemies: deal damage
        var health = other.GetComponentInParent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
