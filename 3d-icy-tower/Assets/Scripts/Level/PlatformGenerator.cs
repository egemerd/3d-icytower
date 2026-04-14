using System.Collections.Generic;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Drag your handcrafted Chunk Prefabs here. They must have the PlatformChunk script attached.")]
    [SerializeField] private PlatformChunk[] chunkPrefabs;

    [Header("Generation Bounds")]
    [Tooltip("How far ahead of the player (in Y axis) should we spawn chunks?")]
    [SerializeField] private float spawnAheadDistance = 60f;
    [Tooltip("How far below the player should we recycle chunks?")]
    [SerializeField] private float despawnBehindDistance = 30f;
    [Tooltip("Number of instances to create per chunk prefab.")]
    [SerializeField] private int poolSizePerChunk = 3;

    [Header("Alignment Settings")]
    [Tooltip("Drag the object from your scene that the generation should start from (e.g., the top of your hand-made starting level).")]
    [SerializeField] private Transform startReferencePoint;

    // We use a Dictionary to keep separate object pools for each type of chunk
    private Dictionary<PlatformChunk, Queue<PlatformChunk>> chunkPools;

    // We need to map an active instance back to its original prefab so we know which pool to return it to
    private Dictionary<PlatformChunk, PlatformChunk> instanceToPrefabMap;

    // Track the active chunks in order
    private Queue<PlatformChunk> activeChunks = new Queue<PlatformChunk>();

    // CHANGED: We now only track the Y axis to avoid any X/Z drift.
    private float nextSpawnY;

    private void Start()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("No chunk prefabs assigned to PlatformGenerator!");
            return;
        }

        if (startReferencePoint == null)
        {
            Debug.LogWarning("No start reference provided, defaulting to PlatformGenerator's position.");
            startReferencePoint = transform; // Fallback to this script's object if forgot to assign
        }

        InitializePools();

        // Start generating at the exact Y height of your reference object
        nextSpawnY = startReferencePoint.position.y;

        // Spawn initially starting at the target Y height
        while (nextSpawnY < playerTransform.position.y + spawnAheadDistance)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        // 1. Generate chunks ahead of the player
        if (playerTransform.position.y + spawnAheadDistance > nextSpawnY)
        {
            SpawnNextChunk();
        }

        // 2. Recycle chunks that fall too far behind
        if (activeChunks.Count > 0)
        {
            PlatformChunk lowestChunk = activeChunks.Peek();
            // Since a chunk has height, we check its connection point to be safe, or just its origin
            if (lowestChunk.connectionPoint.position.y < playerTransform.position.y - despawnBehindDistance)
            {
                RecycleChunk(lowestChunk);
            }
        }
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

                // Map this instance back to its prefab
                instanceToPrefabMap[instance] = prefab;
            }

            chunkPools.Add(prefab, poolQueue);
        }
    }

    private void SpawnNextChunk()
    {
        // Pick a random chunk type
        PlatformChunk randomlyChosenPrefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];
        Queue<PlatformChunk> pool = chunkPools[randomlyChosenPrefab];

        PlatformChunk chunkToSpawn;

        // Try to get one from the pool, otherwise instantiate a new one as backup
        if (pool.Count > 0)
        {
            chunkToSpawn = pool.Dequeue();
        }
        else
        {
            Debug.LogWarning($"Pool for {randomlyChosenPrefab.name} ran empty! Creating a new one. Consider increasing poolSizePerChunk.");
            chunkToSpawn = Instantiate(randomlyChosenPrefab, transform);
            instanceToPrefabMap[chunkToSpawn] = randomlyChosenPrefab; // Map it
        }

        // CHANGED HERE: Lock the X and Z absolutely to the start reference point. 
        // This stops ANY prefab drifting issues, building a perfectly straight tower above your target.
        chunkToSpawn.transform.position = new Vector3(
            startReferencePoint.position.x,
            nextSpawnY,
            startReferencePoint.position.z
        );

        chunkToSpawn.ResetChunk();
        chunkToSpawn.gameObject.SetActive(true);

        // Update the spawn position for the NEXT chunk using this chunk's connection point
        if (chunkToSpawn.connectionPoint != null)
        {
            nextSpawnY = chunkToSpawn.connectionPoint.position.y;
        }
        else
        {
            Debug.LogError($"Chunk {chunkToSpawn.name} is missing a connectionPoint! Using arbitrary offset.");
            nextSpawnY += 20f; // Arbitrary fallback height
        }

        // Track it in our active sequence
        activeChunks.Enqueue(chunkToSpawn);
    }

    private void RecycleChunk(PlatformChunk chunk)
    {
        chunk.gameObject.SetActive(false);
        activeChunks.Dequeue(); // Remove from active sequence

        // Return to the correct pool
        if (instanceToPrefabMap.TryGetValue(chunk, out PlatformChunk prefab))
        {
            chunkPools[prefab].Enqueue(chunk);
        }
    }
}