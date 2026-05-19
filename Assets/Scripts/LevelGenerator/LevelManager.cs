using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class BossRoomChunkDefinition
{
    public bool enabled = true;
    [Min(1)] public int chunkNumber = 5;
    [Min(0)] public int chunkIndex = 0;
    public bool stopChunkGenerationAfterLoaded = true;
    public string roomId = "stage-1-boss";
}

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
    public int chunkHeight = 25;
    public int firstChunkIndex = 0;
    [Range(0.01f, 1f)] public float nextChunkTriggerRatio = 2f / 3f;
    public bool randomizeChunksAfterFirst = true;

    [Header("Boss Rooms")]
    public List<BossRoomChunkDefinition> bossRoomChunks = new List<BossRoomChunkDefinition>();

    private bool hasStarted;
    private float nextChunkTriggerY;
    private bool hasLoadedBossRoom;
    private int loadedBossRoomNumber = -1;
    private BossRoomChunkDefinition loadedBossRoomDefinition;

    public bool HasStarted => hasStarted;
    public bool HasStageGoal => useStageGoalY;
    public float StageGoalY => stageGoalY;
    public IReadOnlyList<ChunkRunSaveData> LoadedChunks => loadedChunks;
    public bool HasLoadedBossRoom => hasLoadedBossRoom;
    public int LoadedBossRoomNumber => loadedBossRoomNumber;

    public event Action<BossRoomChunkDefinition, int> OnBossRoomChunkLoaded;

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
            int previousLevel = currentLevel;
            LoadNextChunk();

            if (currentLevel <= previousLevel)
            {
                nextChunkTriggerY -= Mathf.Max(1f, chunkHeight);
                break;
            }

            SetNextChunkTriggerFromLoadedChunk(currentLevel - 1);
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

        int firstIndex = ResolveNextChunkIndex(firstChunkIndex, out BossRoomChunkDefinition bossRoom);

        if (!chunkLoader.IsValidChunkIndex(firstIndex))
        {
            Debug.LogWarning($"LevelManager: cannot start run because first chunk index {firstIndex} is invalid.", this);
            return;
        }

        hasStarted = true;

        ActivateRestrictionZone();
        LoadChunk(firstIndex, chunkHeight, currentLevel, true, bossRoom);

        SetNextChunkTriggerFromLoadedChunk(currentLevel - 1);
    }

    public void LoadChunk(int index, int height)
    {
        LoadChunk(index, height, currentLevel, true);
    }

    public void LoadChunk(int index, int height, int chunkCount, bool recordChunk)
    {
        LoadChunk(index, height, chunkCount, recordChunk, null);
    }

    private void LoadChunk(int index, int height, int chunkCount, bool recordChunk, BossRoomChunkDefinition bossRoom)
    {
        if (chunkLoader == null)
            return;

        if (!chunkLoader.IsValidChunkIndex(index))
        {
            Debug.LogWarning($"LevelManager: cannot load chunk because chunk index {index} is invalid.", this);
            return;
        }

        chunkLoader.AddChunk(index, height, chunkCount, true);
        chunkLoader.AddChunk(index, height, chunkCount, false);
        currentLevel = Mathf.Max(currentLevel, chunkCount + 1);

        if (recordChunk && !ContainsLoadedChunk(chunkCount))
        {
            loadedChunks.Add(new ChunkRunSaveData
            {
                chunkIndex = index,
                chunkHeight = height,
                chunkCount = chunkCount,
                isBossRoom = bossRoom != null
            });
        }

        if (bossRoom != null)
        {
            hasLoadedBossRoom = true;
            loadedBossRoomDefinition = bossRoom;
            loadedBossRoomNumber = Mathf.Max(1, loadedChunks.Count);
            OnBossRoomChunkLoaded?.Invoke(bossRoom, loadedBossRoomNumber);
        }
    }

    private void LoadNextChunk()
    {
        if (ShouldStopChunkGenerationForBossRoom())
            return;

        int chunkIndex = firstChunkIndex;
        BossRoomChunkDefinition bossRoom = null;

        chunkIndex = ResolveNextChunkIndex(chunkIndex, out bossRoom);

        if (bossRoom == null && randomizeChunksAfterFirst)
        {
            int chunkCount = chunkLoader.GetAvailableChunkCount();
            if (chunkCount <= 0)
                return;

            chunkIndex = UnityEngine.Random.Range(0, chunkCount);
        }

        LoadChunk(chunkIndex, chunkHeight, currentLevel, true, bossRoom);
    }

    private void SetNextChunkTriggerFromLoadedChunk(int chunkCount)
    {
        nextChunkTriggerY = CalculateChunkTriggerY(chunkCount, chunkHeight);
    }

    private float CalculateChunkTriggerY(int chunkCount, int height)
    {
        float chunkTopY = -Mathf.Max(0, height) * Mathf.Max(0, chunkCount);
        float triggerDistance = Mathf.Max(0.1f, Mathf.Max(1, height) * Mathf.Clamp01(nextChunkTriggerRatio));
        return chunkTopY - triggerDistance;
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
            hasLoadedBossRoom = hasLoadedBossRoom,
            loadedBossRoomNumber = loadedBossRoomNumber,
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
                chunkCount = chunk.chunkCount,
                isBossRoom = chunk.isBossRoom
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
        hasLoadedBossRoom = data.hasLoadedBossRoom;
        loadedBossRoomNumber = data.loadedBossRoomNumber;
        loadedBossRoomDefinition = FindBossRoomDefinition(loadedBossRoomNumber);

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
                if (chunk.isBossRoom)
                {
                    hasLoadedBossRoom = true;
                    loadedBossRoomNumber = loadedChunks.Count;
                    loadedBossRoomDefinition = FindBossRoomDefinition(loadedBossRoomNumber);
                }
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

    private int ResolveNextChunkIndex(int fallbackIndex, out BossRoomChunkDefinition bossRoom)
    {
        int nextChunkNumber = loadedChunks.Count + 1;
        bossRoom = FindBossRoomDefinition(nextChunkNumber);

        if (bossRoom == null)
            return fallbackIndex;

        return bossRoom.chunkIndex;
    }

    private BossRoomChunkDefinition FindBossRoomDefinition(int chunkNumber)
    {
        if (chunkNumber <= 0 || bossRoomChunks == null)
            return null;

        for (int i = 0; i < bossRoomChunks.Count; i++)
        {
            BossRoomChunkDefinition definition = bossRoomChunks[i];
            if (definition != null && definition.enabled && definition.chunkNumber == chunkNumber)
                return definition;
        }

        return null;
    }

    private bool ShouldStopChunkGenerationForBossRoom()
    {
        return hasLoadedBossRoom
            && loadedBossRoomDefinition != null
            && loadedBossRoomDefinition.stopChunkGenerationAfterLoaded;
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
