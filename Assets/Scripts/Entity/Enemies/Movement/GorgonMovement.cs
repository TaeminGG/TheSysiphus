using UnityEngine;

public class GorgonMovement : MonoBehaviour, IEnemyMovement
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int LegacyIsWalkingHash = Animator.StringToHash("isWalking");

    public float moveSpeed = 1.2f;
    public int floorZ = 0;
    public int FloorZ => floorZ;

    public bool isDetected = false;

    public Transform sprite;
    public Transform player;
    public Grid grid;

    private Vector3 targetPos = Vector3.zero;
    private GroundedEnemy groundedEnemy;
    private EnemyCrowdControl crowdControl;
    private GorgonAttack attack;
    private Animator anim;
    private bool isArrive = true;

    private void Awake()
    {
        groundedEnemy = GetComponent<GroundedEnemy>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();

        attack = GetComponent<GorgonAttack>();
        if (sprite != null)
            anim = sprite.GetComponent<Animator>();
    }

    private void Start()
    {
        groundedEnemy.floorZ = floorZ;
        player = GameObject.Find("Player").transform;
        grid = FindAnyObjectByType<Grid>();
        targetPos = transform.position;
        isArrive = true;
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void LateUpdate()
    {
        if (sprite != null)
        {
            Vector3 spritePos = sprite.localPosition;
            spritePos.z = floorZ;
            sprite.localPosition = spritePos;

            if (player != null && (attack == null || !attack.IsBusy))
                sprite.GetComponent<SpriteRenderer>().flipX = player.position.x < transform.position.x;
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
            SetWalking(false);
            return;
        }

        SetWalking(true);
        float currentMoveSpeed = moveSpeed;
        if (crowdControl != null)
            currentMoveSpeed *= crowdControl.MoveSpeedMultiplier;

        if (attack != null && attack.IsBusy)
        {
            SetWalking(false);
            return;
        }

        if (isArrive && (attack == null || !attack.IsBusy))
        {
            if (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    currentMoveSpeed * Time.deltaTime);
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
            int nextFloorZ = groundedEnemy.GetGroundZLevel(cellPos) + 2;
            if (nextFloorZ != floorZ)
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
            new Vector2(transform.position.x, transform.position.y)));

        if (!crowdControl.IsKnockbackActive)
        {
            isArrive = true;
            targetPos = transform.position;
        }
    }

    private void SetWalking(bool isWalking)
    {
        if (anim != null)
        {
            anim.SetBool(IsWalkingHash, isWalking);
            anim.SetBool(LegacyIsWalkingHash, isWalking);
        }
    }
}
