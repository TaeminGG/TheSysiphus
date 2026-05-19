using UnityEngine;

public class DelayedGroundBlastAttack : MonoBehaviour, IEnemyAttack
{
    [Header("Timing")]
    public float range = 4f;
    public float readyTime = 1f;
    public float recoveryTime = 0.5f;

    [Header("Blast")]
    public float telegraphRadius = 1.2f;
    public GameObject hitBoxPrefab;
    public AttackData attackData;
    public Color telegraphColor = new Color(1f, 0.4f, 0.1f, 0.35f);

    private IEnemyMovement movement;
    private EnemyCrowdControl crowdControl;
    private PlayerMove playerMove;
    private EnemyAttackTelegraph telegraph;

    private float timer = 0f;
    private bool attackExecuted = false;
    private Vector3 lockedTargetPosition;
    private int lockedTargetFloorZ = 0;

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
            CacheBlastTarget();
            isAttacking = true;
        }

        if (!isAttacking)
            return;

        timer += Time.deltaTime;
        if (!attackExecuted)
            telegraph.ShowCircleTelegraph(lockedTargetPosition, telegraphRadius, lockedTargetFloorZ, telegraphColor);

        if (!attackExecuted && timer >= readyTime)
        {
            attackExecuted = true;
            telegraph?.Hide();
            Attack();
        }

        if (timer >= readyTime + recoveryTime)
            ResetAttackState();
    }

    public void Attack()
    {
        if (hitBoxPrefab == null || attackData == null)
            return;

        GameObject hitBoxObject = Instantiate(hitBoxPrefab, lockedTargetPosition, Quaternion.identity);
        if (hitBoxObject.TryGetComponent(out Projectile projectile))
        {
            projectile.SetFloorZ(lockedTargetFloorZ);
            projectile.SetProjectileStartZ(lockedTargetFloorZ);
        }

        if (hitBoxObject.TryGetComponent(out AttackHitBox hitBox))
            hitBox.EnemySetup(attackData, attackData.damage, attackData.attackDuration, lockedTargetPosition, 0f, lockedTargetFloorZ);
    }

    private bool AttackRange()
    {
        if (playerMove == null || movement == null)
            return false;

        if (crowdControl != null && !crowdControl.CanAttack)
            return false;

        if (isAttacking)
            return false;

        return Vector2.Distance(playerMove.transform.position, transform.position) <= range;
    }

    private void CacheBlastTarget()
    {
        if (playerMove == null)
            return;

        lockedTargetPosition = playerMove.transform.position;
        lockedTargetFloorZ = playerMove.floorZ;
    }

    private void ResetAttackState()
    {
        telegraph?.Hide();
        timer = 0f;
        attackExecuted = false;
        isAttacking = false;
        lockedTargetPosition = Vector3.zero;
        lockedTargetFloorZ = 0;
    }
}
