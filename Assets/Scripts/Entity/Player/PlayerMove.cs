using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Vector2 inputAxis = Vector2.zero;
    private Vector3 axis;
    public Vector2Int targetAxis = Vector2Int.zero;

    [Header("Movement Setting")]
    public float moveSpeed = 3.0f;
    private float moveSpeedSet = 0f;
    public float airResi = 0.5f;                // 공중에서 속도 감소 비율
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("Status")]
    public float playerZ = 0;                   // 플레이어의 가상 Z값 (높이)
    private float verticalVelocity = 0f;        // 낙하 속도
    private bool isGrounded = true;             // 착지 여부
    private bool isJumping = false;             // 점프 여부
    public int floorZ;

    [Header("Dash Setting")]
    public float dashForce = 5.0f;              // 대쉬 하는 힘
    public float dashDuration = 0.5f;           // 대쉬 지속시간
    public float dashCooltime = 2f;             // 대쉬 쿨타임
    private float dashTime = 0f;                // 대쉬 지속시간 확인변수
    private float dashCooltimeCheck = 0f;

    private float spriteZ = 0f;

    private Rigidbody2D rid;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private SurroundTileCheck stc;
    public Transform playerSprite;              // 스프라이트를 가진 자식 오브젝트의 Transform
    private float spriteWorldY = 0f;            // 스프라이트의 Y 월드좌표
    private float yOffset = 0f;

    private int fixedZ;                         // 위, 아래로 이동할 때 착지할 바닥의 Z값

    private bool isOnValidTile = false;       // 플레이어 위치의 타일이 valid 타일인지 여부
    public int groundedZ = 0;

    public Transform shadow;                    // 그림자의 Transform
    public Transform shadowSprite;

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
        if (dashTime < dashDuration)
        {
            dashTime += Time.deltaTime;
            moveSpeed += dashForce;
            dashCooltimeCheck += Time.deltaTime;
        }
        else
        {
            axis = inputAxis;
            moveSpeed = moveSpeedSet;
            dashCooltimeCheck += Time.deltaTime;
        }
            
        Vector3Int prevCell;
        int prevZ;    // 이동 전 FloorZ를 저장하는 변수

        int targetZLevel;

        if(inputAxis.magnitude > 0.1f)
        {
            Vector3 targetPos = shadow.transform.position + axis * moveSpeed * Time.deltaTime;
            targetZLevel = 0;

            prevCell = stc.GetGridPosition(shadow.transform.position);
            prevZ = floorZ;

            if (!stc.GetTileValid(axis, moveSpeed * Time.deltaTime))
            {
                //targetZLevel = stc.GetTileZLevel(axis, moveSpeed * Time.deltaTime) + 2;       // target 타일의 Z레벨 +2 (박스형테 이기 때문에)
                targetZLevel = stc.GetTileZLevelAnother(axis, moveSpeed * Time.deltaTime);

                if (targetZLevel < 0)
                    targetZLevel = 0;
                else
                    targetZLevel += 2;

                //Debug.Log(targetZLevel);

                if (targetZLevel <= playerZ)
                {
                    transform.position = targetPos;
                    isOnValidTile = false;

                    int targetGroundZLevel = stc.GetGroundZLevel(targetPos);
                    if (targetGroundZLevel < 0)
                        targetGroundZLevel = 0;
                    else
                    {
                        if (targetGroundZLevel >= 0)
                            targetGroundZLevel += 2;
                    }
                    if (prevZ > targetGroundZLevel)
                    {
                        fixedZ = targetGroundZLevel;

                        if (targetZLevel < 0)
                        {
                            floorZ = 0;
                        }
                        else
                        {
                            floorZ = targetZLevel;
                        }

                        float diff = prevZ - targetGroundZLevel;
                        targetPos.y -= diff * 0.25f;
                        int fixedGroundZLevel = stc.GetGroundZLevel(targetPos);
                        spriteZ += diff;
                        transform.position = targetPos;
                    }
                    else
                    {
                        if (prevCell != stc.GetGridPosition(shadow.transform.position) && ((fixedZ != targetZLevel) || fixedZ == -1))
                        {
                            if (prevZ < targetZLevel)
                            {
                                fixedZ = targetZLevel;

                                floorZ = targetZLevel;

                                float diff = targetZLevel - prevZ;
                                targetPos.y += diff * 0.25f;
                                spriteZ -= diff;
                                transform.position = targetPos;
                            }
                        }
                    }

                }
            }
            else
            {
                targetZLevel = stc.GetGroundZLevel(targetPos);

                if (targetZLevel < 0)
                    targetZLevel = 0;
                else
                    targetZLevel += 2;

                if(targetZLevel > prevZ)
                {
                    targetZLevel = stc.GetGroundZLevel(targetPos, prevZ);
                }

                // 같은 Z 높이 타일이 아닌 경우 떨어짐 처리
                if (targetZLevel == floorZ)
                {
                    transform.position = targetPos;
                    isOnValidTile = true;
                }
                else
                {
                    transform.position = targetPos;
                    isOnValidTile = true;
                    int fallGroundZ = stc.GetFallGroundZLevel(targetPos);
                    targetZLevel = (fallGroundZ < 0) ? 0 : fallGroundZ + 2;

                    if (prevCell != stc.GetGridPosition(shadow.transform.position) && ((fixedZ != targetZLevel) || fixedZ == -1))
                    {
                        if (prevZ > targetZLevel)
                        {
                            fixedZ = targetZLevel;
                            if (targetZLevel <= 0)
                            {
                                Debug.Log("hohoi  ");
                                floorZ = 0;
                            }
                            else
                                floorZ = targetZLevel;
                            float diff = prevZ - targetZLevel;
                            targetPos.y -= diff * 0.25f;
                            spriteZ += diff;
                            transform.position = targetPos;
                        }
                    }
                }
            }

            
        }

    }
    private void UpdatePosition()
    {
        spriteZ += verticalVelocity * Time.deltaTime;
        yOffset = spriteZ * 0.25f;

        if(playerZ <= floorZ)
        {
            yOffset = 0;
        }
        playerSprite.localPosition = new Vector3(0, yOffset, playerZ);
        shadowSprite.localPosition = new Vector3(0, 0, floorZ);
        
    }
    private void HandleJumpAndGravity()
    {
        Vector3 pivotPos = new Vector3(shadow.position.x, shadow.position.y, 0);

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
        }

        int getZ = stc.GetGroundZLevel(pivotPos);

        if( getZ < 0 )
        {
            floorZ = 0;
        }
        else
        {
            // unvalid 일때 예외처리
            if(playerZ > getZ+2 && isGrounded)
            {
                if (!isOnValidTile || groundedZ >= getZ+2)
                {
                    floorZ = getZ + 2;
                }
            }
        }

        

        playerZ += verticalVelocity * Time.deltaTime;

        // 착지 했을 때
        if (playerZ <= floorZ)
        {
            if (!isGrounded)
            {
                isGrounded = true;
                groundedZ = floorZ;
                spriteZ = 0;
            }
            else
            {

            }

            fixedZ = -1;
            playerZ = floorZ;
            moveSpeed = moveSpeedSet;   // 착지 시 속도 복구
            verticalVelocity = 0;
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

    private void OnDash(InputValue value)
    {
        if (dashCooltimeCheck >= dashCooltime)
        {
            dashCooltimeCheck = 0;
            dashTime = 0;
        }
    }
}
