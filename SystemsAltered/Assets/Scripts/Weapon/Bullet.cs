using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float lifeTime = 3f;
    public float damage = 10f;

    [Header("Fake Enemy Hit")]
    public GameObject fakeHitPopPrefab;

    private Vector3 direction;

    public void Init(Vector3 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Fake enemies: bullet passes through with a visual pop
        if (other.CompareTag("FakeEnemy"))
        {
            if (fakeHitPopPrefab != null)
            {
                GameObject pop = Instantiate(fakeHitPopPrefab, transform.position, Quaternion.identity);
                Destroy(pop, 0.5f);
            }

            // Destroy the fake on hit so the player learns it was fake
            Destroy(other.gameObject);

            // Bullet continues through — don't destroy it
            return;
        }

        // Real enemies: deal damage
        var health = other.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}