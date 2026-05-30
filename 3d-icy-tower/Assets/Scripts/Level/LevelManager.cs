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


    public float NextLevelHeight => nextLevelHeight;
    private void Update()
    {
        if (isEndlessLevel)
        {
            if (playerTransform.position.y > nextLevelHeight)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        if (isBossLevel)
        {
            
        }
    }
}
