using System.Collections.Generic;
using UnityEngine;

public class MerchantStockSpawner : MonoBehaviour
{
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
        SpawnStock();
    }

    [ContextMenu("Spawn Stock")]
    public void SpawnStock()
    {
        ClearSpawnedItems();

        if (pickupItemPrefab == null || displayPoints == null || displayPoints.Length == 0)
            return;

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        List<RuntimeStockEntry> availableEntries = BuildAvailableEntries();
        if (availableEntries.Count == 0)
            return;

        for (int i = 0; i < displayPoints.Length; i++)
        {
            Transform displayPoint = displayPoints[i];
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
            spawnedItems.Add(spawnedItem);

            if (!allowDuplicates)
                availableEntries.RemoveAt(selectedIndex);
        }
    }

    private List<RuntimeStockEntry> BuildAvailableEntries()
    {
        List<RuntimeStockEntry> availableEntries = new List<RuntimeStockEntry>();
        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();

        if (stockPool != null)
        {
            List<ItemSpawnPool.RollResult> poolEntries = stockPool.GetUnlockedEntries(registry);
            for (int i = 0; i < poolEntries.Count; i++)
            {
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
