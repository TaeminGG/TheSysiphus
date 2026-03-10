using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class SurroundTileCheck : MonoBehaviour
{
    public Grid grid;
    public Tilemap tmap;
    public Vector3Int currentPos;
    public Vector3Int targetPos;

    public Transform pivot;
    private Vector2Int axis;
    private PlayerMove pMove;


    private void Awake()
    {
        pMove = GetComponent<PlayerMove>();
    }
    void Start()
    {
        GridUpdate();
        
    }


    Vector3Int GetTileZ()
    {
        Vector3Int cellIndex = tmap.WorldToCell(transform.position - Vector3.forward);
        for (int i = tmap.cellBounds.zMax; i > tmap.cellBounds.zMin - 1; i--)
        {
            var tile = tmap.GetTile(cellIndex);
            if (tile != null)
            {

                return cellIndex;
            }


        }
        return cellIndex;
    }

    public int GetTopTileZ(Vector3 pos)
    {

        for (int z = 10; z >= 0; z -= 2)
        {
            Vector3 offsetVector = grid.CellToWorld(new Vector3Int(0,0,z));
            float offset = offsetVector.y + 0.5f;
            Vector3 searchPos = new Vector3(pos.x, pos.y - offset, 0);

            Vector3Int cellPos = tmap.WorldToCell(searchPos);
            cellPos.z = z;

            if (tmap.HasTile(cellPos))
            {
                return z;
            }
        }
        return -1;
    }
    void Update()
    {

    }

    private void CheckSurroundTile()
    {
        GridUpdate();

        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                bool valid = false;
            }
        }
    }

    private void GridUpdate()
    {
        if (grid != null)
            return;
        grid = FindFirstObjectByType<Grid>();
        Tilemap[] maps = grid.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap map in maps)
        {
            if (map.gameObject.name == "Basic")
            {
                tmap = map;
            }
        }
    }
}
