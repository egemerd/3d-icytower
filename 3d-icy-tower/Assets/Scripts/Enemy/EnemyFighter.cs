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

    private void Start()
    {
        weapon.transform.position  = transform.position - new Vector3 (0,0,-6);
    }
    private void Update()
    {
        EnemyAttack();
        EnemyMovement();
        
    }
    private void Awake()
    {
        ShootRaycast();
        moveSpeed = enemyData.moveSpeed;
        
    }
    public override void EnemyAttack()
    {
        weapon.RotateAround(transform.position, Vector3.right, weaponTurnSpeed * Time.deltaTime);
    }

    public override void EnemyMovement()
    {

        transform.position += moveSpeed * Time.deltaTime * transform.forward;
        
        if (Vector3.Distance(transform.position, startPoint) < turnDistance || Vector3.Distance(transform.position, endPoint) < turnDistance)
        {
            moveSpeed = -moveSpeed;
        }

    }

    private void ShootRaycast()
    {
        Physics.Raycast(transform.position, transform.forward, out RaycastHit hitLeft, 50f, obstacleMask);
        Physics.Raycast(transform.position, -transform.forward, out RaycastHit hitRight, 50f, obstacleMask);

        startPoint = hitLeft.point;
        endPoint = hitRight.point;
    }

    private void OnDrawGizmosSelected()
    {
        // Scene view visualization (when selected)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 50f);
        Gizmos.DrawRay(transform.position, -transform.forward * 50f);

        // Optional: show hit points if available
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(startPoint, 0.1f);
        Gizmos.DrawSphere(endPoint, 0.1f);
    }
}
