using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    private ChunkLoader chunkLoader;
    private readonly List<ChunkRunSaveData> loadedChunks = new List<ChunkRunSaveData>();

    [Header("Run State")]
    public bool autoStart = false;
    public bool startRocksInactive = true;
    public GameObject rocksObject;
    public RockMovement rockMovement;
    public Transform player;

    [Header("Stage Goal")]
    public bool useStageGoalY = true;
    public float stageGoalY = -250f;
    public bool setRocksTargetToStageGoal = true;

    [Header("Chunk Generation")]
    public bool resetChunkCountOnStart = true;
    public int startingChunkCount = 1;
    public int currentLevel = 1;
    public int chunkHeight = 50;
    public int firstChunkIndex = 0;
    public float nextChunkPlayerYStep = 12.5f;
    public bool randomizeChunksAfterFirst = true;

    private bool hasStarted;
    private float nextChunkTriggerY;

    public bool HasStarted => hasStarted;
    public bool HasStageGoal => useStageGoalY;
    public float StageGoalY => stageGoalY;
    public IReadOnlyList<ChunkRunSaveData> LoadedChunks => loadedChunks;

    private void Awake()
    {
        chunkLoader = GetComponent<ChunkLoader>();
        ResolveReferences();

        if (resetChunkCountOnStart)
            currentLevel = Mathf.Max(0, startingChunkCount);

        if (startRocksInactive && rocksObject != null)
            rocksObject.SetActive(false);
    }

    private void Start()
    {
        chunkLoader?.EnsureGridReferences();

        if (autoStart)
            StartRun();
    }

    private void Update()
    {
        TickChunkGeneration();
    }

    public void TickChunkGeneration()
    {
        if (!hasStarted || player == null)
            return;

        while (player.position.y <= nextChunkTriggerY)
        {
            LoadNextChunk();
            nextChunkTriggerY -= Mathf.Max(0.1f, nextChunkPlayerYStep);
        }
    }

    public void StartRun()
    {
        if (hasStarted)
            return;

        ResolveReferences();

        if (chunkLoader == null)
        {
            Debug.LogWarning("LevelManager: cannot start run because ChunkLoader is missing.", this);
            return;
        }

        if (!chunkLoader.IsValidChunkIndex(firstChunkIndex))
        {
            Debug.LogWarning($"LevelManager: cannot start run because first chunk index {firstChunkIndex} is invalid.", this);
            return;
        }

        hasStarted = true;

        ActivateRestrictionZone();
        LoadChunk(firstChunkIndex, chunkHeight);

        float playerY = player != null ? player.position.y : 0f;
        nextChunkTriggerY = playerY - Mathf.Max(0.1f, nextChunkPlayerYStep);
    }

    public void LoadChunk(int index, int height)
    {
        LoadChunk(index, height, currentLevel, true);
    }

    public void LoadChunk(int index, int height, int chunkCount, bool recordChunk)
    {
        if (chunkLoader == null)
            return;

        chunkLoader.AddChunk(index, height, chunkCount, true);
        chunkLoader.AddChunk(index, height, chunkCount, false);
        currentLevel = Mathf.Max(currentLevel, chunkCount + 1);

        if (recordChunk && !ContainsLoadedChunk(chunkCount))
        {
            loadedChunks.Add(new ChunkRunSaveData
            {
                chunkIndex = index,
                chunkHeight = height,
                chunkCount = chunkCount
            });
        }
    }

    private void LoadNextChunk()
    {
        int chunkIndex = firstChunkIndex;

        if (randomizeChunksAfterFirst)
        {
            int chunkCount = chunkLoader.GetAvailableChunkCount();
            if (chunkCount <= 0)
                return;

            chunkIndex = Random.Range(0, chunkCount);
        }

        LoadChunk(chunkIndex, chunkHeight);
    }

    public LevelRunSaveData CaptureRunState()
    {
        LevelRunSaveData data = new LevelRunSaveData
        {
            hasData = true,
            hasStarted = hasStarted,
            currentLevel = currentLevel,
            chunkHeight = chunkHeight,
            nextChunkTriggerY = nextChunkTriggerY,
            loadedChunks = new List<ChunkRunSaveData>()
        };

        for (int i = 0; i < loadedChunks.Count; i++)
        {
            ChunkRunSaveData chunk = loadedChunks[i];
            if (chunk == null)
                continue;

            data.loadedChunks.Add(new ChunkRunSaveData
            {
                chunkIndex = chunk.chunkIndex,
                chunkHeight = chunk.chunkHeight,
                chunkCount = chunk.chunkCount
            });
        }

        return data;
    }

    public void RestoreRunState(LevelRunSaveData data)
    {
        if (data == null || !data.hasData)
            return;

        ResolveReferences();
        loadedChunks.Clear();

        hasStarted = data.hasStarted;
        chunkHeight = data.chunkHeight > 0 ? data.chunkHeight : chunkHeight;

        if (hasStarted)
            ActivateRestrictionZone();

        if (data.loadedChunks != null)
        {
            for (int i = 0; i < data.loadedChunks.Count; i++)
            {
                ChunkRunSaveData chunk = data.loadedChunks[i];
                if (chunk == null)
                    continue;

                LoadChunk(chunk.chunkIndex, chunk.chunkHeight > 0 ? chunk.chunkHeight : chunkHeight, chunk.chunkCount, true);
            }
        }

        EnsureRestoredChunkRuntimeSpawns();

        currentLevel = Mathf.Max(currentLevel, data.currentLevel);
        nextChunkTriggerY = data.nextChunkTriggerY;
    }

    private void EnsureRestoredChunkRuntimeSpawns()
    {
        MerchantStockSpawner[] merchantSpawners = FindObjectsByType<MerchantStockSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < merchantSpawners.Length; i++)
            merchantSpawners[i]?.EnsureStockSpawned();
    }

    private bool ContainsLoadedChunk(int chunkCount)
    {
        for (int i = 0; i < loadedChunks.Count; i++)
        {
            if (loadedChunks[i] != null && loadedChunks[i].chunkCount == chunkCount)
                return true;
        }

        return false;
    }

    private void ActivateRestrictionZone()
    {
        if (rocksObject != null && !rocksObject.activeSelf)
            rocksObject.SetActive(true);

        if (rockMovement == null && rocksObject != null)
            rockMovement = rocksObject.GetComponent<RockMovement>();

        if (rockMovement == null)
            return;

        rockMovement.ClearSpeedModifiers();

        if (useStageGoalY && setRocksTargetToStageGoal)
            rockMovement.SetTargetY(stageGoalY, true);

        rockMovement.ResetProgress();
        rockMovement.ResumeMovement();
    }

    private void ResolveReferences()
    {
        if (chunkLoader == null)
            chunkLoader = GetComponent<ChunkLoader>();

        if (rocksObject == null && rockMovement != null)
            rocksObject = rockMovement.gameObject;

        if (rockMovement == null && rocksObject != null)
            rockMovement = rocksObject.GetComponent<RockMovement>();

        if (rockMovement == null)
        {
            RockMovement[] rockMovements = FindObjectsByType<RockMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (rockMovements.Length > 0)
                rockMovement = rockMovements[0];
        }

        if (rocksObject == null && rockMovement != null)
            rocksObject = rockMovement.gameObject;

        if (player == null)
        {
            PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
            if (playerMove != null)
                player = playerMove.transform;
        }
    }
}
