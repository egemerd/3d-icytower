using System.Collections.Generic;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    // Singleton - Dier scriptlerden kolayca eriþmek için
    public static PlatformGenerator Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Drag your handcrafted Chunk Prefabs here. They must have the PlatformChunk script attached.")]
    [SerializeField] private PlatformChunk[] chunkPrefabs;

    [Header("Platform Visuals (2D Sprite Setup)")]
    [Tooltip("Platformlarýn üzerini kaplayacak olan Sprite Renderer'a sahip Prefabýnýz.")]
    [SerializeField] private GameObject platformSpritePrefab;
    [Tooltip("Sprite görselinin, görünmez olan 3D platform küpüne kýyasla Z eksenindeki offset'i.")]
    [SerializeField] private float spriteXOffset = -0.51f;
    [SerializeField] private float spriteYOffset = -0.51f;

    [Header("Generation Bounds")]
    [SerializeField] private float spawnAheadDistance = 60f;
    [SerializeField] private float despawnBehindDistance = 30f;
    [SerializeField] private int poolSizePerChunk = 3;

    [Header("Alignment Settings")]
    [SerializeField] private Transform startReferencePoint;

    private Dictionary<PlatformChunk, Queue<PlatformChunk>> chunkPools;
    private Dictionary<PlatformChunk, PlatformChunk> instanceToPrefabMap;
    private Queue<PlatformChunk> activeChunks = new Queue<PlatformChunk>();

    private float nextSpawnY;

    private void Awake()
    {
        // Singleton Ayarý
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        if (startReferencePoint == null)
            startReferencePoint = transform;

        InitializePools();
        nextSpawnY = startReferencePoint.position.y;

        while (nextSpawnY < playerTransform.position.y + spawnAheadDistance)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        if (playerTransform.position.y + spawnAheadDistance > nextSpawnY)
        {
            SpawnNextChunk();
        }

        if (activeChunks.Count > 0)
        {
            PlatformChunk lowestChunk = activeChunks.Peek();
            if (lowestChunk.connectionPoint.position.y < playerTransform.position.y - despawnBehindDistance)
            {
                RecycleChunk(lowestChunk);
            }
        }
    }

    public float GetDeathLineY()
    {
        if (activeChunks.Count > 0)
        {
            return activeChunks.Peek().transform.position.y;
        }
        return startReferencePoint != null ? startReferencePoint.position.y - 10f : -9999f;
    }

    private void InitializePools()
    {
        chunkPools = new Dictionary<PlatformChunk, Queue<PlatformChunk>>();
        instanceToPrefabMap = new Dictionary<PlatformChunk, PlatformChunk>();

        foreach (PlatformChunk prefab in chunkPrefabs)
        {
            Queue<PlatformChunk> poolQueue = new Queue<PlatformChunk>();
            for (int i = 0; i < poolSizePerChunk; i++)
            {
                PlatformChunk instance = Instantiate(prefab, transform);
                
                //DecorateChunkPlatforms(instance);
                
                instance.gameObject.SetActive(false);
                poolQueue.Enqueue(instance);
                instanceToPrefabMap[instance] = prefab;
            }
            chunkPools.Add(prefab, poolQueue);
        }
    }

    private void SpawnNextChunk()
    {   
        PlatformChunk randomlyChosenPrefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];
        Queue<PlatformChunk> pool = chunkPools[randomlyChosenPrefab];

        PlatformChunk chunkToSpawn;
        if (pool.Count > 0)
        {
            chunkToSpawn = pool.Dequeue();
        }
        else
        {
            chunkToSpawn = Instantiate(randomlyChosenPrefab, transform);
            //DecorateChunkPlatforms(chunkToSpawn);
            instanceToPrefabMap[chunkToSpawn] = randomlyChosenPrefab;
        }

        chunkToSpawn.transform.position = new Vector3(startReferencePoint.position.x, nextSpawnY, startReferencePoint.position.z);
        chunkToSpawn.ResetChunk();
        chunkToSpawn.gameObject.SetActive(true);

        if (chunkToSpawn.connectionPoint != null)
            nextSpawnY = chunkToSpawn.connectionPoint.position.y;
        else
            nextSpawnY += 64f; 

        activeChunks.Enqueue(chunkToSpawn);
    }

    private void RecycleChunk(PlatformChunk chunk)
    {
        chunk.gameObject.SetActive(false);
        activeChunks.Dequeue();

        if (instanceToPrefabMap.TryGetValue(chunk, out PlatformChunk prefab))
            chunkPools[prefab].Enqueue(chunk);
    }

    /// <summary>
    /// Bir chunk içindeki "platform" kelimesi geçen objeleri bulur, üzerlerine sprite prefabýný ekler 
    /// ve platform boyutuna göre tiled ayarýný dinamik olarak geniþletip merkezlerini kusursuzca hizalar.
    /// </summary>
    private void DecorateChunkPlatforms(PlatformChunk chunkInstance)
    {
        if (platformSpritePrefab == null) return;

        Transform[] allChildren = chunkInstance.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower().Contains("platform") && child != chunkInstance.transform)
            {
                // 1. Orijinal 3D Küp görünümünü kapat (Ýsterseniz bu yorumu açýp orijinal küpleri gizleyebilirsiniz)
                if (child.TryGetComponent<MeshRenderer>(out MeshRenderer mrDisable))
                {
                    mrDisable.enabled = false;
                }

                // 2. Prefabý platformun bir çocuðu olarak Instantiate et
                GameObject spriteInstance = Instantiate(platformSpritePrefab, child);

                // Dönüþünü orijinal prefabdaki gibi yapsýn
                spriteInstance.transform.localRotation = platformSpritePrefab.transform.rotation; 

                // Zýt scale iþlemini uyguluyoruz ki Sprite esneyip bozulmasýn
                spriteInstance.transform.localScale = new Vector3(
                    1f / child.localScale.x,
                    1f / child.localScale.y,
                    1f / child.localScale.z
                );

                // 3. Sprite Renderer Tiled Modunu ve Geniþliðini Ayarla
                if (spriteInstance.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
                {
                    if (sr.sprite != null)
                    {
                        sr.drawMode = SpriteDrawMode.Tiled;
                        // Sadece X (Geniþlik) deðerini platforma eþitliyoruz
                        sr.size = new Vector2(child.localScale.x, sr.sprite.bounds.size.y);
                    }
                }

                // 4. KUSURSUZ HÝZALAMA (Gecikmeli Bounds deðerlerinden kaçýnarak)
                
                // Platformun DÜNYA üzerindeki gerçek ve fiziksel tam orta noktasýný alalým:
                Vector3 platformWorldCenter = child.position;
                if (child.TryGetComponent<Collider>(out Collider col))
                    platformWorldCenter = col.bounds.center;
                else if (child.TryGetComponent<MeshRenderer>(out MeshRenderer mr))
                    platformWorldCenter = mr.bounds.center;

                // Sprite'ýn pozisyonunu hiç tereddütsüz direkt o orta noktaya koyuyoruz
                spriteInstance.transform.position = platformWorldCenter;

                // 5. Z-Offset Ayarý
                // Dünya pozisyonuna attýðýmýz için child'ýn local özellikleri de etkilendi.
                // X ve Y deðerini koruyup, sadece baþtan ayarladýðýnýz Z ekseni ofsetini veriyoruz.
                Vector3 currentLocal = spriteInstance.transform.localPosition;
                spriteInstance.transform.localPosition = new Vector3(spriteXOffset,spriteYOffset,  currentLocal.z);
            }
        }
    }
}