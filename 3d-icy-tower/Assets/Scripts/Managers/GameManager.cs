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
    }

    private void Start()
    {
        // For example: Freeze time until tutorial is done
        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        IsGameStarted = true;
        Time.timeScale = 1f; // Unfreeze the game!
        Debug.Log("Game Started!");
    }
}
