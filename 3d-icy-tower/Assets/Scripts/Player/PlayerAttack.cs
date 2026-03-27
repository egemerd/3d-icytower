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

    private void Awake()
    {
        stateMachine = GetComponent<IStateMachine>();
    }

    private void Start()
    {
        UpdateScanCircleSize();
    }

    private void Update()
    {
        if (isAttacking) return;

        ScanForTarget();

        // Eðer bir hedefimiz varsa timing UI'ý baþlat
        if (currentTarget != null && !timingRoutineActive)
        {
            StartCoroutine(TimingWindowCoroutine());
        }

        // Doðru zamanda tuþa basýldýysa attack baþlat
        if (currentTarget != null && InputManager.Instance.attackAction.WasPressedThisFrame())
        {
            if (isInTimingWindow)
            {
                StopAllCoroutines(); // Timing'i durdur
                timingRoutineActive = false;
                timingUiTransform.gameObject.SetActive(false);

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
                currentTarget.OnLockOff();
                currentTarget = null;
            }
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, scanResults, targetLayer);
        for (int i = 0; i < count; i++)
        {
            if (scanResults[i].TryGetComponent(out ITargetable target))
            {
                currentTarget = target;
                return currentTarget;
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
                SetCircleColor(Color.red);
            else
            {
                SetCircleColor(Color.white);
                timingUiTransform.gameObject.SetActive(false); // Hedef çýkarsa UI gizle
                timingRoutineActive = false;
            }

            currentTarget?.OnLockOff();
            currentTarget = target;
            currentTarget?.OnLockOn(lockOnDelay);
        }
    }

    // YENÝ: Zamanlama mantýðýný ve görselini yönetecek Coroutine
    private IEnumerator TimingWindowCoroutine()
    {
        timingRoutineActive = true;
        timingUiTransform.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < totalTimingDuration && currentTarget != null)
        {
            timingUiTransform.position = currentTarget.GetTransform().position;

            elapsed += Time.deltaTime;
            float t = elapsed / totalTimingDuration;

            // UI Görselini güncelle (Örn: Küçülen bir çember)
            timingUiTransform.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one * 0.5f, t);

            // Zamanlama penceresi içinde miyiz kontrolü
            if (t >= timingWindowStart && t <= timingWindowEnd)
            {
                isInTimingWindow = true;
                timingUiTransform.GetComponent<SpriteRenderer>().color = Color.green; // Oyuncuya "þimdi bas" uyarýsý
            }
            else
            {
                isInTimingWindow = false;
                timingUiTransform.GetComponent<SpriteRenderer>().color = Color.yellow;
            }

            yield return null;
        }

        // Süre bittiðinde baþaramadýysa UI'ý kapat ve resetle
        isInTimingWindow = false;
        timingRoutineActive = false;
        timingUiTransform.gameObject.SetActive(false);
    }

    private IEnumerator AttackCoroutine(ITargetable target)
    {
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
                yield return StartCoroutine(HitstopCoroutine());
            }

            float curveValue = dashCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(startPos, endPos, curveValue);
            yield return null;
        }

        transform.position = endPos;
        target.OnKilled();
        currentTarget = null;
        SetCircleColor(Color.white);

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

        if (TryGetComponent(out PlayerController player))
        {
            // InputManager üzerinden Move deðerini alýyoruz
            Vector2 inputDir = InputManager.Instance.moveInput;

            Vector3 jumpDirection = Vector3.up * postAttackJumpForce;
            float targetZMomentum = 0f; 

            // Eðer oyuncu bir yöne basýyorsa ileri/geri (zMomentum ekseni) ayarlamasýný yap
            if (Mathf.Abs(inputDir.x) > 0.1f)
            {
                // Basýlan yönü belirle (1 veya -1)
                float moveDirection = Mathf.Sign(inputDir.x);
                // Yeni Z yön ve kuvveti hesapla
                targetZMomentum = moveDirection * postAttackForwardForce;
            }

            Vector3 vel = player.Rb.linearVelocity;
            vel.y = 0; // Sadece Y'yi sýfýrlýyoruz.
            player.Rb.linearVelocity = vel;

            // Dikey patlama (Zýplama) kuvvetini Rigidbody üzerinden anlýk olarak veriyoruz
            player.Rb.AddForce(jumpDirection, ForceMode.VelocityChange);

            // Eðer oyuncu saldýrý sonunda hareket tuþuna basmýþsa, yatay patlama için Momentum mekaniðini tetikle
            if (Mathf.Abs(targetZMomentum) > 0f)
            {
                player.SetZMomentum(targetZMomentum);
            }

            stateMachine.ChangeState<JumpingState>();
        }
    }

    // ... (SetCircleColor, OnDrawGizmos gibi diðer alt metotlar ayný kalýr)
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