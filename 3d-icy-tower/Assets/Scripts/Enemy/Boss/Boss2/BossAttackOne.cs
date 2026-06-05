using UnityEngine;
using System.Collections;

public class BossAttackOne : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;             // Bossun ateş noktası
    public GameObject projectilePrefab;
    public GameObject laserPrefab;

    [Header("Angle Settings")]
    [Tooltip("Aşağı yönden sağa/sola maksimum sapma açısı (derece)")]
    public float spreadAngle = 45f;         // ±45° → toplam 90° koni

    [Header("Projectile Settings")]
    public int projectileCount = 5;
    public float projectileIntervalMin = 0.1f;
    public float projectileIntervalMax = 0.4f;

    [Header("Laser Settings")]
    public int laserCount = 3;
    public float laserIntervalMin = 0.2f;
    public float laserIntervalMax = 0.6f;
    public float laserLength = 8f;          // Lazerin dünya uzunluğu

    [Header("Attack Duration")]
    public float totalAttackDuration = 4f;  // Bu süre dolunca saldırı biter

    // SecondBoss tarafından çağrılır
    public IEnumerator Execute(System.Action onComplete)
    {
        // Projectile ve lazer coroutine'lerini paralel başlat
        Coroutine projRoutine = StartCoroutine(FireProjectiles());
        //Coroutine laserRoutine = StartCoroutine(FireLasers());

        yield return new WaitForSeconds(totalAttackDuration);

        // Süre dolunca her ikisini de durdur
        StopCoroutine(projRoutine);
        //StopCoroutine(laserRoutine);

        onComplete?.Invoke();
    }

    // ── Projectile döngüsü ───────────────────────────────────────────────
    private IEnumerator FireProjectiles()
    {
        for (int i = 0; i < projectileCount; i++)
        {
            SpawnProjectile();
            float wait = Random.Range(projectileIntervalMin, projectileIntervalMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector3 dir = RandomConeDirection();
        GameObject obj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));
        BossProjectile proj = obj.GetComponent<BossProjectile>();
        proj?.Launch(dir);
    }

    // ── Lazer döngüsü ────────────────────────────────────────────────────
    private IEnumerator FireLasers()
    {
        for (int i = 0; i < laserCount; i++)
        {
            SpawnLaser();
            float wait = Random.Range(laserIntervalMin, laserIntervalMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnLaser()
    {
        if (laserPrefab == null || firePoint == null) return;

        Vector3 dir = RandomConeDirection();
        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject obj = Instantiate(laserPrefab, firePoint.position, rot);

        // Lazeri firePoint'ten dir yönünde konumlandır ve uzat
        obj.transform.position = firePoint.position + dir * (laserLength * 0.5f);
        obj.transform.localScale = new Vector3(
            obj.transform.localScale.x,
            obj.transform.localScale.y,
            laserLength
        );

        BossLaser laser = obj.GetComponent<BossLaser>();
        if (laser != null)
            StartCoroutine(laser.FireSequence());
    }

    // ── Yardımcı: Koninin içinde rastgele yön ────────────────────────────
    private Vector3 RandomConeDirection()
    {
        // Aşağı yön (Vector3.down) etrafında ±spreadAngle koni
        float angle = Random.Range(-spreadAngle, spreadAngle);

        // 2.5D: sadece Y/Z düzleminde döndür
        Vector3 dir = new Vector3(
            0f,
            Mathf.Cos((180f + angle) * Mathf.Deg2Rad),   // aşağı yönlü
            Mathf.Sin(angle * Mathf.Deg2Rad)
        ).normalized;

        return dir;
    }
}