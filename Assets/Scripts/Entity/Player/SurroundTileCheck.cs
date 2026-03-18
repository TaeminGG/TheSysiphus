using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

public class SurroundTileCheck : MonoBehaviour
{
    public Grid grid;
    public Tilemap tmap;
    public Tilemap bmap;

    public Vector3Int currentPos;
    public Vector3Int targetPos;
    private Vector3Int prevPos;

    public Transform pivot;
    private Vector3 pivotPos;

    private Vector2 mousePos = Vector2.zero;

    //주변 9칸 (제자리 포함)의 Direction Infomation
    private Dictionary<Vector3Int, DirectionInfo> surroundTiles = new Dictionary<Vector3Int, DirectionInfo>();

    public PlayerMove playerMove;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();
        GridUpdate();
    }

    // 현재 플레이어 위치를 기준으로 주변 타일 정보(Box Tile) 수집
    public void UpdateSurroundTiles(Vector3 pos)
    {
        if(grid.WorldToCell(pos) != prevPos)
        {
            surroundTiles.Clear();
        }

        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                bool valid = true;

                // z : 4 2 0
                for (int z = 4; z >= 0; z-=2)
                {
                    Vector3Int cellPos = grid.WorldToCell(pos);

                    cellPos.x += x;
                    cellPos.y += y;

                    for (int depth = -2; depth <= 0; depth++)
                    {
                        Vector3Int depthPos = cellPos + new Vector3Int(depth, depth, 0);

                        depthPos.z = z;

                        bool isMatched = false;
                        if(depth *(-2) == z)
                        {
                            // depth가 0이 아닌 경우에는 플레이어가 땅에 있을 때만 판단
                            if (depth < 0)
                            {
                                if(playerMove.floorZ >= z)
                                {
                                    isMatched = true;
                                }
                                else
                                {
                                    isMatched = false;
                                }
                                    
                            }
                            else //depth가 0인 경우
                            {
                                isMatched = true;
                            }
                        }
                        else
                        {
                            if(depth == 0)
                            {
                                if(playerMove.floorZ  < z && playerMove.floorZ == 0)
                                {
                                    isMatched = true;
                                }
                            }
                        }

                        if (bmap.HasTile(depthPos) && isMatched)
                        {
                            if(isMatched)
                            {
                                // 한 칸 위에도 타일이 있으면 지나갈 수 있게
                                depthPos.z = z + 2;
                                if (bmap.HasTile(depthPos) && playerMove.floorZ >= z+2)
                                {
                                    valid = true;
                                }
                                else
                                    valid = false;
                            }
                            else
                                valid = false;
                        }

                    }

                    

                    if (surroundTiles.ContainsKey(new Vector3Int(x,y,0)))       // 주변 x, y 에 등록된 칸이 있는지
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

        prevPos = grid.WorldToCell(pos);
    }

    // 해당 타일 위에 아무 타일도 없는지 (해당 타일이 가장 윗 타일인지) 체크
    public bool CheckIsUpperTile(Vector3 pos, int height)
    {
        Vector3Int cellPos = grid.WorldToCell(pos);
        int groundZ = GetGroundZLevel(pos);

        // 윗면 타일이 아니면 True 반환 (Z=0)
        if(groundZ < 0)
        {
            return true;
        }
        else
        {
            groundZ += 2;
            Vector3Int checkPos = cellPos - new Vector3Int(groundZ / 2, groundZ / 2, 0);    // 해당 타일이 설치된 타일의 위치
            checkPos.z = groundZ;
            if (bmap.HasTile(checkPos))
            {
                return false;
            }
        }
        return true;
    }
    public bool CheckIsUpperTile(Vector3Int pos)       // Vector3Int를 매개변수로 받음 
    {
        Vector3Int cellPos = pos;
        int groundZ = GetGroundZLevel(pos);

        // 윗면 타일이 아니면 True 반환 (Z=0)
        if (groundZ < 0)
        {
            return true;
        }
        else
        {
            groundZ += 2;
            Vector3Int checkPos = cellPos - new Vector3Int(groundZ / 2, groundZ / 2, 0);    // 해당 타일이 설치된 타일의 위치
            checkPos.z = groundZ;
            if (bmap.HasTile(checkPos))
            {
                return false;
            }
        }
        return true;
    }


    public bool GetTileValid(Vector3 axis, float length)
    {
        Vector3Int cellPos = grid.WorldToCell(pivotPos + new Vector3(axis.x*length, axis.y*length, 0));
        Vector3Int pivotIntPos = grid.WorldToCell(pivotPos);

        Vector3Int testPos = cellPos - pivotIntPos;
        testPos.z = 0;

        return surroundTiles[testPos].isValid;
    }

    // Axis 방향으로 Legnth 만큼 떨어진 타일의 Z 레벨을 얻는 함수
    public int GetTileZLevel(Vector3 axis, float length)
    {
        Vector3Int cellPos = grid.WorldToCell(pivotPos + new Vector3(axis.x * length, axis.y * length, 0));
        Vector3Int pivotIntPos = grid.WorldToCell(pivotPos);

        Vector3Int testPos = cellPos - pivotIntPos;
        Debug.Log("test : " + testPos);
        testPos.z = 0;

        return surroundTiles[testPos].zLevel;
    }
    public int GetTileZLevelAnother(Vector3 axis, float length)
    {
        Vector3Int cellPos = grid.WorldToCell(pivotPos + new Vector3(axis.x * length, axis.y * length, 0));
        Vector3Int pivotIntPos = grid.WorldToCell(pivotPos);

        Vector3Int testPos = cellPos - pivotIntPos;
        Debug.Log("test : " + testPos);
        testPos = new Vector3Int(testPos.x - (playerMove.floorZ / 2), testPos.y - (playerMove.floorZ / 2), 0);

        Debug.Log("fix : " + testPos);

        for(int z = 10; z>=0; z -= 2)
        {
            testPos.z = z;
            if (bmap.HasTile(pivotIntPos + testPos))
                return z;
        }

        return -1;
    }

    // Pos 위치에 설치된 타일(박스 아랫면)의 Z값을 얻는 함수
    public int GetTileZLevel(Vector3 pos)
    {
        Vector3Int intPos = grid.WorldToCell(pos);

        for(int z=4; z>=0; z -= 2)
        {
            intPos.z = z;
            if (bmap.HasTile(intPos))
            {
                return z;
            }
        }
        return -1;
    }

    // Vector3 Pos위치에 설치된 타일(박스 윗면)의 높이(Z)를 얻는 함수
    public int GetGroundZLevel(Vector3 pos)
    {
        Vector3Int intPos = grid.WorldToCell(pos);

        for(int i=4; i >=1; i--)
        {
            // 아래로 i 칸 만큼 떨어진 타일의 Cell Position
            Vector3Int cellPos = intPos + new Vector3Int(-i, -i, 0);

            int z = i * 2 - 2;

            cellPos.z = z;

            if (bmap.HasTile(cellPos))
            {
                return z;
            }
        }

        // bmap에서 확인된 타일이 없으면 평지로 인식
        return -1;
    }
    // Vector3Int Pos위치에 설치된 타일(박스 윗면)의 높이(Z)를 얻는 함수
    public int GetGroundZLevel(Vector3Int pos)
    {
        Vector3Int intPos = pos;

        for (int i = 4; i >= 1; i--)
        {
            // 아래로 i 칸 만큼 떨어진 타일의 Cell Position
            Vector3Int cellPos = intPos + new Vector3Int(-i, -i, 0);

            int z = i * 2 - 2;

            cellPos.z = z;

            if (bmap.HasTile(cellPos))
            {
                return z;
            }
        }

        // bmap에서 확인된 타일이 없으면 평지로 인식
        return -1;
    }

    // 입력받은 Z값부터 순차적으로 비교하여 타일(박스 윗면)의 높이(Z)를 얻는 함수
    // Z = value
    public int GetGroundZLevel(Vector3 pos, int value)
    {
        Vector3Int intPos = grid.WorldToCell(pos);

        for (int i = value-1; i >= 1; i--)
        {
            // 아래로 i 칸 만큼 떨어진 타일의 Cell Position
            Vector3Int cellPos = intPos + new Vector3Int(-i, -i, 0);

            int z = i * 2 - 2;

            cellPos.z = z;

            if (bmap.HasTile(cellPos))
            {
                return z;
            }
        }

        // bmap에서 확인된 타일이 없으면 평지로 인식
        return -1;
    }

    void Update()
    {
        //Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        //Debug.Log("Cursor : " + tmap.WorldToCell(cam.ScreenToWorldPoint(mousePos)));
    }

    private void FixedUpdate()
    {
        pivotPos = pivot.transform.position;
        UpdateSurroundTiles(pivotPos);

    }

    void OnCursor(InputValue value)
    {
        mousePos = value.Get<Vector2>();
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
            if(map.gameObject.name == "Box")
            {
                bmap = map;
            }
        }
    }
}
