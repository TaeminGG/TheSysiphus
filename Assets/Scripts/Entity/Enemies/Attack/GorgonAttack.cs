using UnityEngine;

public enum GorgonAttackMode
{
    None,
    PetrifyingGaze,
    Melee,
    Ranged
}

public class GorgonAttack : MonoBehaviour, IEnemyAttack
{
    [Header("Ranges")]
    public float skillTriggerRange = 2.25f;
    public float skillApplyRange = 1.75f;
    public float meleeRange = 0.75f;
    public float rangedRange = 4.5f;

    [Header("Petrifying Gaze")]
    public float gazeReadyTime = 0.8f;
    public float gazeRecoveryTime = 0.7f;
    public float gazeCoolTime = 4f;
    public float petrifyDuration = 1.5f;
    [Range(-1f, 1f)] public float gazeFacingDotThreshold = 0.55f;
    public Color skillTelegraphColor = new Color(0.45f, 0.9f, 0.45f, 0.3f);

    [Header("Melee")]
    public float meleeReadyTime = 0.35f;
    public float meleeRecoveryTime = 0.45f;
    public AttackHitBox meleeHitBox;
    public AttackData meleeAttackData;

    [Header("Ranged")]
    public float rangedReadyTime = 0.5f;
    public float rangedRecoveryTime = 0.7f;
    public float rangedStartHeightOffset = 1f;
    public float rangedTargetHeightOffset = 0f;
    public GameObject rangedProjectile;
    public AttackData rangedAttackData;
    public float rangedPoisonDuration = 3f;
    public float rangedPoisonDamagePerSecond = 1f;

    private GorgonMovement movement;
    private EnemyCrowdControl crowdControl;
    private PlayerMove playerMove;
    private PlayerStats playerStats;
    private EnemyAttackTelegraph telegraph;

    private GorgonAttackMode currentMode = GorgonAttackMode.None;
    private float currentTimer = 0f;
    private float currentReadyTime = 0f;
    private float currentTotalDuration = 0f;
    private float gazeCooldownTimer = 0f;
    private bool attackExecuted = false;
    private Vector2 lockedAttackDirection = Vector2.down;
    private float lockedAttackAngle = 180f;
    private float lockedRangedTargetZ = 0f;
    private float lockedRangedTargetDistance = 0f;
    private float lockedRangedStartZ = 0f;

    public bool IsBusy => currentMode != GorgonAttackMode.None;
    public bool CanAttack => movement != null
        && SelectAttackMode(GetPlayerDistance(), IsSameFloorAsPlayer(), gazeCooldownTimer <= 0f, skillTriggerRange, meleeRange, rangedRange) != GorgonAttackMode.None;

    private void Start()
    {
        movement = GetComponent<GorgonMovement>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        playerMove = GameObject.Find("Player").GetComponent<PlayerMove>();
        playerStats = playerMove.GetComponent<PlayerStats>();
        telegraph = GetComponent<EnemyAttackTelegraph>();
        if (telegraph == null)
            telegraph = gameObject.AddComponent<EnemyAttackTelegraph>();
    }

    private void Update()
    {
        if (gazeCooldownTimer > 0f)
            gazeCooldownTimer -= Time.deltaTime;

        if (crowdControl != null && crowdControl.ShouldCancelCurrentAttack)
        {
            ResetAttackState();
            return;
        }

        if (!IsBusy && crowdControl != null && !crowdControl.CanAttack)
            return;

        if (!IsBusy)
        {
            if (!movement.isDetected || playerMove == null)
                return;

            TryStartAttack();
            return;
        }

        currentTimer += Time.deltaTime;

        if (!attackExecuted)
            UpdateTelegraph();

        if (!attackExecuted && currentTimer >= currentReadyTime)
        {
            attackExecuted = true;
            telegraph?.Hide();
            ExecuteCurrentAttack();
        }

        if (currentTimer >= currentTotalDuration)
            ResetAttackState();
    }

