using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] Material[] platformMaterials;

    MeshRenderer meshRenderer;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }
    private void Start()
    {
        meshRenderer.material = platformMaterials[Random.Range(0, platformMaterials.Length)];
    }
}
