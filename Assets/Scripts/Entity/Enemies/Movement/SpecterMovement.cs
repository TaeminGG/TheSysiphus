using UnityEngine;

public class SpecterMovement : MonoBehaviour, IEnemyMovement
{
    public float moveSpeed = 1.0f;
    public int floorZ = 0;

    public int FloorZ => floorZ;

    public bool isDetected = false;

    private Vector3 targetPos;

    public Transform sprite;
    private FloatingEnemy floatingEnemy;
    private EnemyCrowdControl crowdControl;
    private SpecterAttack attack;
    private Transform player;
    private Grid grid;
    private Animator anim;

    private bool isArrive = true;

    void Awake()
    {
        floatingEnemy = GetComponent<FloatingEnemy>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();
        attack = GetComponent<SpecterAttack>();
        anim = sprite.GetComponent<Animator>();
    }

    void Start()
    {
        targetPos = transform.position;
        player = GameObject.Find("Player").transform;
        grid = FindAnyObjectByType<Grid>();
    }

    void Update()
    {
        floorZ = player.GetComponent<PlayerMove>().floorZ;
    }

    void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
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
        PlayerMove playerMove = player.GetComponent<PlayerMove>();

        if (crowdControl != null && crowdControl.IsKnockbackActive)
        {
            HandleKnockback();
            return;
        }

        if (!isDetected || (crowdControl != null && !crowdControl.CanMove))
        {
            targetPos = transform.position;
            return;
        }

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

            targetPos = transform.position + (playerMove.transform.position - transform.position).normalized;
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
        transform.position += knockbackStep;
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
