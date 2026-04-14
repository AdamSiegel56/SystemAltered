using UnityEngine;

/// <summary>
/// Bullet fired by enemies. Damages the player on contact and notifies
/// RagePullSystem of the hit direction. Ignores other enemies.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    public float speed = 30f;
    public float lifeTime = 3f;
    public float damage = 8f;

    private Vector3 direction;
    private Vector3 originPosition;

    public void Init(Vector3 dir)
    {
        direction = dir.normalized;
        originPosition = transform.position;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignore other enemies and fake enemies
        if (other.CompareTag("Enemy") || other.CompareTag("FakeEnemy"))
            return;

        // Hit the player
        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, originPosition);
            Destroy(gameObject);
            return;
        }

        // Hit a wall or obstacle
        Destroy(gameObject);
    }
}