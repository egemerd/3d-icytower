using UnityEngine;
using static UnityEngine.UI.Image;

public class EnemyFighter : Enemy
{
    [SerializeField] private float turnDistance;
    [SerializeField] private Transform weapon;
    [SerializeField] private float weaponTurnSpeed;
    [SerializeField] private LayerMask obstacleMask;


    private Vector3 startPoint;
    private Vector3 endPoint;
    float moveSpeed;
    
    private void Update()
    {
        EnemyAttack();
        //EnemyMovement();
        ShootRaycast();
    }
    private void Awake()
    {
        moveSpeed = enemyData.moveSpeed;
        
    }
    public override void EnemyAttack()
    {
        weapon.RotateAround(transform.position, Vector3.up, weaponTurnSpeed * Time.deltaTime);
    }

    public override void EnemyMovement()
    {

        transform.position =+ enemyData.moveSpeed * Time.deltaTime * transform.right;
        if (Vector3.Distance(transform.position, startPoint) < turnDistance || Vector3.Distance(transform.position, startPoint) < turnDistance)
        {
            moveSpeed = -moveSpeed;
        }
        
    }

    private void ShootRaycast()
    {
        Physics.Raycast(transform.position, transform.right, out RaycastHit hitLeft, turnDistance, obstacleMask);
        Physics.Raycast(transform.position, -transform.right, out RaycastHit hitRight, turnDistance, obstacleMask);

        startPoint = hitLeft.point;
        endPoint = hitRight.point;
    }
    
}
