using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[DisallowMultipleComponent]
public sealed class SaveManager : MonoBehaviour, IItemUnlockStateStore
{
    public const string DefaultGameSceneName = "SampleScene";

    public static SaveManager Instance { get; private set; }

    [SerializeField] private string gameSceneName = DefaultGameSceneName;

    private GameSaveRepository repository;
    private ProfileSaveData cachedProfile;
    private ItemUnlockRegistry boundRegistry;
    private RunSaveStateController runSaveStateController;
    private bool isRestoringUnlocks;
    private RunSaveData pendingRunRestore;
    private bool restorePendingAfterSceneLoad;

    public string GameSceneName => string.IsNullOrWhiteSpace(gameSceneName) ? DefaultGameSceneName : gameSceneName;
    public bool HasActiveRun => Repository.HasActiveRun;
    public ProfileSaveData Profile => cachedProfile ??= Repository.LoadProfile();

    private GameSaveRepository Repository
    {
        get
        {
            repository ??= new GameSaveRepository(Application.persistentDataPath);
            return repository;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProfile();
        EnsureEventRouter();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindActiveRegistry();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindRegistry();

        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        BindActiveRegistry();
    }

    public static SaveManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        SaveManager existing = FindFirstObjectByType<SaveManager>();
        if (existing != null)
            return existing;

        GameObject managerObject = new GameObject("SaveManager");
        return managerObject.AddComponent<SaveManager>();
    }

    public void ConfigureRepositoryForTests(GameSaveRepository testRepository)
    {
        repository = testRepository;
        cachedProfile = null;
    }

    public ProfileSaveData LoadProfile()
    {
        cachedProfile = Repository.LoadProfile();
        return cachedProfile;
    }

    public void SaveProfile()
    {
        Repository.SaveProfile(Profile);
    }

    public void CaptureUnlocksFromRegistry(ItemUnlockRegistry registry = null)
    {
        registry ??= ItemUnlockRegistry.FindInstance();
        if (registry == null)
            return;

        Profile.ItemUnlockSnapshot = CreateProfileUnlockSnapshot(registry.CaptureSnapshot());
        SaveProfile();
    }

    public void RestoreUnlocksToRegistry(ItemUnlockRegistry registry = null)
    {
        registry ??= ItemUnlockRegistry.FindInstance();
        if (registry == null)
            return;

        ItemUnlockSnapshot profileSnapshot = Profile.ItemUnlockSnapshot;
        if (profileSnapshot == null || !profileSnapshot.HasAnyUnlocks)
        {
            CaptureUnlocksFromRegistry(registry);
            return;
        }

        isRestoringUnlocks = true;
        registry.RestoreSnapshot(profileSnapshot);
        isRestoringUnlocks = false;
    }

    public RunSaveData StartNewRun(string sceneName = null)
    {
        RunSaveRuntimeState.ClearRunState();
        Repository.DeleteRun();

        RunSaveData runData = RunSaveData.CreateNew(string.IsNullOrWhiteSpace(sceneName) ? GameSceneName : sceneName);
        runData.RunItemSnapshot = CreateProfileUnlockSnapshot(Profile.ItemUnlockSnapshot);
        Repository.SaveRun(runData);

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (registry != null)
            RestoreUnlocksToRegistry(registry);

        return runData;
    }

    public bool TryLoadRun(out RunSaveData runData)
    {
        return Repository.TryLoadRun(out runData);
    }

    public void SaveRun(RunSaveData runData)
    {
        Repository.SaveRun(runData);
    }

    public RunSaveData SaveCurrentRun(string reason = null)
    {
        RunSaveData runData = CaptureCurrentRun(reason);

        if (IsPlayerDeadInRun(runData))
        {
            runData.ActiveRun = false;
            Repository.DeleteRun();
            RunSaveRuntimeState.ClearRunState();
            return runData;
        }

        Repository.SaveRun(runData);
        return runData;
    }

    public RunSaveData CaptureCurrentRun(string reason = null)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = GameSceneName;

        if (TryLoadRun(out RunSaveData existingRun) && !string.IsNullOrWhiteSpace(existingRun.SceneName))
            sceneName = existingRun.SceneName;

        RunSaveData runData = GetRunSaveStateController().Capture(sceneName);
        if (existingRun != null)
        {
            runData.RunId = existingRun.RunId;
            runData.StartedAtUtc = existingRun.StartedAtUtc;
        }

