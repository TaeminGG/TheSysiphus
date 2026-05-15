using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private ChunkLoader chunkLoader;

    [Header("Run State")]
    public bool autoStart = false;
    public bool startRocksInactive = true;
    public GameObject rocksObject;
    public RockMovement rockMovement;
    public Transform player;

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
        if (chunkLoader == null)
            return;

        chunkLoader.AddChunk(index, height, currentLevel, true);
        chunkLoader.AddChunk(index, height, currentLevel, false);
        currentLevel += 1;
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

    private void ActivateRestrictionZone()
    {
        if (rocksObject != null && !rocksObject.activeSelf)
            rocksObject.SetActive(true);

        if (rockMovement == null && rocksObject != null)
            rockMovement = rocksObject.GetComponent<RockMovement>();

        if (rockMovement == null)
            return;

        rockMovement.ClearSpeedModifiers();
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
