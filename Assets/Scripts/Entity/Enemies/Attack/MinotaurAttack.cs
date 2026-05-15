using UnityEngine;

public enum MinotaurAttackMode
{
    None,
    Earthquake,
    Combo,
    Charge
}

public class MinotaurAttack : MonoBehaviour, IEnemyAttack
{
    private static readonly int SkillHash = Animator.StringToHash("Skill");
    private static readonly int MeleeAttackHash = Animator.StringToHash("MeleeAttack");
    private static readonly int MeleeAttackStateHash = Animator.StringToHash("MeleeAttack");
    private static readonly int ChargeHash = Animator.StringToHash("Charge");
    private static readonly int AttackEndedHash = Animator.StringToHash("AttackEnded");

    [Header("Ranges")]
    public float earthquakeRange = 2.5f;
    public float comboRange = 1.1f;
    public float chargeRange = 4f;

    [Header("Earthquake")]
    public float earthquakeReadyTime = 0.8f;
    public float earthquakeRecoveryTime = 0.8f;
    public float earthquakeCoolTime = 5f;
    public float earthquakeJumpForce = 6f;
    public float earthquakeEffectRadius = 1.8f;
    public GameObject earthquakeHitBoxPrefab;
    public AttackData earthquakeAttackData;
    public Color earthquakeTelegraphColor = new Color(1f, 0.55f, 0.1f, 0.35f);

    [Header("Combo")]
    public float comboMoveDistance = 0.7f;
    public float comboMoveSpeed = 5f;
    public float comboHitDelay = 0.1f;
    public float comboHitRecovery = 0.2f;
    public AttackHitBox comboHitBox;
    public AttackData comboFirstAttackData;
    public AttackData comboSecondAttackData;

    [Header("Charge")]
    public float chargeReadyTime = 0.5f;
    public float chargeCoolTime = 3f;
    public float chargeDistance = 3f;
    public float chargeSpeed = 7f;
    public float chargeRecoveryTime = 0.5f;
    public float chargeRadius = 1f;
    public GameObject chargeHitBoxPrefab;
    public AttackData chargeAttackData;

    private MinotaurMovement movement;
    private GroundedEnemy groundedEnemy;
    private EnemyCrowdControl crowdControl;
    private PlayerMove playerMove;
    private EnemyAttackTelegraph telegraph;
    private Animator animator;

    private MinotaurAttackMode currentMode = MinotaurAttackMode.None;
    private float timer = 0f;
    private float earthquakeCooldownTimer = 0f;
    private float chargeCooldownTimer = 0f;
    private int comboStep = 0;
    private bool modeExecuted = false;
    private Vector2 lockedDirection = Vector2.down;
    private float lockedAngle = 180f;
    private float chargeRemainingDistance = 0f;
    private float chargeDuration = 0f;
    private float comboRemainingMoveDistance = 0f;
    private AttackHitBox activeChargeHitBox;
    private GameObject activeChargeHitBoxObject;

    public bool IsBusy => currentMode != MinotaurAttackMode.None;
    public bool CanAttack => SelectAttackMode() != MinotaurAttackMode.None;

    private void Start()
    {
        movement = GetComponent<MinotaurMovement>();
        groundedEnemy = GetComponent<GroundedEnemy>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        playerMove = GameObject.Find("Player")?.GetComponent<PlayerMove>();
        telegraph = GetComponent<EnemyAttackTelegraph>();
        if (telegraph == null)
            telegraph = gameObject.AddComponent<EnemyAttackTelegraph>();

        if (movement != null && movement.sprite != null)
            animator = movement.sprite.GetComponent<Animator>();
    }

    private void Update()
    {
        if (earthquakeCooldownTimer > 0f)
            earthquakeCooldownTimer -= Time.deltaTime;

        if (chargeCooldownTimer > 0f)
            chargeCooldownTimer -= Time.deltaTime;

        if (crowdControl != null && crowdControl.ShouldCancelCurrentAttack)
        {
            ResetAttackState();
            return;
        }

        if (!IsBusy && crowdControl != null && !crowdControl.CanAttack)
            return;

        if (!IsBusy)
        {
            TryStartAttack();
            return;
        }

        timer += Time.deltaTime;

        switch (currentMode)
        {
            case MinotaurAttackMode.Earthquake:
                UpdateEarthquake();
                break;
            case MinotaurAttackMode.Combo:
                UpdateCombo();
                break;
            case MinotaurAttackMode.Charge:
                UpdateCharge();
                break;
        }
    }

