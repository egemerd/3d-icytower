using UnityEngine;

[ExecuteInEditMode]
public class PixelateEffect : MonoBehaviour
{
    public RenderTexture pixelTexture;

    // Bu fonksiyon kamera renderý bitirdiði an tetiklenir
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (pixelTexture != null)
        {
            // Görüntüyü önce senin düþük çözünürlüklü texture'ýna sýkýþtýrýr (Pixelate yapar)
            Graphics.Blit(source, pixelTexture);
            // Sonra o pikselli görüntüyü direkt ekrana basar
            Graphics.Blit(pixelTexture, destination);
        }
        else
        {
            // Eðer texture koymadýysan oyunu normal gösterir
            Graphics.Blit(source, destination);
        }
    }
}
