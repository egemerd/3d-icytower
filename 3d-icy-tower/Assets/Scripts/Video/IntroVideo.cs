using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class IntroVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    private bool hasSkipped = false;
    private VideoInput inputActions; // kendi asset adýn ne ise onu yaz

    private void Start()
    {
        inputActions = new VideoInput();
        inputActions.Video.Skip.performed += _ => SkipToMenu();
        inputActions.Video.Enable();

        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        SkipToMenu();
    }

    private void SkipToMenu()
    {
        if (hasSkipped) return;
        hasSkipped = true;
        videoPlayer.Stop();
        SceneManager.LoadScene(1);
    }

    private void OnDestroy()
    {
        inputActions.Video.Skip.performed -= _ => SkipToMenu();
        inputActions.Video.Disable();
        inputActions.Dispose();
        videoPlayer.loopPointReached -= OnVideoEnd;
    }
}