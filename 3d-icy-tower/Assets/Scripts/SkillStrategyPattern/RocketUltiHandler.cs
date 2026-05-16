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

    // Guard: StartUlti sets this to true the moment it runs.
    // Every subsequent call to StartUlti in the same activation is ignored.
    private bool isActivated = false;

    // Frame-counter guard so the press that triggered the ulti
    // cannot also instantly fire the rocket on the same/next frame.
    private int framesToIgnoreLaunch = 0;
    private const int LAUNCH_IGNORE_FRAMES = 5;

    private Vector3 currentLaunchDir;
    private float currentSpeed;

    

    private void Update()
    {
        if (isAiming)
        {
            HandleAiming();
        }
    }

    private void FixedUpdate()
    {
        if (!isFlying) return;

        Vector3 vel = player.Rb.linearVelocity;

        if (vel.magnitude < 0.1f)
            vel = currentLaunchDir.normalized;

        player.Rb.linearVelocity = vel.normalized * currentSpeed;
        player.SetZMomentum(player.Rb.linearVelocity.z);
    }

    // -------------------------------------------------------------------------
    // Called by AttackingState (or wherever you trigger skills).
    // The isActivated guard means only the FIRST call does anything.
    // Every subsequent call while this component lives is a no-op.
    // -------------------------------------------------------------------------
    public void StartUlti(UltiSkill settings)
    {
        // ── CRITICAL GUARD ───────────────────────────────────────────────────
        // If we are already activated (aiming or flying), do NOTHING.
        // This is what prevents AttackingState.UpdateState() from calling
        // StartUlti every frame and spawning a new arrow each time.
        if (isActivated) return;
        isActivated = true;
        // ────────────────────────────────────────────────────────────────────

        player = GetComponent<PlayerController>();
        skillSettings = settings;

        // Drain ALL pending skill-4 presses right now before anything else.
        // InputManager stores a bool latch; clear it immediately.
        for (int i = 0; i < 10; i++)
            InputManager.Instance.ConsumeSkillPressed(4);

        // Freeze the player via AttackingState so PlayerController's
        // FixedUpdate physics pipeline is bypassed.
        player.ChangeState<AttackingState>();

        // Spawn arrow
        if (skillSettings.arrowPrefab != null)
        {
            // Destroy any stale arrow that might already exist
            if (arrowInstance != null) Destroy(arrowInstance);

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

        // Frame-counter guard: skip N frames of input reading so the
        // triggering press cannot bleed into the launch check.
        framesToIgnoreLaunch = LAUNCH_IGNORE_FRAMES;
    }

    // -------------------------------------------------------------------------
    private void HandleAiming()
    {
        // Drain input and wait out the guard frames
        if (framesToIgnoreLaunch > 0)
        {
            framesToIgnoreLaunch--;
            InputManager.Instance.ConsumeSkillPressed(4); // Drain every guard frame
            return;
        }

        // Oscillate aim angle
        aimAngle += aimDirection * skillSettings.aimOscillationSpeed * Time.deltaTime;

        if (aimAngle >= 90f) { aimAngle = 90f; aimDirection = -1f; }
        if (aimAngle <= -90f) { aimAngle = -90f; aimDirection = 1f; }

        // Update arrow position + rotation
        if (arrowInstance != null)
        {
            arrowInstance.transform.position = player.transform.position + Vector3.up * 1.5f;
            arrowInstance.transform.localRotation = Quaternion.Euler(aimAngle, 0f, 0f);
        }

        // Launch direction from aim angle
        // aimAngle =   0 → straight up   (Y=1, Z=0)
        // aimAngle =  90 → forward        (Y=0, Z=-1)
        // aimAngle = -90 → backward       (Y=0, Z=+1)
        currentLaunchDir = new Vector3(
            0f,
            Mathf.Cos(aimAngle * Mathf.Deg2Rad),
            Mathf.Sin(-aimAngle * Mathf.Deg2Rad)
        );

        if (InputManager.Instance.UltiPressed())
        {
            LaunchRocket(currentLaunchDir);
        }
    }

    // -------------------------------------------------------------------------
    private void LaunchRocket(Vector3 launchDir)
    {
        isAiming = false;
        isFlying = true;
        player.isRocketActive = true;
        Physics.IgnoreLayerCollision(12, 14, true);
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
            arrowInstance = null;
        }

        player.Rb.linearVelocity = Vector3.zero;
        currentSpeed = skillSettings.rocketSpeed;

        Vector3 initialVelocity = launchDir.normalized * currentSpeed;
        player.Rb.linearVelocity = initialVelocity;
        player.SetZMomentum(initialVelocity.z);

        StartCoroutine(RocketTimer());
    }

    // -------------------------------------------------------------------------
    // Phase 1: constant speed (impact feel)
    // Phase 2: speed ramps up with ease-in curve (surge feel)
    // Phase 3: brief hold at peak, then return control
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

        // Phase 2 — accelerate
        elapsed = 0f;
        float startSpeed = skillSettings.rocketSpeed;
        float endSpeed = skillSettings.rocketMaxSpeed;
        float accelDuration = skillSettings.rocketAccelerationDuration;

        while (elapsed < accelDuration)
        {
            float t = elapsed / accelDuration;
            float eased = t * t; // ease-in: slow start, dramatic finish
            currentSpeed = Mathf.Lerp(startSpeed, endSpeed, eased);
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentSpeed = endSpeed;

        // Phase 3 — brief peak hold
        yield return new WaitForSeconds(skillSettings.rocketPeakHoldDuration);

        // Clean up
        isFlying = false;
        player.isAttacking = false;
        player.isRocketActive = false;
        Physics.IgnoreLayerCollision(12, 14, false);

        player.ChangeState<WalkingState>();
        Destroy(this);
    }

}
