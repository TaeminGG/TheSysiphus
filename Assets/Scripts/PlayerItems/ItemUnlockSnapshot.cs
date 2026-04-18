using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemUnlockSnapshot
{
    [SerializeField] private List<string> unlockedPassiveItemIds = new List<string>();
    [SerializeField] private List<string> unlockedActiveItemIds = new List<string>();
    [SerializeField] private List<string> collectedPassiveItemIds = new List<string>();

    public IReadOnlyList<string> UnlockedPassiveItemIds => unlockedPassiveItemIds;
    public IReadOnlyList<string> UnlockedActiveItemIds => unlockedActiveItemIds;
    public IReadOnlyList<string> CollectedPassiveItemIds => collectedPassiveItemIds;

    public void SetUnlockedPassiveItemIds(IEnumerable<string> itemIds)
    {
        Replace(unlockedPassiveItemIds, itemIds);
    }

    public void SetUnlockedActiveItemIds(IEnumerable<string> itemIds)
    {
        Replace(unlockedActiveItemIds, itemIds);
    }

    public void SetCollectedPassiveItemIds(IEnumerable<string> itemIds)
    {
        Replace(collectedPassiveItemIds, itemIds);
    }

    private static void Replace(List<string> target, IEnumerable<string> source)
    {
        target.Clear();
        if (source == null)
            return;

        foreach (string itemId in source)
        {
            if (string.IsNullOrWhiteSpace(itemId) || target.Contains(itemId))
                continue;

            target.Add(itemId);
        }
    }
}

public interface IItemUnlockStateStore
{
    bool TryLoad(out ItemUnlockSnapshot snapshot);

    void Save(ItemUnlockSnapshot snapshot);
}
