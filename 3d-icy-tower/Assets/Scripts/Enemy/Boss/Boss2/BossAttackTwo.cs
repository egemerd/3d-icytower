using UnityEngine;
using System.Collections;

public class BossAttackTwo : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public GameObject laserPrefab;

    [Header("Sweep Angles")]
    [Tooltip("Lazerin başlayacağı açı (0 derece tam aşağısıdır. Sol taraf için eksi, sağ taraf için artı değerler girin. Örn: -90 tam sol, 90 tam sağ)")]
    public float startAngle = -90f;
    [Tooltip("Lazerin biteceği açı. Örn: 90")]
    public float endAngle = 90f;

    [Header("Sweep Settings")]
    public float sweepSpeed = 45f;
    public float pauseBetweenSweeps = 0.4f;

    [Header("Laser Settings")]
    public float laserLength = 12f;

    [Header("Damage")]
    public int damage = 1;
    public float damageRadius = 0.15f;
    public LayerMask playerMask;

    private GameObject laserInstance;
    private Transform laserTransform;
    private bool isDamageActive = false;

    private BossLaserHitbox laserHitbox;

    // ── Ana coroutine ────────────────────────────────────────────────────
    public IEnumerator Execute(System.Action onComplete)
    {
        // 1. Lazeri başlangıç açısında oluştur
        SpawnLaser(startAngle);
        SoundManager.PlaySound(SoundType.BOSS2LASERSOUND, 0.03f);
        // 3. Soldan → Sağa

        yield return StartCoroutine(SweepLaser(startAngle, endAngle));

        // 4. Bekleme

        yield return new WaitForSeconds(pauseBetweenSweeps);
        // 5. Sağdan → Sola

        yield return StartCoroutine(SweepLaser(endAngle, startAngle));

        // 6. Temizle

        if (laserInstance != null) Destroy(laserInstance);

        onComplete?.Invoke();
    }

    // ── Lazeri oluştur ───────────────────────────────────────────────────
    private void SpawnLaser(float angleDegrees)
    {
        if (laserInstance != null) Destroy(laserInstance);

        laserInstance = Instantiate(laserPrefab, firePoint.position, Quaternion.identity);
        laserTransform = laserInstance.transform;
        laserHitbox = laserInstance.GetComponentInChildren<BossLaserHitbox>();
        

        if (laserHitbox != null)
        {
            laserHitbox.damage = damage;
            laserHitbox.playerMask = playerMask;
        }

        UpdateLaserTransform(angleDegrees);
    }

    // ── Lazeri açıya göre döndür ve konumlandır ──────────────────────────
    private void UpdateLaserTransform(float angleDegrees)
    {
        if (laserTransform == null || firePoint == null) return;

        laserTransform.position = firePoint.position;

        // Oyunun Y ve Z ekseninde olduğu için, boss X ekseni etrafında dönmelidir.
        // 0 derece girdisi lazeri tam aşağıya (Y-) bakıtacaktır.
        laserTransform.rotation = Quaternion.Euler(angleDegrees, 0f, 0f);
    }

    // ── Tarama ──────────────────────────────────────────────────────────
    private IEnumerator SweepLaser(float from, float to)
    {
        float current = from;
        float direction = Mathf.Sign(to - from);

        // Hasar aç
        if (laserHitbox != null) laserHitbox.isDamageActive = true;

        while (direction > 0 ? current < to : current > to)
        {
            current += direction * sweepSpeed * Time.deltaTime;
            current = direction > 0
                ? Mathf.Min(current, to)
                : Mathf.Max(current, to);

            UpdateLaserTransform(current);
            yield return null;
        }

        // Hasar kapat
        if (laserHitbox != null) laserHitbox.isDamageActive = false;
    }

    // ── Hasar ────────────────────────────────────────────────────────────
    private void CheckLaserDamage(float angleDegrees)
    {
        if (firePoint == null) return;

        Vector3 dir = AngleToDirection(angleDegrees);
        Vector3 start = firePoint.position;

        if (Physics.SphereCast(start, damageRadius, dir, out RaycastHit hit, laserLength, playerMask))
        {
            IDamagable damagable = hit.collider.GetComponentInParent<IDamagable>();
            damagable?.TakeDamage(damage, 0f, 0.1f);
        }
    }

    // ── 2.5D Y-Z Ekseni Yön Hesaplama (Dinamik ve Her Açıya Uygun) ────────
    private Vector3 AngleToDirection(float angleDegrees)
    {
        // Açıları radyana çeviriyoruz
        float rad = angleDegrees * Mathf.Deg2Rad;

        // 0 derece girildiğinde: Cos(0) = 1, Sin(0) = 0 -> Sonuç: (0, -1, 0) yani tam aşağısı.
        // -90 derece girildiğinde: Cos(-90) = 0, Sin(-90) = -1 -> Sonuç: (0, 0, -1) yani tam sol (Z-).
        // +90 derece girildiğinde: Cos(90) = 0, Sin(90) = 1 -> Sonuç: (0, 0, 1) yani tam sağ (Z+).
        return new Vector3(0f, -Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    
}