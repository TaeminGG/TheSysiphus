using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class ChunkSpawnMarkerBinding
{
    public TileBase markerTile;
    public GameObject prefab;
    public Vector3 positionOffset = Vector3.zero;
}

public class ChunkLoader : MonoBehaviour
{
    private Grid grid;

    [Header("Target Tilemaps")]
    public Tilemap tmap;
    public Tilemap bmap;
    public Tilemap eventMap;

    [Header("Chunk Tilemaps")]
    public GameObject[] tileChunkPrefabs;
    public GameObject[] boxChunkPrefabs;
    public GameObject[] spawnChunkPrefabs;
    public GameObject[] eventChunkPrefabs;

    [Header("Chunk Spawns")]
    public Transform spawnedChunkObjectsRoot;
    public List<ChunkSpawnMarkerBinding> spawnMarkerBindings = new List<ChunkSpawnMarkerBinding>();
    public bool logMissingSpawnBindings = true;

    private void Start()
    {
        GridUpdate();
    }

    public void AddChunk(int index, int height, int currentChunkCount, bool isBox)
    {
        Vector3Int offset = new Vector3Int(-(height * currentChunkCount), -(height * currentChunkCount), 0);

        if (isBox)
        {
            UpdateChunk(GetChunkPrefab(boxChunkPrefabs, index), offset, bmap);
            return;
        }

        UpdateChunk(GetChunkPrefab(tileChunkPrefabs, index), offset, tmap);
        UpdateChunk(GetChunkPrefab(eventChunkPrefabs, index), offset, eventMap);
        SpawnChunkObjects(index, offset, currentChunkCount);
    }

    private void UpdateChunk(GameObject chunkPrefab, Vector3Int offset, Tilemap targetTilemap)
    {
        Tilemap chunkTilemap = GetChunkTilemap(chunkPrefab);
        if (chunkTilemap == null || targetTilemap == null)
            return;

        BoundsInt bounds = chunkTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = chunkTilemap.GetTile(pos);
            if (tile == null)
                continue;

            Vector3Int targetPos = pos + offset;
            targetTilemap.SetTile(targetPos, tile);
        }
    }

    private void SpawnChunkObjects(int index, Vector3Int offset, int currentChunkCount)
    {
        GameObject spawnChunkPrefab = GetChunkPrefab(spawnChunkPrefabs, index);
        if (spawnChunkPrefab == null)
            return;

        Tilemap spawnTilemap = GetChunkTilemap(spawnChunkPrefab);
        if (spawnTilemap == null)
            return;

        Transform chunkRoot = GetOrCreateChunkSpawnRoot(currentChunkCount);
        BoundsInt bounds = spawnTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase markerTile = spawnTilemap.GetTile(pos);
            if (markerTile == null)
                continue;

            ChunkSpawnMarkerBinding binding = FindSpawnBinding(markerTile);
            if (binding == null || binding.prefab == null)
            {
                if (logMissingSpawnBindings)
                {
                    Debug.LogWarning($"ChunkLoader: no spawn binding found for marker tile '{markerTile.name}'.", this);
                }
                continue;
            }

            Vector3Int targetCell = pos + offset;
            Vector3 worldPosition = tmap.GetCellCenterWorld(targetCell) + binding.positionOffset;
            GameObject spawnedObject = Instantiate(binding.prefab, worldPosition, Quaternion.identity, chunkRoot);

            ApplySpawnFloorZ(spawnedObject, GetFloorZAtCell(targetCell));
        }
    }

    private GameObject GetChunkPrefab(GameObject[] prefabs, int index)
    {
        if (prefabs == null || index < 0 || index >= prefabs.Length)
            return null;

        return prefabs[index];
    }

    private Tilemap GetChunkTilemap(GameObject chunkPrefab)
    {
        if (chunkPrefab == null)
            return null;

        Tilemap tilemap = chunkPrefab.GetComponent<Tilemap>();
        if (tilemap != null)
            return tilemap;

        return chunkPrefab.GetComponentInChildren<Tilemap>();
    }

    private ChunkSpawnMarkerBinding FindSpawnBinding(TileBase markerTile)
    {
        for (int i = 0; i < spawnMarkerBindings.Count; i++)
        {
            ChunkSpawnMarkerBinding binding = spawnMarkerBindings[i];
            if (binding != null && binding.markerTile == markerTile)
                return binding;
        }

        return null;
    }

    private Transform GetOrCreateChunkSpawnRoot(int currentChunkCount)
    {
        Transform parent = spawnedChunkObjectsRoot != null ? spawnedChunkObjectsRoot : transform;
        string rootName = $"Chunk_{currentChunkCount}_Spawns";
        Transform existingRoot = parent.Find(rootName);

        if (existingRoot != null)
            return existingRoot;

        GameObject rootObject = new GameObject(rootName);
        rootObject.transform.SetParent(parent);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private int GetFloorZAtCell(Vector3Int targetCell)
    {
        if (bmap == null)
            return 0;

        for (int i = 4; i >= 1; i--)
        {
            Vector3Int cellPos = targetCell + new Vector3Int(-i, -i, 0);
            int z = i * 2 - 2;
            cellPos.z = z;

            if (bmap.HasTile(cellPos))
                return z + 2;
        }

        return 0;
    }

    private void ApplySpawnFloorZ(GameObject spawnedObject, int floorZ)
    {
        if (spawnedObject.TryGetComponent(out UndeadMovement undeadMovement))
            undeadMovement.floorZ = floorZ;

        if (spawnedObject.TryGetComponent(out SpecterMovement specterMovement))
            specterMovement.floorZ = floorZ;

        if (spawnedObject.TryGetComponent(out GorgonMovement gorgonMovement))
            gorgonMovement.floorZ = floorZ;

        if (spawnedObject.TryGetComponent(out GroundedEnemy groundedEnemy))
            groundedEnemy.floorZ = floorZ;

        if (spawnedObject.TryGetComponent(out BreakableProbs breakableProbs))
            breakableProbs.floorZ = floorZ;

        if (spawnedObject.TryGetComponent(out PickupChest pickupChest))
            pickupChest.SetZ(floorZ);

        if (spawnedObject.TryGetComponent(out DroppedItem droppedItem))
            droppedItem.SetZ(floorZ);

        if (spawnedObject.TryGetComponent(out PickupItem pickupItem))
            pickupItem.SetZ(floorZ);

        if (spawnedObject.TryGetComponent(out FountainOfHealth fountain))
            fountain.SetZ(floorZ);
    }

    private void GridUpdate()
    {
        if (grid != null)
            return;

        grid = FindFirstObjectByType<Grid>();
        if (grid == null)
            return;

        Tilemap[] maps = grid.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap map in maps)
        {
            string mapName = map.gameObject.name.ToLowerInvariant();

            if (mapName == "tmap")
                tmap = map;

            if (mapName == "bmap")
                bmap = map;

            if (mapName == "eventmap")
                eventMap = map;
        }
    }
}