    public void Attack()
    {
        TryStartAttack();
    }

    private void TryStartAttack()
    {
        if (movement == null || playerMove == null)
            return;

        currentMode = SelectAttackMode();
        if (currentMode == MinotaurAttackMode.None)
            return;

        timer = 0f;
        modeExecuted = false;
        comboStep = 0;
        CacheDirection();

        if (currentMode == MinotaurAttackMode.Charge)
        {
            chargeRemainingDistance = chargeDistance;
            chargeDuration = chargeDistance / Mathf.Max(0.01f, chargeSpeed);
        }
    }

    private MinotaurAttackMode SelectAttackMode()
    {
        if (playerMove == null || movement == null)
            return MinotaurAttackMode.None;

        float distance = Vector2.Distance(playerMove.transform.position, transform.position);
        bool sameFloor = playerMove.floorZ == movement.floorZ;
        bool earthquakeReady = earthquakeCooldownTimer <= 0f;
        bool chargeReady = chargeCooldownTimer <= 0f;

        if (earthquakeReady && distance <= earthquakeRange)
            return MinotaurAttackMode.Earthquake;

        if (sameFloor && distance <= comboRange)
            return MinotaurAttackMode.Combo;

        if (chargeReady && sameFloor && distance <= chargeRange && distance > comboRange)
            return MinotaurAttackMode.Charge;

        return MinotaurAttackMode.None;
    }

    private void UpdateEarthquake()
    {
        if (!modeExecuted)
            telegraph.ShowCircleTelegraph(transform.position, earthquakeEffectRadius, movement.FloorZ, earthquakeTelegraphColor);

        if (!modeExecuted && timer >= earthquakeReadyTime)
        {
            modeExecuted = true;
            telegraph?.Hide();
            ExecuteEarthquake();
        }

        if (timer >= earthquakeReadyTime + earthquakeRecoveryTime)
            ResetAttackState();
    }

    private void ExecuteEarthquake()
    {
        PlayAttackAnimation(MinotaurAttackMode.Earthquake);
        earthquakeCooldownTimer = earthquakeCoolTime;

        if (earthquakeHitBoxPrefab != null && earthquakeAttackData != null)
        {
            GameObject hitBoxObject = Instantiate(earthquakeHitBoxPrefab, transform.position, Quaternion.identity);
            if (hitBoxObject.TryGetComponent(out TemporaryAttackLifetime lifetime))
                lifetime.Begin(earthquakeAttackData.attackDuration + 0.05f);
            if (hitBoxObject.TryGetComponent(out AttackHitBox hitBox))
            {
                hitBox.EnemySetup(earthquakeAttackData, earthquakeAttackData.damage, earthquakeAttackData.attackDuration, transform.position, 0f, movement.FloorZ);
                ApplyEarthquakeEffectRadius(hitBoxObject);
            }
        }

        if (playerMove == null || earthquakeAttackData == null)
            return;

        if (CanAffectPlayerWithEarthquake())
            playerMove.ForceJump(earthquakeJumpForce);
    }

    private void UpdateCombo()
    {
        if (comboHitBox == null)
        {
            ResetAttackState();
            return;
        }

        if (comboStep == 0)
        {
            if (!TryTelegraphComboStrike(0, 1))
                ResetAttackState();
            return;
        }

        if (comboStep == 1)
        {
            UpdateComboStrikeMovement(2);
            return;
        }

        if (comboStep == 2 && timer >= comboHitRecovery)
        {
            comboStep = 3;
            timer = 0f;
            return;
        }

        if (comboStep == 3)
        {
            if (!TryTelegraphComboStrike(1, 4))
                ResetAttackState();
            return;
        }

        if (comboStep == 4)
        {
            UpdateComboStrikeMovement(5);
            return;
        }

        if (comboStep == 5 && timer >= comboHitRecovery)
            ResetAttackState();
    }

