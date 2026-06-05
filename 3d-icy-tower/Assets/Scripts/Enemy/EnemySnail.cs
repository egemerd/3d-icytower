using UnityEngine;

public class EnemySnail : Enemy
{

    private int attackDamage = 1;

    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered snail attack range!");
            if (other.gameObject.TryGetComponent(out PlayerHealth playerHealth))
            {
                Debug.Log("Player hit by snail attack!");
                playerHealth.GetDamage(attackDamage);
            }
        }
    }

    public override void EnemyAttack(){}

    public override void EnemyMovement(){}
}
