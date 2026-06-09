using System.Collections;
using System.Collections.Generic;
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

    private Vector3 currentLaunchDir;
    private float currentSpeed;

    private ParticleSystem[] speedVFXInstances;

    private static RocketUltiHandler activeInstance = null;
    private PlayerVfxReferences vfxRefs;

    public float radius = 1.5f;

    // -------------------------------------------------------------------------
    // Hit cooldown system — prevents the same collider from being damaged
    // multiple times within HIT_COOLDOWN seconds.
    // Cleared on every bounce so a tight ricochet can immediately re-hit.
    // -------------------------------------------------------------------------
    private const float HIT_COOLDOWN = 0.5f;
    private Dictionary<Collider, float> hitCooldowns = new Dictionary<Collider, float>();

    // Assign in Inspector: include only the layers your enemies/boss live on.
    // If left empty (value 0) the overlap will check ALL layers.
    [SerializeField] private LayerMask enemyHitMask;

    // -------------------------------------------------------------------------

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
    // Billiard physics loop — runs instead of PlayerController.FixedUpdate
    // while isRocketActive is true.
    // -------------------------------------------------------------------------
    private void FixedUpdate()
    {
        if (!isFlying) return;

        // Rotate speed VFX to match travel direction
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

        CheckEnemyCollision();
    }

    // -------------------------------------------------------------------------
    // OverlapSphere hit detection.
    // Checks every collider inside the rocket's radius each FixedUpdate.
    // Direction-independent — catches grazes, side hits, and already-overlapping
    // targets that SphereCast would miss.
    // -------------------------------------------------------------------------
    private void CheckEnemyCollision()
    {
        // Use the layermask if one was assigned; otherwise check everything.
        Collider[] hits = (enemyHitMask.value != 0)
            ? Physics.OverlapSphere(player.transform.position, radius, enemyHitMask)
            : Physics.OverlapSphere(player.transform.position, radius);

        foreach (Collider col in hits)
        {
            // --- per-collider cooldown gate ---
            if (hitCooldowns.TryGetValue(col, out float nextHitTime))
                if (Time.time < nextHitTime) continue;

            // Register (or refresh) the cooldown for this collider
            hitCooldowns[col] = Time.time + HIT_COOLDOWN;

            // --- damage routing ---
            if (col.TryGetComponent<Boss>(out Boss boss))
            {
                boss.TakeDamage(1);
                TimeStop.Instance.StopTime(0.1f, 0.1f);
                ParticleEffects.Instance.PlayOneShot(ParticleType.HitEffect2, col.transform.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
                Debug.Log("Rocket hit Boss: " + col.name);
                continue;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Wall collision — pure billiard reflection.
    // currentLaunchDir is updated here; the very next FixedUpdate uses it.
    // Hit cooldowns are cleared so a tight ricochet can re-damage immediately.
    // -------------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (!isFlying) return;
        if ((player.UltiWallMask.value & (1 << collision.gameObject.layer)) == 0) return;

        Vector3 normal = collision.contacts[0].normal;

        // Enforce 2.5D — keep everything in the Y/Z plane
        normal.x = 0f;
        if (normal.sqrMagnitude < 0.001f) return;
        normal = normal.normalized;

        // Billiard reflection: r = d - 2(d·n)n
        float dot = Vector3.Dot(currentLaunchDir, normal);
        currentLaunchDir = (currentLaunchDir - 2f * dot * normal).normalized;
        currentLaunchDir.x = 0f;

        // Apply immediately so there is no single-frame gap in direction
        player.Rb.linearVelocity = currentLaunchDir * currentSpeed;
        player.SetZMomentum(currentLaunchDir.z * currentSpeed);

        // Clear cooldowns — each bounce resets damage windows so the player
        // is rewarded for bouncing the rocket back into the same enemy/boss.
        hitCooldowns.Clear();
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
        Mathf.Sin(aimAngle * Mathf.Deg2Rad)  // -aimAngle yerine aimAngle
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

        hitCooldowns.Clear();

        currentLaunchDir = launchDir.normalized;
        currentLaunchDir.x = 0f;
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

        player.Rb.useGravity = false;
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

        // Clean up VFX
        if (speedVFXInstances != null)
        {
            foreach (ParticleSystem vfx in speedVFXInstances)
            {
                if (vfx == null) continue;
                vfx.Stop();
            }
        }

        vfxRefs.playerObj.GetComponent<SkinnedMeshRenderer>().enabled = true;

        // Reset state
        isFlying = false;
        player.isRocketActive = false;
        player.isAttacking = false;
        player.Rb.useGravity = true;

        hitCooldowns.Clear();

        Physics.IgnoreLayerCollision(12, 14, false);

        player.ChangeState<WalkingState>();
        Destroy(this);
    }

    // -------------------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!isFlying || player == null || skillSettings == null) return;

        float checkDistance = (currentSpeed * Time.fixedDeltaTime) + 0.2f;
        Vector3 startPoint = player.transform.position;
        Vector3 endPoint = startPoint + (currentLaunchDir.normalized * checkDistance);

        // Green sphere = current position / overlap radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPoint, radius);

        // Red line + sphere = projected next position
        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPoint, endPoint);
        Gizmos.DrawWireSphere(endPoint, radius);
    }
#endif
}