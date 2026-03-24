using UnityEngine;
using UnityEngine.UI;

public class UndeadMovement : MonoBehaviour, IEnemyMovement
{
    public float moveSpeed = 5.0f;

    public int floorZ = 0;              // 해당 적이 있는 Z값 높이 (변경 X)

    public int FloorZ => floorZ;

    public bool isDetected = false;    // 플레이어 포착 여부

    public Transform sprite;
    private Vector3 targetPos = Vector3.zero;
    private GroundedEnemy groundedEnemy;
    private EnemyStats stat;
    private Transform player;
    public Grid grid;
    private Animator anim;

    private bool isArrive = true;      // 넉백 위치에 도착했는 지 여부
    private float knockbackDistance = 0;    // 기존 위치와 넉백 위치 사이 거리
    private Vector3 knockback = Vector3.zero;
    private Vector3 prevPos = Vector3.zero;


    void Awake()
    {
        groundedEnemy = GetComponent<GroundedEnemy>();
        stat = GetComponent<EnemyStats>();
        anim = sprite.GetComponent<Animator>();
    }

    void Start()
    {
        groundedEnemy.floorZ = floorZ;
        player = GameObject.Find("Player").transform;
        grid = FindAnyObjectByType<Grid>();
        targetPos = transform.position;
        isArrive = true;
    }

    void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
        Vector3 spritePos = sprite.localPosition;
        spritePos.z = floorZ;
        sprite.localPosition = spritePos;


        if (targetPos.x < transform.position.x)
        {
            // 넉백 중에는 flip실행 X
            if(isArrive)
                sprite.GetComponent<SpriteRenderer>().flipX = true;
        }
        else
        {
            if (isArrive)
                sprite.GetComponent<SpriteRenderer>().flipX = false;
        }
            
    }
    
    public void Move()
    {
        PlayerMove playerMove = player.GetComponent<PlayerMove>();

        // 플레이어 포착 시 이동
        if (!isDetected)
        {
            targetPos = transform.position;
            anim.SetBool("isWalking", false);
            return;
        }

        anim.SetBool("isWalking", true);

        // 넉백 상태 확인
        if(stat.isKnockback)
        {
            isArrive = false;
            stat.isKnockback = false;
            knockback = stat.knockbackVector;
            prevPos = transform.position;
            knockbackDistance = Vector3.Distance(prevPos, prevPos+knockback);
        }
        else
        {
            if (isArrive)
            {
                // 목표 지점까지 이동
                if (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPos,
                        moveSpeed * Time.deltaTime
                    );
                    return;
                }

                // 다음 이동할 칸 선택
                Vector3Int bestCell = groundedEnemy.GetNextCell();
                if (bestCell != Vector3Int.zero)
                {
                    targetPos = grid.GetCellCenterWorld(bestCell);
                    targetPos.z = 0f;
                }
            }
        }

        // 넉백 처리
        if (!isArrive)
        {
            targetPos = transform.position + (knockback.normalized * 10f * stat.knockBackResist * Time.deltaTime);

            // FloorZ가 0인 경우 따로 처리

            if (floorZ == 0)
            {
                if (groundedEnemy.GetTileValid(targetPos))
                {
                    transform.position = targetPos;
                    if (Vector3.Distance(prevPos, transform.position) >= knockbackDistance)
                    {
                        isArrive = true;

                        // 다음 이동할 칸 선택
                        Vector3Int bestCell = groundedEnemy.GetNextCell();
                        if (bestCell != Vector3Int.zero)
                        {
                            targetPos = grid.GetCellCenterWorld(bestCell);
                            targetPos.z = 0f;
                        }
                    }
                }
                else
                {
                    isArrive = true;

                    // 다음 이동할 칸 선택
                    Vector3Int bestCell = groundedEnemy.GetNextCell();
                    if (bestCell != Vector3Int.zero)
                    {
                        targetPos = grid.GetCellCenterWorld(bestCell);
                        targetPos.z = 0f;
                    }
                }
            }
            else
            {
                Vector3Int cellPos = groundedEnemy.GetGridPosition(targetPos);
                int getZ = groundedEnemy.GetGroundZLevel(cellPos) + 2;

                if (getZ == floorZ)
                {
                    transform.position = targetPos;
                    if (Vector3.Distance(prevPos, transform.position) >= knockbackDistance)
                    {
                        isArrive = true;

                        // 다음 이동할 칸 선택
                        Vector3Int bestCell = groundedEnemy.GetNextCell();
                        if (bestCell != Vector3Int.zero)
                        {
                            targetPos = grid.GetCellCenterWorld(bestCell);
                            targetPos.z = 0f;
                        }
                    }
                }
                else
                {
                    isArrive = true;

                    // 다음 이동할 칸 선택
                    Vector3Int bestCell = groundedEnemy.GetNextCell();
                    if (bestCell != Vector3Int.zero)
                    {
                        targetPos = grid.GetCellCenterWorld(bestCell);
                        targetPos.z = 0f;
                    }
                }
            }
        }
    }
    public void Stop()
    {
        
    }

    public void SetPlayerDetected(bool val)
    {
        isDetected = val;
    }
}
