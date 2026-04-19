using UnityEngine;

public class Enemy1 : Enemy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;


    private Transform playerTarget;
    private float attackTimer;

    private void Start()
    {
        playerTarget = base.playerTransform;
    }

    private void Update()
    {
        AttackCheck();
    }

    private void AttackCheck()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // Check if player is inside the scan radius
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer <= enemyData.scanRadius)
        {
            if (attackTimer <= 0f)
            {
                EnemyAttack();
            }
        }
    }
    public override void EnemyAttack()
    {
        // Reset the cooldown based on the ScriptableObject's data
        attackTimer = enemyData.attackCooldown;

        if (projectilePrefab != null && firePoint != null)
        {
            // Spawn the projectile
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            // Calculate the direction towards the player
            Vector3 direction = (playerTarget.position - firePoint.position).normalized;

            // Apply velocity to the projectile (Requires a Rigidbody on the projectile prefab)
            if (proj.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = direction * enemyData.projectileSpeed;
            }

            // Optional: Make the projectile face the player
            proj.transform.forward = direction;
        }
    }

    

    public override void EnemyMovement()
    {
        throw new System.NotImplementedException();
    }


    private void OnDrawGizmosSelected()
    {
        // Notice we check if enemyData exists so it doesn't throw errors before you assign it
        if (enemyData != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Transparent red
            Gizmos.DrawSphere(transform.position, enemyData.scanRadius);

            Gizmos.color = Color.red; // Solid red wireframe outline
            Gizmos.DrawWireSphere(transform.position, enemyData.scanRadius);
        }
    }
}
