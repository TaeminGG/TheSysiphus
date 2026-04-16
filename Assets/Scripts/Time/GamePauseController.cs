using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GamePauseController : MonoBehaviour
{
    public static GamePauseController Instance { get; private set; }

    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Image pauseBackdropImage;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private TimeControlService timeControlService;
    [SerializeField] private string gameplayActionMap = "Player";
    [SerializeField] private string uiActionMap = "UI";
    [SerializeField] private bool pauseAudioListener = false;
    [Header("Pause Backdrop")]
    [SerializeField] private bool autoCreateBackdrop = true;
    [SerializeField] private Color pauseBackdropColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private float pauseInputCooldown = 0.1f;

    private float lastPauseToggleRealtime = -999f;

    public bool IsPaused => timeControlService != null && timeControlService.IsPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        ResolvePauseBackdrop();
        EnsurePauseMenuExtensions();
        SetPauseVisuals(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        ResolveReferences();
        if (timeControlService == null)
            return;

        timeControlService.SetPaused(paused);

        if (pauseAudioListener)
        {
            AudioListener.pause = paused;
        }

        SetPauseVisuals(paused);

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap(paused ? uiActionMap : gameplayActionMap);
        }
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    private void OnPause(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (Time.unscaledTime - lastPauseToggleRealtime < pauseInputCooldown)
            return;

        lastPauseToggleRealtime = Time.unscaledTime;
        TogglePause();
    }

    private void SetPauseVisuals(bool paused)
    {
        if (pauseBackdropImage != null)
        {
            pauseBackdropImage.gameObject.SetActive(paused);
            pauseBackdropImage.color = pauseBackdropColor;
        }

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(paused);
        }
    }

    private void ResolveReferences()
    {
        if (timeControlService == null)
        {
            timeControlService = TimeControlService.Instance;
        }

        if (timeControlService == null)
        {
            timeControlService = FindFirstObjectByType<TimeControlService>();
        }

        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
    }

    private void ResolvePauseBackdrop()
    {
        if (pauseBackdropImage != null || !autoCreateBackdrop || pauseMenuRoot == null)
            return;

        Canvas parentCanvas = pauseMenuRoot.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return;

        Transform backdropParent = pauseMenuRoot.transform.parent != null ? pauseMenuRoot.transform.parent : parentCanvas.transform;

        GameObject backdropObject = new GameObject("PauseBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdropObject.transform.SetParent(backdropParent, false);

        RectTransform rectTransform = backdropObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        pauseBackdropImage = backdropObject.GetComponent<Image>();
        pauseBackdropImage.color = pauseBackdropColor;
        pauseBackdropImage.raycastTarget = true;

        int menuSiblingIndex = pauseMenuRoot.transform.GetSiblingIndex();
        backdropObject.transform.SetSiblingIndex(Mathf.Max(0, menuSiblingIndex));
        pauseMenuRoot.transform.SetSiblingIndex(backdropObject.transform.GetSiblingIndex() + 1);
    }

    private void EnsurePauseMenuExtensions()
    {
        if (pauseMenuRoot == null || pauseMenuRoot.GetComponent<PauseItemCollectionMenu>() != null)
            return;

        pauseMenuRoot.AddComponent<PauseItemCollectionMenu>();
    }
}
