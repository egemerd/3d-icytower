using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGameStarted { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        Time.fixedDeltaTime = 0.02f; // enforces 50Hz physics, every scene        
    }


    private void Start()
    {
        //Time.timeScale = 0f;
    }

    public void StartGame()
    {
        IsGameStarted = true;
        Time.timeScale = 1f; // Unfreeze the game!
        Debug.Log("Game Started!");
    }
}
