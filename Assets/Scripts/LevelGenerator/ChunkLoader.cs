using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkLoader : MonoBehaviour
{
    private Grid grid;
    public Tilemap tmap;
    public Tilemap bmap;
    public GameObject[] tileChunkPrefabs;         // 로드 할 바닥 청크
    public GameObject[] boxChunkPrefabs;          // 로드 할 박스 청크

    private void Start()
    {
        GridUpdate();
    }

    public void UpdateChunk(GameObject chunk, Vector3Int offset, bool isBox)    // isBox 가 true면 bmap에 복사, 아니면 tmap에 복사
    {
        Tilemap chunkTilemap = chunk.GetComponent<Tilemap>();

        // 청크 오브젝트 타일맵 모든 타일에 대해 기존 타일맵에 복사
        BoundsInt bounds = chunkTilemap.cellBounds;

        foreach(Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = chunkTilemap.GetTile(pos);
            if (tile == null)
                continue;   // 없는 타일 제외

            // Offset 만큼 이동해서 기존 타일맵에 복사
            Vector3Int targetPos = pos + offset;
            if (isBox)
            {
                bmap.SetTile(targetPos, tile);
            }
            else
            {
                tmap.SetTile(targetPos, tile);
            }
            
        }
    }

    public void AddChunk(int index, int height, int currentChunkCount, bool isBox)
    {
        Vector3Int offset = new Vector3Int(-(height * currentChunkCount), -(height * currentChunkCount), 0);
        if (isBox)
            UpdateChunk(boxChunkPrefabs[index], offset, isBox);
        else
            UpdateChunk(tileChunkPrefabs[index], offset, isBox);
    }
    private void GridUpdate()
    {
        if (grid != null)
            return;
        grid = FindFirstObjectByType<Grid>();
        Tilemap[] maps = grid.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap map in maps)
        {
            if (map.gameObject.name == "tmap")
            {
                tmap = map;
            }
            if (map.gameObject.name == "bmap")
            {
                bmap = map;
            }
        }
    }
}
