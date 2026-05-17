using System.Collections.Generic;
using UnityEngine;

public class MerchantStockSpawner : MonoBehaviour
{
    private enum StockSlotItemType
    {
        Passive,
        Active
    }

    [System.Serializable]
    public class MerchantStockEntry
    {
        public bool isPassive = true;
        public PassiveItem passiveItem;
        public ActiveItem activeItem;
        [Min(0)] public int price = 10;
        [Min(0.01f)] public float weight = 1f;

        public bool IsValid()
        {
            return isPassive ? passiveItem != null : activeItem != null;
        }

        public float GetEffectiveWeight()
        {
            ItemRarity rarity = isPassive
                ? (passiveItem != null ? passiveItem.Rarity : ItemRarity.Common)
                : (activeItem != null ? activeItem.Rarity : ItemRarity.Common);

            return Mathf.Max(0.01f, ItemRarityUtility.GetWeight(rarity) * Mathf.Max(0.01f, weight));
        }
    }

    public MerchantNPC merchant;
    public PickupItem pickupItemPrefab;
    public Transform[] displayPoints;
    public ItemSpawnPool stockPool;
    public MerchantStockEntry[] stockEntries;
    public bool allowDuplicates = false;
    public int floorZ = 0;

    private readonly List<PickupItem> spawnedItems = new List<PickupItem>();
    private bool hasSpawnedStock;

    private struct RuntimeStockEntry
    {
        public bool IsPassive;
        public PassiveItem PassiveItem;
        public ActiveItem ActiveItem;
        public int Price;
        public float Weight;
    }

    private void Start()
    {
        EnsureStockSpawned();
    }

    public void EnsureStockSpawned()
    {
        if (hasSpawnedStock)
            return;

        SpawnStock();
    }

    [ContextMenu("Spawn Stock")]
    public void SpawnStock()
    {
        hasSpawnedStock = true;
        ClearSpawnedItems();

        if (pickupItemPrefab == null || displayPoints == null || displayPoints.Length == 0)
            return;

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        List<RuntimeStockEntry> passiveEntries = BuildAvailableEntries(StockSlotItemType.Passive);
        List<RuntimeStockEntry> activeEntries = BuildAvailableEntries(StockSlotItemType.Active);
        if (passiveEntries.Count == 0 && activeEntries.Count == 0)
            return;

        for (int i = 0; i < displayPoints.Length; i++)
        {
            Transform displayPoint = displayPoints[i];
            List<RuntimeStockEntry> availableEntries = i == 0 ? passiveEntries : activeEntries;
            if (displayPoint == null || availableEntries.Count == 0)
                continue;

            int selectedIndex = SelectWeightedIndex(availableEntries);
            if (selectedIndex < 0)
                continue;

            RuntimeStockEntry entry = availableEntries[selectedIndex];
            if (entry.IsPassive && registry != null && !registry.TryReservePassive(entry.PassiveItem))
            {
                availableEntries.RemoveAt(selectedIndex);
                i--;
                continue;
            }

            PickupItem spawnedItem = Instantiate(pickupItemPrefab, displayPoint.position, displayPoint.rotation);
            if (spawnedItem == null)
            {
                if (entry.IsPassive)
                    registry?.ReleasePassiveReservation(entry.PassiveItem);

                continue;
            }

            spawnedItem.ConfigureAsMerchantItem(entry.IsPassive, entry.PassiveItem, entry.ActiveItem, entry.Price, merchant, floorZ, entry.IsPassive);
            AssignRunSaveIdentity(spawnedItem, i);
            spawnedItems.Add(spawnedItem);

            if (entry.IsPassive)
                availableEntries.RemoveAt(selectedIndex);
        }
    }

    private void AssignRunSaveIdentity(PickupItem spawnedItem, int displayIndex)
    {
        if (spawnedItem == null)
            return;

        RunSaveIdentity ownerIdentity = GetComponent<RunSaveIdentity>();
        if (ownerIdentity == null && merchant != null)
            ownerIdentity = merchant.GetComponent<RunSaveIdentity>();

        string ownerSaveId = ownerIdentity != null ? ownerIdentity.SaveId : gameObject.name;
        RunSaveIdentity.Ensure(spawnedItem.gameObject, $"{ownerSaveId}:merchant-stock:{displayIndex}", "MerchantStockItem");
    }

