using System.Collections;
using UnityEngine;

public class RocketUltiHandler : MonoBehaviour
{
    private PlayerController player;
    private UltiSkill skillSettings;
    private GameObject arrowInstance;

    private bool isAiming = false;
    private bool isFlying = false;
    private float aimAngle = 0f;
    private float aimDirection = 1f;

    private bool isActivated = false;

    private int framesToIgnoreLaunch = 0;
    private const int LAUNCH_IGNORE_FRAMES = 5;

    private Vector3 currentLaunchDir; // The current travel direction, updated on every bounce
    private float currentSpeed;

    private ParticleSystem[] speedVFXInstances;

    private static RocketUltiHandler activeInstance = null;
    private PlayerVfxReferences vfxRefs;

    public float radius = 1.5f;
    private void Awake()
    {
        if (activeInstance != null && activeInstance != this) { Destroy(this); return; }
        activeInstance = this;
    }

    private void OnDestroy()
    {
        if (activeInstance == this) activeInstance = null;
    }

    private void Update()
    {
        if (isAiming) HandleAiming();
    }

    // -------------------------------------------------------------------------
    // This is the entire billiard physics loop.
    // It runs INSTEAD of PlayerController.FixedUpdate (which returns early
    // when isRocketActive is true).
    // It does one thing: keep moving in currentLaunchDir at currentSpeed.
    // OnCollisionEnter updates currentLaunchDir when a wall is hit.
    // -------------------------------------------------------------------------
    private void FixedUpdate()
    {
        if (!isFlying) return;

        // Always drive velocity from our own direction + speed.
        // currentLaunchDir is updated instantly in OnCollisionEnter
        // so the very next FixedUpdate after a bounce already uses
        // the correct reflected direction.
        if (speedVFXInstances != null)
        {
            foreach (ParticleSystem vfx in speedVFXInstances)
            {
                if (vfx == null) continue;
                vfx.transform.rotation = Quaternion.LookRotation(currentLaunchDir);
            }
        }
        

        player.Rb.linearVelocity = currentLaunchDir * currentSpeed;
        player.SetZMomentum(currentLaunchDir.z * currentSpeed);

        CheckEnemyCollisionWithRaycast();
    }

    private void CheckEnemyCollisionWithRaycast()
    {
        // 1. Tarama mesafesi (Hız * Süre + tolerans payı)
        float checkDistance = (currentSpeed * Time.fixedDeltaTime) + 0.2f;

        // 2. Roketin kalınlığı (Yarıçapı). Çapı 1 birim olsun istiyorsan radius'u 0.5f yapabilirsin.
        // Dilersen bunu yukarıda [SerializeField] private float rocketRadius = 0.5f; olarak da tanımlayabilirsin.
        float rocketRadius = radius;

        RaycastHit hit;

        // 3. Raycast yerine SPHERECAST kullanıyoruz.
        // Parametreler: (Başlangıç Pozisyonu, Kürenin Yarıçapı, Gidiş Yönü, Çarpışma Bilgisi, Tarama Mesafesi)
        if (Physics.SphereCast(player.transform.position, rocketRadius, currentLaunchDir, out hit, checkDistance))
        {
            GameObject hitObj = hit.collider.gameObject;

            // 1. DURUM: Çarptığımız hacim BOSS'a mı geldi?
            if (hitObj.TryGetComponent<Boss>(out Boss boss))
            {
                boss.TakeDamage(1);
                Debug.Log("SphereCast BOSS'u yakaladı ve hasar verdi: " + hitObj.name);
                return;
            }

            // 2. DURUM: Çarptığımız hacim NORMAL DÜŞMAN'a mı geldi?
            if (hitObj.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.OnKilled(1);
                Debug.Log("SphereCast normal düşmanı yakaladı ve hasar verdi: " + hitObj.name);
            }
        }
    }
    // -------------------------------------------------------------------------
    // Wall collision — pure billiard reflection.
    // This is the ONLY place currentLaunchDir changes after launch.
    // -------------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (!isFlying) return;
        if ((player.UltiWallMask.value & (1 << collision.gameObject.layer)) == 0) return;

        // Get the wall normal
        Vector3 normal = collision.contacts[0].normal;

        // Keep everything in the Y/Z plane — this is a 2.5D game, X is always 0
        normal.x = 0f;
        if (normal.sqrMagnitude < 0.001f) return;
        normal = normal.normalized;

        // Billiard reflection: r = d - 2(d·n)n
        // Angle of incidence == angle of reflection, no energy loss
        float dot = Vector3.Dot(currentLaunchDir, normal);
        currentLaunchDir = (currentLaunchDir - 2f * dot * normal).normalized;
        currentLaunchDir.x = 0f; // enforce 2.5D

