using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FloatingEnemy : MonoBehaviour
{
    public Tilemap tmap;
    public Tilemap bmap;
    public Grid grid;

    private Transform player;

    // 다음 이동할 셀을 찾는 함수
    public Vector3Int GetNextCell()
    {
        Vector3Int currentCell = grid.WorldToCell(transform.position);
        currentCell.z = 0;

        Vector3Int nextCell = currentCell;
        float distance = float.MaxValue;

        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                if (x == 0 && y == 0) continue;  // 제자리는 무시

                Vector3Int offset = new Vector3Int(x, y, 0);

                Vector3Int targetCell = currentCell + offset;
                Vector3 targetWorldPos = grid.GetCellCenterWorld(targetCell);

                float dist = Vector3.Distance(targetWorldPos, player.position);

                if (dist < distance)
                {
                    distance = dist;
                    nextCell = targetCell;
                }

            }
        }
        nextCell.z = 0;
        return nextCell;
    }

    private void Start()
    {
        player = GameObject.Find("Player").transform;
        GridUpdate();
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
