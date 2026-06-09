using System.Collections;
using UnityEngine;
// Input sistemi kütüphanenizin ekli olduğundan emin olun (örn: using UnityEngine.InputSystem;)

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
    [SerializeField] private Transform timingUiTransform; // Yeni: Daralan veya büyüyen zamanlama halkası

    [Header("Attack Feel Settings")]
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timing Attack Settings")]
    [SerializeField] private float timingWindowStart = 0.4f; // Saniyenin % kaçında pencere açılsın?
    [SerializeField] private float timingWindowEnd = 0.6f;   // Saniyenin % kaçında pencere kapansın?
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

    [Header("Attack Damage")]
    [SerializeField] private int attackDamage = 1;

    private bool isAttacking = false;
    float lockOnDelay = 1f;
    private Camera mainCamera;

    ITargetable enemy;

    private EnergySystem energySystem;

    [Header("Perfect Attack Combo Sound")]
    [SerializeField] private float pitchIncreasePerCombo = 0.15f;
    [SerializeField] private int maxComboCount = 5;

    private int perfectComboCount = 0;

    public static bool BossIntroActive = false;

    private void Awake()
    {
        stateMachine = GetComponent<IStateMachine>();
        playerController = GetComponent<PlayerController>();
        energySystem = GetComponent<EnergySystem>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        //UpdateScanCircleSize();
    }

    private void Update()
    {
        if (isAttacking) return;
        if (BossIntroActive) return;

        ScanForTarget();

        if (currentTarget != null && InputManager.Instance.attackAction.WasPressedThisFrame())
        {
            if (currentTarget.IsInPerfectWindow)
            {
                Debug.Log("PERFECT ATTACK!");
                currentTarget.PerfectAttack();
                currentTarget.StopTimingUI();
                stateMachine.ChangeState<AttackingState>();
                energySystem?.AddPerfectAttackEnergy();

                perfectComboCount = Mathf.Min(perfectComboCount + 1, maxComboCount);
                float pitch = 1f + (perfectComboCount - 1) * pitchIncreasePerCombo;
                PlayPerfectSound(pitch);

                StartCoroutine(AttackCoroutine(currentTarget,true)); // Belki extra parametre geçebilirsin bool isPerfect
            }
            else if (currentTarget.IsInTimingWindow)
            {
                Debug.Log("NORMAL ATTACK!");
                perfectComboCount = 0;
                currentTarget.StopTimingUI();
                stateMachine.ChangeState<AttackingState>();
                StartCoroutine(AttackCoroutine(currentTarget,false));
            }
            else
            {
                perfectComboCount = 0;
                Debug.Log("Miss!"); // Çok erken veya çok geç basıldı.
            }
        }
    }

    private void PlayPerfectSound(float pitch)
    {
        AudioSource src = SoundManager.GetAudioSource();
        if (src == null) return;

        AudioClip[] clips = SoundManager.GetClips(SoundType.PLAYERPERFECTATTACK);
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        src.clip = clip;
        src.pitch = pitch;
        src.volume = 0.2f;
        src.Play();
    }

    public ITargetable GetFirstEntryTarget()
    { 
        if (currentTarget != null)
        {
            Transform targetTransform = currentTarget.GetTransform();
            if (targetTransform != null)
            {
                float dist = Vector3.Distance(transform.position, targetTransform.position);

                // Hem menzil içinde hem de ekranda hala görünüyorsa tut
                if (dist <= scanRadius && IsTargetInCameraView(targetTransform.position))
                {
                    return currentTarget;
                }
            }
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, scanResults, targetLayer);

        for (int i = 0; i < count; i++)
        {
            if (scanResults[i].TryGetComponent(out ITargetable target))
            {
                Transform t = target.GetTransform();
                if (t != null && IsTargetInCameraView(t.position))
                {
                    return target;
                }
            }
        }
        return null;
    }

    private bool IsTargetInCameraView(Vector3 targetPosition)
    {
        if (mainCamera == null) return false;

        // Dünya pozisyonunu ekran Viewport (0-1 arası) pozisyonuna çevir
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(targetPosition);

        // x ve y 0 ile 1 arasındaysa ekranın içindedir.
        // z > 0 olması kameranın "önünde" olduğunu, arkasında kalmadığını belirtir.
        bool inScreenBounds = viewportPoint.z > 0f
                           && viewportPoint.x > 0f && viewportPoint.x < 1f
                           && viewportPoint.y > 0f && viewportPoint.y < 1f;

        return inScreenBounds;
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
                currentTarget.StartTimingUI(); // YENİ: Start timing UI over the enemy immediately!
            }
        }
    }

    private IEnumerator AttackCoroutine(ITargetable target, bool isPerfect = false)
    {
        playerController.animator.SetTrigger("Attack");
        isAttacking = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = target.GetTransform().position;
        SoundManager.PlaySound(SoundType.PLAYERATTACK, 0.15f);
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
        playerController.Rb.position = endPos;  // ← ekle
        playerController.Rb.linearVelocity = Vector3.zero;  // ← ekle
        playerController.SetZMomentum(0f);  // ← ekle

        

        ParticleEffects.Instance.PlayOneShot(ParticleType.HitEffect, target.GetTransform().position);
        //ParticleEffects.Instance.PlayOneShot(ParticleType.HitEffect2, target.GetTransform().position);
        TimeStop.Instance.StopTime(0.04f, 0.1f);
        currentTarget = null;
        SetCircleColor(Color.white);
        enemy = target;
        FinishAttack(isPerfect);
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

    private void FinishAttack(bool isPerfect)
    {
        isAttacking = false;
        playerController.animator.ResetTrigger("Attack");
        enemy.OnKilled(attackDamage);
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
            player.EnableJumpTrail(isPerfect);
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