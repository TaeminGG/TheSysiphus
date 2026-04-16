using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ItemRulesEditModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForEditModeItemTests()
    {
        Assert.Pass("EditMode item test scaffold is ready.");
    }

    [Test]
    [Ignore("TODO: Extract active item registration logic and verify successful inventory insertion.")]
    public void ActiveInventory_AddsItem_WhenPickupSucceeds()
    {
    }

    [Test]
    [Ignore("TODO: Extract passive item registration logic and verify successful list insertion.")]
    public void PassiveItemList_AddsPassiveItem_WhenPickupSucceeds()
    {
    }

    [Test]
    [Ignore("TODO: Extract passive stat-application logic and verify stat bonuses are applied once equipped.")]
    public void PassiveItem_AppliesStatBonus_WhenEquipped()
    {
    }

    [Test]
    public void ItemUnlockRegistry_TracksInitialAndRuntimeUnlocks()
    {
        GameObject registryObject = new GameObject("UnlockRegistryTest");
        TestPassiveItem initialPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestPassiveItem runtimePassive = ScriptableObject.CreateInstance<TestPassiveItem>();

        try
        {
            initialPassive.name = "InitialPassive";
            runtimePassive.name = "RuntimePassive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);
            SerializedProperty initialPassives = serializedRegistry.FindProperty("initiallyUnlockedPassiveItems");
            initialPassives.arraySize = 1;
            initialPassives.GetArrayElementAtIndex(0).objectReferenceValue = initialPassive;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();

            registry.ResetToInitialState();

            Assert.That(registry.IsUnlocked(initialPassive), Is.True);
            Assert.That(registry.IsUnlocked(runtimePassive), Is.False);
            Assert.That(registry.Unlock(runtimePassive), Is.True);
            Assert.That(registry.IsUnlocked(runtimePassive), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(initialPassive);
            Object.DestroyImmediate(runtimePassive);
        }
    }

    [Test]
    public void ItemSpawnPool_FiltersLockedItems_WhenRegistryExists()
    {
        GameObject registryObject = new GameObject("UnlockRegistryForPoolTest");
        ItemSpawnPool pool = ScriptableObject.CreateInstance<ItemSpawnPool>();
        TestPassiveItem unlockedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestPassiveItem lockedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestActiveItem unlockedActive = ScriptableObject.CreateInstance<TestActiveItem>();
        TestActiveItem lockedActive = ScriptableObject.CreateInstance<TestActiveItem>();

        try
        {
            unlockedPassive.name = "UnlockedPassive";
            lockedPassive.name = "LockedPassive";
            unlockedActive.name = "UnlockedActive";
            lockedActive.name = "LockedActive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);
            SerializedProperty initialPassives = serializedRegistry.FindProperty("initiallyUnlockedPassiveItems");
            initialPassives.arraySize = 1;
            initialPassives.GetArrayElementAtIndex(0).objectReferenceValue = unlockedPassive;

            SerializedProperty initialActives = serializedRegistry.FindProperty("initiallyUnlockedActiveItems");
            initialActives.arraySize = 1;
            initialActives.GetArrayElementAtIndex(0).objectReferenceValue = unlockedActive;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            registry.ResetToInitialState();

            SerializedObject serializedPool = new SerializedObject(pool);
            SerializedProperty passiveItems = serializedPool.FindProperty("passiveItems");
            passiveItems.arraySize = 2;
            passiveItems.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = unlockedPassive;
            passiveItems.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = lockedPassive;

            SerializedProperty activeItems = serializedPool.FindProperty("activeItems");
            activeItems.arraySize = 2;
            activeItems.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = unlockedActive;
            activeItems.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = lockedActive;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();

            System.Collections.Generic.List<ItemSpawnPool.RollResult> unlockedEntries = pool.GetUnlockedEntries(registry);

            Assert.That(unlockedEntries.Count, Is.EqualTo(2));
            Assert.That(ContainsPassive(unlockedEntries, unlockedPassive), Is.True);
            Assert.That(ContainsPassive(unlockedEntries, lockedPassive), Is.False);
            Assert.That(ContainsActive(unlockedEntries, unlockedActive), Is.True);
            Assert.That(ContainsActive(unlockedEntries, lockedActive), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(pool);
            Object.DestroyImmediate(unlockedPassive);
            Object.DestroyImmediate(lockedPassive);
            Object.DestroyImmediate(unlockedActive);
            Object.DestroyImmediate(lockedActive);
        }
    }

    [Test]
    [Ignore("TODO: Define duplicate-pickup behavior and verify it is handled consistently.")]
    public void ItemRule_HandlesDuplicatePickup_AsDesigned()
    {
    }

    private static bool ContainsPassive(System.Collections.Generic.List<ItemSpawnPool.RollResult> entries, PassiveItem target)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsPassive && entries[i].PassiveItem == target)
                return true;
        }

        return false;
    }

    private static bool ContainsActive(System.Collections.Generic.List<ItemSpawnPool.RollResult> entries, ActiveItem target)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (!entries[i].IsPassive && entries[i].ActiveItem == target)
                return true;
        }

        return false;
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
