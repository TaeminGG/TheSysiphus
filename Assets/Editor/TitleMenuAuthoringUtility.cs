using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class TitleMenuAuthoringUtility
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    private const string GameSceneName = "SampleScene";
    private const string ManagedRootName = "ManagedTitleScreen";
    private const string CatalogPath = "Assets/Scripts/PlayerItems/DefaultItemUnlockCatalog.asset";
    private const string FontPath = "Assets/Fonts/neodgm SDF.asset";

    [MenuItem("Tools/UI/Rebuild Title Menu")]
    public static void RebuildTitleMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        DeleteExistingRoot();

        Camera camera = Camera.main;
        if (camera == null)
            camera = Object.FindFirstObjectByType<Camera>();

        if (camera != null)
        {
            Undo.RecordObject(camera, "Configure title camera");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.029f, 0.038f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            EditorUtility.SetDirty(camera);
        }

        GameObject root = new GameObject(ManagedRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create title menu");

        CreateGameServices(root.transform);
        CreateTitleCanvas(root.transform);
        EnsureEventSystem();
        EnsureBuildScenes();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void RebuildMainSceneTitleMenu()
    {
        RebuildTitleMenu();
    }

    public static void ApplyRequestedTitleMenuChanges()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        GameObject panel = FindSceneObject("CollectionAndSettingsPanel");
        if (panel != null && panel.TryGetComponent(out PauseItemCollectionMenu menu))
        {
            SerializedObject serializedMenu = new SerializedObject(menu);
            serializedMenu.FindProperty("titleMenuMode").boolValue = true;
            serializedMenu.FindProperty("closeTargetOnResume").objectReferenceValue = panel;
            serializedMenu.ApplyModifiedPropertiesWithoutUndo();
            menu.RebuildEditableLayout();
            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(panel);
        }

        SetButtonLabel("NewGameButton", "게임 시작");
        SetButtonLabel("ContinueButton", "이어하기");
        SetButtonLabel("CollectionSettingsButton", "도감 및 설정");
        SetButtonLabel("QuitButton", "게임 종료");
        SetButtonLabel("CloseButton", "닫기");
        SetButtonLabel("ConfirmButton", "시작");
        SetButtonLabel("CancelButton", "취소");
        SetText("ContinueStatus", "저장된 게임 없음");
        SetText("Message", "진행 중인 게임을 삭제하고 새 게임을 시작할까요?");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void DeleteExistingRoot()
    {
        GameObject existing = GameObject.Find(ManagedRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    private static void CreateGameServices(Transform parent)
    {
        GameObject services = new GameObject("GameServices");
        services.transform.SetParent(parent, false);

        SaveManager saveManager = services.AddComponent<SaveManager>();
        SerializedObject serializedSaveManager = new SerializedObject(saveManager);
        serializedSaveManager.FindProperty("gameSceneName").stringValue = GameSceneName;
        serializedSaveManager.ApplyModifiedPropertiesWithoutUndo();

        services.AddComponent<SettingsManager>();
        services.AddComponent<GameServicesBootstrap>();

        GameObject registryObject = new GameObject("ItemUnlockRegistry");
        registryObject.transform.SetParent(parent, false);
        ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();

        ItemUnlockCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemUnlockCatalog>(CatalogPath);
        if (catalog != null)
        {
            SerializedObject serializedRegistry = new SerializedObject(registry);
            serializedRegistry.FindProperty("catalog").objectReferenceValue = catalog;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateTitleCanvas(Transform parent)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        GameObject canvasObject = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        Image background = CreateImage("Background", canvasRect, new Color(0.035f, 0.043f, 0.055f, 1f));
        Stretch(background.rectTransform);

        Image artField = CreateImage("TitleImagePlaceholder", canvasRect, new Color(0.12f, 0.145f, 0.17f, 0.92f));
        RectTransform artRect = artField.rectTransform;
        artRect.anchorMin = new Vector2(0.24f, 0.08f);
        artRect.anchorMax = new Vector2(1f, 1f);
        artRect.offsetMin = Vector2.zero;
        artRect.offsetMax = Vector2.zero;

        Image vignette = CreateImage("ButtonReadabilityShade", canvasRect, new Color(0f, 0f, 0f, 0.34f));
        RectTransform shadeRect = vignette.rectTransform;
        shadeRect.anchorMin = new Vector2(0f, 0f);
        shadeRect.anchorMax = new Vector2(0.42f, 1f);
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;

        TextMeshProUGUI title = CreateText("Title", canvasRect, "MEDIA PROJECT", font, 86f, FontStyles.Bold, TextAlignmentOptions.TopRight);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.47f, 0.72f);
        titleRect.anchorMax = new Vector2(0.95f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        title.color = new Color(0.94f, 0.88f, 0.68f, 1f);

        TextMeshProUGUI subtitle = CreateText("Subtitle", canvasRect, "Isometric Roguelike Action", font, 26f, FontStyles.Normal, TextAlignmentOptions.TopRight);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.47f, 0.68f);
        subtitleRect.anchorMax = new Vector2(0.95f, 0.74f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;
        subtitle.color = new Color(0.83f, 0.84f, 0.82f, 0.88f);

        RectTransform menuRoot = CreateRect("MenuButtons", canvasRect);
        menuRoot.anchorMin = new Vector2(0f, 0f);
        menuRoot.anchorMax = new Vector2(0f, 0f);
        menuRoot.pivot = new Vector2(0f, 0f);
        menuRoot.anchoredPosition = new Vector2(96f, 92f);
        menuRoot.sizeDelta = new Vector2(360f, 330f);

        Button newGameButton = CreateTitleButton(menuRoot, "NewGameButton", "게임 시작", font, 0f);
        Button continueButton = CreateTitleButton(menuRoot, "ContinueButton", "이어하기", font, 74f);
        Button collectionButton = CreateTitleButton(menuRoot, "CollectionSettingsButton", "도감 및 설정", font, 148f);
        Button quitButton = CreateTitleButton(menuRoot, "QuitButton", "게임 종료", font, 222f);

        TextMeshProUGUI continueStatus = CreateText("ContinueStatus", menuRoot, "저장된 게임 없음", font, 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        RectTransform statusRect = continueStatus.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(18f, -130f);
        statusRect.sizeDelta = new Vector2(-36f, 28f);
        continueStatus.color = new Color(0.74f, 0.76f, 0.76f, 0.8f);

        GameObject panel = CreateCollectionAndSettingsPanel(canvasRect, font);
        panel.SetActive(false);

        GameObject confirmationPanel = CreateConfirmationPanel(canvasRect, font);
        confirmationPanel.SetActive(false);

        TitleMenuController controller = canvasObject.AddComponent<TitleMenuController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("gameSceneName").stringValue = GameSceneName;
        serializedController.FindProperty("newGameButton").objectReferenceValue = newGameButton;
        serializedController.FindProperty("continueButton").objectReferenceValue = continueButton;
        serializedController.FindProperty("collectionAndSettingsButton").objectReferenceValue = collectionButton;
        serializedController.FindProperty("quitButton").objectReferenceValue = quitButton;
        serializedController.FindProperty("closeCollectionAndSettingsButton").objectReferenceValue = FindChildComponent<Button>(panel.transform, "CloseButton");
        serializedController.FindProperty("confirmStartNewGameButton").objectReferenceValue = FindChildComponent<Button>(confirmationPanel.transform, "ConfirmButton");
        serializedController.FindProperty("cancelStartNewGameButton").objectReferenceValue = FindChildComponent<Button>(confirmationPanel.transform, "CancelButton");
        serializedController.FindProperty("collectionAndSettingsPanel").objectReferenceValue = panel;
        serializedController.FindProperty("continueStatusLabel").objectReferenceValue = continueStatus;
        serializedController.FindProperty("overwriteRunConfirmationPanel").objectReferenceValue = confirmationPanel;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateCollectionAndSettingsPanel(RectTransform canvasRect, TMP_FontAsset font)
    {
        GameObject panelObject = new GameObject("CollectionAndSettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasRect, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(980f, 560f);
        rect.anchoredPosition = Vector2.zero;

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.07f, 0.075f, 0.08f, 0.98f);

        PauseItemCollectionMenu menu = panelObject.AddComponent<PauseItemCollectionMenu>();
        menu.RebuildEditableLayout();

        Button closeButton = CreateTitleButton(rect, "CloseButton", "닫기", font, 0f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-18f, -18f);
        closeRect.sizeDelta = new Vector2(120f, 44f);
        return panelObject;
    }

    private static GameObject CreateConfirmationPanel(RectTransform canvasRect, TMP_FontAsset font)
    {
        GameObject overlay = new GameObject("OverwriteRunConfirmationPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(canvasRect, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

        Image panelImage = CreateImage("Dialog", overlayRect, new Color(0.09f, 0.095f, 0.1f, 0.98f));
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 240f);
        panelRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI message = CreateText("Message", panelRect, "진행 중인 게임을 삭제하고 새 게임을 시작할까요?", font, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform messageRect = message.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 0.46f);
        messageRect.anchorMax = new Vector2(1f, 0.82f);
        messageRect.offsetMin = new Vector2(38f, 0f);
        messageRect.offsetMax = new Vector2(-38f, 0f);

        Button confirmButton = CreateTitleButton(panelRect, "ConfirmButton", "시작", font, 0f);
        RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.pivot = new Vector2(1f, 0f);
        confirmRect.anchoredPosition = new Vector2(-12f, 36f);
        confirmRect.sizeDelta = new Vector2(180f, 54f);

        Button cancelButton = CreateTitleButton(panelRect, "CancelButton", "취소", font, 0f);
        RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0f, 0f);
        cancelRect.anchoredPosition = new Vector2(12f, 36f);
        cancelRect.sizeDelta = new Vector2(180f, 54f);

        return overlay;
    }

    private static Button CreateTitleButton(RectTransform parent, string name, string label, TMP_FontAsset font, float topOffset)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
        rect.sizeDelta = new Vector2(0f, 58f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.16f, 0.17f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", rect, label, font, 25f, FontStyles.Bold, TextAlignmentOptions.Left);
        Stretch(text.rectTransform, 24f, 0f, 18f, 0f);
        text.color = Color.white;

        return button;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, string text, TMP_FontAsset font, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
        label.fontSizeMax = fontSize;

        return label;
    }

    private static Image CreateImage(string name, RectTransform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void EnsureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent.GetComponent<T>();

        for (int i = 0; i < parent.childCount; i++)
        {
            T component = FindChildComponent<T>(parent.GetChild(i), childName);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void SetButtonLabel(string buttonName, string label)
    {
        GameObject buttonObject = FindSceneObject(buttonName);
        if (buttonObject == null)
            return;

        TextMeshProUGUI text = FindChildComponent<TextMeshProUGUI>(buttonObject.transform, "Label");
        if (text == null)
            text = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null)
            return;

        text.text = label;
        EditorUtility.SetDirty(text);
    }

    private static void SetText(string objectName, string text)
    {
        GameObject target = FindSceneObject(objectName);
        if (target == null)
            return;

        TextMeshProUGUI label = target.GetComponent<TextMeshProUGUI>();
        if (label == null)
            return;

        label.text = text;
        EditorUtility.SetDirty(label);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GameObject gameObject = gameObjects[i];
            if (gameObject == null || !gameObject.scene.IsValid())
                continue;

            if (gameObject.name == objectName)
                return gameObject;
        }

        return null;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        Stretch(rectTransform, 0f, 0f, 0f, 0f);
    }

    private static void Stretch(RectTransform rectTransform, float left, float bottom, float right, float top)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }
}