    private void UpdateCharge()
    {
        if (!modeExecuted)
        {
            telegraph.ShowLineTelegraph(transform.position, lockedDirection, chargeDistance, movement.FloorZ, new Color(1f, 0.25f, 0.1f, 0.35f));

            if (timer >= chargeReadyTime)
            {
                modeExecuted = true;
                chargeCooldownTimer = chargeCoolTime;
                telegraph.Hide();
                StartChargeHitBox();
            }

            return;
        }

        if (chargeRemainingDistance > 0f)
        {
            float moveStep = Mathf.Min(chargeRemainingDistance, chargeSpeed * Time.deltaTime);
            float movedDistance = TryMoveAlongDirection(moveStep);
            if (movedDistance > 0f)
                movement?.SyncTargetToCurrentPosition();
            chargeRemainingDistance -= movedDistance;

            if (movedDistance <= 0.0001f)
                chargeRemainingDistance = 0f;
            return;
        }

        if (timer >= chargeReadyTime + chargeDuration + chargeRecoveryTime)
            ResetAttackState();
    }

    private void StartChargeHitBox()
    {
        PlayAttackAnimation(MinotaurAttackMode.Charge);

        if (chargeHitBoxPrefab == null || chargeAttackData == null)
            return;

        float duration = Mathf.Max(0.05f, chargeDuration);
        GameObject hitBoxObject = Instantiate(chargeHitBoxPrefab, transform.position, Quaternion.identity, transform);
        activeChargeHitBoxObject = hitBoxObject;
        if (hitBoxObject.TryGetComponent(out TemporaryAttackLifetime lifetime))
            lifetime.Begin(duration + 0.05f);
        if (!hitBoxObject.TryGetComponent(out activeChargeHitBox))
            return;

        activeChargeHitBox.EnemySetup(chargeAttackData, chargeAttackData.damage, duration, transform.position, lockedAngle, movement.FloorZ);
        activeChargeHitBox.transform.localPosition = Vector3.zero;
    }

    private float TryMoveAlongDirection(float distance)
    {
        if (groundedEnemy == null || movement == null || distance <= 0f)
            return 0f;

        float remainingDistance = distance;
        float movedDistance = 0f;
        const float stepDistance = 0.05f;

        while (remainingDistance > 0.001f)
        {
            float currentStep = Mathf.Min(stepDistance, remainingDistance);
            Vector3 nextPosition = transform.position + (Vector3)(lockedDirection * currentStep);

            if (!CanMoveTo(nextPosition))
                break;

            transform.position = nextPosition;
            movedDistance += currentStep;
            remainingDistance -= currentStep;
        }

        return movedDistance;
    }

    private bool CanMoveTo(Vector3 nextPosition)
    {
        if (movement.FloorZ == 0)
            return groundedEnemy.GetTileValid(nextPosition);

        Vector3Int cellPos = groundedEnemy.GetGridPosition(nextPosition);
        int nextFloorZ = groundedEnemy.GetGroundZLevel(cellPos) + 2;
        return nextFloorZ == movement.FloorZ;
    }

    private void CacheDirection()
    {
        if (playerMove == null)
            return;

        Vector2 direction = playerMove.transform.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        lockedDirection = direction.normalized;
        lockedAngle = Mathf.Atan2(lockedDirection.y, lockedDirection.x) * Mathf.Rad2Deg - 90f;
    }

    private AttackData GetComboAttackData(int comboIndex)
    {
        if (comboIndex <= 0)
            return comboFirstAttackData != null ? comboFirstAttackData : comboSecondAttackData;

        return comboSecondAttackData != null ? comboSecondAttackData : comboFirstAttackData;
    }

    private bool TryTelegraphComboStrike(int comboIndex, int nextStep)
    {
        AttackData attackData = GetComboAttackData(comboIndex);
        if (attackData == null)
            return false;

        float previewMoveDistance = PreviewMoveDistanceAlongDirection(comboMoveDistance);
        telegraph.ShowSweptHitBoxTelegraph(transform.position, attackData, lockedAngle, previewMoveDistance, movement.FloorZ);

        if (timer < comboHitDelay)
            return true;

        float movementDuration = previewMoveDistance / Mathf.Max(0.01f, comboMoveSpeed);
        float hitBoxDuration = Mathf.Max(attackData.attackDuration, movementDuration);
        PlayAttackAnimation(MinotaurAttackMode.Combo);
        comboHitBox.EnemySetup(attackData, attackData.damage, hitBoxDuration, transform.position, lockedAngle, movement.FloorZ);
        comboRemainingMoveDistance = previewMoveDistance;
        telegraph.Hide();
        comboStep = nextStep;
        timer = 0f;
        return true;
    }

