using UnityEngine;

/// <summary>
/// Larger trigger collider on the player specifically for bullet detection.
/// Forwards hits to PlayerHealth on the parent. This means bullets don't
/// need to hit the exact capsule collider — any hit within this trigger counts.
/// 
/// Setup: Create a child of the Player called "Hitbox".
/// Add a Box or Sphere Collider, set Is Trigger = true,
/// make it slightly larger than the capsule (e.g. Box 1.2 x 2.2 x 1.2).
/// Add this script. Tag the child as "Player".
/// </summary>
public class PlayerHitbox : MonoBehaviour
{
    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogError("PlayerHitbox: No PlayerHealth found in parent hierarchy!");
    }

    void OnTriggerEnter(Collider other)
    {
        // Only respond to enemy bullets
        var enemyBullet = other.GetComponent<EnemyBullet>();
        if (enemyBullet != null && playerHealth != null)
        {
            playerHealth.TakeDamage(enemyBullet.damage, other.transform.position);
            Destroy(other.gameObject);
        }

        var deadZone = other.GetComponent<FallOutOfBounds>();
        if (deadZone != null && playerHealth != null)
        {
            playerHealth.TakeDamage(200, Vector3.zero);
        }
        
        else
        {
            return;
        }
    }
}