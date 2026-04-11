using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 50f;

    public void TakeDamage(float dmg)
    {
        health -= dmg;

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}