    private void UpdateComboStrikeMovement(int nextStep)
    {
        float moveStep = Mathf.Min(comboRemainingMoveDistance, comboMoveSpeed * Time.deltaTime);
        float movedDistance = TryMoveAlongDirection(moveStep);
        if (movedDistance > 0f)
            movement?.SyncTargetToCurrentPosition();

        comboRemainingMoveDistance -= movedDistance;
        if (movedDistance <= 0.0001f || comboRemainingMoveDistance <= 0.0001f)
        {
            comboRemainingMoveDistance = 0f;
            comboStep = nextStep;
            timer = 0f;
        }
    }

    private float PreviewMoveDistanceAlongDirection(float distance)
    {
        if (groundedEnemy == null || movement == null || distance <= 0f)
            return 0f;

        float remainingDistance = distance;
        float movedDistance = 0f;
        const float stepDistance = 0.05f;
        Vector3 previewPosition = transform.position;

        while (remainingDistance > 0.001f)
        {
            float currentStep = Mathf.Min(stepDistance, remainingDistance);
            Vector3 nextPosition = previewPosition + (Vector3)(lockedDirection * currentStep);

            if (!CanMoveTo(nextPosition))
                break;

            previewPosition = nextPosition;
            movedDistance += currentStep;
            remainingDistance -= currentStep;
        }

        return movedDistance;
    }

    private void ApplyEarthquakeEffectRadius(GameObject hitBoxObject)
    {
        float clampedRadius = Mathf.Max(0.05f, earthquakeEffectRadius);

        if (hitBoxObject.TryGetComponent(out CircleCollider2D circle))
        {
            circle.radius = clampedRadius;
            circle.offset = Vector2.zero;
            return;
        }

        if (hitBoxObject.TryGetComponent(out BoxCollider2D box))
        {
            float diameter = clampedRadius * 2f;
            box.size = new Vector2(diameter, diameter);
            box.offset = Vector2.zero;
        }
    }

    private bool CanAffectPlayerWithEarthquake()
    {
        if (playerMove == null || earthquakeAttackData == null || movement == null)
            return false;

        if (Vector2.Distance(playerMove.transform.position, transform.position) > earthquakeEffectRadius + 0.01f)
            return false;

        float playerZ = playerMove.playerZ;
        float floorZ = movement.FloorZ;
        return floorZ + earthquakeAttackData.zUpperRange >= playerZ
            && floorZ - earthquakeAttackData.zLowerRange <= playerZ;
    }

    private void ResetAttackState()
    {
        telegraph?.Hide();

        if (activeChargeHitBox != null)
            activeChargeHitBox.DisableHitBox();

        if (activeChargeHitBoxObject != null)
            Destroy(activeChargeHitBoxObject);

        if (comboHitBox != null)
            comboHitBox.DisableHitBox();

        movement?.SyncTargetToCurrentPosition();

        currentMode = MinotaurAttackMode.None;
        timer = 0f;
        comboStep = 0;
        modeExecuted = false;
        comboRemainingMoveDistance = 0f;
        chargeRemainingDistance = 0f;
        chargeDuration = 0f;
        activeChargeHitBox = null;
        activeChargeHitBoxObject = null;
        lockedDirection = Vector2.down;
        lockedAngle = 180f;

        if (animator != null)
            animator.SetTrigger(AttackEndedHash);
    }

    private void PlayAttackAnimation(MinotaurAttackMode mode)
    {
        if (animator == null)
            return;

        switch (mode)
        {
            case MinotaurAttackMode.Earthquake:
                animator.SetTrigger(SkillHash);
                break;
            case MinotaurAttackMode.Combo:
                animator.ResetTrigger(MeleeAttackHash);
                animator.SetTrigger(MeleeAttackHash);
                animator.Play(MeleeAttackStateHash, 0, 0f);
                break;
            case MinotaurAttackMode.Charge:
                animator.SetTrigger(ChargeHash);
                break;
        }
    }
}
