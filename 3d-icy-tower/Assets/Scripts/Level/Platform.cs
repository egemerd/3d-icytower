using UnityEngine;

public class Platform : MonoBehaviour
{
    [Header("2D Settings")]
    [Tooltip("Platformun üzerine kaplanacak 64x64 pixel tile görseliniz.")]
    public Sprite tileSprite;

    [Tooltip("Sprite'ýn 3D obje yüzeyine göre ne kadar önde duracaðý.")]
    public float zOffset = -0.51f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupPlatformVisuals();
    }

    private void SetupPlatformVisuals()
    {
        if (tileSprite == null)
        {
            Debug.LogWarning("Platform üzerinde Tile Sprite seçili deðil!");
            return;
        }

        // 1. Bir adet Tile'ýn Unity dünyasýndaki(World Space) gerçek geniþliðini bul
        float tileWorldWidth = tileSprite.bounds.size.x;

        // 2. Mevcut platformun X boyutuna göre (örneðin rastgele 3.6 gelmiþ olsun) bu alana kaç adet tile sýðacaðýný hesapla
        float currentScaleX = transform.localScale.x;
        int tileCount = Mathf.Max(1, Mathf.RoundToInt(currentScaleX / tileWorldWidth));

        // 3. Platformun yeni "Kusursuz" geniþliðini belirle (Örn: 3 tile sýðýyorsa geniþliði tam 3 tile boyutu yap)
        float perfectWidth = tileCount * tileWorldWidth;

        // 4. Platformun Scale deðerini düzelt, böylece fiziksel collider ve 3D alan tile ile orantýlý olsun
        Vector3 newPlatformScale = new Vector3(perfectWidth, transform.localScale.y, transform.localScale.z);
        transform.localScale = newPlatformScale;

        // 5. 3D Renderer'ý (Küpün görünümünü) kapat (Artýk sadece 2D sprite görünsün istiyorsan)
        if (TryGetComponent<MeshRenderer>(out MeshRenderer meshRend))
        {
            meshRend.enabled = false;
        }

        // 6. Görsel için yeni bir alt obje oluþtur
        GameObject visualObject = new GameObject("Platform_2DTiles");
        visualObject.transform.SetParent(transform);

        // ÖNEMLÝ: Ana objenin scale'i deðiþtiði için, alt objenin görselini ezmemesi/esnetmemesi için zýt deðerde scale veriyoruz
        visualObject.transform.localScale = new Vector3(
            1f / transform.localScale.x,
            1f / transform.localScale.y,
            1f / transform.localScale.z
        );

        // Sprite'ý tam ortalayarak veya yukarý hizalayarak yerleþtir
        visualObject.transform.localPosition = new Vector3(0, 0, zOffset);

        // 7. Sprite'ý Tiled(Döþeme) modu ile ekle
        SpriteRenderer sr = visualObject.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.drawMode = SpriteDrawMode.Tiled; // Bu mod görseli tek obje içinde defalarca yan yana dizer

        // Size deðerini belirle (X = Tam uyumlu geniþlik, Y = Orijinal Sprite boyu)
        sr.size = new Vector2(perfectWidth, tileSprite.bounds.size.y);
    }
}
