using System.Collections;
using UnityEngine;

public abstract class Boss : Enemy
{
    [Header("Boss Stats")]
    [SerializeField] private float maxHp = 300f;
    private float currentHp;

    protected enum BossPhase { Phase1, Phase2, Phase3 }
    protected BossPhase currentPhase;

    protected virtual void Start()
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

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        if(currentHp <= 0) BossDeath();
        //if (currentHp <= 0) OnKilled(amount);
        Debug.Log($"Boss took {amount} damage, current HP: {currentHp}");
    }

    public void BossDeath()
    {
        //IsDead = true;
        Debug.Log("Boss defeated!");
        StartCoroutine(DelayedDeath());
    }

    private IEnumerator DelayedDeath()
    {
        yield return new WaitForSeconds(0.3f); // oyuncunun jump'ı tamamlaması için bekle
        GameEvents.current.TriggerBossDeath();
    }
}

