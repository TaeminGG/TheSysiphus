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

        RestorePlayer(data.Player);
        RestoreEnemies(data.Enemies);
        RestorePickups(data.Pickups);
        RestoreWorldObjects(data.WorldObjects);

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (registry != null)
            registry.RestoreSnapshot(data.RunItemSnapshot);

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
                isPassive = pickup.isPassive,
                itemId = pickup.isPassive ? (pickup.item?.ItemId ?? string.Empty) : (pickup.activeItem?.ItemId ?? string.Empty),
                position = pickup.transform.position,
                floorZ = pickup.floorZ,
                requiresPurchase = pickup.requiresPurchase,
                price = pickup.price
            });
        }

        DroppedItem[] droppedItems = FindObjectsByType<DroppedItem>(FindObjectsSortMode.None);
        for (int i = 0; i < droppedItems.Length; i++)
        {
            DroppedItem dropped = droppedItems[i];
            if (dropped == null || !dropped.isGold)
                continue;

            RunSaveIdentity identity = dropped.GetComponent<RunSaveIdentity>();
            pickups.Add(new PickupRunSaveData
            {
                saveId = identity != null ? identity.SaveId : dropped.gameObject.name,
                isPassive = false,
                goldAmount = dropped.gold,
                position = dropped.transform.position,
                floorZ = dropped.floorZ
            });
        }

        return pickups;
    }

    private static void RestorePickups(List<PickupRunSaveData> savedPickups)
    {
        // Runtime pickup respawn needs prefab registries. The saved data is captured now so
        // item, gold, and shop pickup restoration can be added without changing save format.
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
