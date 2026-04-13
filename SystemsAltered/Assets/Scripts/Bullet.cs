using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float lifeTime = 3f;
    public float damage = 10f;

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
        var health = other.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}