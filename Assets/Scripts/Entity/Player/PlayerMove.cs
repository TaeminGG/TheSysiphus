using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Vector2 inputAxis = Vector2.zero;
    public Vector2Int targetAxis = Vector2Int.zero;

    [Header("Movement Setting")]
    public float moveSpeed = 3.0f;
    private float moveSpeedSet = 0f;
    public float airResi = 0.5f;                // 공중에서 속도 감소 비율
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("Status")]
    [HideInInspector]
    public float playerZ = 0;                   // 플레이어의 가상 Z값 (높이)
    public float stepHeight = 0.1f;             // 움직여서 통과할 수 있는 높이 최댓값
    private float verticalVelocity = 0f;        // 낙하 속도
    private bool isGrounded = true;             // 착지 여부
    private bool isJumping = false;             // 점프 여부
    private int floorZ;
    private int tmpZ;
    private Vector3Int cellPos;

    private Rigidbody2D rid;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private SurroundTileCheck stc;
    public Transform playerSprite;              // 스프라이트를 가진 자식 오브젝트의 Transform
    private float spriteWorldY = 0f;            // 스프라이트의 Y 월드좌표
    private float yOffset = 0f;


    public Transform shadow;                    // 그림자의 Transform
    private bool isOnFloor = true;              // 플레이어가 바닥에 닿아있는지 여부

    private void Awake()
    {
        rid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = playerSprite.GetComponent<SpriteRenderer>();
        stc = GetComponent<SurroundTileCheck>();
    }

    private void Start()
    {
        moveSpeedSet = moveSpeed;

        floorZ = 0;
        tmpZ = 0;
    }

    private void Update()
    {
    }

    private void FixedUpdate()
    {
        HandleJumpAndGravity();
        PlayerMoveMent();
        UpdatePosition();
    }


    private void PlayerMoveMent()
    {
        Vector3 axis = inputAxis;

        if(inputAxis.magnitude > 0.1f)
        {
            Vector3 targetPos = transform.position + axis * moveSpeed * Time.deltaTime;

            // 공중 체공 여부
            if (!isGrounded)
            {
                int targetTileZ = stc.GetMovementTileZAt(targetPos, playerZ);
                int upperTileZ = stc.GetMovementTileZAt(new Vector3(targetPos.z, targetPos.y + 0.5f, 0), playerZ);

                // targetPos의 타일 높이가 플레이어가 밟고있는 타일 높이보다 높으면 움직임 제한
                if (targetTileZ <= playerZ && upperTileZ + 0.1f <= playerZ)
                {
                    transform.position = targetPos;
                }
                // 벽에 부딪히는 판정
                else
                {

                }
            }
            else
            {
                int targetTileZ = stc.GetMovementTileZAt(targetPos, playerZ);

                // targetPos의 타일 높이가 플레이어가 밟고있는 타일 높이보다 높으면 움직임 제한
                if (targetTileZ <= playerZ)
                {
                    transform.position = targetPos;
                }
                // 벽에 부딪히는 판정
                else
                {

                }
            }
        }

    }
    private void UpdatePosition()
    {
        float yOffset = (playerZ - floorZ) * 0.25f;

        if(spriteWorldY > yOffset+0.05f)
        {
            spriteWorldY += gravity * Time.deltaTime * 0.5f;
        }
        else
        {
            spriteWorldY = yOffset;
        }

        if (playerSprite != null)
        {
            playerSprite.localPosition = new Vector3(0, spriteWorldY, 0);
        }
    }
    private void HandleJumpAndGravity()
    {
        Vector3 pivotPos = new Vector3(stc.pivot.position.x, stc.pivot.position.y, 0);
        
        //Debug.Log("Pivot Pos : " + pivotPos + " Player Pos : " + stc.pivot.position);

        // 점프
        if (isJumping && isGrounded)
        {
            verticalVelocity = jumpForce;
            isGrounded = false;
            isJumping = false;
        }

        // 중력 적용
        if (!isGrounded || playerZ > floorZ)
        {
            moveSpeed = moveSpeedSet * airResi;    // 공중에서 속도 감소
            verticalVelocity += gravity * Time.deltaTime;
            isGrounded = false;

            int getZ = stc.GetJumpTileZAt(pivotPos, playerZ);
            if (inputAxis.magnitude > 0.1f && getZ != -1 && getZ < tmpZ)
            {
                // 진행 방향의 조금 앞(0.3f 거리) 타일 높이를 확인
                Vector3 targetPos = pivotPos + (Vector3)inputAxis.normalized * 0.3f;
                int nextZ = stc.GetJumpTileZAt(targetPos, playerZ);

                // 앞쪽이 여전히 이전 높이(tmpZ)와 같다면, 타일 사이의 틈새를 지나는 중이므로 높이 유지
                if (nextZ == tmpZ)
                {
                    getZ = tmpZ;
                }
            }
            if (getZ != -1 && tmpZ != getZ)
            {
                if (inputAxis.magnitude > 0.1f || getZ > tmpZ)
                {
                    floorZ = getZ;   // 플레이어 발 밑 바닥 타일의 높이 (점프 중)
                }
            }

            Debug.Log("FloorZ : " + floorZ + "       tmpZ : " + tmpZ + "pivotPos : " + pivotPos);
        }

        playerZ += verticalVelocity * Time.deltaTime;

        // 착지 했을 때
        if (playerZ <= floorZ)
        {
            if(!isGrounded)
                cellPos = stc.GetGridPosition(pivotPos);

            isGrounded = true;
            playerZ = floorZ;
            moveSpeed = moveSpeedSet;   // 착지 시 속도 복구
            verticalVelocity = 0;

            tmpZ = floorZ;

            Vector3Int currentCellPos = stc.GetGridPosition(pivotPos);

            if (cellPos != currentCellPos)
            {
                cellPos = currentCellPos;

                int getZ = stc.GetJumpTileZAt(pivotPos, playerZ);
                if (getZ != -1)
                {
                    if (getZ < tmpZ)
                    {
                        float dropHeight = (floorZ - getZ) * 0.25f;
                        floorZ = getZ;

                        // 변화한 Z값 만큼 플레이어의 그림자 위치 (앵커) 아래로 이동
                        Vector3 resetPos = new Vector3(transform.position.x, transform.position.y - dropHeight, 0);

                        //resetPos가 벽의 옆면이 아닐 때 까지 새 위치 계산
                        bool isStuck = true;
                        int tryCount = 1;
                        while (isStuck)
                        {
                            int tileZ = stc.GetMovementTileZAt(resetPos, getZ);
                            if (tileZ > getZ)
                            {
                                resetPos.x += -0.2f * MathF.Pow(-1, tryCount);
                                resetPos.y += -0.1f * MathF.Pow(-1, tryCount);
                            }
                            else
                            {
                                isStuck = false;
                            }

                            if (tryCount >= 100)
                                break;

                            tryCount++;
                        }
                        transform.position = resetPos;
                    }
                }
            }
        }

        //Debug.Log("Player Z : " + playerZ + " Floor Z : " + floorZ);
    }



    private void OnMove(InputValue value)
    {
        inputAxis = new Vector2(value.Get<Vector2>().x, value.Get<Vector2>().y).normalized;
    }

    private void OnJump(InputValue value)
    {
        if(isGrounded)
            isJumping = true;
    }
}
