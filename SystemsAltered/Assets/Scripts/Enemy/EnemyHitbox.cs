using UnityEngine;

/// <summary>
/// Trigger collider on enemy model children that forwards bullet hits
/// to EnemyHealth on the parent. Attach to RealModel child alongside
/// a Box/Capsule Collider (Is Trigger = true). Tag as "Enemy".
///
/// This ensures bullets hit the visible model, not just the NavMeshAgent
/// capsule which might be offset or too small.
/// </summary>
/*
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;

    void Start()
    {

        if (enemyHealth == null)
            Debug.LogError("EnemyHitbox: No EnemyHealth found in parent hierarchy!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("Hit");
            enemyHealth.TakeDamage(other.gameObject.GetComponent<Bullet>().damage);
            Destroy(other.gameObject);
        }
        else
        {
            return;
        }
    }
}
*/