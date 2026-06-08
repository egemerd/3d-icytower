using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private LevelSettings levelSO;
    [SerializeField] private GameObject firstSelected;

    [SerializeField] private GameObject secondSelected;

    [SerializeField] private GameObject textPlay;
    [SerializeField] private GameObject textQuit;
    [SerializeField] private GameObject textEasy;
    [SerializeField] private GameObject textHard;

    [SerializeField] private GameObject firstButtons;
    [SerializeField] private GameObject secondButtons;

    [SerializeField] private int easyHeight = 500;
    [SerializeField] private int hardHeight = 1500;

    private void Start()
    {
        if (firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void PlayGame()
    {
        firstButtons.SetActive(false);
        secondButtons.SetActive(true);

        textPlay.SetActive(false);
        textQuit.SetActive(false);

        textEasy.SetActive(true);
        textHard.SetActive(true);
        EventSystem.current.SetSelectedGameObject(secondSelected);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Easy()
    {
        levelSO.nextLevelHeight = easyHeight;
        SceneManager.LoadScene(2);
    }

    public void Hard()
    {
        levelSO.nextLevelHeight = hardHeight;
        SceneManager.LoadScene(2);
    }
}