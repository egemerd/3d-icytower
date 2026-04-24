using UnityEngine;

public class GameStartTutorial : MonoBehaviour
{
    [Header("Tutorial UI Panels / Images")]
    [SerializeField] private GameObject[] tutorialImages; // 3 resminizi/panelinizi Inspector'dan buraya sürükleyin

    private int currentIndex = 0;

    private void Start()
    {
        // Baþlangýçta sadece ilk resmi aç, diðerlerini kapat
        ShowImage(currentIndex);
    }

    private void Update()
    {
        // 1 = Sað Click (Right Mouse Button)
        if (InputManager.Instance.attackAction.WasPressedThisFrame())
        {
            NextImage();
        }
    }

    private void NextImage()
    {
        currentIndex++;

        if (currentIndex < tutorialImages.Length)
        {
            // Eðer hala gösterecek resim varsa onu göster
            ShowImage(currentIndex);
        }
        else
        {
            // Bütün resimler bittiyse Canvas'ý gizle ve oyunu baþlat
            gameObject.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }
    }

    private void ShowImage(int index)
    {
        // Tüm listeyi dönüp, sadece sýrasý gelen index'teki objeyi aktif yapýyoruz
        for (int i = 0; i < tutorialImages.Length; i++)
        {
            if (tutorialImages[i] != null)
            {
                tutorialImages[i].SetActive(i == index);
            }
        }
    }
}
