using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = SaveManager.DefaultGameSceneName;

    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button collectionAndSettingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button closeCollectionAndSettingsButton;
    [SerializeField] private Button confirmStartNewGameButton;
    [SerializeField] private Button cancelStartNewGameButton;

    [Header("Optional UI")]
    [SerializeField] private GameObject collectionAndSettingsPanel;
    [SerializeField] private TextMeshProUGUI continueStatusLabel;
    [SerializeField] private GameObject overwriteRunConfirmationPanel;

    private SaveManager saveManager;

    private void Awake()
    {
        saveManager = SaveManager.EnsureInstance();
        SettingsManager.EnsureInstance();
        WireButtons();
    }

    private void OnEnable()
    {
        RefreshContinueState();
    }

    public void RefreshContinueState()
    {
        saveManager ??= SaveManager.EnsureInstance();
        bool hasActiveRun = saveManager.HasActiveRun;

        if (continueButton != null)
            continueButton.interactable = hasActiveRun;

        if (continueStatusLabel != null)
            continueStatusLabel.text = hasActiveRun ? string.Empty : "저장된 게임 없음";
    }

    public void StartNewGame()
    {
        saveManager ??= SaveManager.EnsureInstance();

        if (saveManager.HasActiveRun && overwriteRunConfirmationPanel != null)
        {
            overwriteRunConfirmationPanel.SetActive(true);
            return;
        }

        ConfirmStartNewGame();
    }

    public void ConfirmStartNewGame()
    {
        saveManager ??= SaveManager.EnsureInstance();
        string sceneName = ResolveGameSceneName();
        saveManager.StartNewRun(sceneName);
        SceneManager.LoadScene(sceneName);
    }

    public void CancelStartNewGame()
    {
        if (overwriteRunConfirmationPanel != null)
            overwriteRunConfirmationPanel.SetActive(false);
    }

    public void ContinueGame()
    {
        saveManager ??= SaveManager.EnsureInstance();
        if (!saveManager.TryLoadRun(out RunSaveData runData))
        {
            RefreshContinueState();
            return;
        }

        SceneManager.LoadScene(string.IsNullOrWhiteSpace(runData.SceneName) ? ResolveGameSceneName() : runData.SceneName);
    }

    public void ToggleCollectionAndSettings()
    {
        if (collectionAndSettingsPanel == null)
            return;

        collectionAndSettingsPanel.SetActive(!collectionAndSettingsPanel.activeSelf);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void WireButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(StartNewGame);
            newGameButton.onClick.AddListener(StartNewGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueGame);
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (collectionAndSettingsButton != null)
        {
            collectionAndSettingsButton.onClick.RemoveListener(ToggleCollectionAndSettings);
            collectionAndSettingsButton.onClick.AddListener(ToggleCollectionAndSettings);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }

        if (closeCollectionAndSettingsButton != null)
        {
            closeCollectionAndSettingsButton.onClick.RemoveListener(CloseCollectionAndSettings);
            closeCollectionAndSettingsButton.onClick.AddListener(CloseCollectionAndSettings);
        }

        if (confirmStartNewGameButton != null)
        {
            confirmStartNewGameButton.onClick.RemoveListener(ConfirmStartNewGame);
            confirmStartNewGameButton.onClick.AddListener(ConfirmStartNewGame);
        }

        if (cancelStartNewGameButton != null)
        {
            cancelStartNewGameButton.onClick.RemoveListener(CancelStartNewGame);
            cancelStartNewGameButton.onClick.AddListener(CancelStartNewGame);
        }
    }

    private void CloseCollectionAndSettings()
    {
        if (collectionAndSettingsPanel != null)
            collectionAndSettingsPanel.SetActive(false);
    }

    private string ResolveGameSceneName()
    {
        if (!string.IsNullOrWhiteSpace(gameSceneName))
            return gameSceneName;

        return saveManager != null ? saveManager.GameSceneName : SaveManager.DefaultGameSceneName;
    }
}
