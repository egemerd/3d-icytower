using UnityEngine;

public class Enemy1 : Enemy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;


    private Transform playerTarget;
    private float attackTimer;
    private Enemy1Projectile projectile;

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
                Debug.LogError("enemy atttack");
                EnemyAttack();
            }
        }
    }
    public override void EnemyAttack()
    {
        attackTimer = enemyData.attackCooldown;

        if (projectilePrefab != null && firePoint != null)
        {
            Vector3 direction = (playerTarget.position - firePoint.position).normalized;
            
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            if (proj.TryGetComponent(out Enemy1Projectile projScript))
            {
                projScript.InitializeVariables(enemyData.attackDamage, enemyData.projectileSpeed, direction);
            }
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
