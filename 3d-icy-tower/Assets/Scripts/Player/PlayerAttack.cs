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
            DashAttack(currentTarget);
        }
    }

    public ITargetable GetFirstEntryTarget()
    {
        // 1. ADIM: Mevcut bir hedefimiz var mý ve hala menzilde mi?
        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.GetTransform().position);
            if (dist <= scanRadius)
            {
                // Hedef hala menzilde, onu döndürmeye devam et (Deðiþtirme!)
                return currentTarget;
            }
            else
            {
                // Hedef menzilden çýktý, temizle
                currentTarget.OnLockOff();
                currentTarget = null;
            }
        }

        // 2. ADIM: Eðer hedefimiz yoksa veya menzilden çýktýysa yeni bir tane ara
        int count = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, scanResults, targetLayer);

        for (int i = 0; i < count; i++)
        {
            if (scanResults[i].TryGetComponent(out ITargetable target))
            {
                // Menzile giren ÝLK geçerli hedefi seç ve hafýzaya al
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
                SetCircleColor(Color.red); // Hedef bulundu! (Kýrmýzý)
            }
            else
            {
                SetCircleColor(Color.white); // Hedef yok (Beyaz/Normal)
            }
            currentTarget?.OnLockOff();
            currentTarget = target;
            currentTarget?.OnLockOn();
        }
    }

    public void DashAttack(ITargetable target)
    {
        
        transform.position = target.GetTransform().position;
        target.OnKilled();
        currentTarget = null;
        SetCircleColor(Color.white);
    }

    private IEnumerator AttackCoroutine()
    {
        yield return null; 
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
            // Unity'de standart bir Circle Sprite çapý genelde 1 birimdir. 
            // Yarýçapý 'scanRadius' yapmak için scale deðerini Radius'un 2 katý yapmalýyýz.
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
