using UnityEngine;

[DefaultExecutionOrder(1000)]
public class EnemyDirectionalAnimator : MonoBehaviour
{
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int LegacyIsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackEndedHash = Animator.StringToHash("AttackEnded");
    private const float MovementHoldTime = 0.08f;

    [SerializeField] private Transform sprite;
    [SerializeField] private bool clearSpriteFlip = true;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private IEnemyAttack attack;
    private IEnemyMovement movement;
    private Transform player;
    private Vector3 previousPosition;
    private Vector2 lastFacingDirection = Vector2.right;
    private Vector2 lastMovementDirection = Vector2.right;
    private Vector2 lockedAttackDirection = Vector2.right;
    private float lastMovementTime = float.NegativeInfinity;
    private bool wasBusy;

    private void Awake()
    {
        movement = GetComponent<IEnemyMovement>();
        attack = GetComponent<IEnemyAttack>();

        if (sprite == null)
            sprite = ResolveSpriteTransform();

        if (sprite != null)
        {
            animator = sprite.GetComponent<Animator>();
            spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        }

        previousPosition = transform.position;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void LateUpdate()
    {
        if (animator == null)
            return;

        bool isBusy = attack != null && attack.IsBusy;
        if (isBusy && !wasBusy)
        {
            lockedAttackDirection = ResolveAttackDirection();
            animator.ResetTrigger(AttackEndedHash);
            animator.SetTrigger(AttackHash);
        }
        else if (!isBusy && wasBusy)
        {
            animator.SetTrigger(AttackEndedHash);
        }

        wasBusy = isBusy;

        Vector2 movementDelta = transform.position - previousPosition;
        if (movementDelta.sqrMagnitude > 0.000001f)
        {
            lastMovementDirection = movementDelta.normalized;
            lastMovementTime = Time.time;
        }

        Vector2 facingDirection = ResolveFacingDirection(isBusy, movementDelta);
        if (facingDirection.sqrMagnitude > 0.001f)
            lastFacingDirection = facingDirection.normalized;

        animator.SetFloat(DirectionHash, DirectionToIndex(lastFacingDirection));

        bool movementReportedWalking = animator.GetBool(IsWalkingHash) || animator.GetBool(LegacyIsWalkingHash);
        bool recentlyMoved = Time.time - lastMovementTime <= MovementHoldTime;
        bool isWalking = !isBusy && movement != null && (movementReportedWalking || recentlyMoved);
        animator.SetBool(IsWalkingHash, isWalking);
        animator.SetBool(LegacyIsWalkingHash, isWalking);

        if (clearSpriteFlip && spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
        }

        previousPosition = transform.position;
    }

    private Transform ResolveSpriteTransform()
    {
        Animator childAnimator = GetComponentInChildren<Animator>();
        return childAnimator != null ? childAnimator.transform : null;
    }

    private Vector2 ResolveFacingDirection(bool isBusy, Vector2 movementDelta)
    {
        if (isBusy)
            return lockedAttackDirection;

        if (movementDelta.sqrMagnitude > 0.000001f)
            return movementDelta;

        if (Time.time - lastMovementTime <= MovementHoldTime)
            return lastMovementDirection;

        if (player != null)
            return player.position - transform.position;

        return lastFacingDirection;
    }

    private Vector2 ResolveAttackDirection()
    {
        if (player != null)
        {
            Vector2 playerDirection = player.position - transform.position;
            if (playerDirection.sqrMagnitude > 0.001f)
                return playerDirection.normalized;
        }

        if (Time.time - lastMovementTime <= MovementHoldTime)
            return lastMovementDirection;

        return lastFacingDirection;
    }

    private static float DirectionToIndex(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return 0f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        int counterClockwiseIndex = Mathf.RoundToInt(angle / 45f) % 8;
        return counterClockwiseIndex == 0 ? 0f : 8f - counterClockwiseIndex;
    }
}
