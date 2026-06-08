using UnityEngine;
using UnityEngine.EventSystems;

public class ArrowFollower : MonoBehaviour
{
    [Header("Real UI Buttons (Canvas)")]
    [SerializeField] private GameObject realPlayButton;
    [SerializeField] private GameObject realQuitButton;
    [SerializeField] private GameObject realEasyButton;
    [SerializeField] private GameObject realHardButton;

    [Header("3D Mesh Visuals (In Scene)")]
    [SerializeField] private Transform meshPlayTransform;
    [SerializeField] private Transform meshQuitTransform;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(-2f, 0f, 0f); // Okun mesh'in ne kadar solunda duracaðý
    [SerializeField] private float moveSpeed = 10f; // Okun geçiþ yumuþaklýðý (Ýstemezsen direkt de ýþýnlayabilirsin)

    private void Update()
    {
        // EventSystem'den þu an gamepad ile seçili olan gerçek Canvas butonunu alýyoruz
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null) return;

        // Hedef konumu belirle
        Vector3 targetPosition = transform.position;

        if (currentSelected == realPlayButton)
        {
            targetPosition = meshPlayTransform.position + offset;
        }
        else if (currentSelected == realQuitButton)
        {
            targetPosition = meshQuitTransform.position + offset;
        }
        else if (currentSelected == realEasyButton)
        {
            targetPosition = meshPlayTransform.position + offset; // Easy butonu Play butonunun yanýnda
        }
        else if (currentSelected == realHardButton)
        {
            targetPosition = meshQuitTransform.position + offset; // Hard butonu Quit butonunun yanýnda
        }

        // Oku hedef konuma yumuþakça kaydýr (Illüzyonu güzelleþtirir)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }
}