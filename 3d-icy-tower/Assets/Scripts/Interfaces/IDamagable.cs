using UnityEngine;

public interface IDamagable 
{
    void TakeDamage(int amount);

    void ApplyKnockback(Vector3 hitDirection, float knockbackForce);
}
