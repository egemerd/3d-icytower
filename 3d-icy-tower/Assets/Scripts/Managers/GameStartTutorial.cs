using UnityEngine;

public class GameStartTutorial : MonoBehaviour
{
    [Header("Tutorial UI Panels / Images")]
    [SerializeField] private GameObject[] tutorialImages;
    private int currentIndex = 0;

    private void Start()
    {
        if (GameSessionState.tutorialShown)
        {
            gameObject.SetActive(false);
            return;
        }

        ShowImage(currentIndex);
    }

    private void Update()
    {
        if (InputManager.Instance.jumpAction.WasPressedThisFrame())
            NextImage();
    }

    private void NextImage()
    {
        currentIndex++;
        if (currentIndex < tutorialImages.Length)
        {
            ShowImage(currentIndex);
        }
        else
        {
            GameSessionState.tutorialShown = true;
            gameObject.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();
        }
    }

    private void ShowImage(int index)
    {
        for (int i = 0; i < tutorialImages.Length; i++)
        {
            if (tutorialImages[i] != null)
                tutorialImages[i].SetActive(i == index);
        }
    }
}

public static class GameSessionState
{
    public static bool tutorialShown = false;
}