using UnityEngine;

public abstract class Boss : Enemy
{
    [Header("Boss Stats")]
    [SerializeField] private float maxHp = 300f;
    private float currentHp;

    protected enum BossPhase { Phase1, Phase2, Phase3 }
    protected BossPhase currentPhase;

    private void Start()
    {
        currentHp = maxHp;
        OnBossStart(); // child'ın kendi Start'ı
    }

    private void Update()
    {
        RunStateMachine(); // child implement eder
    }


    protected abstract void RunStateMachine();        
    protected virtual void OnBossStart() { }  

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        if (currentHp <= 0) OnKilled();
    }
}