    public void Attack()
    {
        TryStartAttack();
    }

    public static GorgonAttackMode SelectAttackMode(float distance, bool sameFloor, bool skillReady, float skillRangeValue, float meleeRangeValue, float rangedRangeValue)
    {
        if (skillReady && distance <= skillRangeValue)
            return GorgonAttackMode.PetrifyingGaze;

        if (sameFloor && distance <= meleeRangeValue)
            return GorgonAttackMode.Melee;

        bool outsideMeleeWindow = distance > meleeRangeValue || !sameFloor;
        if (outsideMeleeWindow && distance <= rangedRangeValue)
            return GorgonAttackMode.Ranged;

        return GorgonAttackMode.None;
    }

    public static bool IsFacingTarget(Vector2 facingDirection, Vector2 targetDirection, float dotThreshold)
    {
        if (targetDirection.sqrMagnitude <= 0.001f)
            return true;

        if (facingDirection.sqrMagnitude <= 0.001f)
            return false;

        return Vector2.Dot(facingDirection.normalized, targetDirection.normalized) >= dotThreshold;
    }

    public static bool CanApplyPetrify(float distance, float applyRange)
    {
        return distance <= applyRange;
    }

    private void TryStartAttack()
    {
        GorgonAttackMode nextMode = SelectAttackMode(
            GetPlayerDistance(),
            IsSameFloorAsPlayer(),
            gazeCooldownTimer <= 0f,
            skillTriggerRange,
            meleeRange,
            rangedRange);

        if (nextMode == GorgonAttackMode.None)
            return;

        currentMode = nextMode;
        currentTimer = 0f;
        attackExecuted = false;
        CacheAttackDirection();

        switch (currentMode)
        {
            case GorgonAttackMode.PetrifyingGaze:
                currentReadyTime = gazeReadyTime;
                currentTotalDuration = gazeReadyTime + gazeRecoveryTime;
                break;
            case GorgonAttackMode.Melee:
                currentReadyTime = meleeReadyTime;
                currentTotalDuration = meleeReadyTime + meleeRecoveryTime;
                break;
            case GorgonAttackMode.Ranged:
                currentReadyTime = rangedReadyTime;
                currentTotalDuration = rangedReadyTime + rangedRecoveryTime;
                CacheRangedShotData();
                break;
        }
    }

    private void ExecuteCurrentAttack()
    {
        switch (currentMode)
        {
            case GorgonAttackMode.PetrifyingGaze:
                gazeCooldownTimer = gazeCoolTime;
                TryApplyPetrify();
                break;
            case GorgonAttackMode.Melee:
                FireMeleeAttack();
                break;
            case GorgonAttackMode.Ranged:
                FireRangedAttack();
                break;
        }
    }

    private void UpdateTelegraph()
    {
        if (telegraph == null || playerMove == null || movement == null)
            return;

        int currentFloorZ = movement.floorZ;

        switch (currentMode)
        {
            case GorgonAttackMode.PetrifyingGaze:
                telegraph.ShowCircleTelegraph(transform.position, skillApplyRange, currentFloorZ, skillTelegraphColor);
                break;
            case GorgonAttackMode.Melee:
                telegraph.ShowHitBoxTelegraph(
                    transform.position,
                    meleeAttackData,
                    lockedAttackAngle,
                    currentFloorZ);
                break;
            case GorgonAttackMode.Ranged:
                if (rangedProjectile != null)
                {
                    Projectile projectileComponent = rangedProjectile.GetComponent<Projectile>();
                    if (projectileComponent != null)
                    {
                        float previewDistance = projectileComponent.GetPreviewTravelDistance(
                            transform.position,
                            lockedAttackDirection,
                            lockedRangedStartZ,
                            lockedRangedTargetZ,
                            lockedRangedTargetDistance,
                            0.02f);

                        if (previewDistance > 0.01f)
                            telegraph.ShowLineTelegraph(
                                transform.position,
                                lockedAttackDirection,
                                previewDistance,
                                currentFloorZ,
                                new Color(1f, 0.15f, 0.1f, 0.3f),
                                lockedRangedStartZ);
                    }
                }
                break;
        }
    }

