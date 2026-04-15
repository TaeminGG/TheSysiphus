using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDeathScreenUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI restartButtonLabel;
    [SerializeField] private TextMeshProUGUI mainMenuButtonLabel;

    private Coroutine fadeRoutine;

    public event Action RestartRequested;
    public event Action MainMenuRequested;

    private void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(NotifyRestartRequested);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(NotifyMainMenuRequested);

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(NotifyRestartRequested);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(NotifyMainMenuRequested);
    }

    public void InitializeRuntimeUi(Canvas parentCanvas)
    {
        if (parentCanvas == null || rootCanvasGroup != null)
            return;

        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;

        GameObject rootObject = CreateUiObject("DeathScreenRoot", transform);
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        rootCanvasGroup = rootObject.AddComponent<CanvasGroup>();
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
        rootObject.SetActive(false);

        GameObject panelObject = CreateUiObject("DeathPanel", rootObject.transform);
        panelRoot = panelObject.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = new Vector2(420f, 260f);
        panelRoot.anchoredPosition = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        titleText = CreateText("Title", panelRoot, fontAsset, "Game Over", 40f, FontStyles.Bold);
        titleText.rectTransform.sizeDelta = new Vector2(360f, 60f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 72f);

        subtitleText = CreateText("Subtitle", panelRoot, fontAsset, "Restart and main menu actions can be connected here later.", 18f, FontStyles.Normal);
        subtitleText.rectTransform.sizeDelta = new Vector2(360f, 64f);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        subtitleText.enableWordWrapping = true;

        restartButton = CreateButton("RestartButton", panelRoot, new Vector2(-90f, -70f), fontAsset, "Restart", out restartButtonLabel);
        mainMenuButton = CreateButton("MainMenuButton", panelRoot, new Vector2(90f, -70f), fontAsset, "Main Menu", out mainMenuButtonLabel);

        restartButton.onClick.AddListener(NotifyRestartRequested);
        mainMenuButton.onClick.AddListener(NotifyMainMenuRequested);
    }

    public void HideImmediate()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (rootCanvasGroup == null)
            return;

        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
        rootCanvasGroup.gameObject.SetActive(false);
    }

    public void Show(float fadeDuration)
    {
        if (rootCanvasGroup == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInRoutine(fadeDuration));
    }

    private IEnumerator FadeInRoutine(float fadeDuration)
    {
        rootCanvasGroup.gameObject.SetActive(true);
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        if (fadeDuration <= 0f)
        {
            rootCanvasGroup.alpha = 1f;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rootCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }

        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;
        fadeRoutine = null;
    }

    private void NotifyRestartRequested()
    {
        RestartRequested?.Invoke();
    }

    private void NotifyMainMenuRequested()
    {
        MainMenuRequested?.Invoke();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset fontAsset, string content, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(string name, RectTransform parent, Vector2 anchoredPosition, TMP_FontAsset fontAsset, string label, out TextMeshProUGUI labelText)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(160f, 48f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f, 1f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        labelText = CreateText("Label", buttonRect, fontAsset, label, 22f, FontStyles.Bold);
        StretchToParent(labelText.rectTransform);
        labelText.margin = new Vector4(8f, 6f, 8f, 6f);
        return button;
    }
}
