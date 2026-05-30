using UnityEngine;

public interface IDamagable 
{
    void TakeDamage(int amount, float timeScale ,float duration);

    void ApplyKnockback(Vector3 hitDirection, float knockbackForce);
}
