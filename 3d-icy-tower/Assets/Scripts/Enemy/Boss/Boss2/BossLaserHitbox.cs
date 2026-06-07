using UnityEngine;

public class BossLaserHitbox : MonoBehaviour
{
    [HideInInspector] public int damage = 1;
    [HideInInspector] public LayerMask playerMask;
    [HideInInspector] public bool isDamageActive = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Laser trigger hit: {other.gameObject.name} layer: {other.gameObject.layer}");

        if (!isDamageActive)
        {
            Debug.Log("Damage not active!");
            return;
        }
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0)
        {
            Debug.Log("Layer mask mismatch!");
            return;
        }

        IDamagable damagable = other.GetComponentInParent<IDamagable>();
        damagable?.TakeDamage(damage, 0f, 0.1f);
    }
}