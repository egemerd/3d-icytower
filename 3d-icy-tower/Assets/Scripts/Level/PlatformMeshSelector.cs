using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformMeshSelector : MonoBehaviour
{
    [SerializeField] private GameObject biom01Platform;
    [SerializeField] private GameObject biom02Platform;
    [SerializeField] private GameObject biom03Platform;

    private void Start()
    {
        int currentBiom = SceneManager.GetActiveScene().buildIndex;
        SelectPlatformMesh(currentBiom);
    }

    private void SelectPlatformMesh(int biom)
    {
        biom01Platform.SetActive(biom == 2 || biom== 3); 
        biom02Platform.SetActive(biom == 4);
        biom03Platform.SetActive(biom == 6);
    }
}