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
        currentPhase = BossPhase.Phase1;
        OnBossStart(); // child'ın kendi Start'ı
    }

    private void Update()
    {
        CheckPhase();
        RunStateMachine(); // child implement eder
    }

    // ── PHASE ──────────────────────────
    private void CheckPhase()
    {
        float hp = currentHp / maxHp;

        if (hp > 0.6f) TryEnterPhase(BossPhase.Phase1);
        else if (hp > 0.3f) TryEnterPhase(BossPhase.Phase2);
        else TryEnterPhase(BossPhase.Phase3);
    }

    private void TryEnterPhase(BossPhase phase)
    {
        if (currentPhase == phase) return;
        currentPhase = phase;
        OnPhaseChanged(phase);
    }

    // ── CHILD'IN DOLDURACAĞI METODLAR ──
    protected abstract void RunStateMachine();          // state machine tamamen child'da
    protected virtual void OnPhaseChanged(BossPhase phase) { } // opsiyonel
    protected virtual void OnBossStart() { }            // opsiyonel start

    // ── DAMAGE ─────────────────────────
    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        if (currentHp <= 0) OnKilled();
    }
}
