using UnityEngine;

public class UndeadMovement : MonoBehaviour, IEnemyMovement
{
    public float moveSpeed = 5.0f;

    public int floorZ = 0;
    public int FloorZ => floorZ;

    public bool isDetected = false;

    public Transform sprite;
    private Vector3 targetPos = Vector3.zero;
    private GroundedEnemy groundedEnemy;
    private UndeadAttack attack;
    private EnemyCrowdControl crowdControl;
    public Transform player;
    public Grid grid;
    private Animator anim;

    private bool isArrive = true;

    void Awake()
    {
        groundedEnemy = GetComponent<GroundedEnemy>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();
        anim = sprite.GetComponent<Animator>();
        attack = GetComponent<UndeadAttack>();
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
            if (isArrive && !attack.isAttacking)
                sprite.GetComponent<SpriteRenderer>().flipX = true;
        }
        else
        {
            if (isArrive && !attack.isAttacking)
                sprite.GetComponent<SpriteRenderer>().flipX = false;
        }
    }

    public void Move()
    {
        if (crowdControl != null && crowdControl.IsKnockbackActive)
        {
            HandleKnockback();
            return;
        }

        if (!isDetected || (crowdControl != null && !crowdControl.CanMove))
        {
            targetPos = transform.position;
            anim.SetBool("isWalking", false);
            return;
        }

        anim.SetBool("isWalking", true);
        float currentMoveSpeed = moveSpeed;
        if (crowdControl != null)
            currentMoveSpeed *= crowdControl.MoveSpeedMultiplier;

        if (isArrive && !attack.isAttacking)
        {
            if (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    currentMoveSpeed * Time.deltaTime
                );
                return;
            }

            Vector3Int bestCell = groundedEnemy.GetNextCell();
            if (bestCell != Vector3Int.zero)
            {
                targetPos = grid.GetCellCenterWorld(bestCell);
                targetPos.z = 0f;
            }
        }
    }

    public void ApplyCrowdControl(EnemyCrowdControlRequest request)
    {
        if (crowdControl == null)
            crowdControl = GetComponent<EnemyCrowdControl>();

        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();

        crowdControl.ApplyEffect(request);
    }

    public void SetPlayerDetected(bool val)
    {
        isDetected = val;
    }

    private void HandleKnockback()
    {
        isArrive = false;

        Vector3 previousPosition = transform.position;
        Vector3 knockbackStep = (Vector3)(crowdControl.GetKnockbackDirection() * crowdControl.GetKnockbackMoveSpeed() * Time.deltaTime);
        Vector3 nextPosition = transform.position + knockbackStep;

        if (floorZ == 0)
        {
            if (!groundedEnemy.GetTileValid(nextPosition))
            {
                crowdControl.ClearKnockback();
                isArrive = true;
                targetPos = transform.position;
                return;
            }
        }
        else
        {
            Vector3Int cellPos = groundedEnemy.GetGridPosition(nextPosition);
            int getZ = groundedEnemy.GetGroundZLevel(cellPos) + 2;

            if (getZ != floorZ)
            {
                crowdControl.ClearKnockback();
                isArrive = true;
                targetPos = transform.position;
                return;
            }
        }

        transform.position = nextPosition;
        crowdControl.ConsumeKnockbackDistance(Vector2.Distance(
            new Vector2(previousPosition.x, previousPosition.y),
            new Vector2(transform.position.x, transform.position.y))
        );

        if (!crowdControl.IsKnockbackActive)
        {
            isArrive = true;
            targetPos = transform.position;
        }
    }
}
