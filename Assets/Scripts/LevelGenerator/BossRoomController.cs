using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BossRoomController : MonoBehaviour
{
    [Header("Boss")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public bool spawnBossOnEnter = true;
    public bool applyBossFloorZ = true;
    public int bossFloorZ;

    [Header("Restriction Rocks")]
    public RockMovement rockMovement;
    public bool stopRocksOnEnter = true;
    [Min(0f)] public float rocksResumeDelay = 3f;
    public bool overrideRockSpeedOnResume = true;
    [Min(0f)] public float resumedRockSpeed = 2f;

    [Header("Stage Clear")]
    public StageClearController stageClearController;
    public bool clearStageWhenBossDies = true;

    [Header("Events")]
    public UnityEvent onBossRoomEntered = new UnityEvent();
    public UnityEvent onBossSpawned = new UnityEvent();
    public UnityEvent onBossDefeated = new UnityEvent();

    private bool hasEntered;
    private bool hasSpawnedBoss;
    private bool hasCompletedBoss;
    private GameObject activeBoss;
    private EnemyStats activeBossStats;
    private Coroutine rocksResumeRoutine;

    public bool HasEntered => hasEntered;
    public bool HasSpawnedBoss => hasSpawnedBoss;
    public bool HasCompletedBoss => hasCompletedBoss;
    public GameObject ActiveBoss => activeBoss;

    public event Action BossRoomEntered;
    public event Action<GameObject> BossSpawned;
    public event Action BossDefeated;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null)
            return;

        EnterBossRoom();
    }

    public void EnterBossRoom()
    {
        if (hasEntered)
            return;

        hasEntered = true;
        ResolveReferences();
        ControlRocksForBossRoom();

        BossRoomEntered?.Invoke();
        onBossRoomEntered?.Invoke();

        if (spawnBossOnEnter)
            SpawnBoss();
    }

    public GameObject SpawnBoss()
    {
        if (hasSpawnedBoss)
            return activeBoss;

        if (bossPrefab == null)
        {
            Debug.LogWarning("BossRoomController: boss prefab is missing.", this);
            return null;
        }

        Transform spawnPoint = bossSpawnPoint != null ? bossSpawnPoint : transform;
        activeBoss = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
        hasSpawnedBoss = true;

        if (applyBossFloorZ)
            ChunkLoader.ApplySpawnFloorZToObject(activeBoss, bossFloorZ);

        WireBossDeath(activeBoss);

        BossSpawned?.Invoke(activeBoss);
        onBossSpawned?.Invoke();

        return activeBoss;
    }

    private void WireBossDeath(GameObject bossObject)
    {
        if (bossObject == null)
            return;

        activeBossStats = bossObject.GetComponent<EnemyStats>();
        if (activeBossStats == null)
            activeBossStats = bossObject.GetComponentInChildren<EnemyStats>();

        if (activeBossStats == null)
        {
            Debug.LogWarning("BossRoomController: spawned boss does not have EnemyStats, so defeat cannot be detected.", this);
            return;
        }

        activeBossStats.OnDeath += HandleBossDeath;
    }

    private void HandleBossDeath()
    {
        if (hasCompletedBoss)
            return;

        hasCompletedBoss = true;

        if (activeBossStats != null)
            activeBossStats.OnDeath -= HandleBossDeath;

        BossDefeated?.Invoke();
        onBossDefeated?.Invoke();

        if (clearStageWhenBossDies)
        {
            ResolveReferences();
            stageClearController?.CompleteStage();
        }
    }

    private void ControlRocksForBossRoom()
    {
        if (rockMovement == null)
            return;

        if (rocksResumeRoutine != null)
            StopCoroutine(rocksResumeRoutine);

        if (stopRocksOnEnter)
            rockMovement.StopMovement();

        rocksResumeRoutine = StartCoroutine(ResumeRocksAfterDelay());
    }

    private IEnumerator ResumeRocksAfterDelay()
    {
        if (rocksResumeDelay > 0f)
            yield return new WaitForSeconds(rocksResumeDelay);

        if (rockMovement != null)
        {
            if (overrideRockSpeedOnResume)
                rockMovement.SetBaseSpeed(resumedRockSpeed);

            rockMovement.ResumeMovement();
        }

        rocksResumeRoutine = null;
    }

    private void ResolveReferences()
    {
        if (rockMovement == null)
        {
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
                rockMovement = levelManager.rockMovement;
        }

        if (rockMovement == null)
            rockMovement = FindFirstObjectByType<RockMovement>();

        if (stageClearController == null)
            stageClearController = FindFirstObjectByType<StageClearController>();
    }

    private void OnDestroy()
    {
        if (activeBossStats != null)
            activeBossStats.OnDeath -= HandleBossDeath;
    }
}
