using System.Collections.Generic;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    // Singleton - Diðer scriptlerden (PlayerHealth) kolayca eriþmek için
    public static PlatformGenerator Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Drag your handcrafted Chunk Prefabs here. They must have the PlatformChunk script attached.")]
    [SerializeField] private PlatformChunk[] chunkPrefabs;

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

    // YENÝ EKLENEN KISIM: Oyuncunun altýna düþerse öleceði birleþim yerini (En alt chunk'ýn Y noktasý) verir
    public float GetDeathLineY()
    {
        if (activeChunks.Count > 0)
        {
            // activeChunks.Peek() her zaman sahnede var olan en alt chunk'týr. 
            // transform.position.y ise bir önceki (silinmiþ olan) chunk ile birleþtiði yerdir.
            return activeChunks.Peek().transform.position.y;
        }
        
        // Eðer henüz chunk yoksa baþlangýç referans noktasýný ölüm bölgesi say
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
            instanceToPrefabMap[chunkToSpawn] = randomlyChosenPrefab;
        }

        chunkToSpawn.transform.position = new Vector3(startReferencePoint.position.x, nextSpawnY, startReferencePoint.position.z);
        chunkToSpawn.ResetChunk();
        chunkToSpawn.gameObject.SetActive(true);

        if (chunkToSpawn.connectionPoint != null)
            nextSpawnY = chunkToSpawn.connectionPoint.position.y;
        else
            nextSpawnY += 64f; // Senin objelerinin büyüklüðü 64 ise, fallback olarak 64 verilebilir

        activeChunks.Enqueue(chunkToSpawn);
    }

    private void RecycleChunk(PlatformChunk chunk)
    {
        chunk.gameObject.SetActive(false);
        activeChunks.Dequeue();

        if (instanceToPrefabMap.TryGetValue(chunk, out PlatformChunk prefab))
            chunkPools[prefab].Enqueue(chunk);
    }
}