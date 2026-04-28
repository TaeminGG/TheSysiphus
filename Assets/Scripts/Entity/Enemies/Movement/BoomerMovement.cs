using UnityEngine;

public class BoomerMovement : MonoBehaviour, IEnemyMovement
{
    public float moveSpeed = 1.35f;
    public int floorZ = 0;
    public Transform sprite;

    public bool isDetected = false;

    private Vector3 targetPos;
    private EnemyCrowdControl crowdControl;
    private BoomerAttack attack;
    private Transform player;
    private Animator anim;
    private bool isArrive = true;

    public int FloorZ => floorZ;

    private void Awake()
    {
        crowdControl = GetComponent<EnemyCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();
        attack = GetComponent<BoomerAttack>();
        if (sprite != null)
            anim = sprite.GetComponent<Animator>();
    }

    private void Start()
    {
        targetPos = transform.position;
        player = GameObject.Find("Player")?.transform;
    }

    private void Update()
    {
        if (player != null && player.TryGetComponent(out PlayerMove playerMove))
            floorZ = playerMove.floorZ;
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void LateUpdate()
    {
        if (sprite == null)
            return;

        sprite.GetComponent<SpriteRenderer>().flipX = targetPos.x < transform.position.x;
    }

    public void Move()
    {
        if (player == null)
            return;

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

        if (attack != null && attack.IsBusy)
        {
            SetWalking(false);
            return;
        }

        SetWalking(true);
        float currentMoveSpeed = moveSpeed;
        if (crowdControl != null)
            currentMoveSpeed *= crowdControl.MoveSpeedMultiplier;

        if (isArrive)
        {
            if (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, currentMoveSpeed * Time.deltaTime);
                return;
            }

            targetPos = transform.position + (player.position - transform.position).normalized;
        }
    }

    public void SetPlayerDetected(bool val)
    {
        isDetected = val;
    }

    public void ApplyCrowdControl(EnemyCrowdControlRequest request)
    {
        if (crowdControl == null)
            crowdControl = GetComponent<EnemyCrowdControl>() ?? gameObject.AddComponent<EnemyCrowdControl>();

        crowdControl.ApplyEffect(request);
    }

    private void HandleKnockback()
    {
        isArrive = false;

        Vector3 previousPosition = transform.position;
        Vector3 knockbackStep = (Vector3)(crowdControl.GetKnockbackDirection() * crowdControl.GetKnockbackMoveSpeed() * Time.deltaTime);
        transform.position += knockbackStep;
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
            anim.SetBool("isWalking", isWalking);
    }
}
