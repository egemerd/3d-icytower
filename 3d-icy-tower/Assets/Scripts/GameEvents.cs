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

    public void TriggerEnemyDetection()
    {
        Debug.Log("TriggerEnemyDetection called.");
        onEnemyDetected?.Invoke();
    }
}
