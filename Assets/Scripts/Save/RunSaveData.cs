using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunSaveData
{
    [SerializeField] private bool activeRun;
    [SerializeField] private string runId = string.Empty;
    [SerializeField] private string sceneName = string.Empty;
    [SerializeField] private string startedAtUtc = string.Empty;
    [SerializeField] private string savedAtUtc = string.Empty;
    [SerializeField] private PlayerRunSaveData player = new PlayerRunSaveData();
    [SerializeField] private List<EnemyRunSaveData> enemies = new List<EnemyRunSaveData>();
    [SerializeField] private List<PickupRunSaveData> pickups = new List<PickupRunSaveData>();
    [SerializeField] private List<WorldObjectRunSaveData> worldObjects = new List<WorldObjectRunSaveData>();
    [SerializeField] private ItemUnlockSnapshot runItemSnapshot = new ItemUnlockSnapshot();

    public bool ActiveRun
    {
        get => activeRun;
        set => activeRun = value;
    }

    public string RunId
    {
        get => runId;
        set => runId = value ?? string.Empty;
    }

    public string SceneName
    {
        get => sceneName;
        set => sceneName = value ?? string.Empty;
    }

    public string StartedAtUtc
    {
        get => startedAtUtc;
        set => startedAtUtc = value ?? string.Empty;
    }

    public string SavedAtUtc
    {
        get => savedAtUtc;
        set => savedAtUtc = value ?? string.Empty;
    }

    public PlayerRunSaveData Player
    {
        get
        {
            player ??= new PlayerRunSaveData();
            return player;
        }
        set => player = value ?? new PlayerRunSaveData();
    }

    public List<EnemyRunSaveData> Enemies
    {
        get
        {
            enemies ??= new List<EnemyRunSaveData>();
            return enemies;
        }
        set => enemies = value ?? new List<EnemyRunSaveData>();
    }

    public List<PickupRunSaveData> Pickups
    {
        get
        {
            pickups ??= new List<PickupRunSaveData>();
            return pickups;
        }
        set => pickups = value ?? new List<PickupRunSaveData>();
    }

    public List<WorldObjectRunSaveData> WorldObjects
    {
        get
        {
            worldObjects ??= new List<WorldObjectRunSaveData>();
            return worldObjects;
        }
        set => worldObjects = value ?? new List<WorldObjectRunSaveData>();
    }

    public ItemUnlockSnapshot RunItemSnapshot
    {
        get
        {
            runItemSnapshot ??= new ItemUnlockSnapshot();
            return runItemSnapshot;
        }
        set => runItemSnapshot = value ?? new ItemUnlockSnapshot();
    }

    public static RunSaveData CreateNew(string sceneName)
    {
        string now = DateTime.UtcNow.ToString("O");
        return new RunSaveData
        {
            ActiveRun = true,
            RunId = Guid.NewGuid().ToString("N"),
            SceneName = sceneName,
            StartedAtUtc = now,
            SavedAtUtc = now
        };
    }

    public void MarkSavedNow()
    {
        activeRun = true;
        savedAtUtc = DateTime.UtcNow.ToString("O");
    }
}

[Serializable]
public class PlayerRunSaveData
{
    public bool hasData;
    public Vector3 position;
    public int floorZ;
    public float playerZ;
    public float currentHp;
    public float maxHp;
    public float damage;
    public float attackSpeed;
    public int gold;
    public int potionCount;
    public int maxPotionCount;
    public List<string> passiveItemIds = new List<string>();
    public List<string> activeItemIds = new List<string>();
}

[Serializable]
public class EnemyRunSaveData
{
    public string saveId = string.Empty;
    public string prefabId = string.Empty;
    public Vector3 position;
    public int floorZ;
    public float currentHp;
    public float maxHp;
    public bool isDead;
}

[Serializable]
public class PickupRunSaveData
{
    public string saveId = string.Empty;
    public bool isPassive;
    public string itemId = string.Empty;
    public int goldAmount;
    public Vector3 position;
    public int floorZ;
    public bool requiresPurchase;
    public int price;
}

[Serializable]
public class WorldObjectRunSaveData
{
    public string saveId = string.Empty;
    public string stateKey = string.Empty;
    public Vector3 position;
    public bool consumed;
    public string payloadJson = string.Empty;
}
