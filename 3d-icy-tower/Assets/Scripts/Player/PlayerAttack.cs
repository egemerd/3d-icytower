using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private IStateMachine stateMachine;

    [Header("Enemy Detection")]
    [SerializeField] private float scanRadius = 10f;
    [SerializeField] private LayerMask targetLayer;
    private Collider[] scanResults = new Collider[5]; // Ayný anda max 5 hedef
    private ITargetable currentTarget;

    [Header("UI Visuals")]
    [SerializeField] private Transform scanCircleTransform; // 2D Çemberi tutan Transform
    [SerializeField] private SpriteRenderer scanCircleRenderer;

    [Header("Attack Feel Settings")]
    [SerializeField] private float dashDuration = 0.15f; // Düþmana ne kadar sürede varacak?
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Dash'in ivmesi

    [Header("Hitstop Settings")]
    [SerializeField] private float hitstopTriggerPercent = 0.85f; // Dash'in yolunun % kaçýnda zaman dursun? (0.85 = %85)
    [SerializeField] private float hitstopDuration = 0.1f; // Vurunca oyun ne kadar süre donacak?
    [SerializeField] private float hitstopTimeScale = 0.05f;

    private bool isAttacking = false;


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

        if (currentTarget != null && InputManager.Instance.attackAction.WasPressedThisFrame())
        {
            stateMachine.ChangeState<AttackingState>();
            // Artýk düz metod yerine Coroutine baþlatýyoruz
            StartCoroutine(AttackCoroutine(currentTarget));

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

        //Eðer hedefimiz yoksa veya menzilden çýktýysa yeni bir tane ara
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
        var target= GetFirstEntryTarget();
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
            currentTarget?.OnLockOff();
            currentTarget = target;
            currentTarget?.OnLockOn();
        }
    }


    private IEnumerator AttackCoroutine(ITargetable target)
    {
        isAttacking = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = target.GetTransform().position;

        float elapsed = 0f;
        bool hitstopActivated = false;

        // FAZ 1: Düþmana Doðru Dash
        while (elapsed < dashDuration)
        {
            // Zaman yavaþlamasýndan etkilenmemek için unscaledDeltaTime kullanýyoruz.
            // Böylece oyun yavaþlasa bile bizim kameramýz/dashimiz akýcý kalýr.
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / dashDuration;

            // Önceden belirlenen % (örn: %85) noktasýna gelince Hitstop tetikle
            if (t >= hitstopTriggerPercent && !hitstopActivated)
            {
                hitstopActivated = true;
                yield return StartCoroutine(HitstopCoroutine()); // Zamaný bük ve bekle
            }

            // Çarpýþmayý hesapla (Curve kullanarak)
            float curveValue = dashCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(startPos, endPos, curveValue);

            yield return null;
        }

        transform.position = endPos;

        // FAZ 2: Vuruþun Kesinleþmesi
        target.OnKilled();
        currentTarget = null;
        SetCircleColor(Color.white);

        // FAZ 3: Sýçrama ile Dash'ten çýkýþ
        FinishAttack();
    }

    private IEnumerator HitstopCoroutine()
    {
        Time.timeScale = hitstopTimeScale; // Evreni durdur/yavaþlat

        float timer = 0f;
        while (timer < hitstopDuration)
        {
            // Bizim bekleme süremiz gerçek saniyeler üzerinden iþlesin (TimeScale 0 olsa bile)
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f; // Evreni normale döndür
    }

    private void FinishAttack()
    {
        isAttacking = false;

        if (TryGetComponent(out PlayerController player))
        {
            // Yerçekimi sýfýrlandýktan sonra karaktere darbe (boost) ekle
            Vector3 vel = player.Rb.linearVelocity;
            vel.y = 0;
            player.Rb.linearVelocity = vel;

            player.Rb.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);

            stateMachine.ChangeState<JumpingState>();
        }
    }

    

    private void SetCircleColor(Color color)
    {
        if (scanCircleRenderer != null)
        {
            scanCircleRenderer.color = new Color(color.r, color.g, color.b, 0.3f); // %30 saydam
        }
    }

    private void UpdateScanCircleSize()
    {
        if (scanCircleTransform != null)
        {
            float diameter = scanRadius * 2f;
            scanCircleTransform.localScale = new Vector3(diameter, diameter, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        // Physics.OverlapSphere ve Vector3.Distance'ýn gerçekten taradýðý ALAN:
        Gizmos.color = new Color(0, 1, 0, 0.2f); // Yarý saydam YEÞÝL
        Gizmos.DrawSphere(transform.position, scanRadius);

        // Kenar hatlarýný daha iyi görmek için bir tel çerçeve çizelim (WireSphere)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }

    
}
