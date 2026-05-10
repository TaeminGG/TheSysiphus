using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SaveSystemEditModeTests
{
    private string testDirectory;

    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(Application.temporaryCachePath, "MediaProjectSaveTests", System.Guid.NewGuid().ToString("N"));
        RunSaveRuntimeState.ClearRunState();
    }

    [TearDown]
    public void TearDown()
    {
        RunSaveRuntimeState.ClearRunState();
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, true);
    }

    [Test]
    public void Repository_ReturnsFreshProfile_WhenNoProfileExists()
    {
        GameSaveRepository repository = new GameSaveRepository(testDirectory);

        ProfileSaveData profile = repository.LoadProfile();

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.Version, Is.EqualTo(ProfileSaveData.CurrentVersion));
        Assert.That(profile.ItemUnlockSnapshot, Is.Not.Null);
        Assert.That(profile.Settings, Is.Not.Null);
    }

    [Test]
    public void Repository_SavesAndLoadsProfileSettings()
    {
        GameSaveRepository repository = new GameSaveRepository(testDirectory);
        ProfileSaveData profile = new ProfileSaveData();
        profile.Settings.MasterVolume = 0.35f;
        profile.Settings.MusicVolume = 0.5f;
        profile.Settings.Fullscreen = false;
        profile.Settings.MouseSensitivity = 1.75f;

        repository.SaveProfile(profile);
        ProfileSaveData loaded = repository.LoadProfile();

        Assert.That(loaded.Settings.MasterVolume, Is.EqualTo(0.35f).Within(0.001f));
        Assert.That(loaded.Settings.MusicVolume, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(loaded.Settings.Fullscreen, Is.False);
        Assert.That(loaded.Settings.MouseSensitivity, Is.EqualTo(1.75f).Within(0.001f));
        Assert.That(File.Exists(repository.ProfilePath), Is.True);
    }

    [Test]
    public void Repository_TracksAndDeletesActiveRun()
    {
        GameSaveRepository repository = new GameSaveRepository(testDirectory);

        Assert.That(repository.HasActiveRun, Is.False);

        RunSaveData runData = repository.StartNewRun("SampleScene");

        Assert.That(runData.ActiveRun, Is.True);
        Assert.That(repository.HasActiveRun, Is.True);
        Assert.That(repository.TryLoadRun(out RunSaveData loaded), Is.True);
        Assert.That(loaded.SceneName, Is.EqualTo("SampleScene"));

        repository.DeleteRun();

        Assert.That(repository.HasActiveRun, Is.False);
        Assert.That(File.Exists(repository.RunPath), Is.False);
    }

    [Test]
    public void RunSaveStateController_CapturesAndRestoresPlayerContinuityState()
    {
        GameObject controllerObject = new GameObject("RunSaveStateControllerTest");
        GameObject registryObject = new GameObject("RunSaveRegistryTest");
        GameObject player = new GameObject("Player");
        TestPassiveItem passiveItem = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestActiveItem activeItem = ScriptableObject.CreateInstance<TestActiveItem>();
        ItemUnlockCatalog catalog = ScriptableObject.CreateInstance<ItemUnlockCatalog>();

        try
        {
            passiveItem.name = "ContinuityPassive";
            activeItem.name = "ContinuityActive";
            ConfigureCatalog(catalog, passiveItem, activeItem);

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);
            serializedRegistry.FindProperty("catalog").objectReferenceValue = catalog;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            registry.ResetToInitialState();

            PlayerStats stats = player.AddComponent<PlayerStats>();
            PassiveItemList passiveItemList = player.AddComponent<PassiveItemList>();
            ActiveInventory activeInventory = player.AddComponent<ActiveInventory>();
            PlayerMove playerMove = player.AddComponent<PlayerMove>();

            player.transform.position = new Vector3(12.5f, -3.25f, 0f);
            playerMove.floorZ = 4;
            playerMove.playerZ = 4;
            stats.RestoreRunState(150f, 87f, 42, 2, 5);
            passiveItemList.RestoreItems(new[] { passiveItem }, stats);
            activeInventory.RestoreItems(new[] { activeItem });

            RunSaveStateController controller = controllerObject.AddComponent<RunSaveStateController>();

            RunSaveData captured = controller.Capture("SampleScene");

            player.transform.position = Vector3.zero;
            playerMove.floorZ = 0;
            playerMove.playerZ = 0;
            stats.RestoreRunState(100f, 15f, 0, 0, 5);
            passiveItemList.RestoreItems(System.Array.Empty<PassiveItem>(), stats);
            activeInventory.RestoreItems(System.Array.Empty<ActiveItem>());

            Assert.That(controller.Restore(captured), Is.True);

            Assert.That(player.transform.position, Is.EqualTo(new Vector3(12.5f, -3.25f, 0f)));
            Assert.That(playerMove.floorZ, Is.EqualTo(4));
            Assert.That(stats.GetMaxHp(), Is.EqualTo(150f).Within(0.001f));
            Assert.That(stats.GetCurrentHp(), Is.EqualTo(87f).Within(0.001f));
            Assert.That(stats.GetGold(), Is.EqualTo(42));
            Assert.That(stats.GetPotionCount(), Is.EqualTo(2));
            Assert.That(passiveItemList.GetPassvieItems(), Does.Contain(passiveItem));
            Assert.That(activeInventory.GetItems(), Does.Contain(activeItem));
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(passiveItem);
            Object.DestroyImmediate(activeItem);
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void SaveManager_SaveCurrentRun_PersistsCapturedRunForContinue()
    {
        GameSaveRepository repository = new GameSaveRepository(testDirectory);
        GameObject managerObject = new GameObject("SaveManagerRunTest");
        GameObject player = new GameObject("Player");

        try
        {
            SaveManager saveManager = managerObject.AddComponent<SaveManager>();
            saveManager.ConfigureRepositoryForTests(repository);
            saveManager.StartNewRun("SampleScene");

            PlayerStats stats = player.AddComponent<PlayerStats>();
            PlayerMove playerMove = player.AddComponent<PlayerMove>();
            player.transform.position = new Vector3(3f, 5f, 0f);
            playerMove.floorZ = 2;
            playerMove.playerZ = 2;
            stats.RestoreRunState(125f, 64f, 9, 3, 5);

            RunSaveData saved = saveManager.SaveCurrentRun("test");

            Assert.That(File.Exists(repository.RunPath), Is.True);
            Assert.That(repository.TryLoadRun(out RunSaveData loaded), Is.True);
            Assert.That(loaded.RunId, Is.EqualTo(saved.RunId));
            Assert.That(loaded.Player.hasData, Is.True);
            Assert.That(loaded.Player.position, Is.EqualTo(new Vector3(3f, 5f, 0f)));
            Assert.That(loaded.Player.floorZ, Is.EqualTo(2));
            Assert.That(loaded.Player.currentHp, Is.EqualTo(64f).Within(0.001f));
            Assert.That(loaded.Player.gold, Is.EqualTo(9));
            Assert.That(loaded.Player.potionCount, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void RunSaveStateController_PersistsDeadEnemyTombstones()
    {
        GameObject controllerObject = new GameObject("RunSaveDeadEnemyControllerTest");

        try
        {
            RunSaveRuntimeState.MarkEnemyDead("enemy-save-id");
            RunSaveStateController controller = controllerObject.AddComponent<RunSaveStateController>();

            RunSaveData captured = controller.Capture("SampleScene");

            Assert.That(captured.Enemies.Exists(enemy => enemy.saveId == "enemy-save-id" && enemy.isDead), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void SaveManager_SavesUnlocksWithoutRunCollectedState()
    {
        GameSaveRepository repository = new GameSaveRepository(testDirectory);
        GameObject managerObject = new GameObject("SaveManagerTest");
        SaveManager saveManager = null;

        try
        {
            saveManager = managerObject.AddComponent<SaveManager>();
            saveManager.ConfigureRepositoryForTests(repository);

            ItemUnlockSnapshot snapshot = new ItemUnlockSnapshot();
            snapshot.SetUnlockedPassiveItemIds(new[] { "passive_unlock" });
            snapshot.SetUnlockedActiveItemIds(new[] { "active_unlock" });
            snapshot.SetCollectedPassiveItemIds(new[] { "passive_unlock" });

            saveManager.Save(snapshot);

            ProfileSaveData loaded = repository.LoadProfile();
            Assert.That(loaded.ItemUnlockSnapshot.UnlockedPassiveItemIds, Does.Contain("passive_unlock"));
            Assert.That(loaded.ItemUnlockSnapshot.UnlockedActiveItemIds, Does.Contain("active_unlock"));
            Assert.That(loaded.ItemUnlockSnapshot.CollectedPassiveItemIds, Is.Empty);
        }
        finally
        {
            if (saveManager != null)
                Object.DestroyImmediate(saveManager.gameObject);
            else
                Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void PauseCollectionMenu_ReportsUnlockedCatalogItemDetails()
    {
        GameObject registryObject = new GameObject("CollectionRegistryTest");
        GameObject menuObject = new GameObject("CollectionMenuTest");
        TestPassiveItem unlockedItem = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestPassiveItem lockedItem = ScriptableObject.CreateInstance<TestPassiveItem>();
        ItemUnlockCatalog catalog = ScriptableObject.CreateInstance<ItemUnlockCatalog>();

        try
        {
            unlockedItem.name = "UnlockedPassiveId";
            unlockedItem.itemName = "Unlocked Passive";
            unlockedItem.description = "Unlocked item description.";
            lockedItem.name = "LockedPassiveId";
            lockedItem.itemName = "Locked Passive";
            lockedItem.description = "Locked item description.";

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty passiveItems = serializedCatalog.FindProperty("passiveItems");
            passiveItems.arraySize = 2;
            passiveItems.GetArrayElementAtIndex(0).objectReferenceValue = unlockedItem;
            passiveItems.GetArrayElementAtIndex(1).objectReferenceValue = lockedItem;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);
            serializedRegistry.FindProperty("catalog").objectReferenceValue = catalog;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            registry.ResetToInitialState();
            Assert.That(registry.Unlock(unlockedItem), Is.True);

            PauseItemCollectionMenu menu = menuObject.AddComponent<PauseItemCollectionMenu>();

            Assert.That(menu.TryGetCollectionEntryForTests("Unlocked Passive", true, out PauseItemCollectionMenu.ItemEntryViewModel unlockedEntry), Is.True);
            Assert.That(unlockedEntry.IsUnlocked, Is.True);
            Assert.That(unlockedEntry.Description, Is.EqualTo("Unlocked item description."));

            Assert.That(menu.TryGetCollectionEntryForTests("Locked Passive", true, out PauseItemCollectionMenu.ItemEntryViewModel lockedEntry), Is.True);
            Assert.That(lockedEntry.IsUnlocked, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(menuObject);
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(unlockedItem);
            Object.DestroyImmediate(lockedItem);
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void PauseCollectionMenu_TitleModeRemovesCurrentItemsTabAndResumeClosesPanel()
    {
        GameObject panelObject = new GameObject("TitleCollectionPanel", typeof(RectTransform));

        try
        {
            PauseItemCollectionMenu menu = panelObject.AddComponent<PauseItemCollectionMenu>();
            menu.ConfigureTitleMenuModeForTests(true, panelObject);
            menu.RebuildEditableLayout();

            Assert.That(FindChild(panelObject.transform, "Tab_CurrentItems"), Is.Null);
            Assert.That(FindChild(panelObject.transform, "Tab_Collection"), Is.Not.Null);
            Assert.That(FindChild(panelObject.transform, "Tab_Settings"), Is.Not.Null);

            MethodInfo resumeMethod = typeof(PauseItemCollectionMenu).GetMethod("HandleResumeClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resumeMethod, Is.Not.Null);

            panelObject.SetActive(true);
            resumeMethod.Invoke(menu, null);

            Assert.That(panelObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = FindChild(parent.GetChild(i), childName);
            if (child != null)
                return child;
        }

        return null;
    }

    private static void ConfigureCatalog(ItemUnlockCatalog catalog, PassiveItem passiveItem, ActiveItem activeItem)
    {
        SerializedObject serializedCatalog = new SerializedObject(catalog);

        SerializedProperty passiveItems = serializedCatalog.FindProperty("passiveItems");
        passiveItems.arraySize = passiveItem != null ? 1 : 0;
        if (passiveItem != null)
            passiveItems.GetArrayElementAtIndex(0).objectReferenceValue = passiveItem;

        SerializedProperty activeItems = serializedCatalog.FindProperty("activeItems");
        activeItems.arraySize = activeItem != null ? 1 : 0;
        if (activeItem != null)
            activeItems.GetArrayElementAtIndex(0).objectReferenceValue = activeItem;

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
    }

    private class TestPassiveItem : PassiveItem
    {
        public override void OnEquip(PlayerStats stats)
        {
        }

        public override void OnUnequip(PlayerStats stats)
        {
        }
    }

    private class TestActiveItem : ActiveItem
    {
        public override void OnUsed()
        {
        }
    }
}
