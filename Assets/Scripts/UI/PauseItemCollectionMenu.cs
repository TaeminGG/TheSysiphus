using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseItemCollectionMenu : MonoBehaviour
{
    private enum PauseSection
    {
        CurrentItems,
        Collection,
        Settings
    }

    private enum CollectionCategory
    {
        Passive,
        Active
    }

    [Serializable]
    private class PauseMenuStyle
    {
        [Header("Window")]
        public Sprite windowSprite = null;
        public Color windowColor = new Color(0.08f, 0.08f, 0.08f, 0.98f);

        [Header("Panels")]
        public Sprite navigationPanelSprite = null;
        public Color navigationPanelColor = new Color(0.12f, 0.12f, 0.12f, 0.98f);
        public Sprite contentPanelSprite = null;
        public Color contentPanelColor = new Color(0.11f, 0.11f, 0.11f, 0.98f);
        public Sprite subPanelSprite = null;
        public Color subPanelColor = new Color(0.16f, 0.16f, 0.16f, 0.98f);
        public Sprite detailPanelSprite = null;
        public Color detailPanelColor = new Color(0.14f, 0.14f, 0.14f, 0.98f);

        [Header("Navigation")]
        public Sprite tabSprite = null;
        public Sprite selectedTabSprite = null;
        public Color tabColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color selectedTabColor = new Color(0.78f, 0.69f, 0.42f, 1f);
        public Color tabTextColor = Color.white;
        public Color selectedTabTextColor = Color.black;

        [Header("Action Buttons")]
        public Sprite actionButtonSprite = null;
        public Color actionButtonColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        public Color actionButtonTextColor = Color.white;
        public Color quitButtonColor = new Color(0.46f, 0.2f, 0.18f, 1f);

        [Header("Item Slots")]
        public Sprite slotSprite = null;
        public Sprite lockedSlotSprite = null;
        public Color slotColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        public Color lockedSlotColor = new Color(0.34f, 0.34f, 0.34f, 1f);
        public Color selectedOutlineColor = new Color(0.88f, 0.79f, 0.49f, 1f);
    }

    private sealed class PauseTabView
    {
        public PauseSection Section;
        public Button Button;
        public Image Background;
        public TextMeshProUGUI Label;
    }

    private sealed class CollectionTabView
    {
        public CollectionCategory Category;
        public Button Button;
        public Image Background;
        public TextMeshProUGUI Label;
    }

    [SerializeField] private Vector2 windowSize = new Vector2(980f, 560f);
    [SerializeField] private PauseMenuStyle style = new PauseMenuStyle();

    private readonly List<GameObject> legacyRootObjects = new List<GameObject>();
    private readonly List<PauseTabView> navigationTabs = new List<PauseTabView>();
    private readonly List<CollectionTabView> collectionTabs = new List<CollectionTabView>();
    private readonly List<PauseItemCollectionSlot> dynamicSlots = new List<PauseItemCollectionSlot>();

    private RectTransform rootRectTransform;
    private RectTransform runtimeRoot;
    private RectTransform leftContentHost;
    private RectTransform currentPassiveContentRoot;
    private RectTransform collectionGridContentRoot;
    private GameObject currentItemsPanel;
    private GameObject collectionPanel;
    private GameObject settingsPanel;
    private Button resumeButton;
    private Button quitButton;
    private TextMeshProUGUI contentTitleLabel;
    private TextMeshProUGUI detailNameLabel;
    private TextMeshProUGUI detailDescriptionLabel;
    private Image detailIcon;
    private TMP_FontAsset fontAsset;
    private ItemUnlockRegistry registry;
    private ItemUnlockCatalog catalog;
    private PassiveItemList passiveItemList;
    private GamePauseController pauseController;
    private PauseSection currentSection = PauseSection.CurrentItems;
    private CollectionCategory currentCollectionCategory = CollectionCategory.Passive;
    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ResolveDependencies();

        if (registry != null)
            registry.UnlocksChanged += HandleUnlocksChanged;

        SetLegacyRootObjectsActive(false);
        ShowSection(PauseSection.CurrentItems);
    }

    private void OnDisable()
    {
        if (registry != null)
            registry.UnlocksChanged -= HandleUnlocksChanged;

        ClearDetail("Select an item to inspect its details.", string.Empty, null, true);
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        rootRectTransform = transform as RectTransform;
        if (rootRectTransform == null)
            return;

        CacheLegacyRootObjects();
        ResolveDependencies();
        ResolveFontAsset();
        BuildRuntimeLayout();
        SetLegacyRootObjectsActive(false);

        isInitialized = true;
    }

    private void CacheLegacyRootObjects()
    {
        legacyRootObjects.Clear();

        for (int i = 0; i < transform.childCount; i++)
            legacyRootObjects.Add(transform.GetChild(i).gameObject);
    }

    private void ResolveDependencies()
    {
        pauseController = GamePauseController.Instance;
        if (pauseController == null)
            pauseController = FindFirstObjectByType<GamePauseController>();

        registry = ItemUnlockRegistry.FindInstance();
        if (registry != null)
            catalog = registry.Catalog;

        if (passiveItemList == null || passiveItemList.Equals(null))
            passiveItemList = FindFirstObjectByType<PassiveItemList>();

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

        if (TMP_Settings.defaultFontAsset != null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
            return;
        }

        TextMeshProUGUI anyText = FindFirstObjectByType<TextMeshProUGUI>();
        if (anyText != null)
            fontAsset = anyText.font;
    }

    private void BuildRuntimeLayout()
    {
        runtimeRoot = CreateUiObject("RuntimePauseLayout", rootRectTransform).GetComponent<RectTransform>();
        runtimeRoot.anchorMin = new Vector2(0.5f, 0.5f);
        runtimeRoot.anchorMax = new Vector2(0.5f, 0.5f);
        runtimeRoot.pivot = new Vector2(0.5f, 0.5f);
        runtimeRoot.sizeDelta = windowSize;
        runtimeRoot.anchoredPosition = Vector2.zero;

        Image rootImage = runtimeRoot.gameObject.AddComponent<Image>();
        ApplyImageStyle(rootImage, style.windowSprite, style.windowColor);

        RectTransform navigationPanel = CreateUiObject("NavigationPanel", runtimeRoot).GetComponent<RectTransform>();
        navigationPanel.anchorMin = new Vector2(0f, 0f);
        navigationPanel.anchorMax = new Vector2(0f, 1f);
        navigationPanel.offsetMin = new Vector2(16f, 16f);
        navigationPanel.offsetMax = new Vector2(246f, -16f);
        ApplyImageStyle(navigationPanel.gameObject.AddComponent<Image>(), style.navigationPanelSprite, style.navigationPanelColor);

        RectTransform contentPanel = CreateUiObject("ContentPanel", runtimeRoot).GetComponent<RectTransform>();
        contentPanel.anchorMin = new Vector2(0f, 0f);
        contentPanel.anchorMax = new Vector2(1f, 1f);
        contentPanel.offsetMin = new Vector2(262f, 16f);
        contentPanel.offsetMax = new Vector2(-16f, -16f);
        ApplyImageStyle(contentPanel.gameObject.AddComponent<Image>(), style.contentPanelSprite, style.contentPanelColor);

        BuildNavigationPanel(navigationPanel);
        BuildContentPanel(contentPanel);
    }

    private void BuildNavigationPanel(RectTransform navigationPanel)
    {
        CreateText("PauseTitle", navigationPanel, new Vector2(24f, -26f), new Vector2(-24f, 44f), "Pause", 34f, FontStyles.Bold, TextAlignmentOptions.Left);

        RectTransform tabContainer = CreateUiObject("NavigationTabs", navigationPanel).GetComponent<RectTransform>();
        tabContainer.anchorMin = new Vector2(0f, 1f);
        tabContainer.anchorMax = new Vector2(1f, 1f);
        tabContainer.pivot = new Vector2(0.5f, 1f);
        tabContainer.anchoredPosition = new Vector2(0f, -88f);
        tabContainer.sizeDelta = new Vector2(0f, 220f);

        VerticalLayoutGroup tabLayout = tabContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        tabLayout.padding = new RectOffset(16, 16, 0, 0);
        tabLayout.spacing = 10f;
        tabLayout.childAlignment = TextAnchor.UpperCenter;
        tabLayout.childControlHeight = false;
        tabLayout.childControlWidth = true;
        tabLayout.childForceExpandHeight = false;
        tabLayout.childForceExpandWidth = true;

        navigationTabs.Add(CreateNavigationTab(tabContainer, PauseSection.CurrentItems, "Current Items"));
        navigationTabs.Add(CreateNavigationTab(tabContainer, PauseSection.Collection, "Collection"));
        navigationTabs.Add(CreateNavigationTab(tabContainer, PauseSection.Settings, "Settings"));

        RectTransform actionContainer = CreateUiObject("ActionButtons", navigationPanel).GetComponent<RectTransform>();
        actionContainer.anchorMin = new Vector2(0f, 0f);
        actionContainer.anchorMax = new Vector2(1f, 0f);
        actionContainer.pivot = new Vector2(0.5f, 0f);
        actionContainer.anchoredPosition = new Vector2(0f, 18f);
        actionContainer.sizeDelta = new Vector2(0f, 112f);

        VerticalLayoutGroup actionLayout = actionContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        actionLayout.padding = new RectOffset(16, 16, 0, 0);
        actionLayout.spacing = 12f;
        actionLayout.childAlignment = TextAnchor.LowerCenter;
        actionLayout.childControlHeight = false;
        actionLayout.childControlWidth = true;
        actionLayout.childForceExpandHeight = false;
        actionLayout.childForceExpandWidth = true;

        resumeButton = CreateActionButton(actionContainer, "ResumeButton", "Resume", style.actionButtonColor, HandleResumeClicked);
        quitButton = CreateActionButton(actionContainer, "QuitButton", "Quit", style.quitButtonColor, HandleQuitClicked);
    }

    private void BuildContentPanel(RectTransform contentPanel)
    {
        contentTitleLabel = CreateText("ContentTitle", contentPanel, new Vector2(28f, -26f), new Vector2(-28f, 42f), "Current Items", 30f, FontStyles.Bold, TextAlignmentOptions.Left);

        RectTransform bodyRect = CreateUiObject("Body", contentPanel).GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(20f, 20f);
        bodyRect.offsetMax = new Vector2(-20f, -74f);

        leftContentHost = CreateUiObject("LeftContentHost", bodyRect).GetComponent<RectTransform>();
        leftContentHost.anchorMin = new Vector2(0f, 0f);
        leftContentHost.anchorMax = new Vector2(1f, 1f);
        leftContentHost.offsetMin = new Vector2(0f, 0f);
        leftContentHost.offsetMax = new Vector2(-260f, 0f);

        RectTransform detailPanel = CreateUiObject("DetailPanel", bodyRect).GetComponent<RectTransform>();
        detailPanel.anchorMin = new Vector2(1f, 0f);
        detailPanel.anchorMax = new Vector2(1f, 1f);
        detailPanel.offsetMin = new Vector2(-244f, 0f);
        detailPanel.offsetMax = new Vector2(-4f, 0f);
        ApplyImageStyle(detailPanel.gameObject.AddComponent<Image>(), style.detailPanelSprite, style.detailPanelColor);
        BuildDetailPanel(detailPanel);

        BuildCurrentItemsPanel();
        BuildCollectionPanel();
        BuildSettingsPanel();
    }

    private void BuildDetailPanel(RectTransform detailPanel)
    {
        GameObject iconFrameObject = CreateUiObject("DetailIconFrame", detailPanel);
        RectTransform iconFrameRect = iconFrameObject.GetComponent<RectTransform>();
        iconFrameRect.anchorMin = new Vector2(0.5f, 1f);
        iconFrameRect.anchorMax = new Vector2(0.5f, 1f);
        iconFrameRect.pivot = new Vector2(0.5f, 1f);
        iconFrameRect.anchoredPosition = new Vector2(0f, -24f);
        iconFrameRect.sizeDelta = new Vector2(100f, 100f);

        Image iconFrameImage = iconFrameObject.AddComponent<Image>();
        ApplyImageStyle(iconFrameImage, style.subPanelSprite, style.subPanelColor);

        GameObject iconObject = CreateUiObject("DetailIcon", iconFrameRect);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(72f, 72f);

        detailIcon = iconObject.AddComponent<Image>();
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;

        detailNameLabel = CreateText("DetailName", detailPanel, new Vector2(18f, -144f), new Vector2(-18f, 34f), string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        detailDescriptionLabel = CreateText("DetailDescription", detailPanel, new Vector2(18f, -196f), new Vector2(-18f, 262f), string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        detailDescriptionLabel.textWrappingMode = TextWrappingModes.Normal;

        ClearDetail("Select an item to inspect its details.", string.Empty, null, true);
    }

    private void BuildCurrentItemsPanel()
    {
        currentItemsPanel = CreateUiObject("CurrentItemsPanel", leftContentHost).gameObject;
        RectTransform panelRect = currentItemsPanel.GetComponent<RectTransform>();
        StretchToParent(panelRect);

        VerticalLayoutGroup layout = currentItemsPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform passiveFrame = CreateSubSection(currentItemsPanel.transform, null, -1f);
        LayoutElement passiveLayoutElement = passiveFrame.gameObject.AddComponent<LayoutElement>();
        passiveLayoutElement.flexibleHeight = 1f;

        currentPassiveContentRoot = CreateGridScrollArea(passiveFrame, out _, 5, new Vector2(74f, 74f), 10f);
    }

    private void BuildCollectionPanel()
    {
        collectionPanel = CreateUiObject("CollectionPanel", leftContentHost).gameObject;
        RectTransform panelRect = collectionPanel.GetComponent<RectTransform>();
        StretchToParent(panelRect);
        collectionPanel.SetActive(false);

        VerticalLayoutGroup layout = collectionPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform categoryBar = CreateSubSection(collectionPanel.transform, "Archive", 92f);
        RectTransform categoryButtons = CreateUiObject("CategoryButtons", categoryBar).GetComponent<RectTransform>();
        categoryButtons.anchorMin = new Vector2(0f, 0f);
        categoryButtons.anchorMax = new Vector2(1f, 1f);
        categoryButtons.offsetMin = new Vector2(16f, 14f);
        categoryButtons.offsetMax = new Vector2(-16f, -36f);

        HorizontalLayoutGroup categoryLayout = categoryButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        categoryLayout.spacing = 10f;
        categoryLayout.childAlignment = TextAnchor.MiddleLeft;
        categoryLayout.childControlWidth = false;
        categoryLayout.childControlHeight = false;
        categoryLayout.childForceExpandWidth = false;
        categoryLayout.childForceExpandHeight = false;

        collectionTabs.Add(CreateCollectionCategoryTab(categoryButtons, CollectionCategory.Passive, "Passive"));
        collectionTabs.Add(CreateCollectionCategoryTab(categoryButtons, CollectionCategory.Active, "Active"));

        RectTransform gridFrame = CreateSubSection(collectionPanel.transform, null, -1f);
        LayoutElement gridLayoutElement = gridFrame.gameObject.AddComponent<LayoutElement>();
        gridLayoutElement.flexibleHeight = 1f;

        collectionGridContentRoot = CreateGridScrollArea(gridFrame, out _, 5, new Vector2(74f, 74f), 10f);
    }

    private void BuildSettingsPanel()
    {
        settingsPanel = CreateUiObject("SettingsPanel", leftContentHost).gameObject;
        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        StretchToParent(panelRect);
        settingsPanel.SetActive(false);

        VerticalLayoutGroup layout = settingsPanel.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;

        RectTransform placeholderFrame = CreateSubSection(settingsPanel.transform, "Settings", -1f);
        LayoutElement layoutElement = placeholderFrame.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleHeight = 1f;

        TextMeshProUGUI placeholder = CreateText(
            "SettingsPlaceholder",
            placeholderFrame,
            new Vector2(24f, -76f),
            new Vector2(-24f, 220f),
            "Settings tab is reserved for a later update.\n\nThe pause flow and fixed action buttons already use the same frame so this panel can be expanded without rebuilding the menu.",
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            new Color(1f, 1f, 1f, 0.85f));
        placeholder.textWrappingMode = TextWrappingModes.Normal;
    }

    private PauseTabView CreateNavigationTab(Transform parent, PauseSection section, string label)
    {
        Button button = CreateButton(parent, $"Tab_{section}", label, out TextMeshProUGUI labelText, out Image backgroundImage);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 52f);

        button.onClick.AddListener(() => ShowSection(section));

        return new PauseTabView
        {
            Section = section,
            Button = button,
            Background = backgroundImage,
            Label = labelText
        };
    }

    private CollectionTabView CreateCollectionCategoryTab(Transform parent, CollectionCategory category, string label)
    {
        Button button = CreateButton(parent, $"Collection_{category}", label, out TextMeshProUGUI labelText, out Image backgroundImage);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(136f, 34f);

        button.onClick.AddListener(() =>
        {
            currentCollectionCategory = category;
            RefreshCollectionPanel();
        });

        return new CollectionTabView
        {
            Category = category,
            Button = button,
            Background = backgroundImage,
            Label = labelText
        };
    }

    private Button CreateActionButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction clicked)
    {
        Button button = CreateButton(parent, name, label, out TextMeshProUGUI labelText, out Image backgroundImage);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 48f);
        ApplyImageStyle(backgroundImage, style.actionButtonSprite, color);
        labelText.color = style.actionButtonTextColor;
        button.onClick.AddListener(clicked);
        return button;
    }

    private void HandleResumeClicked()
    {
        pauseController?.ResumeGame();
    }

    private void HandleQuitClicked()
    {
        pauseController?.QuitGame();
    }

    private void HandleUnlocksChanged()
    {
        if (!isActiveAndEnabled || currentSection != PauseSection.Collection)
            return;

        RefreshCollectionPanel();
    }

    private void ShowSection(PauseSection section)
    {
        ResolveDependencies();
        currentSection = section;

        currentItemsPanel.SetActive(section == PauseSection.CurrentItems);
        collectionPanel.SetActive(section == PauseSection.Collection);
        settingsPanel.SetActive(section == PauseSection.Settings);

        switch (section)
        {
            case PauseSection.CurrentItems:
                contentTitleLabel.text = "Current Items";
                RefreshCurrentItemsPanel();
                break;
            case PauseSection.Collection:
                contentTitleLabel.text = "Collection";
                RefreshCollectionPanel();
                break;
            default:
                contentTitleLabel.text = "Settings";
                ClearDynamicContent();
                ClearDetail("Settings options can be added here later.", string.Empty, null, true);
                break;
        }

        UpdateNavigationTabVisuals();
    }

    private void RefreshCurrentItemsPanel()
    {
        ClearDynamicContent();

        List<ItemEntryViewModel> passiveEntries = BuildCurrentPassiveEntries();
        ItemEntryViewModel? firstEntry = null;

        for (int i = 0; i < passiveEntries.Count; i++)
        {
            CreateCollectionSlot(currentPassiveContentRoot, passiveEntries[i]);
            if (!firstEntry.HasValue && passiveEntries[i].IsSelectable)
                firstEntry = passiveEntries[i];
        }

        if (firstEntry.HasValue)
            FocusEntry(firstEntry.Value);
        else
            ClearDetail(string.Empty, string.Empty, null, true);
    }

    private List<ItemEntryViewModel> BuildCurrentPassiveEntries()
    {
        List<ItemEntryViewModel> entries = new List<ItemEntryViewModel>();
        IReadOnlyList<PassiveItem> items = passiveItemList != null ? passiveItemList.GetPassvieItems() : Array.Empty<PassiveItem>();

        for (int i = 0; i < items.Count; i++)
        {
            PassiveItem item = items[i];
            if (item == null)
                continue;

            entries.Add(new ItemEntryViewModel
            {
                Icon = item.icon,
                DisplayName = string.IsNullOrWhiteSpace(item.itemName) ? "Unnamed Passive Item" : item.itemName,
                Description = string.IsNullOrWhiteSpace(item.description) ? "No description." : item.description,
                CategoryLabel = string.Empty,
                IsUnlocked = true,
                IsPassive = true,
                IsSelectable = true
            });
        }

        return entries;
    }

    private void RefreshCollectionPanel()
    {
        ClearDynamicContent();
        UpdateCollectionTabVisuals();

        if (catalog == null)
        {
            CreateEmptyState(collectionGridContentRoot, "Collection data is not ready.");
            ClearDetail("Collection data is not available.", string.Empty, null, true);
            return;
        }

        List<ItemEntryViewModel> entries = BuildCollectionEntries();
        ItemEntryViewModel? firstEntry = null;

        for (int i = 0; i < entries.Count; i++)
        {
            CreateCollectionSlot(collectionGridContentRoot, entries[i]);
            if (!firstEntry.HasValue && entries[i].IsSelectable)
                firstEntry = entries[i];
        }

        if (entries.Count == 0)
        {
            CreateEmptyState(collectionGridContentRoot, "No items registered in this category.");
            ClearDetail("No collection entries found.", string.Empty, null, true);
            return;
        }

        if (firstEntry.HasValue)
            FocusEntry(firstEntry.Value);
        else
            ClearDetail("Select an item to inspect its details.", string.Empty, null, true);
    }

    private List<ItemEntryViewModel> BuildCollectionEntries()
    {
        List<ItemEntryViewModel> entries = new List<ItemEntryViewModel>();

        if (currentCollectionCategory == CollectionCategory.Passive)
        {
            IReadOnlyList<PassiveItem> passiveItems = catalog.PassiveItems;
            for (int i = 0; i < passiveItems.Count; i++)
            {
                PassiveItem item = passiveItems[i];
                if (item == null)
                    continue;

                entries.Add(new ItemEntryViewModel
                {
                    Icon = item.icon,
                    DisplayName = string.IsNullOrWhiteSpace(item.itemName) ? "Unnamed Passive Item" : item.itemName,
                    Description = string.IsNullOrWhiteSpace(item.description) ? "No description." : item.description,
                    CategoryLabel = "Passive Item",
                    IsUnlocked = registry != null && registry.IsUnlocked(item),
                    IsPassive = true,
                    IsSelectable = true
                });
            }
        }
        else
        {
            IReadOnlyList<ActiveItem> activeItems = catalog.ActiveItems;
            for (int i = 0; i < activeItems.Count; i++)
            {
                ActiveItem item = activeItems[i];
                if (item == null)
                    continue;

                entries.Add(new ItemEntryViewModel
                {
                    Icon = item.icon,
                    DisplayName = string.IsNullOrWhiteSpace(item.itemName) ? "Unnamed Active Item" : item.itemName,
                    Description = string.IsNullOrWhiteSpace(item.description) ? "No description." : item.description,
                    CategoryLabel = "Active Item",
                    IsUnlocked = registry != null && registry.IsUnlocked(item),
                    IsPassive = false,
                    IsSelectable = true
                });
            }
        }

        entries.Sort((left, right) =>
        {
            int unlockCompare = right.IsUnlocked.CompareTo(left.IsUnlocked);
            if (unlockCompare != 0)
                return unlockCompare;

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        return entries;
    }

    private void CreateCollectionSlot(Transform parent, ItemEntryViewModel entry)
    {
        GameObject slotObject = CreateUiObject(entry.IsPassive ? "PassiveCollectionSlot" : "ActiveCollectionSlot", parent);
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(74f, 74f);

        bool isUnlocked = entry.IsUnlocked;
        Image background = slotObject.AddComponent<Image>();
        ApplyImageStyle(background, isUnlocked ? style.slotSprite : style.lockedSlotSprite, isUnlocked ? style.slotColor : style.lockedSlotColor);

        Outline outline = slotObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = slotObject.AddComponent<Button>();
        button.targetGraphic = background;
        ApplyButtonColors(button, background.color);

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
        iconImage.color = isUnlocked ? Color.white : Color.black;

        PauseItemCollectionSlot slot = slotObject.AddComponent<PauseItemCollectionSlot>();
        slot.Initialize(entry, FocusEntry, null);
        slot.SetSelectionOutline(outline, style.selectedOutlineColor);
        dynamicSlots.Add(slot);
    }

    private void CreateEmptyState(Transform parent, string message)
    {
        GameObject labelObject = CreateUiObject("EmptyState", parent);
        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, 56f);

        LayoutElement layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 56f;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = fontAsset;
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.text = message;
        label.color = new Color(1f, 1f, 1f, 0.68f);
        label.textWrappingMode = TextWrappingModes.Normal;
    }

    private void FocusEntry(ItemEntryViewModel entry)
    {
        for (int i = 0; i < dynamicSlots.Count; i++)
        {
            if (dynamicSlots[i] == null)
                continue;

            dynamicSlots[i].SetSelected(dynamicSlots[i].Matches(entry));
        }

        if (!entry.IsUnlocked)
        {
            ClearDetail("???", entry.CategoryLabel, entry.Icon, false);
            detailDescriptionLabel.text = "Unlock this item during play to reveal its full information in the collection.";
            if (detailIcon != null)
                detailIcon.color = Color.black;
            return;
        }

        ClearDetail(entry.DisplayName, entry.CategoryLabel, entry.Icon, false);
        detailDescriptionLabel.text = entry.Description;
    }

    private void ClearDetail(string title, string category, Sprite icon, bool clearIcon)
    {
        if (detailNameLabel != null)
            detailNameLabel.text = title ?? string.Empty;

        if (detailDescriptionLabel != null)
            detailDescriptionLabel.text = string.Empty;

        if (detailIcon == null)
            return;

        if (clearIcon)
        {
            detailIcon.sprite = null;
            detailIcon.color = Color.clear;
            return;
        }

        detailIcon.sprite = icon;
        detailIcon.color = icon != null ? Color.white : Color.clear;
    }

    private void ClearDynamicContent()
    {
        ClearTransformChildren(currentPassiveContentRoot);
        ClearTransformChildren(collectionGridContentRoot);
        dynamicSlots.Clear();
    }

    private static void ClearTransformChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void UpdateNavigationTabVisuals()
    {
        for (int i = 0; i < navigationTabs.Count; i++)
        {
            PauseTabView tab = navigationTabs[i];
            bool isSelected = tab.Section == currentSection;
            ApplyImageStyle(tab.Background, isSelected ? style.selectedTabSprite : style.tabSprite, isSelected ? style.selectedTabColor : style.tabColor);
            tab.Label.color = isSelected ? style.selectedTabTextColor : style.tabTextColor;
        }
    }

    private void UpdateCollectionTabVisuals()
    {
        for (int i = 0; i < collectionTabs.Count; i++)
        {
            CollectionTabView tab = collectionTabs[i];
            bool isSelected = tab.Category == currentCollectionCategory;
            ApplyImageStyle(tab.Background, isSelected ? style.selectedTabSprite : style.tabSprite, isSelected ? style.selectedTabColor : style.tabColor);
            tab.Label.color = isSelected ? style.selectedTabTextColor : style.tabTextColor;
        }
    }

    private void SetLegacyRootObjectsActive(bool active)
    {
        for (int i = 0; i < legacyRootObjects.Count; i++)
        {
            if (legacyRootObjects[i] == null)
                continue;

            legacyRootObjects[i].SetActive(active);
        }

        if (runtimeRoot != null)
            runtimeRoot.gameObject.SetActive(true);
    }

    private RectTransform CreateSubSection(Transform parent, string title, float preferredHeight)
    {
        string objectName = string.IsNullOrWhiteSpace(title) ? "SectionFrame" : title.Replace(" ", string.Empty) + "Frame";
        GameObject frameObject = CreateUiObject(objectName, parent);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();

        if (preferredHeight > 0f)
        {
            LayoutElement layoutElement = frameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
        }

        Image frameImage = frameObject.AddComponent<Image>();
        ApplyImageStyle(frameImage, style.subPanelSprite, style.subPanelColor);

        if (!string.IsNullOrWhiteSpace(title))
            CreateText("Title", frameRect, new Vector2(16f, -14f), new Vector2(-16f, 24f), title, 20f, FontStyles.Bold, TextAlignmentOptions.Left);

        return frameRect;
    }

    private RectTransform CreateVerticalScrollArea(RectTransform parent, out ScrollRect scrollRect)
    {
        RectTransform viewport = CreateScrollViewport(parent, out scrollRect);

        GameObject contentObject = CreateUiObject("Content", viewport);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        return contentRect;
    }

    private RectTransform CreateGridScrollArea(RectTransform parent, out ScrollRect scrollRect, int columnCount, Vector2 cellSize, float topInset = 42f)
    {
        RectTransform viewport = CreateScrollViewport(parent, out scrollRect, topInset);

        GameObject contentObject = CreateUiObject("Content", viewport);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        GridLayoutGroup gridLayout = contentObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(10f, 10f);
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Mathf.Max(1, columnCount);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        return contentRect;
    }

    private RectTransform CreateScrollViewport(RectTransform parent, out ScrollRect scrollRect, float topInset = 42f)
    {
        GameObject viewportObject = CreateUiObject("Viewport", parent);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-24f, -Mathf.Abs(topInset));

        Image viewportImage = viewportObject.AddComponent<Image>();
        ApplyImageStyle(viewportImage, null, new Color(0.07f, 0.07f, 0.07f, 0.45f));

        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect = parent.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 25f;

        GameObject scrollbarObject = CreateUiObject("Scrollbar", parent);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.offsetMin = new Vector2(-14f, 10f);
        scrollbarRect.offsetMax = new Vector2(-4f, -Mathf.Abs(topInset));

        Image scrollbarImage = scrollbarObject.AddComponent<Image>();
        ApplyImageStyle(scrollbarImage, null, new Color(0.06f, 0.06f, 0.06f, 0.7f));

        GameObject slidingAreaObject = CreateUiObject("SlidingArea", scrollbarRect);
        RectTransform slidingAreaRect = slidingAreaObject.GetComponent<RectTransform>();
        StretchToParent(slidingAreaRect);

        GameObject handleObject = CreateUiObject("Handle", slidingAreaRect);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 48f);

        Image handleImage = handleObject.AddComponent<Image>();
        ApplyImageStyle(handleImage, style.selectedTabSprite, new Color(0.8f, 0.8f, 0.8f, 1f));

        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        return viewportRect;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 offsetMin, Vector2 offsetMax, Sprite sprite, Color color)
    {
        GameObject panelObject = CreateUiObject(name, parent);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = offsetMin;
        panelRect.offsetMax = offsetMax;

        Image panelImage = panelObject.AddComponent<Image>();
        ApplyImageStyle(panelImage, sprite, color);
        return panelRect;
    }

    private Button CreateButton(Transform parent, string name, string labelText, out TextMeshProUGUI label, out Image backgroundImage)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();

        backgroundImage = buttonObject.AddComponent<Image>();
        ApplyImageStyle(backgroundImage, style.tabSprite, style.tabColor);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = backgroundImage;
        ApplyButtonColors(button, backgroundImage.color);

        label = CreateText("Label", buttonRect, new Vector2(0f, 0f), new Vector2(0f, 0f), labelText, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchToParent(label.rectTransform);
        label.margin = new Vector4(12f, 8f, 12f, 8f);
        label.color = style.tabTextColor;

        return button;
    }

    private void ApplyButtonColors(Button button, Color baseColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.08f;
        colors.pressedColor = baseColor * 0.9f;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
        button.colors = colors;
    }

    private void ApplyImageStyle(Image image, Sprite sprite, Color color)
    {
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, Vector2 offsetMin, Vector2 offsetMax, string content, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color? colorOverride = null)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        float left = offsetMin.x;
        float top = Mathf.Abs(offsetMin.y);
        float right = offsetMax.x;
        float height = Mathf.Abs(offsetMax.y);

        rect.offsetMin = new Vector2(left, -(top + height));
        rect.offsetMax = new Vector2(right, -top);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = colorOverride ?? Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    public struct ItemEntryViewModel
    {
        public Sprite Icon;
        public string DisplayName;
        public string Description;
        public string CategoryLabel;
        public bool IsUnlocked;
        public bool IsPassive;
        public bool IsSelectable;
    }
}
