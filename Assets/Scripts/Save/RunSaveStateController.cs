using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RunSaveStateController : MonoBehaviour
{
    public RunSaveData Capture(string sceneName)
    {
        RunSaveData data = RunSaveData.CreateNew(string.IsNullOrWhiteSpace(sceneName) ? SceneManager.GetActiveScene().name : sceneName);
        data.Player = CapturePlayer();
        data.Level = CaptureLevel();
        data.Enemies = CaptureEnemies();
        data.Pickups = CapturePickups();
        data.WorldObjects = CaptureWorldObjects();

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (registry != null)
            data.RunItemSnapshot = registry.CaptureSnapshot();

        return data;
    }

    public bool Restore(RunSaveData data)
    {
        if (data == null || !data.ActiveRun)
            return false;

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (registry != null)
            registry.RestoreSnapshot(data.RunItemSnapshot);

        RestoreLevel(data.Level);
        RestorePlayer(data.Player);
        RestoreEnemies(data.Enemies);
        RestorePickups(data.Pickups);
        RestoreWorldObjects(data.WorldObjects);

        return true;
    }

    private static PlayerRunSaveData CapturePlayer()
    {
        PlayerRunSaveData data = new PlayerRunSaveData();
        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
        if (playerMove == null)
            return data;

        data.hasData = true;
        data.position = playerMove.transform.position;
        data.floorZ = playerMove.floorZ;
        data.playerZ = playerMove.playerZ;

        if (playerMove.TryGetComponent(out PlayerStats stats))
        {
            data.currentHp = stats.GetCurrentHp();
            data.maxHp = stats.GetMaxHp();
            data.damage = stats.damage;
            data.attackSpeed = stats.attackSpeed;
            data.gold = stats.GetGold();
            data.potionCount = stats.GetPotionCount();
            data.maxPotionCount = stats.GetMaxPotionCount();
        }

        if (playerMove.TryGetComponent(out PassiveItemList passiveItemList))
            data.passiveItemIds = CapturePassiveItemIds(passiveItemList);

        if (playerMove.TryGetComponent(out ActiveInventory activeInventory))
            data.activeItemIds = CaptureActiveItemIds(activeInventory);

        return data;
    }

    private static void RestorePlayer(PlayerRunSaveData data)
    {
        if (data == null || !data.hasData)
            return;

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
        if (playerMove == null)
            return;

        playerMove.RestoreRunPosition(data.position, data.floorZ, data.playerZ);

        playerMove.TryGetComponent(out PlayerStats stats);

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        ItemUnlockCatalog catalog = registry != null ? registry.Catalog : null;

        if (playerMove.TryGetComponent(out PassiveItemList passiveItemList))
            passiveItemList.RestoreItems(ResolvePassiveItems(data.passiveItemIds, catalog), stats);

        if (playerMove.TryGetComponent(out ActiveInventory activeInventory))
            activeInventory.RestoreItems(ResolveActiveItems(data.activeItemIds, catalog));

        if (stats != null)
            stats.RestoreRunState(data.maxHp, data.currentHp, data.gold, data.potionCount, data.maxPotionCount, data.damage, data.attackSpeed);
    }

    private static LevelRunSaveData CaptureLevel()
    {
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
            return new LevelRunSaveData();

        return levelManager.CaptureRunState();
    }

    private static void RestoreLevel(LevelRunSaveData data)
    {
        if (data == null || !data.hasData)
            return;

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
            return;

        levelManager.RestoreRunState(data);
    }

    private static List<EnemyRunSaveData> CaptureEnemies()
    {
        List<EnemyRunSaveData> enemies = new List<EnemyRunSaveData>();
        EnemyStats[] enemyStats = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);
        for (int i = 0; i < enemyStats.Length; i++)
        {
            EnemyStats enemy = enemyStats[i];
            if (enemy == null)
                continue;

            RunSaveIdentity identity = enemy.GetComponent<RunSaveIdentity>();
            enemies.Add(new EnemyRunSaveData
            {
                saveId = identity != null ? identity.SaveId : enemy.gameObject.name,
                prefabId = identity != null ? identity.PrefabId : enemy.gameObject.name,
                position = enemy.transform.position,
                floorZ = ResolveEnemyFloorZ(enemy),
                currentHp = enemy.GetCurrentHp(),
                maxHp = enemy.maxHp,
                isDead = enemy.IsDead
            });
        }

        foreach (string deadEnemyId in RunSaveRuntimeState.DeadEnemyIds)
        {
            if (string.IsNullOrWhiteSpace(deadEnemyId) || ContainsEnemySaveId(enemies, deadEnemyId))
                continue;

            enemies.Add(new EnemyRunSaveData
            {
                saveId = deadEnemyId,
                isDead = true
            });
        }

        return enemies;
    }

    private static void RestoreEnemies(List<EnemyRunSaveData> savedEnemies)
    {
        if (savedEnemies == null)
            return;

        Dictionary<string, EnemyRunSaveData> savedById = new Dictionary<string, EnemyRunSaveData>();
        for (int i = 0; i < savedEnemies.Count; i++)
        {
            EnemyRunSaveData saved = savedEnemies[i];
            if (saved == null || string.IsNullOrWhiteSpace(saved.saveId) || savedById.ContainsKey(saved.saveId))
                continue;

            savedById.Add(saved.saveId, saved);
        }

        EnemyStats[] currentEnemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);
        for (int i = 0; i < currentEnemies.Length; i++)
        {
            EnemyStats enemy = currentEnemies[i];
            if (enemy == null)
                continue;

            RunSaveIdentity identity = enemy.GetComponent<RunSaveIdentity>();
            string saveId = identity != null ? identity.SaveId : enemy.gameObject.name;
            if (!savedById.TryGetValue(saveId, out EnemyRunSaveData saved))
                continue;

            if (saved.isDead)
            {
                Destroy(enemy.gameObject);
                continue;
            }

            enemy.transform.position = saved.position;
            enemy.RestoreRunState(saved.maxHp, saved.currentHp);
        }
    }

    private static List<PickupRunSaveData> CapturePickups()
    {
        List<PickupRunSaveData> pickups = new List<PickupRunSaveData>();
        PickupItem[] pickupItems = FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        for (int i = 0; i < pickupItems.Length; i++)
        {
            PickupItem pickup = pickupItems[i];
            if (pickup == null)
                continue;

            RunSaveIdentity identity = pickup.GetComponent<RunSaveIdentity>();
            pickups.Add(new PickupRunSaveData
            {
                saveId = identity != null ? identity.SaveId : pickup.gameObject.name,
                prefabId = identity != null ? identity.PrefabId : pickup.gameObject.name,
                isDroppedItem = false,
                isPassive = pickup.isPassive,
                isGold = false,
                itemId = pickup.isPassive ? (pickup.item?.ItemId ?? string.Empty) : (pickup.activeItem?.ItemId ?? string.Empty),
                position = pickup.transform.position,
                floorZ = pickup.floorZ,
                requiresPurchase = pickup.requiresPurchase,
                price = pickup.price,
                consumed = false
            });
        }

        DroppedItem[] droppedItems = FindObjectsByType<DroppedItem>(FindObjectsSortMode.None);
        for (int i = 0; i < droppedItems.Length; i++)
        {
            DroppedItem dropped = droppedItems[i];
            if (dropped == null)
                continue;

            RunSaveIdentity identity = dropped.GetComponent<RunSaveIdentity>();
            pickups.Add(new PickupRunSaveData
            {
                saveId = identity != null ? identity.SaveId : dropped.gameObject.name,
                prefabId = identity != null ? identity.PrefabId : dropped.gameObject.name,
                isDroppedItem = true,
                isPassive = false,
                isGold = dropped.isGold,
                goldAmount = dropped.gold,
                position = dropped.transform.position,
                floorZ = dropped.floorZ,
                consumed = false
            });
        }

        foreach (string consumedPickupId in RunSaveRuntimeState.ConsumedPickupIds)
        {
            if (string.IsNullOrWhiteSpace(consumedPickupId) || ContainsPickupSaveId(pickups, consumedPickupId))
                continue;

            pickups.Add(new PickupRunSaveData
            {
                saveId = consumedPickupId,
                consumed = true
            });
        }

        return pickups;
    }

    private static void RestorePickups(List<PickupRunSaveData> savedPickups)
    {
        if (savedPickups == null)
            return;

        Dictionary<string, PickupRunSaveData> savedById = new Dictionary<string, PickupRunSaveData>();
        for (int i = 0; i < savedPickups.Count; i++)
        {
            PickupRunSaveData saved = savedPickups[i];
            if (saved == null || string.IsNullOrWhiteSpace(saved.saveId) || savedById.ContainsKey(saved.saveId))
                continue;

            savedById.Add(saved.saveId, saved);
        }

        HashSet<string> restoredIds = new HashSet<string>();
        RestoreExistingPickupItems(savedById, restoredIds);
        RestoreExistingDroppedItems(savedById, restoredIds);
        CreateMissingPickups(savedPickups, restoredIds);
    }

    private static List<WorldObjectRunSaveData> CaptureWorldObjects()
    {
        List<WorldObjectRunSaveData> worldObjects = new List<WorldObjectRunSaveData>();
        RunSaveWorldObject[] objects = FindObjectsByType<RunSaveWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < objects.Length; i++)
        {
            RunSaveWorldObject worldObject = objects[i];
            if (worldObject == null)
                continue;

            worldObjects.Add(worldObject.Capture());
        }

        return worldObjects;
    }

    private static void RestoreWorldObjects(List<WorldObjectRunSaveData> savedObjects)
    {
        if (savedObjects == null)
            return;

        Dictionary<string, WorldObjectRunSaveData> savedById = new Dictionary<string, WorldObjectRunSaveData>();
        for (int i = 0; i < savedObjects.Count; i++)
        {
            WorldObjectRunSaveData saved = savedObjects[i];
            if (saved == null || string.IsNullOrWhiteSpace(saved.saveId) || savedById.ContainsKey(saved.saveId))
                continue;

            savedById.Add(saved.saveId, saved);
        }

        RunSaveWorldObject[] objects = FindObjectsByType<RunSaveWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < objects.Length; i++)
        {
            RunSaveWorldObject worldObject = objects[i];
            if (worldObject == null)
                continue;

            string saveId = worldObject.SaveId;
            if (savedById.TryGetValue(saveId, out WorldObjectRunSaveData saved))
                worldObject.Restore(saved);
        }
    }

    private static int ResolveEnemyFloorZ(EnemyStats enemy)
    {
        IEnemyMovement movement = enemy != null ? enemy.GetComponent<IEnemyMovement>() : null;
        if (movement != null)
            return movement.FloorZ;

        return 0;
    }

    private static bool ContainsEnemySaveId(List<EnemyRunSaveData> enemies, string saveId)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].saveId == saveId)
                return true;
        }

        return false;
    }

    private static bool ContainsPickupSaveId(List<PickupRunSaveData> pickups, string saveId)
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            if (pickups[i] != null && pickups[i].saveId == saveId)
                return true;
        }

        return false;
    }

    private static void RestoreExistingPickupItems(Dictionary<string, PickupRunSaveData> savedById, HashSet<string> restoredIds)
    {
        PickupItem[] currentPickups = FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        ItemUnlockCatalog catalog = ResolveCatalog();

        for (int i = 0; i < currentPickups.Length; i++)
        {
            PickupItem pickup = currentPickups[i];
            if (pickup == null)
                continue;

            RunSaveIdentity identity = pickup.GetComponent<RunSaveIdentity>();
            string saveId = identity != null ? identity.SaveId : pickup.gameObject.name;
            if (!savedById.TryGetValue(saveId, out PickupRunSaveData saved) || saved.consumed || saved.isDroppedItem)
            {
                Object.Destroy(pickup.gameObject);
                continue;
            }

            RestorePickupItem(pickup, saved, catalog, true);
            restoredIds.Add(saveId);
        }
    }

    private static void RestoreExistingDroppedItems(Dictionary<string, PickupRunSaveData> savedById, HashSet<string> restoredIds)
    {
        DroppedItem[] currentDrops = FindObjectsByType<DroppedItem>(FindObjectsSortMode.None);
        for (int i = 0; i < currentDrops.Length; i++)
        {
            DroppedItem drop = currentDrops[i];
            if (drop == null)
                continue;

            RunSaveIdentity identity = drop.GetComponent<RunSaveIdentity>();
            string saveId = identity != null ? identity.SaveId : drop.gameObject.name;
            if (!savedById.TryGetValue(saveId, out PickupRunSaveData saved) || saved.consumed || !saved.isDroppedItem)
            {
                Object.Destroy(drop.gameObject);
                continue;
            }

            drop.transform.position = saved.position;
            drop.RestoreRunState(saved.isGold, saved.goldAmount, saved.floorZ);
            restoredIds.Add(saveId);
        }
    }

    private static void CreateMissingPickups(List<PickupRunSaveData> savedPickups, HashSet<string> restoredIds)
    {
        ItemUnlockCatalog catalog = ResolveCatalog();
        for (int i = 0; i < savedPickups.Count; i++)
        {
            PickupRunSaveData saved = savedPickups[i];
            if (saved == null || saved.consumed || string.IsNullOrWhiteSpace(saved.saveId) || restoredIds.Contains(saved.saveId))
                continue;

            if (saved.isDroppedItem)
            {
                CreateMissingDroppedItem(saved);
                continue;
            }

            CreateMissingPickupItem(saved, catalog);
        }
    }

    private static void CreateMissingPickupItem(PickupRunSaveData saved, ItemUnlockCatalog catalog)
    {
        GameObject template = FindPickupItemTemplate();
        if (template == null)
            return;

        GameObject created = Object.Instantiate(template, saved.position, Quaternion.identity);
        RunSaveIdentity.Ensure(created, saved.saveId, string.IsNullOrWhiteSpace(saved.prefabId) ? template.name : saved.prefabId);

        PickupItem pickup = created.GetComponent<PickupItem>();
        if (pickup == null)
        {
            Object.Destroy(created);
            return;
        }

        RestorePickupItem(pickup, saved, catalog, true);
    }

    private static void CreateMissingDroppedItem(PickupRunSaveData saved)
    {
        GameObject template = FindDroppedItemTemplate();
        if (template == null)
            return;

        GameObject created = Object.Instantiate(template, saved.position, Quaternion.identity);
        RunSaveIdentity.Ensure(created, saved.saveId, string.IsNullOrWhiteSpace(saved.prefabId) ? template.name : saved.prefabId);
        DroppedItem drop = created.GetComponent<DroppedItem>();
        if (drop == null)
        {
            Object.Destroy(created);
            return;
        }

        drop.RestoreRunState(saved.isGold, saved.goldAmount, saved.floorZ);
    }

    private static void RestorePickupItem(PickupItem pickup, PickupRunSaveData saved, ItemUnlockCatalog catalog, bool preserveSavedAvailability)
    {
        PassiveItem passiveItem = null;
        ActiveItem activeItem = null;
        if (saved.isPassive)
            catalog?.TryGetPassiveItem(saved.itemId, out passiveItem);
        else
            catalog?.TryGetActiveItem(saved.itemId, out activeItem);

        pickup.transform.position = saved.position;
        pickup.RestoreRunState(saved.isPassive, passiveItem, activeItem, saved.floorZ, saved.requiresPurchase, saved.price, preserveSavedAvailability);
    }

    private static ItemUnlockCatalog ResolveCatalog()
    {
        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        return registry != null ? registry.Catalog : null;
    }

    private static GameObject FindPickupItemTemplate()
    {
        PickupChest[] chests = FindObjectsByType<PickupChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < chests.Length; i++)
        {
            if (chests[i] != null && chests[i].pickupItemPrefab != null)
                return chests[i].pickupItemPrefab;
        }

        MerchantStockSpawner[] merchants = FindObjectsByType<MerchantStockSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < merchants.Length; i++)
        {
            if (merchants[i] != null && merchants[i].pickupItemPrefab != null)
                return merchants[i].pickupItemPrefab.gameObject;
        }

        PickupItem existing = FindFirstObjectByType<PickupItem>();
        return existing != null ? existing.gameObject : null;
    }

    private static GameObject FindDroppedItemTemplate()
    {
        BreakableProbs[] breakables = FindObjectsByType<BreakableProbs>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < breakables.Length; i++)
        {
            if (breakables[i] != null && breakables[i].dropItem != null && breakables[i].dropItem.GetComponent<DroppedItem>() != null)
                return breakables[i].dropItem;
        }

        ItemDrop[] enemyDrops = FindObjectsByType<ItemDrop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < enemyDrops.Length; i++)
        {
            if (enemyDrops[i] != null && enemyDrops[i].item != null && enemyDrops[i].item.GetComponent<DroppedItem>() != null)
                return enemyDrops[i].item;
        }

        ActiveInventory inventory = FindFirstObjectByType<ActiveInventory>();
        if (inventory != null && inventory.dropItem != null && inventory.dropItem.GetComponent<DroppedItem>() != null)
            return inventory.dropItem;

        DroppedItem existing = FindFirstObjectByType<DroppedItem>();
        return existing != null ? existing.gameObject : null;
    }

    private static List<string> CapturePassiveItemIds(PassiveItemList passiveItemList)
    {
        List<string> ids = new List<string>();
        List<PassiveItem> items = passiveItemList.GetPassvieItems();
        for (int i = 0; i < items.Count; i++)
        {
            string itemId = items[i]?.ItemId;
            if (!string.IsNullOrWhiteSpace(itemId) && !ids.Contains(itemId))
                ids.Add(itemId);
        }

        return ids;
    }

    private static List<string> CaptureActiveItemIds(ActiveInventory activeInventory)
    {
        List<string> ids = new List<string>();
        List<ActiveItem> items = activeInventory.GetItems();
        for (int i = 0; i < items.Count; i++)
        {
            string itemId = items[i]?.ItemId;
            if (!string.IsNullOrWhiteSpace(itemId))
                ids.Add(itemId);
        }

        return ids;
    }

    private static List<PassiveItem> ResolvePassiveItems(List<string> itemIds, ItemUnlockCatalog catalog)
    {
        List<PassiveItem> items = new List<PassiveItem>();
        if (itemIds == null || catalog == null)
            return items;

        for (int i = 0; i < itemIds.Count; i++)
        {
            if (catalog.TryGetPassiveItem(itemIds[i], out PassiveItem item) && item != null)
                items.Add(item);
        }

        return items;
    }

    private static List<ActiveItem> ResolveActiveItems(List<string> itemIds, ItemUnlockCatalog catalog)
    {
        List<ActiveItem> items = new List<ActiveItem>();
        if (itemIds == null || catalog == null)
            return items;

        for (int i = 0; i < itemIds.Count; i++)
        {
            if (catalog.TryGetActiveItem(itemIds[i], out ActiveItem item) && item != null)
                items.Add(item);
        }

        return items;
    }
}
