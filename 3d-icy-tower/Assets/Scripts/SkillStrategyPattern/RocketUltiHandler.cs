using UnityEngine;
using System.Collections;

public class RocketUltiHandler : MonoBehaviour
{
    private PlayerController player;
    private UltiSkill skillSettings;
    private GameObject arrowInstance;

    private bool isAiming = false;
    private bool isFlying = false;
    private float aimAngle = 0f;
    private float aimDirection = 1f;
    private bool canLaunch = false;

    private void Update()
    {
        if (isAiming)
        {
            HandleAiming();
        }
    }

    private void FixedUpdate()
    {      
        if (isFlying)
        {
            // Maintain continuous speed in its current heading (bounces will reflect naturally)
            Vector3 vel = player.Rb.linearVelocity;
            
            // If the player hit a dead corner, fallback so they don't get stuck at 0 velocity
            if (vel.magnitude < 0.1f) vel = Vector3.up;

            player.Rb.linearVelocity = vel.normalized * skillSettings.rocketSpeed;
            player.SetZMomentum(player.Rb.linearVelocity.z);
        }
    }

    public void StartUlti(UltiSkill settings)
    {
        player = GetComponent<PlayerController>();
        skillSettings = settings;

        // Put player in attacking state to completely bypass PlayerController's FixedUpdate logic
        player.ChangeState<AttackingState>();

        // Spawn Arrow Indicator
        if (skillSettings.arrowPrefab != null)
        {
            arrowInstance = Instantiate(skillSettings.arrowPrefab, player.transform.position + Vector3.up * 1.5f, Quaternion.identity);
        }

        aimAngle = 0f; // 0 represents completely UP
        isAiming = true;
        canLaunch = false;

        // Clear the initial press that started the ulti
        InputManager.Instance.ConsumeSkillPressed(4);
        StartCoroutine(EnableLaunchNextFrame());
    }

    private void HandleAiming()
    {
        aimAngle += aimDirection * 100f * Time.deltaTime; // Adjusted speed for shorter rotation span

        // Limit the rotation from -45 to 45 degrees
        if (aimAngle >= 90f) { aimAngle = 90f; aimDirection = -1f; }
        if (aimAngle <= -90f) { aimAngle = -90f; aimDirection = 1f; }

        if (arrowInstance != null)
        {
            // Rotate strictly on the local X axis.
            // 0 degrees on local X means aiming upwards (assuming your arrow prefab's default "Up" is established).
            // A positive/negative rotation on X will tilt it correctly.
            arrowInstance.transform.localRotation = Quaternion.Euler(aimAngle, 0f, 0f);
        }

        Vector3 launchDir = new Vector3(0, Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(-aimAngle * Mathf.Deg2Rad));

        // Listen for the second click/jump to Fire
        if (canLaunch && InputManager.Instance.ConsumeSkillPressed(4))
        {
            LaunchRocket(launchDir);
        }
    }

    private IEnumerator EnableLaunchNextFrame()
    {
        yield return null;
        canLaunch = true;
    }

    private void LaunchRocket(Vector3 launchDir)
    {
        Debug.Log("Rocket Launched in direction: " + launchDir);
        isAiming = false;
        isFlying = true;

        if (arrowInstance != null) Destroy(arrowInstance);

        // Apply massive velocity entirely along the Y/Z plane
        player.Rb.linearVelocity = launchDir.normalized * skillSettings.rocketSpeed;
        player.SetZMomentum(player.Rb.linearVelocity.z);

        StartCoroutine(RocketTimer());
    }

    private IEnumerator RocketTimer()
    {
        // Wait for the duration where speed does not drop
        yield return new WaitForSeconds(skillSettings.rocketDuration);

        // Clean up and return normal combat/walking control to the player
        isFlying = false;
        player.isAttacking = false; 
        player.ChangeState<WalkingState>(); // Returns physics functionality to the main script

        Destroy(this); // Remove this helper behavior completely
    }
}
