using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private enum MovementMode
    {
        Normal,
        Knockback
    }

    private Vector2 inputAxis = Vector2.zero;
    private Vector3 axis;
    public Vector2Int targetAxis = Vector2Int.zero;

    private enum LookAt
    {
        E,
        W,
        S,
        N,
        NE,
        NW,
        SE,
        SW
    }
    private LookAt lookAt = LookAt.S;

    [Header("Movement Setting")]
    public float moveSpeed = 3.0f;
    private float moveSpeedSet = 0f;
    public float airResi = 0.5f;
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("Status")]
    public float playerZ = 0;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private bool isJumping = false;
    public int floorZ;
    public int groundedZ = 0;
    public float spritePivotY = 0.38f;

    [Header("Dash Setting")]
    public float dashForce = 5.0f;
    public float dashDuration = 0.5f;
    public float dashCooltime = 2f;
    private float dashTime = 0f;
    private float dashCooltimeCheck = 0f;

    private float spriteZ = 0f;

    private Rigidbody2D rid;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private SurroundTileCheck stc;
    private PlayerStats stats;
    private PlayerCrowdControl crowdControl;
    private PassiveItemList passiveItemList;
    private float yOffset = 0f;

    private int fixedZ;

    private bool isOnValidTile = false;

    [Header("Transform")]
    public Transform shadow;
    public Transform shadowSprite;
    public Transform playerSprite;

    private bool isAttacking = false;
    private float dashCooldownMultiplier = 1f;
    private float jumpForceMultiplier = 1f;

    private readonly List<IPlayerInteractable> interactables = new List<IPlayerInteractable>();
    private IPlayerInteractable currentInteractable;

    public event System.Action OnDashStarted;
    public event System.Action OnDashEnded;

    public bool IsDashing => dashTime < dashDuration;
    public Vector2 LastDashDirection { get; private set; } = Vector2.down;

    private void Awake()
    {
        rid = GetComponent<Rigidbody2D>();
        anim = playerSprite.GetComponent<Animator>();
        spriteRenderer = playerSprite.GetComponent<SpriteRenderer>();
        stc = GetComponent<SurroundTileCheck>();
        stats = GetComponent<PlayerStats>();
        crowdControl = GetComponent<PlayerCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<PlayerCrowdControl>();
        passiveItemList = GetComponent<PassiveItemList>();
    }

    private void Start()
    {
        moveSpeedSet = moveSpeed;

        floorZ = 0;
        dashTime = dashDuration;
        dashCooltimeCheck = GetResolvedDashCooldown();
    }

    private void Update()
    {
        RefreshCurrentInteractable();
    }

    private void FixedUpdate()
    {
        HandleJumpAndGravity();

        if (crowdControl != null && crowdControl.IsKnockbackActive)
        {
            EndDashIfActive();
            isAttacking = false;
            axis = crowdControl.GetKnockbackDirection();
            float movedDistance = HandleKnockbackMovement(crowdControl.GetKnockbackDirection(), crowdControl.GetKnockbackMoveSpeed());
            crowdControl.ConsumeKnockbackDistance(movedDistance);
        }
        else
        {
            PlayerMoveMent();
        }

        UpdatePosition();
    }

    public void AttackLookAt()
    {
        float angle = GetComponent<PlayerAttackDirection>().GetAttackAngle();

        if (-22.5f < angle || 22.5f >= angle)
        {
            axis = Vector2.right;
        }
        if (22.5f < angle && 67.5f >= angle)
        {
            axis = new Vector2(1, 1).normalized;
        }
        if (67.5f < angle && 112.5f >= angle)
        {
            axis = Vector2.up;
        }
        if (112.5f < angle && 157.5f >= angle)
        {
            axis = new Vector2(-1, 1).normalized;
        }
        if (157.5f < angle || -157.5f >= angle)
        {
            axis = Vector2.left;
        }
        if (-157.5f < angle && -112.5f >= angle)
        {
            axis = new Vector2(-1, -1).normalized;
        }
        if (-112.5f < angle && -67.5f >= angle)
        {
            axis = Vector2.down;
        }
        if (-67.5f < angle && -22.5f >= angle)
        {
            axis = new Vector2(1, -1).normalized;
        }
    }

    private void PlayerMoveMent()
    {
        bool isDashing = dashTime < dashDuration;
        if (isDashing)
        {
            dashTime += Time.deltaTime;
            dashCooltimeCheck += Time.deltaTime;
            if (dashTime >= dashDuration)
                EndDashIfActive();
        }
        else
        {
            if (GetComponent<AttackController>().isAttacking)
            {
                isAttacking = true;
            }
            else
            {
                axis = inputAxis;
                isAttacking = false;
            }
            moveSpeed = moveSpeedSet;
            dashCooltimeCheck += Time.deltaTime;

            if (axis.x < 0f)
            {
                if (axis.y < 0f)
                    lookAt = LookAt.SW;
                else if (axis.y > 0f)
                    lookAt = LookAt.NW;
                else if (axis.y == 0f)
                    lookAt = LookAt.W;
            }
            else if (axis.x > 0f)
            {
                if (axis.y < 0f)
                    lookAt = LookAt.SE;
                else if (axis.y > 0f)
                    lookAt = LookAt.NE;
                else if (axis.y == 0f)
                    lookAt = LookAt.E;
            }
            else
            {
                if (axis.y > 0f)
                    lookAt = LookAt.N;
                else if (axis.y < 0f)
                    lookAt = LookAt.S;
            }
        }

        if (lookAt == LookAt.S)
            anim.SetInteger("Look", 0);
        else if (lookAt == LookAt.SE || lookAt == LookAt.SW)
            anim.SetInteger("Look", 1);
        else if (lookAt == LookAt.E || lookAt == LookAt.W)
            anim.SetInteger("Look", 2);
        else if (lookAt == LookAt.NE || lookAt == LookAt.NW)
            anim.SetInteger("Look", 3);
        else if (lookAt == LookAt.N)
            anim.SetInteger("Look", 4);

        if (lookAt == LookAt.SW || lookAt == LookAt.NW || lookAt == LookAt.W)
            spriteRenderer.flipX = true;
        else
            spriteRenderer.flipX = false;

        if (isAttacking)
            axis = Vector2.zero;

        if (crowdControl != null && !crowdControl.CanMove)
        {
            EndDashIfActive();
            return;
        }

        float currentMoveSpeed = moveSpeedSet;
        if (isDashing)
            currentMoveSpeed += dashForce;

        if (!isDashing && crowdControl != null)
            currentMoveSpeed *= crowdControl.MoveSpeedMultiplier;

        if (!isDashing && (!isGrounded || playerZ > floorZ))
            currentMoveSpeed *= airResi;

        MoveCharacter(axis, currentMoveSpeed);
    }

    private float MoveCharacter(Vector2 moveAxis, float currentMoveSpeed)
    {
        return MoveCharacterDistance(moveAxis, currentMoveSpeed * Time.deltaTime, MovementMode.Normal);
    }

    private float MoveCharacterDistance(Vector2 moveAxis, float moveDistance, MovementMode movementMode)
    {
        if (moveAxis.sqrMagnitude <= 0.001f || moveDistance <= 0.001f)
            return 0f;

        stc.SyncToPosition(shadow.transform.position);

        Vector2 normalizedAxis = moveAxis.normalized;
        Vector3 startPos = shadow.transform.position;
        Vector3Int prevCell = stc.GetGridPosition(shadow.transform.position);
        int prevZ = floorZ;
        int targetZLevel = 0;
        Vector3 targetPos = shadow.transform.position + (Vector3)(normalizedAxis * moveDistance);
        int targetGroundZLevel = NormalizeFloorZ(stc.GetGroundZLevel(targetPos));

        bool isTileValid = stc.GetTileValid(normalizedAxis, moveDistance);

        if (movementMode == MovementMode.Knockback && IsKnockbackMovementBlocked(normalizedAxis, moveDistance, prevZ))
            return 0f;

        if (!isTileValid)
        {
            targetZLevel = stc.GetTileZLevelAnother(normalizedAxis, moveDistance);

            if (targetZLevel < 0)
                targetZLevel = 0;
            else
                targetZLevel += 2;

            if (targetZLevel <= playerZ)
            {
                transform.position = targetPos;
                isOnValidTile = false;

                if (prevZ > targetGroundZLevel)
                {
                    int fallGroundZ = stc.GetFallGroundZLevel(targetPos);
                    targetZLevel = (fallGroundZ < 0) ? 0 : fallGroundZ + 2;

                    if (prevCell != stc.GetGridPosition(shadow.transform.position) && ((fixedZ != targetZLevel) || fixedZ == -1))
                    {
                        if (prevZ > targetZLevel)
                        {
                            fixedZ = targetZLevel;
                            floorZ = targetZLevel <= 0 ? 0 : targetZLevel;

                            float diff = prevZ - targetZLevel;
                            targetPos.y -= diff * 0.25f;
                            spriteZ += diff;
                            transform.position = targetPos;
                        }
                    }
                }
                else if (prevCell != stc.GetGridPosition(shadow.transform.position) && ((fixedZ != targetZLevel) || fixedZ == -1))
                {
                    if (prevZ < targetZLevel)
                    {
                        fixedZ = targetZLevel;
                        floorZ = targetZLevel;

                        float diff = targetZLevel - prevZ;
                        targetPos.y += diff * 0.25f;
                        spriteZ -= diff;
                        transform.position = targetPos;
                    }
                }
            }
        }
        else
        {
            targetZLevel = stc.GetGroundZLevel(targetPos);

            if (targetZLevel < 0)
                targetZLevel = 0;
            else
                targetZLevel += 2;

            if (targetZLevel > prevZ)
            {
                targetZLevel = stc.GetGroundZLevel(targetPos, prevZ);
            }

            if (targetZLevel == floorZ)
            {
                transform.position = targetPos;
                isOnValidTile = true;
            }
            else
            {
                transform.position = targetPos;
                isOnValidTile = true;
                int fallGroundZ = stc.GetFallGroundZLevel(targetPos);
                targetZLevel = (fallGroundZ < 0) ? 0 : fallGroundZ + 2;

                if (prevCell != stc.GetGridPosition(shadow.transform.position) && ((fixedZ != targetZLevel) || fixedZ == -1))
                {
                    if (prevZ > targetZLevel)
                    {
                        fixedZ = targetZLevel;
                        floorZ = targetZLevel <= 0 ? 0 : targetZLevel;

                        float diff = prevZ - targetZLevel;
                        targetPos.y -= diff * 0.25f;
                        spriteZ += diff;
                        transform.position = targetPos;
                    }
                }
            }
        }

        return Vector2.Distance(new Vector2(startPos.x, startPos.y), new Vector2(transform.position.x, transform.position.y));
    }

    private float HandleKnockbackMovement(Vector2 knockbackDirection, float knockbackMoveSpeed)
    {
        if (knockbackDirection.sqrMagnitude <= 0.001f)
            return 0f;

        float remainingDistance = knockbackMoveSpeed * Time.deltaTime;
        float movedDistance = 0f;
        const float stepDistance = 0.05f;

        while (remainingDistance > 0.001f)
        {
            float currentStepDistance = Mathf.Min(stepDistance, remainingDistance);
            float movedStepDistance = MoveCharacterDistance(knockbackDirection, currentStepDistance, MovementMode.Knockback);

            if (movedStepDistance <= 0.0001f)
            {
                crowdControl?.ClearKnockback();
                break;
            }

            movedDistance += movedStepDistance;
            remainingDistance -= currentStepDistance;
        }

        return movedDistance;
    }

    private bool IsKnockbackMovementBlocked(Vector2 moveAxis, float moveDistance, int currentFloorZ)
    {
        stc.SyncToPosition(shadow.transform.position);

        bool isTileValid = stc.GetTileValid(moveAxis, moveDistance);
        Vector3 targetPos = shadow.transform.position + (Vector3)(moveAxis.normalized * moveDistance);

        if (isTileValid)
        {
            int reachableFloorZ = GetReachableFloorZ(targetPos, currentFloorZ);
            return reachableFloorZ > currentFloorZ;
        }

        int blockingTileFloorZ = NormalizeFloorZ(stc.GetTileZLevelAnother(moveAxis, moveDistance));
        if (blockingTileFloorZ >= currentFloorZ)
            return true;

        int fallFloorZ = NormalizeFloorZ(stc.GetFallGroundZLevel(targetPos));
        return fallFloorZ > currentFloorZ;
    }

    private int GetReachableFloorZ(Vector3 targetPos, int currentFloorZ)
    {
        int targetFloorZ = NormalizeFloorZ(stc.GetGroundZLevel(targetPos));

        if (targetFloorZ > currentFloorZ)
            targetFloorZ = NormalizeFloorZ(stc.GetGroundZLevel(targetPos, currentFloorZ));

        return targetFloorZ;
    }

    private static int NormalizeFloorZ(int tileZLevel)
    {
        if (tileZLevel < 0)
            return 0;

        return tileZLevel + 2;
    }

    private void UpdatePosition()
    {
        spriteZ += verticalVelocity * Time.deltaTime;
        yOffset = spriteZ * 0.25f;

        if (playerZ <= floorZ)
        {
            yOffset = 0;
        }

        float correctedZ = playerZ + (spritePivotY - 0.18f) / 0.26f;

        playerSprite.parent.localPosition = new Vector3(0, spritePivotY + yOffset, 0);
        playerSprite.localPosition = new Vector3(0, 0, correctedZ);
        shadowSprite.localPosition = new Vector3(0, 0, floorZ);
    }

    private void HandleJumpAndGravity()
    {
        Vector3 pivotPos = new Vector3(shadow.position.x, shadow.position.y, 0);

        if (isJumping && isGrounded)
        {
            verticalVelocity = GetResolvedJumpForce();
            isGrounded = false;
            isJumping = false;
        }

        if (!isGrounded || playerZ > floorZ)
        {
            verticalVelocity += gravity * Time.deltaTime;
            isGrounded = false;
        }

        int getZ = stc.GetGroundZLevel(pivotPos);

        if (getZ < 0)
        {
            floorZ = 0;
        }
        else
        {
            if (playerZ > getZ + 2 && isGrounded)
            {
                if (!isOnValidTile || groundedZ >= getZ + 2)
                {
                    floorZ = getZ + 2;
                }
            }
        }

        playerZ += verticalVelocity * Time.deltaTime;

        if (playerZ <= floorZ)
        {
            if (!isGrounded)
            {
                isGrounded = true;
                groundedZ = floorZ;
                spriteZ = 0;
            }

            fixedZ = -1;
            playerZ = floorZ;
            moveSpeed = moveSpeedSet;
            verticalVelocity = 0;
        }
    }

    public bool IsPlayerOnGround()
    {
        return isGrounded;
    }

    public Vector2 GetFacingDirection()
    {
        return lookAt switch
        {
            LookAt.E => Vector2.right,
            LookAt.W => Vector2.left,
            LookAt.S => Vector2.down,
            LookAt.N => Vector2.up,
            LookAt.NE => new Vector2(1f, 1f).normalized,
            LookAt.NW => new Vector2(-1f, 1f).normalized,
            LookAt.SE => new Vector2(1f, -1f).normalized,
            LookAt.SW => new Vector2(-1f, -1f).normalized,
            _ => Vector2.down
        };
    }

    public bool IsFacingPosition(Vector3 worldPosition, float dotThreshold = 0.55f)
    {
        Vector2 toTarget = worldPosition - transform.position;
        if (toTarget.sqrMagnitude <= 0.001f)
            return true;

        return Vector2.Dot(GetFacingDirection(), toTarget.normalized) >= dotThreshold;
    }

    public void AddSpeed(float speed)
    {
        moveSpeed += speed;
        moveSpeedSet += speed;
    }

    public void ReduceSpeed(float speed)
    {
        moveSpeed -= speed;
        moveSpeedSet -= speed;
    }

    public void AddDashCooldownReduction(float percent)
    {
        dashCooldownMultiplier *= Mathf.Max(0.01f, 1f - percent);
    }

    public void RemoveDashCooldownReduction(float percent)
    {
        dashCooldownMultiplier /= Mathf.Max(0.01f, 1f - percent);
    }

    public float GetResolvedDashCooldown()
    {
        return dashCooltime * dashCooldownMultiplier;
    }

    public void AddJumpForceMultiplier(float percent)
    {
        jumpForceMultiplier = Mathf.Max(0.01f, jumpForceMultiplier + percent);
    }

    public void RemoveJumpForceMultiplier(float percent)
    {
        jumpForceMultiplier = Mathf.Max(0.01f, jumpForceMultiplier - percent);
    }

    public float GetResolvedJumpForce()
    {
        return jumpForce * jumpForceMultiplier;
    }

    public void RegisterInteractable(IPlayerInteractable interactable)
    {
        if (IsInteractableDestroyed(interactable) || interactables.Contains(interactable))
            return;

        interactables.Add(interactable);
        RefreshCurrentInteractable();
    }

    public void UnregisterInteractable(IPlayerInteractable interactable)
    {
        if (IsInteractableDestroyed(interactable))
            return;

        if (!interactables.Remove(interactable))
            return;

        if (ReferenceEquals(currentInteractable, interactable))
        {
            SetInteractableFocus(currentInteractable, false);
            currentInteractable = null;
        }

        RefreshCurrentInteractable();
    }

    private void RefreshCurrentInteractable()
    {
        for (int i = interactables.Count - 1; i >= 0; i--)
        {
            if (!IsInteractableDestroyed(interactables[i]))
                continue;

            if (ReferenceEquals(currentInteractable, interactables[i]))
                currentInteractable = null;

            interactables.RemoveAt(i);
        }

        IPlayerInteractable bestInteractable = null;
        int bestPriority = int.MinValue;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < interactables.Count; i++)
        {
            IPlayerInteractable interactable = interactables[i];
            if (!interactable.CanInteract(this))
                continue;

            int priority = interactable.InteractionPriority;
            float distance = (interactable.InteractionPosition - transform.position).sqrMagnitude;

            if (bestInteractable == null || priority > bestPriority || (priority == bestPriority && distance < bestDistance))
            {
                bestInteractable = interactable;
                bestPriority = priority;
                bestDistance = distance;
            }
        }

        if (ReferenceEquals(currentInteractable, bestInteractable))
            return;

        SetInteractableFocus(currentInteractable, false);
        currentInteractable = bestInteractable;
        SetInteractableFocus(currentInteractable, true);
    }

    private void SetInteractableFocus(IPlayerInteractable interactable, bool isFocused)
    {
        if (IsInteractableDestroyed(interactable))
            return;

        interactable.SetInteractionFocus(this, isFocused);
    }

    private bool IsInteractableDestroyed(IPlayerInteractable interactable)
    {
        if (interactable == null)
            return true;

        if (interactable is MonoBehaviour behaviour)
            return behaviour == null;

        return false;
    }

    private void OnMove(InputValue value)
    {
        inputAxis = new Vector2(value.Get<Vector2>().x, value.Get<Vector2>().y).normalized;
    }

    private void OnJump(InputValue value)
    {
        if ((crowdControl == null || crowdControl.CanMove) && isGrounded && !GetComponent<AttackController>().isAttacking)
            isJumping = true;
    }

    private void OnInteract(InputValue value)
    {
        if (crowdControl != null && !crowdControl.CanInteract)
            return;

        RefreshCurrentInteractable();
        currentInteractable?.Interact(this);
        RefreshCurrentInteractable();
    }

    private void OnDash(InputValue value)
    {
        if (crowdControl != null && !crowdControl.CanDash)
            return;

        if (dashCooltimeCheck >= GetResolvedDashCooldown() && !GetComponent<AttackController>().isAttacking)
        {
            LastDashDirection = inputAxis.sqrMagnitude > 0.001f ? inputAxis.normalized : GetFacingDirection();
            axis = LastDashDirection;
            stats.Dash();
            dashCooltimeCheck = 0;
            dashTime = 0;
            OnDashStarted?.Invoke();
        }
    }

    private void EndDashIfActive()
    {
        if (dashTime >= dashDuration && Mathf.Approximately(dashTime, dashDuration))
            return;

        dashTime = dashDuration;
        OnDashEnded?.Invoke();
    }

    private void OnDestroy()
    {
        OnDashStarted = null;
        OnDashEnded = null;
    }
}
