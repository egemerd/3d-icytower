using UnityEngine;
using System.Collections;

public class BossLaser : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 1;
    public LayerMask playerMask;

    [Header("Visuals")]
    [Tooltip("Anticipation sýrasýnda gösterilecek child obje")]
    public GameObject anticipationVisual;
    [Tooltip("Aktif hasar verirken gösterilecek child obje")]
    public GameObject activeVisual;

    [Header("Timing")]
    public float anticipationDuration = 1f;
    public float activeDuration = 0.5f;

    // BossAttackOne tarafýndan çaðrýlýr
    public IEnumerator FireSequence()
    {
        // 1. Anticipation — ince çizgi görünür, hasar yok
        if (anticipationVisual != null) anticipationVisual.SetActive(true);
        if (activeVisual != null) activeVisual.SetActive(false);

        yield return new WaitForSeconds(anticipationDuration);

        // 2. Aktif — kalýn lazer, hasar var
        if (anticipationVisual != null) anticipationVisual.SetActive(false);
        if (activeVisual != null) activeVisual.SetActive(true);

        float elapsed = 0f;
        while (elapsed < activeDuration)
        {
            // Her frame overlap ile oyuncu kontrolü
            Collider[] hits = Physics.OverlapBox(
                transform.position,
                transform.localScale * 0.5f,
                transform.rotation,
                playerMask
            );

            foreach (var hit in hits)
            {
                IDamagable damagable = hit.GetComponentInParent<IDamagable>();
                damagable?.TakeDamage(damage, 0f, 0.1f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}