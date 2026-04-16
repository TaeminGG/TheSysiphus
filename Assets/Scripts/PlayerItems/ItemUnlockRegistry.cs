using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemUnlockRegistry : MonoBehaviour
{
    [SerializeField] private ItemUnlockCatalog catalog;
    [SerializeField] private List<PassiveItem> initiallyUnlockedPassiveItems = new List<PassiveItem>();
    [SerializeField] private List<ActiveItem> initiallyUnlockedActiveItems = new List<ActiveItem>();

    private static ItemUnlockRegistry instance;

    private readonly HashSet<string> unlockedPassiveItemIds = new HashSet<string>();
    private readonly HashSet<string> unlockedActiveItemIds = new HashSet<string>();
    private readonly List<PassiveItem> unlockedPassiveItems = new List<PassiveItem>();
    private readonly List<ActiveItem> unlockedActiveItems = new List<ActiveItem>();

    public event Action UnlocksChanged;

    public static ItemUnlockRegistry Instance => FindInstance();

    public ItemUnlockCatalog Catalog => catalog;
    public IReadOnlyList<PassiveItem> UnlockedPassiveItems => unlockedPassiveItems;
    public IReadOnlyList<ActiveItem> UnlockedActiveItems => unlockedActiveItems;

    public static ItemUnlockRegistry FindInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ItemUnlockRegistry>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Multiple ItemUnlockRegistry instances found. Using the first active registry.", this);
            return;
        }

        instance = this;
        ResetToInitialState();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        UnlocksChanged = null;
    }

    public void ResetToInitialState()
    {
        unlockedPassiveItemIds.Clear();
        unlockedActiveItemIds.Clear();
        unlockedPassiveItems.Clear();
        unlockedActiveItems.Clear();

        for (int i = 0; i < initiallyUnlockedPassiveItems.Count; i++)
            RegisterUnlockedPassive(initiallyUnlockedPassiveItems[i]?.ItemId, initiallyUnlockedPassiveItems[i]);

        for (int i = 0; i < initiallyUnlockedActiveItems.Count; i++)
            RegisterUnlockedActive(initiallyUnlockedActiveItems[i]?.ItemId, initiallyUnlockedActiveItems[i]);

        UnlocksChanged?.Invoke();
    }

    public bool Unlock(PassiveItem item)
    {
        bool added = RegisterUnlockedPassive(item?.ItemId, item);
        if (added)
            UnlocksChanged?.Invoke();

        return added;
    }

    public bool Unlock(ActiveItem item)
    {
        bool added = RegisterUnlockedActive(item?.ItemId, item);
        if (added)
            UnlocksChanged?.Invoke();

        return added;
    }

    public bool IsUnlocked(PassiveItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            return false;

        if (unlockedPassiveItemIds.Contains(item.ItemId))
        {
            EnsurePassiveReference(item);
            return true;
        }

        return false;
    }

    public bool IsUnlocked(ActiveItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            return false;

        if (unlockedActiveItemIds.Contains(item.ItemId))
        {
            EnsureActiveReference(item);
            return true;
        }

        return false;
    }

    public ItemUnlockSnapshot CaptureSnapshot()
    {
        ItemUnlockSnapshot snapshot = new ItemUnlockSnapshot();
        snapshot.SetUnlockedPassiveItemIds(unlockedPassiveItemIds);
        snapshot.SetUnlockedActiveItemIds(unlockedActiveItemIds);
        return snapshot;
    }

    public void RestoreSnapshot(ItemUnlockSnapshot snapshot)
    {
        ResetToInitialState();

        if (snapshot == null)
            return;

        IReadOnlyList<string> passiveIds = snapshot.UnlockedPassiveItemIds;
        for (int i = 0; i < passiveIds.Count; i++)
        {
            string itemId = passiveIds[i];
            PassiveItem item = ResolvePassiveItem(itemId);
            RegisterUnlockedPassive(itemId, item);
        }

        IReadOnlyList<string> activeIds = snapshot.UnlockedActiveItemIds;
        for (int i = 0; i < activeIds.Count; i++)
        {
            string itemId = activeIds[i];
            ActiveItem item = ResolveActiveItem(itemId);
            RegisterUnlockedActive(itemId, item);
        }

        UnlocksChanged?.Invoke();
    }

    private PassiveItem ResolvePassiveItem(string itemId)
    {
        if (catalog != null && catalog.TryGetPassiveItem(itemId, out PassiveItem item))
            return item;

        return FindInInitialList(initiallyUnlockedPassiveItems, itemId);
    }

    private ActiveItem ResolveActiveItem(string itemId)
    {
        if (catalog != null && catalog.TryGetActiveItem(itemId, out ActiveItem item))
            return item;

        return FindInInitialList(initiallyUnlockedActiveItems, itemId);
    }

    private static T FindInInitialList<T>(List<T> items, string itemId) where T : ScriptableObject
    {
        for (int i = 0; i < items.Count; i++)
        {
            T item = items[i];
            if (item == null)
                continue;

            if (item is PassiveItem passiveItem && passiveItem.ItemId == itemId)
                return item;

            if (item is ActiveItem activeItem && activeItem.ItemId == itemId)
                return item;
        }

        return null;
    }

    private bool RegisterUnlockedPassive(string itemId, PassiveItem item)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !unlockedPassiveItemIds.Add(itemId))
            return false;

        if (item != null && !unlockedPassiveItems.Contains(item))
            unlockedPassiveItems.Add(item);

        return true;
    }

    private bool RegisterUnlockedActive(string itemId, ActiveItem item)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !unlockedActiveItemIds.Add(itemId))
            return false;

        if (item != null && !unlockedActiveItems.Contains(item))
            unlockedActiveItems.Add(item);

        return true;
    }

    private void EnsurePassiveReference(PassiveItem item)
    {
        if (item != null && !unlockedPassiveItems.Contains(item))
            unlockedPassiveItems.Add(item);
    }

    private void EnsureActiveReference(ActiveItem item)
    {
        if (item != null && !unlockedActiveItems.Contains(item))
            unlockedActiveItems.Add(item);
    }
}
