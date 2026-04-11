using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Vector2 inputAxis = Vector2.zero;
    private Vector3 axis;
    public Vector2Int targetAxis = Vector2Int.zero;

    private enum LookAt
    {
        E,
        W,
        S,
        N,
        NE,
        NW,
        SE,
        SW
    }
    private LookAt lookAt = LookAt.S;

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
    public int groundedZ = 0;
    public float spritePivotY = 0.38f;

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
    private PlayerStats stats;
    private PassiveItemList passiveItemList;
    private float yOffset = 0f;

    private int fixedZ;                         // 위, 아래로 이동할 때 착지할 바닥의 Z값

    private bool isOnValidTile = false;       // 플레이어 위치의 타일이 valid 타일인지 여부

    [Header("Transform")]
    public Transform shadow;                    // 그림자의 Transform
    public Transform shadowSprite;
    public Transform playerSprite;              // 스프라이트를 가진 자식 오브젝트의 Transform

    private bool isAttacking = false;

    // 아이템 획득 관련
    public static event Action OnPickup;

    private void Awake()
    {
        rid = GetComponent<Rigidbody2D>();
        anim = playerSprite.GetComponent<Animator>();
        spriteRenderer = playerSprite.GetComponent<SpriteRenderer>();
        stc = GetComponent<SurroundTileCheck>();
        stats=GetComponent<PlayerStats>();
        passiveItemList = GetComponent<PassiveItemList>();
    }

    private void Start()
    {
        moveSpeedSet = moveSpeed;

        floorZ = 0;
        dashTime = dashDuration;
        dashCooltimeCheck = dashCooltime;
    }

    private void FixedUpdate()
    {
        HandleJumpAndGravity();
        PlayerMoveMent();
        UpdatePosition();
    }

    // 공격 시 공격 방향을 바라보게 설정하는 함수 (AttackController.cs 에서 실행)
    public void AttackLookAt()
    {
        float angle = GetComponent<PlayerAttackDirection>().GetAttackAngle();

        if (-22.5f < angle || 22.5f >= angle)
        {
            axis = Vector2.right;
        }
        if (22.5f < angle && 67.5f >= angle)
        {
            axis = new Vector2(1, 1).normalized;
        }
        if (67.5f < angle && 112.5f >= angle)
        {
            axis = Vector2.up;
        }
        if (112.5f < angle && 157.5f >= angle)
        {
            axis = new Vector2(-1, 1).normalized;
        }
        if (157.5f < angle || -157.5f >= angle)
        {
            axis = Vector2.left;
        }
        if (-157.5f < angle && -112.5f >= angle)
        {
            axis = new Vector2(-1, -1).normalized;
        }
        if (-112.5f < angle && -67.5f >= angle)
        {
            axis = Vector2.down;
        }
        if (-67.5f < angle && -22.5f >= angle)
        {
            axis = new Vector2(1, -1).normalized;
        }
    }

    private void PlayerMoveMent()
    {
        // 대쉬 중 방향 전환을 막음
        bool isDashing = dashTime < dashDuration;
        if (isDashing)
        {
            dashTime += Time.deltaTime;
            dashCooltimeCheck += Time.deltaTime;
        }
        else
        {
            // 공격 중 이동 불가
            if (GetComponent<AttackController>().isAttacking)
            {
                isAttacking = true;
            }
            else
            {
                axis = inputAxis;
                isAttacking = false;
            }
            moveSpeed = moveSpeedSet;
            dashCooltimeCheck += Time.deltaTime;

            if(axis.x < 0f)
            {
                if (axis.y < 0f)
                    lookAt = LookAt.SW;
                else
                {
                    if (axis.y > 0f)
                        lookAt = LookAt.NW;
                    else
                    {
                        if (axis.y == 0f)
                            lookAt = LookAt.W;
                    }
                }
            }
            else if(axis.x > 0f)
            {
                if (axis.y < 0f)
                    lookAt = LookAt.SE;
                else
                {
                    if (axis.y > 0f)
                        lookAt = LookAt.NE;
                    else
                    {
                        if (axis.y == 0f)
                            lookAt = LookAt.E;
                    }
                }
            }
            else
            {
                if (axis.y > 0f)
                    lookAt = LookAt.N;
                else if (axis.y < 0f)
                    lookAt = LookAt.S;
            }

        }

        // 바라보는 방향에 따라 Animation Controller 정수값 변경
        if (lookAt == LookAt.S)
            anim.SetInteger("Look", 0);
        else if (lookAt == LookAt.SE || lookAt == LookAt.SW)
            anim.SetInteger("Look", 1);
        else if (lookAt == LookAt.E || lookAt == LookAt.W)
            anim.SetInteger("Look", 2);
        else if (lookAt == LookAt.NE || lookAt == LookAt.NW)
            anim.SetInteger("Look", 3);
        else if (lookAt == LookAt.N)
            anim.SetInteger("Look", 4);

        if (lookAt == LookAt.SW || lookAt == LookAt.NW || lookAt == LookAt.W)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;


        if (isAttacking)
            axis = Vector2.zero;

        float currentMoveSpeed = moveSpeedSet;
        if (isDashing)
            currentMoveSpeed += dashForce;

        if (!isDashing && (!isGrounded || playerZ > floorZ))
            currentMoveSpeed *= airResi;

        Vector3Int prevCell;
        int prevZ;    // 이동 전 FloorZ를 저장하는 변수

        int targetZLevel;

        if(inputAxis.magnitude > 0.1f)
        {
            Vector3 targetPos = shadow.transform.position + axis * currentMoveSpeed * Time.deltaTime;
            targetZLevel = 0;

            prevCell = stc.GetGridPosition(shadow.transform.position);
            prevZ = floorZ;

            if (!stc.GetTileValid(axis, currentMoveSpeed * Time.deltaTime))
            {
                targetZLevel = stc.GetTileZLevelAnother(axis, currentMoveSpeed * Time.deltaTime);

                if (targetZLevel < 0)
                    targetZLevel = 0;
                else
                    targetZLevel += 2;

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


        float correctedZ = playerZ + (spritePivotY-0.18f) / 0.26f;

        // 스프라이트의 부모(pivot)의 Z값을 바꾸고 스프라이트는 Y값만 변경
        playerSprite.parent.localPosition = new Vector3(0, spritePivotY + yOffset, 0);
        playerSprite.localPosition = new Vector3(0, 0, correctedZ);
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
    }

    public bool IsPlayerOnGround()
    {
        return isGrounded;
    }

    public void AddSpeed(float speed)
    {
        moveSpeed += speed;
        moveSpeedSet += speed;
    }
    public void ReduceSpeed(float speed)
    {
        moveSpeed -= speed;
        moveSpeedSet -= speed;
    }


    private void OnMove(InputValue value)
    {
        inputAxis = new Vector2(value.Get<Vector2>().x, value.Get<Vector2>().y).normalized;
    }

    private void OnJump(InputValue value)
    {
        if(isGrounded && !GetComponent<AttackController>().isAttacking)
            isJumping = true;
    }

    private void OnInteract(InputValue value)
    {
        OnPickup?.Invoke();
    }

    private void OnDash(InputValue value)
    {
        if (dashCooltimeCheck >= dashCooltime && !GetComponent<AttackController>().isAttacking)
        {
            stats.Dash();
            dashCooltimeCheck = 0;
            dashTime = 0;
        }
    }
}
