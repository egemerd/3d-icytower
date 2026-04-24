using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Enemy1Projectile : MonoBehaviour
{
    // Set this to match the attackDamage in EnemyDataSO
    Rigidbody rb;

    public int attackDamage;
    public float projectileSpeed;
    private Vector3 projectileDirection;    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        MoveProjectile();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // Assuming the player has a PlayerHealth component to manage health
            if (other.gameObject.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.GetDamage(attackDamage);
            }

            Destroy(gameObject, 0.2f);
        }
    }

    public void InitializeVariables(int damage, float speed,Vector3 direction)
    {
        attackDamage =damage;
        projectileSpeed = speed;
        projectileDirection = direction;
    }
    public void MoveProjectile()
    {        
        rb.linearVelocity = projectileDirection * projectileSpeed;
        transform.forward = projectileDirection;
    }
}
