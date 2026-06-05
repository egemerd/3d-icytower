using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CircleMaskEffect : MonoBehaviour
{
    public Material circleMaskMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, circleMaskMaterial);
    }
}