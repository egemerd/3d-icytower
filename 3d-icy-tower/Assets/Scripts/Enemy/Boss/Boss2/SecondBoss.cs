using UnityEngine;

public class SecondBoss : Boss
{
    private enum SecondBossState { Idle, AttackOne, AttackTwo, Death }
    private SecondBossState currentState;

    [Header("Idle Settings")]
    [SerializeField] private float idleDuration = 2f;
    private float idleTimer;

    private Rigidbody rb;

    private BossAttackOne attackOne;
    private BossAttackTwo attackTwo;


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

        EnterState(SecondBossState.Idle);
    }

    protected override void RunStateMachine()
    {
        switch (currentState)
        {
            case SecondBossState.Idle: StateIdle(); break;
            case SecondBossState.AttackOne: StateAttackOne(); break;
            case SecondBossState.AttackTwo: StateAttackTwo(); break;
            case SecondBossState.Death: break;
        }
    }

    private void EnterState(SecondBossState newState)
    {
        rb.linearVelocity = Vector3.zero;
        currentState = newState;

        switch (newState)
        {
            case SecondBossState.Idle:
                Debug.Log("Entered Idle state");
                idleTimer = idleDuration;
                break;

            case SecondBossState.AttackOne:
                StartCoroutine(attackOne.Execute(() => EnterState(SecondBossState.Idle)));
                Debug.Log("Entered AttackOne state");
                break;

            case SecondBossState.AttackTwo:
                Debug.Log("Entered AttackTwo state");
                StartCoroutine(attackTwo.Execute(() => EnterState(SecondBossState.Idle)));
                break;
        }
    }

    public override void OnKilled(int damage)
    {
        if (currentState == SecondBossState.Idle)
        {
            TakeDamage(damage);
        }
    }
    private void StateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
            EnterState(PickNextState());
    }

    private void StateAttackOne()
    {
        // TODO: AttackOne logic
    }

    private void StateAttackTwo()
    {
        // TODO: AttackTwo logic
    }

    private SecondBossState PickNextState()
    {
        int rand = Random.Range(0, 2);
        return rand == 0 ? SecondBossState.AttackOne : SecondBossState.AttackTwo;
    }

    private void TriggerSecondBossDeath()
    {
        EnterState(SecondBossState.Death);
        GameEvents.current.TriggerSecondBossDeathAnimationEnd();
    }

    public override void EnemyAttack() { }
    public override void EnemyMovement() { }
}