using System;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    public static GameEvents current;

    private void Awake()
    {
        // Singleton güvenliði
        if (current != null && current != this)
        {
            Destroy(gameObject);
            return;
        }
        current = this;
        DontDestroyOnLoad(gameObject);
    }

    public event Action onEnemyDetected;
    public event Action onEnemyDead;    
    public event Action onGameOver;
    public event Action onPlayerDead;
    public event Action onBossDead;
    public event Action onBossDeathAnimationEnd;
    public event Action onSecondBossDeathAnimationEnd;
    public event Action onSecondBossDeath;

    public void TriggerEnemyDetection()
    {
        Debug.Log("TriggerEnemyDetection called.");
        onEnemyDetected?.Invoke();
    }
    public void TriggerEnemyDeath()
    {
        onEnemyDead?.Invoke();
    }

    public void TriggerGameOver()
    {
        Debug.Log("TriggerGameOver triggered.");
        onGameOver?.Invoke();
    }

    public void TriggerPlayerDead()
    { 
        onPlayerDead?.Invoke();
    }   

    public void TriggerBossDeath()
    {
        Debug.Log("TriggerBossDeath triggered.");
        onBossDead?.Invoke();
    }
    public void TriggerBossDeathAnimationEnd()
    {
        Debug.Log("TriggerBossDeathAnimationEnd triggered.");
        onBossDeathAnimationEnd?.Invoke();
    }

    public void TriggerSecondBossDeathAnimationEnd()
    {
        Debug.Log("TriggerSecondBossDeathAnimationEnd triggered.");
        onSecondBossDeathAnimationEnd?.Invoke();
    }
    public void TriggerSecondBossDeath()
    {
        Debug.Log("TriggerSecondBossDeath triggered.");
        onSecondBossDeath?.Invoke();
    }
}
