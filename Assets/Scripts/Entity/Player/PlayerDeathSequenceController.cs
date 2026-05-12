using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class PlayerDeathSequenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private CameraChase cameraChase;
    [SerializeField] private TimeControlService timeControlService;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private PlayerDeathScreenUI deathScreenUI;

    [Header("Death Actions")]
    [SerializeField] private string titleSceneName = "Main";

    [Header("Camera Sequence")]
    [SerializeField] private bool snapCameraToPlayerOnDeath = false;
    [SerializeField] private float deathCameraMoveSpeed = 12f;
    [SerializeField] private float deathZoomSize = 3.5f;
    [SerializeField] private float deathZoomDuration = 0.8f;

    [Header("Player Animation")]
    [SerializeField] private bool keepPlayerAnimationRunning = true;
    [SerializeField] private string deathAnimationTrigger = "";

    [Header("World Freeze")]
    [SerializeField] private bool freezeWorldOnDeath = true;

    [Header("UI Sequence")]
    [SerializeField] private bool autoCreateDeathUi = true;
    [SerializeField] private float deathUiDelay = 1.5f;
    [SerializeField] private float deathUiFadeDuration = 0.25f;

    [Header("Scene Dimming")]
    [SerializeField, Range(0f, 1f)] private float dimStrength = 0.75f;
    [SerializeField] private float dimDuration = 0.4f;

    private readonly List<SpriteRendererState> spriteRendererStates = new List<SpriteRendererState>();
    private readonly List<TilemapState> tilemapStates = new List<TilemapState>();
    private readonly List<AnimatorState> playerAnimatorStates = new List<AnimatorState>();

    private bool hasStartedSequence;
    private bool isHandlingDeathAction;
    private Coroutine sequenceRoutine;
    private PlayerDeathScreenUI wiredDeathScreenUI;

    public event Action DeathSequenceStarted;
    public event Action DeathUiShown;

    private void Awake()
    {
        ResolveReferences();
        EnsureDeathUi();
        WireDeathScreenActions();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureDeathUi();
        WireDeathScreenActions();
        if (playerStats != null)
            playerStats.OnDeath += HandlePlayerDeath;
    }

    private void Start()
    {
        if (deathScreenUI != null)
            deathScreenUI.HideImmediate();
    }

    private void OnDisable()
    {
        if (playerStats != null)
            playerStats.OnDeath -= HandlePlayerDeath;

        UnwireDeathScreenActions();
    }

    private void HandlePlayerDeath()
    {
        if (hasStartedSequence)
            return;

        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        hasStartedSequence = true;
        ResolveReferences();
        EnsureDeathUi();

        timeControlService?.ClearHitStop();
        DeathSequenceStarted?.Invoke();

        ConfigureCameraForDeath();
        ConfigurePlayerAnimationForDeath();

        if (freezeWorldOnDeath)
            FreezeWorld();

        CaptureSceneVisualStates();

        if (cameraChase != null)
            cameraChase.ZoomTo(deathZoomSize, deathZoomDuration, true);

        if (dimStrength > 0f)
            StartCoroutine(DimSceneRoutine());

        if (deathUiDelay > 0f)
            yield return new WaitForSecondsRealtime(deathUiDelay);

        if (deathScreenUI != null)
        {
            deathScreenUI.Show(deathUiFadeDuration);
            DeathUiShown?.Invoke();
        }

        sequenceRoutine = null;
    }

    private void ConfigureCameraForDeath()
    {
        if (cameraChase == null)
            return;

        Transform focusTarget = playerMove != null && playerMove.playerSprite != null
            ? playerMove.playerSprite
            : transform;

        cameraChase.SetFollowTarget(focusTarget);
        cameraChase.SetMouseLookOffsetEnabled(false, clearOffsetImmediately: true);
        cameraChase.SetCameraSpeed(deathCameraMoveSpeed);
        cameraChase.SetUseUnscaledTime(freezeWorldOnDeath);

        if (snapCameraToPlayerOnDeath)
            cameraChase.SnapToFollowTarget();
    }

    private void ConfigurePlayerAnimationForDeath()
    {
        if (!keepPlayerAnimationRunning)
            return;

        playerAnimatorStates.Clear();

        Animator[] playerAnimators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < playerAnimators.Length; i++)
        {
            Animator animator = playerAnimators[i];
            if (animator == null)
                continue;

            playerAnimatorStates.Add(new AnimatorState
            {
                Animator = animator,
                OriginalUpdateMode = animator.updateMode
            });

            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            if (!string.IsNullOrWhiteSpace(deathAnimationTrigger))
                animator.SetTrigger(deathAnimationTrigger);
        }
    }

    private void FreezeWorld()
    {
        if (timeControlService != null)
            timeControlService.SetPaused(true);
        else
            Time.timeScale = 0f;
    }

    private void HandleRestartRequested()
    {
        if (isHandlingDeathAction)
            return;

        isHandlingDeathAction = true;

        SaveManager saveManager = SaveManager.EnsureInstance();
        saveManager.CaptureUnlocksFromRegistry();

        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
            sceneName = saveManager.GameSceneName;

        saveManager.StartNewRun(sceneName);
        ResumeWorldForSceneTransition();
        SceneManager.LoadScene(sceneName);
    }

    private void HandleMainMenuRequested()
    {
        if (isHandlingDeathAction)
            return;

        isHandlingDeathAction = true;

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

    private void ResolveReferences()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerMove == null && playerStats != null)
            playerMove = playerStats.GetComponent<PlayerMove>();

        if (timeControlService == null)
            timeControlService = TimeControlService.Instance;

        if (timeControlService == null)
            timeControlService = FindFirstObjectByType<TimeControlService>();

        if (cameraChase == null && CameraFeedbackService.Instance != null)
            cameraChase = CameraFeedbackService.Instance.GetCameraChase();

        if (cameraChase == null && Camera.main != null)
            cameraChase = Camera.main.GetComponent<CameraChase>();

        if (cameraChase == null)
            cameraChase = FindFirstObjectByType<CameraChase>();

        if (uiCanvas == null)
            uiCanvas = FindUiCanvas();
    }

    private void EnsureDeathUi()
    {
        if (deathScreenUI != null)
        {
            if (!deathScreenUI.IsInitialized)
            {
                if (uiCanvas == null)
                    uiCanvas = FindUiCanvas();

                if (uiCanvas != null)
                    deathScreenUI.InitializeRuntimeUi(uiCanvas);
            }

            return;
        }

        if (!autoCreateDeathUi)
            return;

        if (uiCanvas == null)
            uiCanvas = FindUiCanvas();

        if (uiCanvas == null)
            return;

        GameObject uiHost = new GameObject("PlayerDeathScreenUI");
        uiHost.transform.SetParent(uiCanvas.transform, false);
        deathScreenUI = uiHost.AddComponent<PlayerDeathScreenUI>();
        deathScreenUI.InitializeRuntimeUi(uiCanvas);
        deathScreenUI.HideImmediate();
    }

    private void WireDeathScreenActions()
    {
        if (deathScreenUI == null || wiredDeathScreenUI == deathScreenUI)
            return;

        UnwireDeathScreenActions();
        deathScreenUI.RestartRequested += HandleRestartRequested;
        deathScreenUI.MainMenuRequested += HandleMainMenuRequested;
        wiredDeathScreenUI = deathScreenUI;
    }

    private void UnwireDeathScreenActions()
    {
        if (wiredDeathScreenUI == null)
            return;

        wiredDeathScreenUI.RestartRequested -= HandleRestartRequested;
        wiredDeathScreenUI.MainMenuRequested -= HandleMainMenuRequested;
        wiredDeathScreenUI = null;
    }

    private string ResolveTitleSceneName()
    {
        return string.IsNullOrWhiteSpace(titleSceneName) ? "Main" : titleSceneName;
    }

    private Canvas FindUiCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode != RenderMode.WorldSpace)
                return canvases[i];
        }

        return null;
    }

    private void CaptureSceneVisualStates()
    {
        spriteRendererStates.Clear();
        tilemapStates.Clear();

        SpriteRenderer[] spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null || spriteRenderer.transform.IsChildOf(transform))
                continue;

            spriteRendererStates.Add(new SpriteRendererState
            {
                Renderer = spriteRenderer,
                OriginalColor = spriteRenderer.color
            });
        }

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null || tilemap.transform.IsChildOf(transform))
                continue;

            tilemapStates.Add(new TilemapState
            {
                Tilemap = tilemap,
                OriginalColor = tilemap.color
            });
        }
    }

    private IEnumerator DimSceneRoutine()
    {
        if (dimDuration <= 0f)
        {
            ApplyDim(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < dimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyDim(Mathf.Clamp01(elapsed / dimDuration));
            yield return null;
        }

        ApplyDim(1f);
    }

    private void ApplyDim(float normalized)
    {
        float brightnessMultiplier = Mathf.Lerp(1f, 1f - dimStrength, normalized);

        for (int i = 0; i < spriteRendererStates.Count; i++)
        {
            SpriteRendererState state = spriteRendererStates[i];
            if (state.Renderer == null)
                continue;

            state.Renderer.color = MultiplyColor(state.OriginalColor, brightnessMultiplier);
        }

        for (int i = 0; i < tilemapStates.Count; i++)
        {
            TilemapState state = tilemapStates[i];
            if (state.Tilemap == null)
                continue;

            state.Tilemap.color = MultiplyColor(state.OriginalColor, brightnessMultiplier);
        }
    }

    private static Color MultiplyColor(Color originalColor, float brightnessMultiplier)
    {
        return new Color(
            originalColor.r * brightnessMultiplier,
            originalColor.g * brightnessMultiplier,
            originalColor.b * brightnessMultiplier,
            originalColor.a);
    }

    private struct SpriteRendererState
    {
        public SpriteRenderer Renderer;
        public Color OriginalColor;
    }

    private struct TilemapState
    {
        public Tilemap Tilemap;
        public Color OriginalColor;
    }

    private struct AnimatorState
    {
        public Animator Animator;
        public AnimatorUpdateMode OriginalUpdateMode;
    }
}