    private void TryApplyPetrify()
    {
        if (playerMove == null || playerStats == null)
            return;

        if (!CanApplyPetrify(GetPlayerDistance(), skillApplyRange))
            return;

        Vector2 targetDirection = transform.position - playerMove.transform.position;
        bool isPlayerLookingAtGorgon = IsFacingTarget(playerMove.GetFacingDirection(), targetDirection, gazeFacingDotThreshold);

        if (isPlayerLookingAtGorgon)
            playerStats.ApplyPetrify(petrifyDuration);
    }

    private void FireMeleeAttack()
    {
        if (meleeHitBox == null || meleeAttackData == null || playerMove == null)
            return;

        meleeHitBox.EnemySetup(meleeAttackData, meleeAttackData.damage, meleeAttackData.attackDuration, transform.position, lockedAttackAngle, movement.floorZ);
    }

    private void FireRangedAttack()
    {
        if (rangedProjectile == null || rangedAttackData == null || playerMove == null)
            return;

        GameObject projectileObject = Instantiate(rangedProjectile, transform.position, Quaternion.Euler(0f, 0f, lockedAttackAngle));

        if (projectileObject.TryGetComponent(out Projectile projectile))
        {
            projectile.SetFloorZ(movement.floorZ);
            projectile.SetProjectileStartZ(lockedRangedStartZ);
            projectile.ConfigureHeightShot(lockedRangedTargetZ, lockedRangedTargetDistance);
            projectile.EnableGroundBlocking(true);
        }

        if (projectileObject.TryGetComponent(out AttackHitBox hitBox))
        {
            hitBox.EnemySetup(
                rangedAttackData,
                rangedAttackData.damage,
                rangedAttackData.attackDuration,
                transform.position,
                lockedAttackAngle,
                lockedRangedStartZ);
            hitBox.ConfigurePoisonOnPlayerHit(rangedPoisonDuration, rangedPoisonDamagePerSecond);
        }
    }

    private float GetPlayerDistance()
    {
        if (playerMove == null)
            return float.MaxValue;

        return Vector2.Distance(playerMove.transform.position, transform.position);
    }

    private bool IsSameFloorAsPlayer()
    {
        return playerMove != null && playerMove.floorZ == movement.floorZ;
    }

    private void ResetAttackState()
    {
        telegraph?.Hide();

        if (currentMode == GorgonAttackMode.Melee && meleeHitBox != null)
            meleeHitBox.DisableHitBox();

        currentMode = GorgonAttackMode.None;
        currentTimer = 0f;
        currentReadyTime = 0f;
        currentTotalDuration = 0f;
        attackExecuted = false;
        lockedAttackDirection = Vector2.down;
        lockedAttackAngle = 180f;
        lockedRangedTargetZ = 0f;
        lockedRangedTargetDistance = 0f;
        lockedRangedStartZ = 0f;
    }

    private void CacheAttackDirection()
    {
        if (currentMode == GorgonAttackMode.PetrifyingGaze || playerMove == null)
            return;

        Vector2 direction = playerMove.transform.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        lockedAttackDirection = direction.normalized;
        lockedAttackAngle = Mathf.Atan2(lockedAttackDirection.y, lockedAttackDirection.x) * Mathf.Rad2Deg - 90f;
    }

    private void CacheRangedShotData()
    {
        if (playerMove == null || movement == null)
            return;

        lockedRangedTargetZ = playerMove.playerZ + rangedTargetHeightOffset;
        lockedRangedTargetDistance = Mathf.Max(0.01f, Vector2.Distance(transform.position, playerMove.transform.position));
        lockedRangedStartZ = movement.floorZ + rangedStartHeightOffset;
    }
}
