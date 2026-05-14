using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDiveAttack : MonoBehaviour
{
    [Header("Attack Data")]
    [SerializeField] private AttackData fallingAttackData;
    [SerializeField] private AttackData landingShockwaveData;
    [SerializeField] private LayerMask enemyLayer = 0;

    [Header("Fall")]
    [Min(0.1f)]
    [SerializeField] private float fallSpeed = 18f;
    [Min(0f)]
    [SerializeField] private float minHeightAboveFloorToStart = 1f;
    [Min(0.01f)]
    [SerializeField] private float fallingDamageInterval = 0.08f;
    [Min(0.1f)]
    [SerializeField] private float maxDiveDuration = 3f;

    [Header("Recovery")]
    [Min(0f)]
    [SerializeField] private float postLandingRecoveryDuration = 0.25f;

    [Header("Attack Speed Scaling")]
    [Min(0f)]
    [SerializeField] private float fallSpeedAttackSpeedScale = 1f;
    [Min(0f)]
    [SerializeField] private float recoveryAttackSpeedScale = 1f;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector3 fallingVisualOffset = Vector3.zero;
    [SerializeField] private Vector3 landingVisualOffset = Vector3.zero;

    private readonly HashSet<Collider2D> fallingHitTargets = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> landingHitTargets = new HashSet<Collider2D>();

    private PlayerMove playerMove;
    private PlayerStats playerStats;
    private PlayerCrowdControl crowdControl;
    private PlayerDirectionalAnimator directionalAnimator;
    private HitSparkController hitSparkController;

    private bool hasUsedThisAirborne;
    private bool isDiving;
    private float fallingDamageTimer;
    private float diveTimer;
    private int controlLockToken;
    private float damageMultiplier = 1f;
    private float landingShockwaveRangeMultiplier = 1f;
    private Coroutine endDiveAnimationCoroutine;

    private PlayerControlLock DiveControlLocks =>
        PlayerControlLock.Move | PlayerControlLock.Attack | PlayerControlLock.Item | PlayerControlLock.Heal | PlayerControlLock.Dash;

    public event System.Action OnDiveStarted;
    public event System.Action OnDiveEnded;

    public bool IsDiving => isDiving;
    public bool HasUsedThisAirborne => hasUsedThisAirborne;
    public float HeightAboveFloor => GetHeightAboveFloor();
    public bool HasEnoughHeightToStartDive => GetHeightAboveFloor() >= Mathf.Max(0f, minHeightAboveFloorToStart);
    public float FallSpeed
    {
        get => fallSpeed;
        set => fallSpeed = Mathf.Max(0.1f, value);
    }

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        playerStats = GetComponent<PlayerStats>();
        directionalAnimator = GetComponent<PlayerDirectionalAnimator>();
        if (directionalAnimator == null)
            directionalAnimator = gameObject.AddComponent<PlayerDirectionalAnimator>();
        crowdControl = GetComponent<PlayerCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<PlayerCrowdControl>();
    }

    private void OnEnable()
    {
        if (playerMove != null)
            playerMove.OnLanded += HandleLanded;
    }

    private void OnDisable()
    {
        if (playerMove != null)
            playerMove.OnLanded -= HandleLanded;

        StopDiveAnimationEndRoutine();
        directionalAnimator?.EndDiveAttack();
        EndDive(false);
    }

    private void Update()
    {
        if (playerMove == null)
            return;

        if (!isDiving && playerMove.IsPlayerOnGround())
        {
            hasUsedThisAirborne = false;
            return;
        }

        if (!isDiving)
            return;

        diveTimer += Time.deltaTime;
        playerMove.BeginForcedFall(GetResolvedFallSpeed());

        fallingDamageTimer -= Time.deltaTime;
        if (fallingDamageTimer <= 0f)
        {
            fallingDamageTimer = fallingDamageInterval;
            ApplyAreaDamage(fallingAttackData, fallingHitTargets, playerMove.playerZ, transform.position);
        }

        if (playerMove.IsPlayerOnGround() || diveTimer >= maxDiveDuration)
            EndDive(playerMove.IsPlayerOnGround());
    }

    public bool TryStartDiveAttack()
    {
        if (!CanStartDiveAttack())
            return false;

        isDiving = true;
        hasUsedThisAirborne = true;
        diveTimer = 0f;
        fallingDamageTimer = 0f;
        fallingHitTargets.Clear();
        landingHitTargets.Clear();
        StopDiveAnimationEndRoutine();
        controlLockToken = crowdControl != null ? crowdControl.AddControlLock(DiveControlLocks) : 0;

        playerMove.BeginForcedFall(GetResolvedFallSpeed());
        directionalAnimator?.PlayDiveAttack(playerMove.GetFacingDirection());
        SpawnVisual(fallingAttackData, transform.position + fallingVisualOffset);
        OnDiveStarted?.Invoke();
        return true;
    }

    private bool CanStartDiveAttack()
    {
        if (isDiving || hasUsedThisAirborne)
            return false;

        if (playerMove == null || playerStats == null)
            return false;

        if (!playerMove.IsAirborne || playerMove.IsDashing)
            return false;

        if (!HasEnoughHeightToStartDive)
            return false;

        if (crowdControl != null && (!crowdControl.CanMove || !crowdControl.CanAttack || !crowdControl.CanDash))
            return false;

        return fallingAttackData != null || landingShockwaveData != null;
    }

    private void HandleLanded()
    {
        if (isDiving)
            EndDive(true);
    }

    private void EndDive(bool landed)
    {
        if (!isDiving && controlLockToken == 0)
            return;

        bool shouldApplyShockwave = isDiving && landed;
        isDiving = false;
        bool shouldNotifyDiveEnded = controlLockToken != 0;

        if (!landed)
            playerMove?.EndForcedFall();

        if (controlLockToken != 0 && crowdControl != null)
        {
            crowdControl.RemoveControlLock(controlLockToken);
            controlLockToken = 0;
        }

        if (shouldApplyShockwave)
        {
            Vector3 landingPosition = transform.position;
            ApplyAreaDamage(landingShockwaveData, landingHitTargets, playerMove != null ? playerMove.floorZ : 0f, landingPosition);
            SpawnVisual(landingShockwaveData, landingPosition + landingVisualOffset);
            float recoveryDuration = ApplyPostLandingRecovery();
            ScheduleDiveAnimationEnd(recoveryDuration);
        }
        else
        {
            EndDiveAnimation();
        }

        if (shouldNotifyDiveEnded)
            OnDiveEnded?.Invoke();
    }

    private float ApplyPostLandingRecovery()
    {
        float resolvedDuration = GetResolvedPostLandingRecoveryDuration();
        if (crowdControl == null || resolvedDuration <= 0f)
            return 0f;

        crowdControl.ApplyEffect(new PlayerCrowdControlRequest
        {
            Type = PlayerCrowdControlType.Custom,
            Duration = resolvedDuration,
            MoveSpeedMultiplier = 1f,
            Locks = PlayerControlLock.Move | PlayerControlLock.Attack | PlayerControlLock.Dash,
            Knockback = Vector2.zero
        });

        return resolvedDuration;
    }

    private void ScheduleDiveAnimationEnd(float delay)
    {
        StopDiveAnimationEndRoutine();

        if (delay <= 0f)
        {
            EndDiveAnimation();
            return;
        }

        endDiveAnimationCoroutine = StartCoroutine(EndDiveAnimationAfterDelay(delay));
    }

    private IEnumerator EndDiveAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        endDiveAnimationCoroutine = null;
        EndDiveAnimation();
    }

    private void EndDiveAnimation()
    {
        StopDiveAnimationEndRoutine();
        directionalAnimator?.EndDiveAttack();
    }

    private void StopDiveAnimationEndRoutine()
    {
        if (endDiveAnimationCoroutine == null)
            return;

        StopCoroutine(endDiveAnimationCoroutine);
        endDiveAnimationCoroutine = null;
    }

    public float GetResolvedFallSpeed()
    {
        return fallSpeed * GetAttackSpeedMultiplier(fallSpeedAttackSpeedScale);
    }

    public float GetResolvedPostLandingRecoveryDuration()
    {
        return postLandingRecoveryDuration / GetAttackSpeedMultiplier(recoveryAttackSpeedScale);
    }

    private float GetHeightAboveFloor()
    {
        if (playerMove == null)
            return 0f;

        return Mathf.Max(0f, playerMove.playerZ - playerMove.floorZ);
    }

    private float GetAttackSpeedMultiplier(float scale)
    {
        float attackSpeed = playerStats != null ? playerStats.attackSpeed : 1f;
        return Mathf.Max(0.01f, 1f + ((attackSpeed - 1f) * Mathf.Max(0f, scale)));
    }

    private void ApplyAreaDamage(AttackData attackData, HashSet<Collider2D> hitTargets, float attackZ, Vector3 origin)
    {
        if (attackData == null)
            return;

        LayerMask resolvedLayer = enemyLayer.value != 0 ? enemyLayer : LayerMask.GetMask("Enemies", "Enemy");
        bool isLandingShockwave = ReferenceEquals(attackData, landingShockwaveData);
        float radius = GetAttackRadius(attackData, isLandingShockwave);
        Vector2 center = origin + (Vector3)attackData.hitboxOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, resolvedLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hitTargets.Contains(hit))
                continue;

            if (!CanHitTargetAtZ(hit, attackData, attackZ))
                continue;

            if (!hit.TryGetComponent<IDamageable>(out var target))
                continue;

            hitTargets.Add(hit);
            ApplyDamageToTarget(hit, target, attackData, origin);
        }
    }

    private void ApplyDamageToTarget(Collider2D hit, IDamageable target, AttackData attackData, Vector3 origin)
    {
        float finalDamage = attackData.GetFinalDamage(playerStats != null ? playerStats.damage : 0f) * damageMultiplier;
        Vector2 knockbackDirection = hit.bounds.center - origin;
        if (knockbackDirection.sqrMagnitude <= 0.001f)
            knockbackDirection = Vector2.down;

        Vector2 knockback = knockbackDirection.normalized * attackData.knockbackForce;
        bool wasDead = target.IsDead;
        target.TakeDamage(finalDamage, knockback);
        if (!wasDead && target.IsDead)
            playerStats?.NotifyKill();

        PlayHitFeedback(attackData, hit, knockback.normalized);
    }

    private static bool CanHitTargetAtZ(Collider2D hit, AttackData attackData, float attackZ)
    {
        if (!hit.TryGetComponent<IEnemyMovement>(out var enemyMovement))
            return true;

        float targetZ = enemyMovement.FloorZ;
        return attackZ + attackData.zUpperRange >= targetZ
            && attackZ - attackData.zLowerRange <= targetZ;
    }

    private float GetAttackRadius(AttackData attackData, bool isLandingShockwave)
    {
        float multiplier = isLandingShockwave ? landingShockwaveRangeMultiplier : 1f;
        return Mathf.Max(0.05f, Mathf.Max(attackData.hitboxSize.x, attackData.hitboxSize.y) * 0.5f * multiplier);
    }

    public void AddDamageMultiplier(float percent)
    {
        damageMultiplier *= Mathf.Max(0.01f, 1f + percent);
    }

    public void RemoveDamageMultiplier(float percent)
    {
        damageMultiplier /= Mathf.Max(0.01f, 1f + percent);
    }

    public void AddLandingShockwaveRangeMultiplier(float percent)
    {
        landingShockwaveRangeMultiplier *= Mathf.Max(0.01f, 1f + percent);
    }

    public void RemoveLandingShockwaveRangeMultiplier(float percent)
    {
        landingShockwaveRangeMultiplier /= Mathf.Max(0.01f, 1f + percent);
    }

    private void PlayHitFeedback(AttackData attackData, Collider2D hit, Vector2 direction)
    {
        if (attackData.onHitCameraPreset != null)
            CameraFeedbackService.Instance?.Play(attackData.onHitCameraPreset);

        if (attackData.onHitStopDuration > 0f)
        {
            HitStopController.Instance?.Play(
                attackData.onHitStopDuration,
                attackData.onHitStopTimeScale,
                attackData.onHitStopPriority);
        }

        if (attackData.onHitSparkPrefab == null)
            return;

        if (hitSparkController == null)
            hitSparkController = FindFirstObjectByType<HitSparkController>();

        if (hitSparkController == null)
            return;

        HitSparkSettings settings = attackData.onHitSparkSettings;
        if (settings.direction.sqrMagnitude <= 0.001f)
            settings.direction = direction;

        hitSparkController.Play(attackData.onHitSparkPrefab, hit.bounds.center, settings);
    }

    private void SpawnVisual(AttackData attackData, Vector3 worldPosition)
    {
        if (attackData == null || attackData.visual == null)
            return;

        Transform parent = visualRoot != null ? visualRoot : null;
        GameObject visualObject = Instantiate(attackData.visual, worldPosition, Quaternion.identity, parent);
        float lifetime = Mathf.Max(0.05f, attackData.attackDuration);
        Destroy(visualObject, lifetime);
    }

    private void OnDiveAttack(InputValue value)
    {
        if (!value.isPressed)
            return;

        TryStartDiveAttack();
    }

    private void OnDestroy()
    {
        OnDiveStarted = null;
        OnDiveEnded = null;
    }
}
