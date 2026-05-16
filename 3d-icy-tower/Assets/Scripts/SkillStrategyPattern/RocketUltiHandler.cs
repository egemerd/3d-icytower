using System.Collections;
using UnityEditor.SettingsManagement;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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

    private static RocketUltiHandler activeInstance = null;

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
        player.Rb.linearVelocity = currentLaunchDir * currentSpeed;
        player.SetZMomentum(currentLaunchDir.z * currentSpeed);
    }

    // -------------------------------------------------------------------------
    // Wall collision — pure billiard reflection.
    // This is the ONLY place currentLaunchDir changes after launch.
    // -------------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (!isFlying) return;
        if ((player.WallMask.value & (1 << collision.gameObject.layer)) == 0) return;

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

        Physics.IgnoreLayerCollision(12, 14, true);

        if (arrowInstance != null) { Destroy(arrowInstance); arrowInstance = null; }

        currentLaunchDir = launchDir.normalized;
        currentLaunchDir.x = 0f; // enforce 2.5D from the very start
        currentSpeed = skillSettings.rocketSpeed;

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

        // Clean up
        isFlying = false;
        player.isRocketActive = false;
        player.isAttacking = false;
        player.Rb.useGravity = true;

        Physics.IgnoreLayerCollision(12, 14, false);

        player.ChangeState<WalkingState>();
        Destroy(this);
    }


}