        return runData;
    }

    public bool RestoreCurrentRun()
    {
        if (!TryLoadRun(out RunSaveData runData))
            return false;

        return RestoreRun(runData);
    }

    public bool RestoreRun(RunSaveData runData)
    {
        EnsureRunUnlockSnapshot(runData);

        bool restored = GetRunSaveStateController().Restore(runData);
        if (restored)
            RunSaveRuntimeState.RestoreFromRun(runData);

        return restored;
    }

    public bool ContinueRun()
    {
        if (!TryLoadRun(out RunSaveData runData) || IsPlayerDeadInRun(runData))
        {
            DeleteRun();
            return false;
        }

        pendingRunRestore = runData;
        restorePendingAfterSceneLoad = true;

        string sceneName = string.IsNullOrWhiteSpace(runData.SceneName) ? GameSceneName : runData.SceneName;
        SceneManager.LoadScene(sceneName);
        return true;
    }

    public void DeleteRun()
    {
        Repository.DeleteRun();
    }

    public bool TryLoad(out ItemUnlockSnapshot snapshot)
    {
        snapshot = LoadProfile().ItemUnlockSnapshot;
        return snapshot != null;
    }

    public void Save(ItemUnlockSnapshot snapshot)
    {
        Profile.ItemUnlockSnapshot = CreateProfileUnlockSnapshot(snapshot);
        SaveProfile();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnbindRegistry();
        BindActiveRegistry();

        if (restorePendingAfterSceneLoad && pendingRunRestore != null)
            StartCoroutine(RestorePendingRunAfterSceneStart(scene.name));
    }

    private IEnumerator RestorePendingRunAfterSceneStart(string loadedSceneName)
    {
        yield return null;

        if (!restorePendingAfterSceneLoad || pendingRunRestore == null)
            yield break;

        string restoreSceneName = string.IsNullOrWhiteSpace(pendingRunRestore.SceneName) ? GameSceneName : pendingRunRestore.SceneName;
        if (!string.Equals(loadedSceneName, restoreSceneName, System.StringComparison.Ordinal))
            yield break;

        RestoreRun(pendingRunRestore);
        pendingRunRestore = null;
        restorePendingAfterSceneLoad = false;

        GamePauseController.Instance?.ResumeGame();
    }

    private RunSaveStateController GetRunSaveStateController()
    {
        if (runSaveStateController != null)
            return runSaveStateController;

        runSaveStateController = GetComponent<RunSaveStateController>();
        if (runSaveStateController == null)
            runSaveStateController = gameObject.AddComponent<RunSaveStateController>();

        return runSaveStateController;
    }

    private void EnsureEventRouter()
    {
        if (GetComponent<RunSaveEventRouter>() == null)
            gameObject.AddComponent<RunSaveEventRouter>();
    }

    private void BindActiveRegistry()
    {
        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (registry == boundRegistry)
            return;

        UnbindRegistry();

        boundRegistry = registry;
        if (boundRegistry == null)
            return;

        boundRegistry.UnlocksChanged += HandleRegistryUnlocksChanged;
        RestoreUnlocksToRegistry(boundRegistry);
    }

    private void UnbindRegistry()
    {
        if (boundRegistry == null)
            return;

        boundRegistry.UnlocksChanged -= HandleRegistryUnlocksChanged;
        boundRegistry = null;
    }

    private void HandleRegistryUnlocksChanged()
    {
        if (isRestoringUnlocks || boundRegistry == null)
            return;

        CaptureUnlocksFromRegistry(boundRegistry);
    }

    private static ItemUnlockSnapshot CreateProfileUnlockSnapshot(ItemUnlockSnapshot source)
    {
        return source != null ? source.Clone(includeCollected: false) : new ItemUnlockSnapshot();
    }

    private void EnsureRunUnlockSnapshot(RunSaveData runData)
    {
        if (runData == null)
            return;

        if (runData.RunItemSnapshot != null && runData.RunItemSnapshot.HasAnyUnlocks)
            return;

        runData.RunItemSnapshot = CreateProfileUnlockSnapshot(Profile.ItemUnlockSnapshot);
    }

    private static bool IsPlayerDeadInRun(RunSaveData runData)
    {
        return runData != null
            && runData.Player != null
            && runData.Player.hasData
            && runData.Player.currentHp <= 0f;
    }
}