        // Write the new velocity immediately so there is zero-frame gap
        // between the reflection and the rigidbody moving in the new direction
        player.Rb.linearVelocity = currentLaunchDir * currentSpeed;
        player.SetZMomentum(currentLaunchDir.z * currentSpeed);
    }

    // -------------------------------------------------------------------------
    public void StartUlti(UltiSkill settings)
    {
        if (isActivated) return;
        isActivated = true;

        player = GetComponent<PlayerController>();
        skillSettings = settings;

        for (int i = 0; i < 10; i++)
            InputManager.Instance.ConsumeSkillPressed(4);

        player.ChangeState<AttackingState>();

        if (arrowInstance != null) Destroy(arrowInstance);
        if (skillSettings.arrowPrefab != null)
        {
            arrowInstance = Instantiate(
                skillSettings.arrowPrefab,
                player.transform.position + Vector3.up * 1.5f,
                Quaternion.identity
            );
        }

        aimAngle = 0f;
        aimDirection = 1f;
        isAiming = true;
        isFlying = false;

        framesToIgnoreLaunch = LAUNCH_IGNORE_FRAMES;

        vfxRefs = player.GetComponent<PlayerVfxReferences>();
        if (vfxRefs != null)
            speedVFXInstances = vfxRefs.rocketSpeedVFXs;

    }

    // -------------------------------------------------------------------------
    private void HandleAiming()
    {
        if (framesToIgnoreLaunch > 0)
        {
            framesToIgnoreLaunch--;
            InputManager.Instance.ConsumeSkillPressed(4);
            return;
        }

        aimAngle += aimDirection * skillSettings.aimOscillationSpeed * Time.deltaTime;
        if (aimAngle >= 90f) { aimAngle = 90f; aimDirection = -1f; }
        if (aimAngle <= -90f) { aimAngle = -90f; aimDirection = 1f; }

        if (arrowInstance != null)
        {
            arrowInstance.transform.position = player.transform.position + Vector3.up * 1.5f;
            arrowInstance.transform.localRotation = Quaternion.Euler(aimAngle, 0f, 0f);
        }

        currentLaunchDir = new Vector3(
            0f,
            Mathf.Cos(aimAngle * Mathf.Deg2Rad),
            Mathf.Sin(-aimAngle * Mathf.Deg2Rad)
        ).normalized;

        if (InputManager.Instance.UltiPressed())
            LaunchRocket(currentLaunchDir);
    }

    // -------------------------------------------------------------------------
    private void LaunchRocket(Vector3 launchDir)
    {
        isAiming = false;
        isFlying = true;
        player.isRocketActive = true;
        vfxRefs.playerObj.GetComponent<SkinnedMeshRenderer>().enabled = false;
        Physics.IgnoreLayerCollision(12, 14, true);

        if (arrowInstance != null) { Destroy(arrowInstance); arrowInstance = null; }


        currentLaunchDir = launchDir.normalized;
        currentLaunchDir.x = 0f; // enforce 2.5D from the very start
        currentSpeed = skillSettings.rocketSpeed;

        if (speedVFXInstances != null)
        {
            foreach (ParticleSystem vfx in speedVFXInstances)
            {
                if (vfx == null) continue;
                vfx.transform.rotation = Quaternion.LookRotation(launchDir);
                vfx.Play();
            }
        }

        player.Rb.useGravity = false; // keep gravity off for the full rocket duration
        player.Rb.linearVelocity = currentLaunchDir * currentSpeed;
        player.SetZMomentum(currentLaunchDir.z * currentSpeed);

        StartCoroutine(RocketTimer());
    }

    // -------------------------------------------------------------------------
    private IEnumerator RocketTimer()
    {
        // Phase 1 — constant speed
        float elapsed = 0f;
        while (elapsed < skillSettings.rocketDuration)
        {
            currentSpeed = skillSettings.rocketSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2 — ease-in acceleration
        elapsed = 0f;
        float startSpeed = skillSettings.rocketSpeed;
        float endSpeed = skillSettings.rocketMaxSpeed;
        float accelDuration = skillSettings.rocketAccelerationDuration;

        while (elapsed < accelDuration)
        {
            float t = elapsed / accelDuration;
            currentSpeed = Mathf.Lerp(startSpeed, endSpeed, t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentSpeed = endSpeed;

        // Phase 3 — peak hold
        yield return new WaitForSeconds(skillSettings.rocketPeakHoldDuration);

        if (speedVFXInstances != null)
        {
            foreach (ParticleSystem vfx in speedVFXInstances)
            {
                if (vfx == null) continue;
                vfx.Stop();
            }
        }
        vfxRefs.playerObj.GetComponent<SkinnedMeshRenderer>().enabled = true;
        // Clean up
        isFlying = false;
        player.isRocketActive = false;
        player.isAttacking = false;
        player.Rb.useGravity = true;

        Physics.IgnoreLayerCollision(12, 14, false);

        player.ChangeState<WalkingState>();
        Destroy(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!isFlying || player == null || skillSettings == null) return;

        // Koddaki yarıçap ile buradaki çizim yarıçapı aynı olmalı
        float rocketRadius = radius;
        float checkDistance = (currentSpeed * Time.fixedDeltaTime) + 0.2f;

        Vector3 startPoint = player.transform.position;
        Vector3 endPoint = startPoint + (currentLaunchDir.normalized * checkDistance);

        // 1. Roketin o karedeki BAŞLANGIÇ hacmini YEŞİL bir tel küre olarak çizer
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPoint, rocketRadius);

        // 2. Kürenin merkezlerinin birbirine bağlandığı KIRMIZI rotayı çizer
        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPoint, endPoint);

        // 3. Roketin o kare ulaştığı HEDEF hacmini KIRMIZI bir tel küre olarak çizer
        Gizmos.DrawWireSphere(endPoint, rocketRadius);
    }
#endif

}

