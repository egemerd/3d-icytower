using UnityEngine;

public class EnemyFigterSword : MonoBehaviour
{
    public int damage;

    private void Start()
    {
        damage = GetComponentInParent<EnemyFighter>().attackDamage;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log(damage);
            other.GetComponent<PlayerHealth>().GetDamage(damage);
        }
    }
}
