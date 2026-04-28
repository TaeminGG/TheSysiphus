using UnityEngine;

public class EnemyProjectileAttack : MonoBehaviour, IEnemyAttack
{
    [Header("Timing")]
    public float range = 4.5f;
    public float readyTime = 0.5f;
    public float recoveryTime = 0.7f;

    [Header("Rules")]
    public bool requireCasterFloorAtLeastTargetFloor = false;

    [Header("Projectile")]
    public float rangedStartHeightOffset = 1f;
    public float rangedTargetHeightOffset = 0f;
    public GameObject projectilePrefab;
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
    private bool isShotFired = false;
    private Vector2 lockedAttackDirection = Vector2.down;
    private float lockedAttackAngle = 180f;
    private float lockedTargetZ = 0f;
    private float lockedTargetDistance = 0f;
    private float lockedStartZ = 0f;

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
            CacheShotData();
            isAttacking = true;
        }

        if (!isAttacking)
            return;

        timer += Time.deltaTime;

        if (!isShotFired)
            UpdateTelegraph();

        if (!isShotFired && timer >= readyTime)
        {
            isShotFired = true;
            telegraph?.Hide();
            Attack();
        }

        if (timer >= readyTime + recoveryTime)
            ResetAttackState();
    }

    public void Attack()
    {
        if (projectilePrefab == null || attackData == null || movement == null)
            return;

        GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0f, 0f, lockedAttackAngle));

        if (projectileObject.TryGetComponent(out Projectile projectile))
        {
            projectile.SetFloorZ(movement.FloorZ);
            projectile.SetProjectileStartZ(lockedStartZ);
            projectile.ConfigureHeightShot(lockedTargetZ, lockedTargetDistance);
            projectile.EnableGroundBlocking(true);
        }

        if (projectileObject.TryGetComponent(out AttackHitBox hitBox))
        {
            hitBox.EnemySetup(attackData, attackData.damage, attackData.attackDuration, transform.position, lockedAttackAngle, lockedStartZ);
            hitBox.ConfigurePoisonOnPlayerHit(poisonDuration, poisonDamagePerSecond);
            hitBox.ConfigureBurnOnPlayerHit(burnDuration, burnTickInterval, burnDamagePerTick);
        }
    }

    private void UpdateTelegraph()
    {
        if (telegraph == null || projectilePrefab == null || movement == null)
            return;

        Projectile projectileComponent = projectilePrefab.GetComponent<Projectile>();
        if (projectileComponent == null)
            return;

        float previewDistance = projectileComponent.GetPreviewTravelDistance(
            transform.position,
            lockedAttackDirection,
            lockedStartZ,
            lockedTargetZ,
            lockedTargetDistance,
            0.02f);

        if (previewDistance <= 0.01f)
        {
            telegraph.Hide();
            return;
        }

        telegraph.ShowLineTelegraph(
            transform.position,
            lockedAttackDirection,
            previewDistance,
            movement.FloorZ,
            new Color(1f, 0.15f, 0.1f, 0.3f),
            lockedStartZ);
    }

    private bool AttackRange()
    {
        if (playerMove == null || movement == null)
            return false;

        if (crowdControl != null && !crowdControl.CanAttack)
            return false;

        if (isAttacking)
            return false;

        if (requireCasterFloorAtLeastTargetFloor && movement.FloorZ < playerMove.floorZ)
            return false;

        return Vector2.Distance(playerMove.transform.position, transform.position) <= range;
    }

    private void CacheShotData()
    {
        if (playerMove == null || movement == null)
            return;

        Vector2 direction = playerMove.transform.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        lockedAttackDirection = direction.normalized;
        lockedAttackAngle = Mathf.Atan2(lockedAttackDirection.y, lockedAttackDirection.x) * Mathf.Rad2Deg - 90f;
        lockedTargetZ = playerMove.playerZ + rangedTargetHeightOffset;
        lockedTargetDistance = Mathf.Max(0.01f, Vector2.Distance(transform.position, playerMove.transform.position));
        lockedStartZ = movement.FloorZ + rangedStartHeightOffset;
    }

    private void ResetAttackState()
    {
        telegraph?.Hide();
        timer = 0f;
        isAttacking = false;
        isShotFired = false;
        lockedAttackDirection = Vector2.down;
        lockedAttackAngle = 180f;
        lockedTargetZ = 0f;
        lockedTargetDistance = 0f;
        lockedStartZ = 0f;
    }
}
