using System.Collections;
using UnityEngine;
// Input sistemi kütüphanenizin ekli olduðundan emin olun (örn: using UnityEngine.InputSystem;)

public class PlayerAttack : MonoBehaviour
{
    private IStateMachine stateMachine;

    [Header("Enemy Detection")]
    [SerializeField] private float scanRadius = 10f;
    [SerializeField] private LayerMask targetLayer;
    private Collider[] scanResults = new Collider[5];
    private ITargetable currentTarget;
    private PlayerController playerController;

    [Header("UI Visuals")]
    [SerializeField] private Transform scanCircleTransform;
    [SerializeField] private SpriteRenderer scanCircleRenderer;
    [SerializeField] private Transform timingUiTransform; // Yeni: Daralan veya büyüyen zamanlama halkasý

    [Header("Attack Feel Settings")]
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timing Attack Settings")]
    [SerializeField] private float timingWindowStart = 0.4f; // Saniyenin % kaçýnda pencere açýlsýn?
    [SerializeField] private float timingWindowEnd = 0.6f;   // Saniyenin % kaçýnda pencere kapansýn?
    [SerializeField] private float totalTimingDuration = 1f; // Tüm sürecin tamamlanma süresi
    private bool isInTimingWindow = false;
    private bool timingRoutineActive = false;

    [Header("Hitstop Settings")]
    [SerializeField] private float hitstopTriggerPercent = 0.85f;
    [SerializeField] private float hitstopDuration = 0.1f;
    [SerializeField] private float hitstopTimeScale = 0.05f;

    [Header("Post Attack Movement")]
    [SerializeField] private float postAttackJumpForce = 8f;
    [SerializeField] private float postAttackForwardForce = 15f; // Hareket yönüne uygulanacak itme

    private bool isAttacking = false;
    float lockOnDelay = 1f;

    ITargetable enemy;

    private void Awake()
    {
        stateMachine = GetComponent<IStateMachine>();
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        UpdateScanCircleSize();
    }

    private void Update()
    {
        if (isAttacking) return;

        ScanForTarget();

        // Right timing window attack
        if (currentTarget != null && InputManager.Instance.attackAction.WasPressedThisFrame())
        {
            if (currentTarget.IsInTimingWindow) // Check the ENEMY's window
            {
                currentTarget.StopTimingUI(); // Stop the UI on the enemy

                stateMachine.ChangeState<AttackingState>();
                StartCoroutine(AttackCoroutine(currentTarget));
            }
            else
            {
                Debug.Log("Yanlýþ zamanlama!");
            }
        }
    }


    public ITargetable GetFirstEntryTarget()
    {
        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.GetTransform().position);
            if (dist <= scanRadius)
            {
                return currentTarget;
            }
            else
            {
                currentTarget.StopTimingUI(); // Stop Enemy UI on lock-off
                currentTarget.OnLockOff();
                currentTarget = null;
            }
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, scanResults, targetLayer);
        for (int i = 0; i < count; i++)
        {
            if (scanResults[i].TryGetComponent(out ITargetable target))
            {
                return target;
            }
        }
        return null;
    }

    private void ScanForTarget()
    {
        var target = GetFirstEntryTarget();
        if (target != currentTarget)
        {
            if (target != null)
            {
                SetCircleColor(Color.red);
            }
            else
            {
                SetCircleColor(Color.white);
            }

            // Lock off old target
            if (currentTarget != null)
            {
                currentTarget.StopTimingUI();
                currentTarget.OnLockOff();
            }

            currentTarget = target;

            // Lock on new target
            if (currentTarget != null)
            {
                currentTarget.OnLockOn(lockOnDelay);
                currentTarget.StartTimingUI(); // YENÝ: Start timing UI over the enemy immediately!
            }
        }
    }

    private IEnumerator AttackCoroutine(ITargetable target)
    {
        playerController.animator.SetTrigger("Attack");
        isAttacking = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = target.GetTransform().position;

        float elapsed = 0f;
        bool hitstopActivated = false;

        while (elapsed < dashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dashDuration;

            if (t >= hitstopTriggerPercent && !hitstopActivated)
            {
                hitstopActivated = true;
                //yield return StartCoroutine(HitstopCoroutine());
            }

            float curveValue = dashCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(startPos, endPos, curveValue);
            yield return null;
        }

        transform.position = endPos;
        ParticleEffects.Instance.PlayOneShot(ParticleType.HitEffect, target.GetTransform().position);
        ParticleEffects.Instance.PlayOneShot(ParticleType.HitEffect2, target.GetTransform().position);
        TimeStop.Instance.StopTime(0.04f, 0.1f);
        currentTarget = null;
        SetCircleColor(Color.white);
        enemy = target;
        FinishAttack();
    }

    private IEnumerator HitstopCoroutine()
    {
        Time.timeScale = hitstopTimeScale;
        float timer = 0f;
        while (timer < hitstopDuration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1f;
    }

    private void FinishAttack()
    {
        isAttacking = false;
        playerController.animator.ResetTrigger("Attack");
        enemy.OnKilled();
        if (TryGetComponent(out PlayerController player))
        {
            Vector2 inputDir = InputManager.Instance.moveInput;

            float targetZMomentum = inputDir.x * postAttackForwardForce;
            float finalJumpForce = postAttackJumpForce;

            if (inputDir.y > 0.1f)
            {
                finalJumpForce += (inputDir.y * postAttackJumpForce * 0.5f);
            }

            Vector3 vel = player.Rb.linearVelocity;
            vel.y = 0;
            player.Rb.linearVelocity = vel;

            Vector3 jumpDirection = Vector3.up * finalJumpForce;
            player.Rb.AddForce(jumpDirection, ForceMode.VelocityChange);

            player.SetZMomentum(targetZMomentum);
            stateMachine.ChangeState<JumpingState>();
        }
    }

    private void SetCircleColor(Color color)
    {
        if (scanCircleRenderer != null)
            scanCircleRenderer.color = new Color(color.r, color.g, color.b, 0.3f);
    }

    private void UpdateScanCircleSize()
    {
        if (scanCircleTransform != null)
            scanCircleTransform.localScale = new Vector3(scanRadius * 2f, scanRadius * 2f, 1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, scanRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}