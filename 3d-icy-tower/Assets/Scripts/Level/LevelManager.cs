using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Level Settings")]
    [SerializeField] private float nextLevelHeight = 1000f;

    [Header("Level Type")]
    [SerializeField] private bool isBossLevel = false;
    [SerializeField] private bool isEndlessLevel = false;

    [Header("Boss Level Settings")]
    [SerializeField] private bool loadNextSceneOnBossDeath = true;

    private bool bossDefeated;
    private bool isLoadingScene;

    public float NextLevelHeight => nextLevelHeight;


    private void Start()
    {
        GameEvents.current.onBossDeathAnimationEnd += OnBossDead;

    }

    private void OnDestroy()
    {
        GameEvents.current.onBossDeathAnimationEnd -= OnBossDead;
    }
    private void OnEnable()
    {
        GameEvents.current.onGameOver += RestartScene;
        
    }

    private void OnDisable()
    {
        GameEvents.current.onGameOver -= RestartScene;

    }


    private void Update()
    {
        if (isEndlessLevel)
        {
            if (playerTransform.position.y > nextLevelHeight)
            {
                LoadNextScene();
            }
        }
        if (isBossLevel && bossDefeated && loadNextSceneOnBossDeath)
        {
            LoadNextScene();
        }
    }

    private void OnBossDead()
    {
        bossDefeated = true;
    }

    private void LoadNextScene()
    {
        if (isLoadingScene) return;

        isLoadingScene = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
