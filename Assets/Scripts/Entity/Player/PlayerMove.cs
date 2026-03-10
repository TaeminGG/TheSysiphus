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
    public float playerZ = 0;                     // 플레이어의 가상 Z값 (높이)
    private float verticalVelocity = 0f;        // 낙하 속도
    private bool isGrounded = true;             // 착지 여부
    private bool isJumping = false;             // 점프 여부

    private Rigidbody2D rid;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private SurroundTileCheck stc;


    public Transform shadow;                    // 그림자의 Transform
    private bool isOnFloor = true;              // 플레이어가 바닥에 닿아있는지 여부

    private void Awake()
    {
        rid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stc = GetComponent<SurroundTileCheck>();
    }

    private void Start()
    {
        moveSpeedSet = moveSpeed;
    }

    private void Update()
    {
        PlayerMoveMent();
        UpdatePosition();
        HandleJumpAndGravity();
    }
    private void PlayerMoveMent()
    {
        Vector3 axis = inputAxis;

        if(inputAxis.magnitude > 0.1f)
        {
            Vector3 targetPos = transform.position + axis * moveSpeed * Time.deltaTime;

            int targetTileZ = stc.GetTopTileZ(targetPos);

            if(targetTileZ <= playerZ + 0.1f)
                transform.position = targetPos;
        }
    }
    private void UpdatePosition()
    {
        float yOffset = playerZ * 0.5f;
    }
    private void HandleJumpAndGravity()
    {
        int floorZ = stc.GetTopTileZ(stc.pivot.position);   // 플레이어 발 밑 가장 높은 Z값을 가진 타일 확인 (밟고 있는 바닥)

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

        playerZ += verticalVelocity * Time.deltaTime;

        // 착지 했을 때
        if(playerZ <= floorZ)
        {
            moveSpeed = moveSpeedSet;   // 착지 시 속도 복구
            playerZ = floorZ;
            verticalVelocity = 0;
            isGrounded = true;
        }

        Debug.Log("Floor Z : " + floorZ);
        Debug.Log("Player Z : " + playerZ);
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
