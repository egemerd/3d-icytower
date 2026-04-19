using UnityEngine;

public class Enemy1Projectile : MonoBehaviour
{
    public int projectileDamage = 1; // Set this to match the attackDamage in EnemyDataSO
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Assuming the player has a PlayerHealth component to manage health
            if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.GetDamage(projectileDamage);
            }

            Destroy(gameObject, 0.2f);
        }
    }
}
