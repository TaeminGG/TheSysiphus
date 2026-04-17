using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSpawnPool", menuName = "Scriptable Objects/Items/Spawn Pool")]
public class ItemSpawnPool : ScriptableObject
{
    [Serializable]
    public class PassiveEntry
    {
        public PassiveItem item;
        public ItemRarity rarity = ItemRarity.Common;
        [Min(0.01f)] public float weightMultiplier = 1f;
        [Min(0)] public int price = 0;

        public float Weight => Mathf.Max(0.01f, ItemRarityUtility.GetWeight(rarity) * Mathf.Max(0.01f, weightMultiplier));
    }

    [Serializable]
    public class ActiveEntry
    {
        public ActiveItem item;
        public ItemRarity rarity = ItemRarity.Common;
        [Min(0.01f)] public float weightMultiplier = 1f;
        [Min(0)] public int price = 0;

        public float Weight => Mathf.Max(0.01f, ItemRarityUtility.GetWeight(rarity) * Mathf.Max(0.01f, weightMultiplier));
    }

    public struct RollResult
    {
        public bool IsPassive;
        public PassiveItem PassiveItem;
        public ActiveItem ActiveItem;
        public int Price;
        public float Weight;
    }

    [SerializeField] private List<PassiveEntry> passiveItems = new List<PassiveEntry>();
    [SerializeField] private List<ActiveEntry> activeItems = new List<ActiveEntry>();

    public List<RollResult> GetUnlockedEntries(ItemUnlockRegistry registry)
    {
        List<RollResult> results = new List<RollResult>();
        AppendUnlockedPassiveEntries(results, registry);
        AppendUnlockedActiveEntries(results, registry);
        return results;
    }

    public bool TryRollChestLoot(ItemUnlockRegistry registry, out RollResult result)
    {
        List<RollResult> unlockedPassives = new List<RollResult>();
        List<RollResult> unlockedActives = new List<RollResult>();

        AppendUnlockedPassiveEntries(unlockedPassives, registry);
        AppendUnlockedActiveEntries(unlockedActives, registry);

        bool hasPassive = unlockedPassives.Count > 0;
        bool hasActive = unlockedActives.Count > 0;

        if (!hasPassive && !hasActive)
        {
            result = default;
            return false;
        }

        if (hasPassive && (!hasActive || UnityEngine.Random.value < 0.5f))
            return TryRollFromEntries(unlockedPassives, out result);

        return TryRollFromEntries(unlockedActives, out result);
    }

    private void AppendUnlockedPassiveEntries(List<RollResult> results, ItemUnlockRegistry registry)
    {
        for (int i = 0; i < passiveItems.Count; i++)
        {
            PassiveEntry entry = passiveItems[i];
            if (entry == null || entry.item == null)
                continue;

            if (registry != null && !registry.IsUnlocked(entry.item))
                continue;

            results.Add(new RollResult
            {
                IsPassive = true,
                PassiveItem = entry.item,
                ActiveItem = null,
                Price = entry.price,
                Weight = entry.Weight
            });
        }
    }

    private void AppendUnlockedActiveEntries(List<RollResult> results, ItemUnlockRegistry registry)
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            ActiveEntry entry = activeItems[i];
            if (entry == null || entry.item == null)
                continue;

            if (registry != null && !registry.IsUnlocked(entry.item))
                continue;

            results.Add(new RollResult
            {
                IsPassive = false,
                PassiveItem = null,
                ActiveItem = entry.item,
                Price = entry.price,
                Weight = entry.Weight
            });
        }
    }

    private static bool TryRollFromEntries(List<RollResult> results, out RollResult result)
    {
        float totalWeight = 0f;
        for (int i = 0; i < results.Count; i++)
            totalWeight += Mathf.Max(0.01f, results[i].Weight);

        if (totalWeight <= 0f)
        {
            result = default;
            return false;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < results.Count; i++)
        {
            cumulative += Mathf.Max(0.01f, results[i].Weight);
            if (roll <= cumulative)
            {
                result = results[i];
                return true;
            }
        }

        result = results[results.Count - 1];
        return true;
    }
}
