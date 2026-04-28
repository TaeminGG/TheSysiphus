using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour, IEnemyAttack
{
    [Header("Timing")]
    public float range = 1f;
    public float readyTime = 0.4f;
    public float recoveryTime = 0.6f;

    [Header("Rules")]
    public bool requireSameFloor = true;
    public bool ignorePlayerZCheck = false;

    [Header("Hit")]
    public AttackHitBox hitBox;
    public AttackData attackData;

    [Header("On Hit Ailments")]
    public float poisonDuration = 0f;
    public float poisonDamagePerSecond = 0f;
    public float burnDuration = 0f;
    public float burnTickInterval = 0f;
    public float burnDamagePerTick = 0f;

    private IEnemyMovement movement;
    private EnemyCrowdControl crowdControl;
    private PlayerMove playerMove;
    private EnemyAttackTelegraph telegraph;

    private float timer = 0f;
    private bool isAttacked = false;
    private Vector2 lockedAttackDirection = Vector2.down;
    private float lockedAttackAngle = 180f;

    public bool isAttacking = false;
    public bool IsBusy => isAttacking;
    public bool CanAttack => AttackRange();

    private void Start()
    {
        movement = GetComponent<IEnemyMovement>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        playerMove = GameObject.Find("Player")?.GetComponent<PlayerMove>();
        telegraph = GetComponent<EnemyAttackTelegraph>();
        if (telegraph == null)
            telegraph = gameObject.AddComponent<EnemyAttackTelegraph>();
    }

    private void Update()
    {
        if (crowdControl != null && crowdControl.ShouldCancelCurrentAttack)
        {
            ResetAttackState();
            return;
        }

        if (!isAttacking && crowdControl != null && !crowdControl.CanAttack)
            return;

        if (CanAttack && !isAttacking)
        {
            CacheAttackDirection();
            isAttacking = true;
        }

        if (!isAttacking)
            return;

        timer += Time.deltaTime;

        if (!isAttacked)
            UpdateTelegraph();

        if (!isAttacked && timer >= readyTime)
        {
            isAttacked = true;
            telegraph?.Hide();
            Attack();
        }

        if (timer >= readyTime + recoveryTime)
            ResetAttackState();
    }

    public void Attack()
    {
        if (hitBox == null || attackData == null || movement == null)
            return;

        hitBox.EnemySetup(attackData, attackData.damage, attackData.attackDuration, transform.position, lockedAttackAngle, movement.FloorZ);
        hitBox.ConfigurePlayerZCheck(ignorePlayerZCheck);
        hitBox.ConfigurePoisonOnPlayerHit(poisonDuration, poisonDamagePerSecond);
        hitBox.ConfigureBurnOnPlayerHit(burnDuration, burnTickInterval, burnDamagePerTick);
    }

    private void UpdateTelegraph()
    {
        if (telegraph == null || attackData == null || movement == null)
            return;

        telegraph.ShowHitBoxTelegraph(transform.position, attackData, lockedAttackAngle, movement.FloorZ);
    }

    private bool AttackRange()
    {
        if (playerMove == null || movement == null)
            return false;

        if (crowdControl != null && !crowdControl.CanAttack)
            return false;

        if (requireSameFloor && playerMove.floorZ != movement.FloorZ)
            return false;

        if (isAttacking)
            return false;

        return Vector2.Distance(playerMove.transform.position, transform.position) <= range;
    }

    private void ResetAttackState()
    {
        telegraph?.Hide();
        timer = 0f;
        isAttacking = false;
        isAttacked = false;
        lockedAttackDirection = Vector2.down;
        lockedAttackAngle = 180f;
    }

    private void CacheAttackDirection()
    {
        if (playerMove == null)
            return;

        Vector2 direction = playerMove.transform.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        lockedAttackDirection = direction.normalized;
        lockedAttackAngle = Mathf.Atan2(lockedAttackDirection.y, lockedAttackDirection.x) * Mathf.Rad2Deg - 90f;
    }
}
