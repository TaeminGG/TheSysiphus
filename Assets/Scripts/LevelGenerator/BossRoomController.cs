using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    public bool unlockLegendaryItemsWhenBossDies = true;

    [Header("Boss Clear Screen")]
    public bool showClearScreenWhenBossDies = true;
    public bool freezeWorldOnBossClear = true;
    public bool autoCreateBossClearUi = true;
    public Canvas uiCanvas;
    public PlayerDeathScreenUI bossClearScreenUI;
    public TimeControlService timeControlService;
    public string titleSceneName = "Main";
    public string bossClearTitle = "Stage Clear";
    public string bossClearSubtitle = "보스를 처치했습니다";
    [Min(0f)] public float bossClearUiDelay = 0.35f;
    [Min(0f)] public float bossClearUiFadeDuration = 0.25f;

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
    private Coroutine bossClearRoutine;
    private bool isHandlingClearAction;
    private PlayerDeathScreenUI wiredBossClearScreenUI;

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
        EnsureBossClearUi();
        WireBossClearScreenActions();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureBossClearUi();
        WireBossClearScreenActions();
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

        if (unlockLegendaryItemsWhenBossDies)
            UnlockLegendaryItemsForBossClear();

        if (clearStageWhenBossDies)
        {
            ResolveReferences();
            stageClearController?.CompleteStage();
        }

        if (showClearScreenWhenBossDies)
        {
            if (bossClearRoutine != null)
                StopCoroutine(bossClearRoutine);

            bossClearRoutine = StartCoroutine(ShowBossClearScreenRoutine());
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
        if (timeControlService == null)
            timeControlService = TimeControlService.Instance;

        if (timeControlService == null)
            timeControlService = FindFirstObjectByType<TimeControlService>();

        if (uiCanvas == null)
            uiCanvas = FindUiCanvas();

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

    private IEnumerator ShowBossClearScreenRoutine()
    {
        ResolveReferences();
        EnsureBossClearUi();
        WireBossClearScreenActions();

        if (bossClearScreenUI == null)
        {
            bossClearRoutine = null;
            yield break;
        }

        if (freezeWorldOnBossClear)
            PauseWorldForBossClear();

        if (bossClearUiDelay > 0f)
            yield return new WaitForSecondsRealtime(bossClearUiDelay);

        bossClearScreenUI.Configure(
            bossClearTitle,
            bossClearSubtitle,
            string.Empty,
            "메인으로",
            showRestartButton: false,
            showMainMenuButton: true);
        bossClearScreenUI.Show(bossClearUiFadeDuration);

        bossClearRoutine = null;
    }

    private void PauseWorldForBossClear()
    {
        if (timeControlService != null)
        {
            timeControlService.ClearHitStop();
            timeControlService.SetPaused(true);
            return;
        }

        Time.timeScale = 0f;
    }

    private void UnlockLegendaryItemsForBossClear()
    {
        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (registry == null)
            return;

        SaveManager saveManager = SaveManager.EnsureInstance();
        saveManager.RestoreUnlocksToRegistry(registry);

        registry.UnlockItemsByRarity(ItemRarity.Legendary);
        saveManager.CaptureUnlocksFromRegistry(registry);
    }

    private void HandleBossClearMainMenuRequested()
    {
        if (isHandlingClearAction)
            return;

        isHandlingClearAction = true;

        SaveManager saveManager = SaveManager.EnsureInstance();
        saveManager.CaptureUnlocksFromRegistry();
        saveManager.DeleteRun();

        ResumeWorldForSceneTransition();
        SceneManager.LoadScene(ResolveTitleSceneName());
    }

    private void ResumeWorldForSceneTransition()
    {
        if (timeControlService == null)
            timeControlService = TimeControlService.Instance;

        if (timeControlService == null)
            timeControlService = FindFirstObjectByType<TimeControlService>();

        if (timeControlService != null)
        {
            timeControlService.ClearHitStop();
            timeControlService.SetPaused(false);
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void EnsureBossClearUi()
    {
        if (!showClearScreenWhenBossDies)
            return;

        if (bossClearScreenUI != null)
        {
            if (!bossClearScreenUI.IsInitialized)
            {
                if (uiCanvas == null)
                    uiCanvas = FindUiCanvas();

                if (uiCanvas != null)
                    bossClearScreenUI.InitializeRuntimeUi(uiCanvas);
            }

            bossClearScreenUI.Configure(
                bossClearTitle,
                bossClearSubtitle,
                string.Empty,
                "메인으로",
                showRestartButton: false,
                showMainMenuButton: true);
            bossClearScreenUI.HideImmediate();
            return;
        }

        if (!autoCreateBossClearUi)
            return;

        if (uiCanvas == null)
            uiCanvas = FindUiCanvas();

        if (uiCanvas == null)
            return;

        GameObject uiHost = new GameObject("BossClearScreenUI");
        uiHost.transform.SetParent(uiCanvas.transform, false);
        bossClearScreenUI = uiHost.AddComponent<PlayerDeathScreenUI>();
        bossClearScreenUI.InitializeRuntimeUi(uiCanvas);
        bossClearScreenUI.Configure(
            bossClearTitle,
            bossClearSubtitle,
            string.Empty,
            "메인으로",
            showRestartButton: false,
            showMainMenuButton: true);
        bossClearScreenUI.HideImmediate();
    }

    private void WireBossClearScreenActions()
    {
        if (bossClearScreenUI == null || wiredBossClearScreenUI == bossClearScreenUI)
            return;

        UnwireBossClearScreenActions();
        bossClearScreenUI.MainMenuRequested += HandleBossClearMainMenuRequested;
        wiredBossClearScreenUI = bossClearScreenUI;
    }

    private void UnwireBossClearScreenActions()
    {
        if (wiredBossClearScreenUI == null)
            return;

        wiredBossClearScreenUI.MainMenuRequested -= HandleBossClearMainMenuRequested;
        wiredBossClearScreenUI = null;
    }

    private string ResolveTitleSceneName()
    {
        return string.IsNullOrWhiteSpace(titleSceneName) ? "Main" : titleSceneName;
    }

    private static Canvas FindUiCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode != RenderMode.WorldSpace)
                return canvases[i];
        }

        return null;
    }

    private void OnDestroy()
    {
        if (activeBossStats != null)
            activeBossStats.OnDeath -= HandleBossDeath;

        UnwireBossClearScreenActions();
    }
}
