using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class GroundedEnemy : MonoBehaviour
{
    public Tilemap tmap;
    public Tilemap bmap;
    public Grid grid;

    public int floorZ = 0;

    private Transform player;
    private IEnemyMovement movement;


    private Vector3Int prevPos;
    public class DirectionInfo
    {
        public bool isValid;        // 해당 위치에 움직여서 갈 수 있는지 여부 (벽인지 아닌지)
        public int zLevel;          // 해당 위치의 Z값

        public DirectionInfo(bool valid, int z)
        {
            isValid = valid;
            zLevel = z;
        }
    }

    //주변 9칸 (제자리 포함)의 Direction Infomation
    private Dictionary<Vector3Int, DirectionInfo> surroundTiles = new Dictionary<Vector3Int, DirectionInfo>();

    public void UpdateSurroundTiles(Vector3 pos)
    {

        if (grid.WorldToCell(pos) != prevPos)
        {
            surroundTiles.Clear();
        }

        // floorZ(활동 Zlevl)가 0일 떈 bmap에 타일이 존재(높이 무관)하면 unvalid

        if (floorZ == 0)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    bool valid = true;

                    // z : 4 2 0
                    for (int z = 6; z >= 0; z -= 2)
                    {
                        Vector3Int cellPos = grid.WorldToCell(pos);

                        cellPos.x += x;
                        cellPos.y += y;
                        cellPos.z = z;

                        if (bmap.HasTile(cellPos))
                        {
                            valid = false;
                        }

                        if (surroundTiles.ContainsKey(new Vector3Int(x, y, 0)))       // 주변 x, y 에 등록된 칸이 있는지
                        {
                            if (surroundTiles[new Vector3Int(x, y, 0)].isValid)     // 등록된 칸이 Valid 인지 확인하고 갱신 혹은 등록
                            {
                                surroundTiles[new Vector3Int(x, y, 0)] = new DirectionInfo(valid, z);
                                if (!valid)
                                    break;
                            }
                            else
                            {
                                surroundTiles[new Vector3Int(x, y, 0)] = new DirectionInfo(valid, z);
                                if (!valid)
                                    break;
                            }
                        }
                        else
                        {
                            surroundTiles.Add(new Vector3Int(x, y, 0), new DirectionInfo(valid, z));
                        }
                    }
                }
            }
        }

        prevPos = grid.WorldToCell(pos);
    }

    public bool GetTileValid(Vector3 pos)
    {
        Vector3Int cellPos = grid.WorldToCell(pos);
        Vector3Int intPos = grid.WorldToCell(transform.position);
        Vector3Int testPos = grid.WorldToCell(cellPos - intPos);
        testPos.x = Mathf.Clamp(testPos.x, -1, 1);
        testPos.y = Mathf.Clamp(testPos.y, -1, 1);
        testPos.z = 0;

        return surroundTiles[testPos].isValid;
    }

    // Vector3Int Pos위치에 설치된 타일(박스 윗면)의 높이(Z)를 얻는 함수
    public int GetGroundZLevel(Vector3Int pos)
    {

        for(int z=6; z>=0; z -= 2)
        {
            int depth = floorZ / 2;
            Vector3Int cellPos = pos + new Vector3Int(-depth, -depth, 0);

            cellPos.z = z;

            if (bmap.HasTile(cellPos))
            {
                return z;
            }
        }

        // bmap에서 확인된 타일이 없으면 평지로 인식
        return -1;
    }

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
                if(x==0 && y==0) continue;  // 제자리는 무시
                
                Vector3Int offset = new Vector3Int(x, y, 0);

                // 높이(Z)가 0일 떄와 아닐 때 나누어 처리
                if(floorZ == 0)
                {
                    // surroundTile에 존재하고 이동 가능한 셀만 처리
                    if (!surroundTiles.ContainsKey(offset)) continue;
                    if (!surroundTiles[offset].isValid) continue;

                    Vector3Int targetCell = currentCell + offset;
                    Vector3 targetWorldPos = grid.GetCellCenterWorld(targetCell);

                    float dist = Vector3.Distance(targetWorldPos, player.position);

                    if (dist < distance)
                    {
                        distance = dist;
                        nextCell = targetCell;
                    }
                }
                else
                {
                    Vector3Int targetCell = currentCell + offset;
                    Vector3 targetWorldPos = grid.GetCellCenterWorld(targetCell);

                    int getZ = GetGroundZLevel(targetCell) + 2;

                    if (getZ != floorZ)
                    {
                        continue;
                    }


                    float dist = Vector3.Distance(targetWorldPos, player.position);

                    if (dist < distance)
                    {
                        distance = dist;
                        nextCell = targetCell;
                    }
                }
                
            }
        }
        nextCell.z = 0;
        return nextCell;
    }

    private void Awake()
    {
        
    }
    private void Start()
    {
        player = GameObject.Find("Player").transform;
        GridUpdate();
    }

    private void FixedUpdate()
    {
        UpdateSurroundTiles(transform.position);
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