    private List<RuntimeStockEntry> BuildAvailableEntries(StockSlotItemType itemType)
    {
        List<RuntimeStockEntry> availableEntries = new List<RuntimeStockEntry>();
        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();

        AppendUnlockedRegistryEntries(availableEntries, registry, itemType);
        if (registry != null)
            return availableEntries;

        if (stockPool != null)
        {
            List<ItemSpawnPool.RollResult> poolEntries = stockPool.GetUnlockedEntries(registry);
            for (int i = 0; i < poolEntries.Count; i++)
            {
                if (!MatchesItemType(poolEntries[i].IsPassive, itemType))
                    continue;

                availableEntries.Add(new RuntimeStockEntry
                {
                    IsPassive = poolEntries[i].IsPassive,
                    PassiveItem = poolEntries[i].PassiveItem,
                    ActiveItem = poolEntries[i].ActiveItem,
                    Price = poolEntries[i].Price,
                    Weight = poolEntries[i].Weight
                });
            }

            return availableEntries;
        }

        if (stockEntries == null)
            return availableEntries;

        for (int i = 0; i < stockEntries.Length; i++)
        {
            MerchantStockEntry stockEntry = stockEntries[i];
            if (stockEntry == null || !stockEntry.IsValid())
                continue;

            if (!MatchesItemType(stockEntry.isPassive, itemType))
                continue;

            if (stockEntry.isPassive && registry != null && registry.IsUnavailable(stockEntry.passiveItem))
                continue;

            availableEntries.Add(new RuntimeStockEntry
            {
                IsPassive = stockEntry.isPassive,
                PassiveItem = stockEntry.passiveItem,
                ActiveItem = stockEntry.activeItem,
                Price = stockEntry.price,
                Weight = stockEntry.GetEffectiveWeight()
            });
        }

        return availableEntries;
    }

    private void AppendUnlockedRegistryEntries(List<RuntimeStockEntry> availableEntries, ItemUnlockRegistry registry, StockSlotItemType itemType)
    {
        if (registry == null)
            return;

        if (itemType == StockSlotItemType.Passive)
        {
            IReadOnlyList<PassiveItem> unlockedPassiveItems = registry.UnlockedPassiveItems;
            for (int i = 0; i < unlockedPassiveItems.Count; i++)
            {
                PassiveItem item = unlockedPassiveItems[i];
                if (item == null || registry.IsUnavailable(item))
                    continue;

                availableEntries.Add(new RuntimeStockEntry
                {
                    IsPassive = true,
                    PassiveItem = item,
                    ActiveItem = null,
                    Price = ResolvePrice(item),
                    Weight = ItemRarityUtility.GetWeight(item.Rarity)
                });
            }

            return;
        }

        IReadOnlyList<ActiveItem> unlockedActiveItems = registry.UnlockedActiveItems;
        for (int i = 0; i < unlockedActiveItems.Count; i++)
        {
            ActiveItem item = unlockedActiveItems[i];
            if (item == null)
                continue;

            availableEntries.Add(new RuntimeStockEntry
            {
                IsPassive = false,
                PassiveItem = null,
                ActiveItem = item,
                Price = ResolvePrice(item),
                Weight = ItemRarityUtility.GetWeight(item.Rarity)
            });
        }
    }

    private static bool MatchesItemType(bool isPassive, StockSlotItemType itemType)
    {
        return itemType == StockSlotItemType.Passive ? isPassive : !isPassive;
    }

    private int ResolvePrice(PassiveItem item)
    {
        if (item == null)
            return 0;

        if (stockPool != null && stockPool.TryGetConfiguredPrice(item, out int poolPrice))
            return poolPrice;

        if (stockEntries != null)
        {
            for (int i = 0; i < stockEntries.Length; i++)
            {
                MerchantStockEntry entry = stockEntries[i];
                if (entry != null && entry.isPassive && entry.passiveItem == item)
                    return entry.price;
            }
        }

        return GetDefaultPrice(true, item.Rarity);
    }

    private int ResolvePrice(ActiveItem item)
    {
        if (item == null)
            return 0;

        if (stockPool != null && stockPool.TryGetConfiguredPrice(item, out int poolPrice))
            return poolPrice;

        if (stockEntries != null)
        {
            for (int i = 0; i < stockEntries.Length; i++)
            {
                MerchantStockEntry entry = stockEntries[i];
                if (entry != null && !entry.isPassive && entry.activeItem == item)
                    return entry.price;
            }
        }

        return GetDefaultPrice(false, item.Rarity);
    }

    private static int GetDefaultPrice(bool isPassive, ItemRarity rarity)
    {
        if (!isPassive)
            return rarity >= ItemRarity.Epic ? 6 : 5;

        switch (rarity)
        {
            case ItemRarity.Common:
                return 3;
            case ItemRarity.Uncommon:
                return 4;
            case ItemRarity.Rare:
                return 6;
            case ItemRarity.Epic:
                return 8;
            case ItemRarity.Legendary:
                return 10;
            default:
                return 3;
        }
    }

    private int SelectWeightedIndex(List<RuntimeStockEntry> availableEntries)
    {
        float totalWeight = 0f;
        for (int i = 0; i < availableEntries.Count; i++)
            totalWeight += Mathf.Max(0.01f, availableEntries[i].Weight);

        if (totalWeight <= 0f)
            return -1;

        float roll = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        for (int i = 0; i < availableEntries.Count; i++)
        {
            cumulativeWeight += Mathf.Max(0.01f, availableEntries[i].Weight);

            if (roll <= cumulativeWeight)
                return i;
        }

        return availableEntries.Count - 1;
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(spawnedItems[i].gameObject);
            else
                DestroyImmediate(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }
}
