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
}
