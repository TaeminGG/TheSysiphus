using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseItemCollectionMenu : MonoBehaviour
{
    private enum CollectionCategory
    {
        Passive,
        Active
    }

    private readonly List<GameObject> rootMenuObjects = new List<GameObject>();
    private readonly List<PauseItemCollectionSlot> spawnedSlots = new List<PauseItemCollectionSlot>();

    private RectTransform rootRectTransform;
    private Button openCollectionButton;
    private GameObject collectionPanel;
    private RectTransform contentRoot;
    private Button passiveTabButton;
    private Button activeTabButton;
    private TextMeshProUGUI passiveTabLabel;
    private TextMeshProUGUI activeTabLabel;
    private Image detailIcon;
    private TextMeshProUGUI detailNameLabel;
    private TextMeshProUGUI detailDescriptionLabel;
    private TMP_FontAsset fontAsset;

    private ItemUnlockRegistry registry;
    private ItemUnlockCatalog catalog;
    private CollectionCategory currentCategory = CollectionCategory.Passive;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ResolveRegistryAndCatalog();

        if (registry != null)
            registry.UnlocksChanged += HandleUnlocksChanged;

        ShowRootMenu();
    }

    private void OnDisable()
    {
        if (registry != null)
            registry.UnlocksChanged -= HandleUnlocksChanged;

        ShowRootMenu();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        rootRectTransform = transform as RectTransform;
        if (rootRectTransform == null)
            return;

        ResolveRegistryAndCatalog();
        ResolveFontAsset();
        ResolveOpenCollectionButton();
        CacheRootMenuObjects();
        BuildCollectionPanel();
        ConfigureOpenCollectionButton();

        isInitialized = true;
    }

    private void ResolveRegistryAndCatalog()
    {
        registry = ItemUnlockRegistry.FindInstance();
        if (registry != null && catalog == null)
            catalog = registry.Catalog;
    }

    private void ResolveFontAsset()
    {
        if (fontAsset != null)
            return;

        TextMeshProUGUI existingText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (existingText != null)
        {
            fontAsset = existingText.font;
            return;
        }

        TextMeshProUGUI anyText = FindFirstObjectByType<TextMeshProUGUI>();
        if (anyText != null)
            fontAsset = anyText.font;
    }

    private void ResolveOpenCollectionButton()
    {
        if (openCollectionButton != null)
            return;

        Transform buttonTransform = transform.Find("ItemList");
        if (buttonTransform != null)
            openCollectionButton = buttonTransform.GetComponent<Button>();
    }

    private void CacheRootMenuObjects()
    {
        rootMenuObjects.Clear();

        for (int i = 0; i < transform.childCount; i++)
            rootMenuObjects.Add(transform.GetChild(i).gameObject);
    }

    private void BuildCollectionPanel()
    {
        collectionPanel = CreateUiObject("CollectionPanel", rootRectTransform);
        RectTransform panelRect = collectionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -20f);
        panelRect.sizeDelta = new Vector2(780f, 470f);

        Image panelImage = collectionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.11f, 0.11f, 0.11f, 0.96f);

        CreateText("CollectionTitle", panelRect, new Vector2(0f, 200f), new Vector2(320f, 44f), "Item Collection", 30f, FontStyles.Bold, TextAlignmentOptions.Center);

        passiveTabButton = CreateButton("PassiveTab", panelRect, new Vector2(-170f, 150f), new Vector2(140f, 40f), "Passive", out passiveTabLabel);
        activeTabButton = CreateButton("ActiveTab", panelRect, new Vector2(-10f, 150f), new Vector2(140f, 40f), "Active", out activeTabLabel);
        Button backButton = CreateButton("BackButton", panelRect, new Vector2(280f, 150f), new Vector2(120f, 40f), "Back", out _);

        passiveTabButton.onClick.AddListener(ShowPassiveItems);
        activeTabButton.onClick.AddListener(ShowActiveItems);
        backButton.onClick.AddListener(ShowRootMenu);

        RectTransform listFrame = CreatePanel("ListFrame", panelRect, new Vector2(-150f, -20f), new Vector2(430f, 320f), new Color(0.17f, 0.17f, 0.17f, 0.95f));
        CreateScrollView(listFrame);

        RectTransform detailFrame = CreatePanel("DetailFrame", panelRect, new Vector2(220f, -20f), new Vector2(240f, 320f), new Color(0.15f, 0.15f, 0.15f, 0.95f));
        BuildDetailArea(detailFrame);

        collectionPanel.SetActive(false);
    }

    private void CreateScrollView(RectTransform parent)
    {
        GameObject viewportObject = CreateUiObject("Viewport", parent);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-26f, -10f);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.08f, 0.08f, 0.55f);
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;

        ScrollRect scrollRect = parent.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 20f;

        GameObject contentObject = CreateUiObject("Content", viewportRect);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup gridLayout = contentObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(70f, 70f);
        gridLayout.spacing = new Vector2(10f, 10f);
        gridLayout.padding = new RectOffset(8, 8, 8, 8);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRoot;

        GameObject scrollbarObject = CreateUiObject("Scrollbar", parent);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.offsetMin = new Vector2(-18f, 10f);
        scrollbarRect.offsetMax = new Vector2(-6f, -10f);

        Image scrollbarBackground = scrollbarObject.AddComponent<Image>();
        scrollbarBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.8f);

        GameObject slidingAreaObject = CreateUiObject("SlidingArea", scrollbarRect);
        RectTransform slidingAreaRect = slidingAreaObject.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = Vector2.zero;
        slidingAreaRect.offsetMax = Vector2.zero;

        GameObject handleObject = CreateUiObject("Handle", slidingAreaRect);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 48f);

        Image handleImage = handleObject.AddComponent<Image>();
        handleImage.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.3f;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    private void BuildDetailArea(RectTransform parent)
    {
        GameObject iconObject = CreateUiObject("DetailIcon", parent);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -18f);
        iconRect.sizeDelta = new Vector2(84f, 84f);

        Image iconFrame = iconObject.AddComponent<Image>();
        iconFrame.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        GameObject iconSpriteObject = CreateUiObject("DetailIconSprite", iconRect);
        RectTransform iconSpriteRect = iconSpriteObject.GetComponent<RectTransform>();
        iconSpriteRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconSpriteRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconSpriteRect.pivot = new Vector2(0.5f, 0.5f);
        iconSpriteRect.anchoredPosition = Vector2.zero;
        iconSpriteRect.sizeDelta = new Vector2(64f, 64f);

        detailIcon = iconSpriteObject.AddComponent<Image>();
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;

        detailNameLabel = CreateText("DetailName", parent, new Vector2(0f, -60f), new Vector2(200f, 36f), string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        detailDescriptionLabel = CreateText("DetailDescription", parent, new Vector2(0f, -168f), new Vector2(200f, 148f), string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        detailDescriptionLabel.textWrappingMode = TextWrappingModes.Normal;
    }

    private void ConfigureOpenCollectionButton()
    {
        if (openCollectionButton == null)
            return;

        openCollectionButton.onClick.RemoveAllListeners();
        openCollectionButton.onClick.AddListener(OpenCollectionMenu);
    }

    private void HandleUnlocksChanged()
    {
        if (!collectionPanel.activeSelf)
            return;

        RefreshCollection();
    }

    private void OpenCollectionMenu()
    {
        if (!isInitialized)
            return;

        for (int i = 0; i < rootMenuObjects.Count; i++)
        {
            if (rootMenuObjects[i] == null || rootMenuObjects[i] == collectionPanel)
                continue;

            rootMenuObjects[i].SetActive(false);
        }

        collectionPanel.SetActive(true);
        ShowPassiveItems();
    }

    private void ShowRootMenu()
    {
        if (collectionPanel != null)
            collectionPanel.SetActive(false);

        for (int i = 0; i < rootMenuObjects.Count; i++)
        {
            if (rootMenuObjects[i] == null || rootMenuObjects[i] == collectionPanel)
                continue;

            rootMenuObjects[i].SetActive(true);
        }

        ClearDetail();
    }

    private void ShowPassiveItems()
    {
        currentCategory = CollectionCategory.Passive;
        RefreshCollection();
    }

    private void ShowActiveItems()
    {
        currentCategory = CollectionCategory.Active;
        RefreshCollection();
    }

    private void RefreshCollection()
    {
        if (contentRoot == null)
            return;

        UpdateTabVisuals();
        ClearSlots();
        ResolveRegistryAndCatalog();

        List<ItemEntryViewModel> entries = BuildEntriesForCurrentCategory();
        for (int i = 0; i < entries.Count; i++)
            CreateSlot(entries[i]);

        ClearDetail();
    }

    private List<ItemEntryViewModel> BuildEntriesForCurrentCategory()
    {
        List<ItemEntryViewModel> unlockedEntries = new List<ItemEntryViewModel>();
        List<ItemEntryViewModel> lockedEntries = new List<ItemEntryViewModel>();
        bool isPassiveCategory = currentCategory == CollectionCategory.Passive;

        if (catalog == null)
            return unlockedEntries;

        if (isPassiveCategory)
        {
            IReadOnlyList<PassiveItem> passiveItems = catalog.PassiveItems;
            for (int i = 0; i < passiveItems.Count; i++)
                AddEntry(passiveItems[i], registry != null && registry.IsUnlocked(passiveItems[i]), true, unlockedEntries, lockedEntries);
        }
        else
        {
            IReadOnlyList<ActiveItem> activeItems = catalog.ActiveItems;
            for (int i = 0; i < activeItems.Count; i++)
                AddEntry(activeItems[i], registry != null && registry.IsUnlocked(activeItems[i]), false, unlockedEntries, lockedEntries);
        }

        unlockedEntries.AddRange(lockedEntries);
        return unlockedEntries;
    }

    private void AddEntry(PassiveItem item, bool isUnlocked, bool isPassive, List<ItemEntryViewModel> unlockedEntries, List<ItemEntryViewModel> lockedEntries)
    {
        if (item == null)
            return;

        ItemEntryViewModel entry = new ItemEntryViewModel
        {
            Icon = item.icon,
            DisplayName = item.itemName,
            Description = item.description,
            IsUnlocked = isUnlocked,
            IsPassive = isPassive
        };

        if (isUnlocked)
            unlockedEntries.Add(entry);
        else
            lockedEntries.Add(entry);
    }

    private void AddEntry(ActiveItem item, bool isUnlocked, bool isPassive, List<ItemEntryViewModel> unlockedEntries, List<ItemEntryViewModel> lockedEntries)
    {
        if (item == null)
            return;

        ItemEntryViewModel entry = new ItemEntryViewModel
        {
            Icon = item.icon,
            DisplayName = item.itemName,
            Description = item.description,
            IsUnlocked = isUnlocked,
            IsPassive = isPassive
        };

        if (isUnlocked)
            unlockedEntries.Add(entry);
        else
            lockedEntries.Add(entry);
    }

    private void CreateSlot(ItemEntryViewModel entry)
    {
        GameObject slotObject = CreateUiObject(entry.IsPassive ? "PassiveSlot" : "ActiveSlot", contentRoot);
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(70f, 70f);

        Image slotBackground = slotObject.AddComponent<Image>();
        slotBackground.color = entry.IsUnlocked
            ? new Color(0.24f, 0.24f, 0.24f, 1f)
            : new Color(0.14f, 0.14f, 0.14f, 1f);

        GameObject iconObject = CreateUiObject("Icon", slotRect);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(52f, 52f);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = entry.Icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.color = entry.IsUnlocked ? Color.white : Color.black;

        PauseItemCollectionSlot slot = slotObject.AddComponent<PauseItemCollectionSlot>();
        slot.Initialize(entry, HandleSlotHovered, HandleSlotHoverEnded);
        spawnedSlots.Add(slot);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] == null)
                continue;

            Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();
    }

    private void HandleSlotHovered(ItemEntryViewModel entry)
    {
        if (detailNameLabel == null || detailDescriptionLabel == null)
            return;

        if (entry.IsUnlocked)
        {
            detailNameLabel.text = string.IsNullOrWhiteSpace(entry.DisplayName) ? "Unnamed Item" : entry.DisplayName;
            detailDescriptionLabel.text = string.IsNullOrWhiteSpace(entry.Description) ? "No description." : entry.Description;
            detailIcon.color = Color.white;
        }
        else
        {
            detailNameLabel.text = "???";
            detailDescriptionLabel.text = "???";
            detailIcon.color = Color.black;
        }

        detailIcon.sprite = entry.Icon;
    }

    private void HandleSlotHoverEnded()
    {
        ClearDetail();
    }

    private void ClearDetail()
    {
        if (detailNameLabel == null || detailDescriptionLabel == null)
            return;

        detailNameLabel.text = string.Empty;
        detailDescriptionLabel.text = string.Empty;

        if (detailIcon != null)
        {
            detailIcon.sprite = null;
            detailIcon.color = Color.clear;
        }
    }

    private void UpdateTabVisuals()
    {
        UpdateTabButton(passiveTabButton, passiveTabLabel, currentCategory == CollectionCategory.Passive);
        UpdateTabButton(activeTabButton, activeTabLabel, currentCategory == CollectionCategory.Active);
    }

    private static void UpdateTabButton(Button button, TextMeshProUGUI label, bool isSelected)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image != null)
            image.color = isSelected ? new Color(0.75f, 0.7f, 0.45f, 1f) : new Color(0.28f, 0.28f, 0.28f, 1f);

        if (label != null)
            label.color = isSelected ? Color.black : Color.white;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panelObject = CreateUiObject(name, parent);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = color;
        return panelRect;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string content, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string labelText, out TextMeshProUGUI label)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.28f, 0.28f, 0.28f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        ColorBlock colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        button.colors = colors;

        label = CreateText("Label", buttonRect, Vector2.zero, size, labelText, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    public struct ItemEntryViewModel
    {
        public Sprite Icon;
        public string DisplayName;
        public string Description;
        public bool IsUnlocked;
        public bool IsPassive;
    }
}
