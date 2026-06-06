using UnityEngine;
using System.Collections;
using DG.Tweening;

public class SecondBoss : Boss
{
    private enum SecondBossState { Idle, AttackOne, AttackTwo, Death }
    private SecondBossState currentState;

    [Header("Idle Settings")]
    [SerializeField] private float idleDuration = 2f;
    private float idleTimer;

    [Header("State Weights")]
    [SerializeField] private float weightIdle = 1f;
    [SerializeField] private float weightAttackOne = 2f;
    [SerializeField] private float weightAttackTwo = 2f;

    [Header("Face Rotations")]
    [Tooltip("Dönen child obje (zar mesh'i)")]
    [SerializeField] private Transform diceBody;
    [Tooltip("Idle yüzünün hedef rotasyonu")]
    [SerializeField] private Vector3 idleFaceRotation = new Vector3(0f, 0f, 0f);
    [Tooltip("AttackOne yüzünün hedef rotasyonu")]
    [SerializeField] private Vector3 attackOneFaceRotation = new Vector3(90f, 0f, 0f);
    [Tooltip("AttackTwo yüzünün hedef rotasyonu")]
    [SerializeField] private Vector3 attackTwoFaceRotation = new Vector3(0f, 90f, 0f);

    [Header("Roll Animation")]
    [Tooltip("Sağa/sola sallanma açısı (derece)")]
    [SerializeField] private float wobbleAngle = 25f;
    [Tooltip("Sallanma süresi (tek yön)")]
    [SerializeField] private float wobbleDuration = 0.18f;
    [Tooltip("Kaç kez sallansın")]
    [SerializeField] private int wobbleCount = 3;
    [Tooltip("Final rotasyona geçiş süresi")]
    [SerializeField] private float snapDuration = 0.35f;
    [SerializeField] private Ease snapEase = Ease.OutBack;

    private Rigidbody rb;
    private BossAttackOne attackOne;
    private BossAttackTwo attackTwo;
    private bool isRolling = false;

    protected override void Start()
    {
        base.Start();
        GameEvents.current.onSecondBossDeath += TriggerSecondBossDeath;
    }

    private void OnDestroy()
    {
        GameEvents.current.onSecondBossDeath -= TriggerSecondBossDeath;
    }

    protected override void OnBossStart()
    {
        rb = GetComponent<Rigidbody>();
        attackTwo = GetComponent<BossAttackTwo>();
        attackOne = GetComponent<BossAttackOne>();
        rb.useGravity = false;

        // Başlangıçta idle yüzüne snap et (animasyonsuz)
        diceBody.localRotation = Quaternion.Euler(idleFaceRotation); 
        EnterState(SecondBossState.Idle);
    }

    protected override void RunStateMachine()
    {
        switch (currentState)
        {
            case SecondBossState.Idle: StateIdle(); break;
            case SecondBossState.AttackOne: break;
            case SecondBossState.AttackTwo: break;
            case SecondBossState.Death: break;
        }
    }

    // ── State geçişi: önce animasyon, sonra attack başlar ───────────────
    private void EnterState(SecondBossState newState)
    {
        rb.linearVelocity = Vector3.zero;
        currentState = newState;

        switch (newState)
        {
            case SecondBossState.Idle:
                StartCoroutine(RollThenExecute(
                idleFaceRotation,
                    () => idleTimer = idleDuration
                 ));
                break;
            case SecondBossState.AttackOne:
                StartCoroutine(RollThenExecute(
                    attackOneFaceRotation,
                    () => StartCoroutine(attackOne.Execute(() => EnterState(SecondBossState.Idle)))
                ));
                break;

            case SecondBossState.AttackTwo:
                StartCoroutine(RollThenExecute(
                    attackTwoFaceRotation,
                    () => StartCoroutine(attackTwo.Execute(() => EnterState(SecondBossState.Idle)))
                ));
                break;
        }
    }

    // ── Sallanma animasyonu → hedef rotasyon → callback ─────────────────
    private IEnumerator RollThenExecute(Vector3 targetEuler, System.Action onComplete)
    {
        isRolling = true;
        DOTween.Kill(diceBody);

        Quaternion targetRot = Quaternion.Euler(targetEuler);

        // Her seferinde farklı rastgele eksenlerle çılgın dönüşler
        for (int i = 0; i < wobbleCount; i++)
        {
            // Tamamen rastgele eksen (çapraz, eğik, her yön)
            Vector3 randomAxis = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

            // Her seferinde farklı büyük açı
            float randomAngle = Random.Range(wobbleAngle * 0.6f, wobbleAngle * 1.4f);
            float sign = (i % 2 == 0) ? 1f : -1f;

            Quaternion current = diceBody.localRotation;
            Quaternion wobbleTo = current * Quaternion.AngleAxis(randomAngle * sign, randomAxis);

            bool done = false;
            diceBody.DOLocalRotateQuaternion(wobbleTo, wobbleDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => done = true);
            yield return new WaitUntil(() => done);
        }

        // Final snap
        bool snapDone = false;
        diceBody.DOLocalRotateQuaternion(targetRot, snapDuration)
                .SetEase(snapEase)
                .OnComplete(() => snapDone = true);
        yield return new WaitUntil(() => snapDone);

        isRolling = false;
        onComplete?.Invoke();
    }

    // ── Idle → bir sonraki state ─────────────────────────────────────────
    private void StateIdle()
    {
        if (isRolling) return;
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
            EnterState(PickNextState());
    }

    public override void OnKilled(int damage)
    {
        if (currentState == SecondBossState.Idle && !isRolling)
            TakeDamage(damage);
    }

    private SecondBossState PickNextState()
    {
        float total = weightIdle + weightAttackOne + weightAttackTwo;
        float roll = Random.Range(0f, total);

        if (roll < weightIdle)
            return SecondBossState.Idle;

        roll -= weightIdle;

        if (roll < weightAttackOne)
            return SecondBossState.AttackOne;

        return SecondBossState.AttackTwo;
    }

    private void TriggerSecondBossDeath()
    {
        EnterState(SecondBossState.Death);
        GameEvents.current.TriggerSecondBossDeathAnimationEnd();
    }

    public override void EnemyAttack() { }
    public override void EnemyMovement() { }
}