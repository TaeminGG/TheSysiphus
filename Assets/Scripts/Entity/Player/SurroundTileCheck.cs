using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SurroundTileCheck : MonoBehaviour
{
    public Grid grid;
    public Tilemap tmap;
    public Vector3Int currentPos;
    public Vector3Int targetPos;

    public Transform pivot;

    private Vector2 mousePos = Vector2.zero;

    void Start()
    {
        GridUpdate();
        
    }

    public void SurroundTiles(Vector3 pos)
    {
        for (int i = 0; i < 8; i++)
        {

        }
    }

    public int GetMovementTileZAt(Vector3 pos, float pz)
    {
        // Z(높이) 오프셋을 제거한 평면 좌표
        Vector3 logicalPos = new Vector3(pos.x, pos.y, 0);
        Vector3Int cellPos = tmap.WorldToCell(logicalPos);

        int maxZ = 0;
        bool tileFound = false;

        cellPos.x -= 1;
        cellPos.y -= 1;

        for (int z = 8; z >= 0; z -= 2)
        {
            cellPos.z = z;
            if (tmap.HasTile(cellPos))
            {
                maxZ = z;
                tileFound = true;
                break;
            }

        }

        return tileFound ? maxZ : -1;
    }

    public int GetJumpTileZAt(Vector3 pos, float pz)
    {
        // Z(높이) 오프셋을 제거한 평면 좌표
        Vector3 logicalPos = new Vector3(pos.x, pos.y, 0);
        Vector3Int cellPos = tmap.WorldToCell(logicalPos);

        // 해당 셀의 모든 Z층 검사 후 가장 높은 Z값의 타일 반환
        int maxZ = 0;
        bool tileFound = false;

        // 해당 셀의 3칸 아래의 Z값이 4, 2칸 아래 Z값이 2, 해당 셀의 Z값이 0 인 경우
        for (int i = 2; i >= 0; i--)
        {
            Vector3Int checkPos = Vector3Int.zero;
            Debug.Log("Cell Pos : " + cellPos + "   under2 : " + new Vector2Int(cellPos.x-2, cellPos.y-2));

            if (i == 2)
            {
                checkPos.x = cellPos.x - 2;
                checkPos.y = cellPos.y - 2;
                checkPos.z = 4;
                
                if (tmap.HasTile(checkPos))
                {
                    maxZ = 4;
                    tileFound = true;
                    break;
                }
            }

            if (i == 1)
            {
                checkPos.x = cellPos.x - 1;
                checkPos.y = cellPos.y - 1;
                checkPos.z = 2;
                if (tmap.HasTile(checkPos))
                {
                    maxZ = 2;
                    tileFound = true;
                    break;
                }
            }

            if (i == 0)
            {
                if(tmap.HasTile(cellPos))
                    return 0;
            }
        }

        return tileFound ? maxZ : -1;
    }

    // 공중에 있을 때 실행하여 점프 후 바닥을 판정 플레이어가 있는 위치 바닥의 Z값을 기준으로 계산
    public int GetMaxTileZAt(Vector3 pos, float pz)
    {
        // Z(높이) 오프셋을 제거한 평면 좌표
        Vector3 logicalPos = new Vector3(pos.x, pos.y, 0);
        Vector3Int cellPos = tmap.WorldToCell(logicalPos);

        // 해당 셀의 모든 Z층 검사 후 가장 높은 Z값의 타일 반환
        int maxZ = 0;
        bool tileFound = false;
        
        for(int z = 8; z >= 0; z -= 2)
        {
            cellPos.z = z;
            if (tmap.HasTile(cellPos))
            {
                maxZ = z;
                tileFound = true;
                break;
            }
        }

        return tileFound ? maxZ : -1;

    }

    void Update()
    {
        //Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        //Debug.Log("Cursor : " + tmap.WorldToCell(cam.ScreenToWorldPoint(mousePos)));
    }

    void OnCursor(InputValue value)
    {
        mousePos = value.Get<Vector2>();
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

    public Vector3Int GetGridPosition(Vector3 pos)
    {
        return tmap.WorldToCell(pos);
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
