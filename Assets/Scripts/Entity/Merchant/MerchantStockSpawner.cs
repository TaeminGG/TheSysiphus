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
    }

    public MerchantNPC merchant;
    public PickupItem pickupItemPrefab;
    public Transform[] displayPoints;
    public MerchantStockEntry[] stockEntries;
    public bool allowDuplicates = false;
    public int floorZ = 0;

    private readonly List<PickupItem> spawnedItems = new List<PickupItem>();

    private void Start()
    {
        SpawnStock();
    }

    [ContextMenu("Spawn Stock")]
    public void SpawnStock()
    {
        ClearSpawnedItems();

        if (pickupItemPrefab == null || displayPoints == null || displayPoints.Length == 0 || stockEntries == null || stockEntries.Length == 0)
            return;

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < stockEntries.Length; i++)
        {
            if (stockEntries[i] != null && stockEntries[i].IsValid())
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
            return;

        for (int i = 0; i < displayPoints.Length; i++)
        {
            Transform displayPoint = displayPoints[i];
            if (displayPoint == null || availableIndices.Count == 0)
                continue;

            int selectedIndex = SelectWeightedIndex(availableIndices);
            if (selectedIndex < 0)
                continue;

            MerchantStockEntry entry = stockEntries[selectedIndex];
            PickupItem spawnedItem = Instantiate(pickupItemPrefab, displayPoint.position, displayPoint.rotation);
            spawnedItem.ConfigureAsMerchantItem(entry.isPassive, entry.passiveItem, entry.activeItem, entry.price, merchant, floorZ);
            spawnedItems.Add(spawnedItem);

            if (!allowDuplicates)
                availableIndices.Remove(selectedIndex);
        }
    }

    private int SelectWeightedIndex(List<int> availableIndices)
    {
        float totalWeight = 0f;
        for (int i = 0; i < availableIndices.Count; i++)
            totalWeight += Mathf.Max(0.01f, stockEntries[availableIndices[i]].weight);

        if (totalWeight <= 0f)
            return -1;

        float roll = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        for (int i = 0; i < availableIndices.Count; i++)
        {
            int index = availableIndices[i];
            cumulativeWeight += Mathf.Max(0.01f, stockEntries[index].weight);

            if (roll <= cumulativeWeight)
                return index;
        }

        return availableIndices[availableIndices.Count - 1];
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
