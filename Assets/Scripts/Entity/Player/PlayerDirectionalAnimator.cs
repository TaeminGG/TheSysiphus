using System.Collections.Generic;
using UnityEngine;

public class PlayerDirectionalAnimator : MonoBehaviour
{
    public enum Direction8
    {
        South = 0,
        SouthEast = 1,
        East = 2,
        NorthEast = 3,
        North = 4,
        NorthWest = 5,
        West = 6,
        SouthWest = 7
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animator Parameters")]
    [SerializeField] private string directionParameter = "Direction";
    [SerializeField] private string legacyLookParameter = "Look";
    [SerializeField] private string directionXParameter = "DirectionX";
    [SerializeField] private string directionYParameter = "DirectionY";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string movingParameter = "IsMoving";
    [SerializeField] private string groundedParameter = "IsGrounded";
    [SerializeField] private string airborneParameter = "IsAirborne";
    [SerializeField] private string dashingParameter = "IsDashing";
    [SerializeField] private string divingParameter = "IsDiving";
    [SerializeField] private string attackSpeedParameter = "AttackSpeed";
    [SerializeField] private string attackEndedTrigger = "AttackEnded";
    [SerializeField] private string diveAttackTrigger = "DiveAttack";
    [SerializeField] private string jumpTrigger = "Jump";

    [Header("Compatibility")]
    [SerializeField] private bool mirrorLeftDirections = false;

    private readonly HashSet<int> parameters = new HashSet<int>();
    private RuntimeAnimatorController cachedController;
    private Direction8 currentDirection = Direction8.South;
    private Vector2 currentDirectionVector = Vector2.down;

    public Direction8 CurrentDirection => currentDirection;
    public Vector2 CurrentDirectionVector => currentDirectionVector;
    public bool MirrorLeftDirections => mirrorLeftDirections;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        RebuildParameterCache();
    }

    public void Initialize(Animator targetAnimator, SpriteRenderer targetSpriteRenderer)
    {
        if (targetAnimator != null)
            animator = targetAnimator;

        if (targetSpriteRenderer != null)
            spriteRenderer = targetSpriteRenderer;

        RebuildParameterCache();
        ApplyDirection(currentDirectionVector);
    }

    public void SyncLocomotion(Vector2 facingDirection, Vector2 moveDirection, bool isDashing, bool isAirborne, bool isGrounded)
    {
        ApplyDirection(facingDirection);

        Vector2 normalizedMove = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : Vector2.zero;
        SetFloat(moveXParameter, normalizedMove.x);
        SetFloat(moveYParameter, normalizedMove.y);
        SetBool(movingParameter, normalizedMove.sqrMagnitude > 0.001f);
        SetBool(dashingParameter, isDashing);
        SetBool(airborneParameter, isAirborne);
        SetBool(groundedParameter, isGrounded);
    }

    public void PlayAttack(string triggerName, Vector2 attackDirection, float playbackSpeed)
    {
        ApplyDirection(attackDirection);
        SetFloat(attackSpeedParameter, playbackSpeed);
        SetTrigger(triggerName);
    }

    public float GetAttackClipLength(string clipNamePrefix, string triggerName, Vector2 attackDirection)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        string resolvedPrefix = ResolveAttackClipNamePrefix(clipNamePrefix, triggerName);
        if (string.IsNullOrWhiteSpace(resolvedPrefix))
            return 0f;

        Direction8 direction = GetDirection(attackDirection);
        string directionSuffix = GetDirectionSuffix(direction);
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
                continue;

            if (clip.name == $"Player_{resolvedPrefix}_{directionSuffix}" || clip.name == $"{resolvedPrefix}_{directionSuffix}")
                return clip.length;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name.StartsWith($"{resolvedPrefix}_", System.StringComparison.Ordinal))
                return clip.length;
        }

        return 0f;
    }

    public void EndAttack()
    {
        SetTrigger(attackEndedTrigger);
    }

    public void PlayDiveAttack(Vector2 diveDirection)
    {
        ApplyDirection(diveDirection);
        SetBool(divingParameter, true);
        SetTrigger(diveAttackTrigger);
    }

    public void EndDiveAttack()
    {
        SetBool(divingParameter, false);
    }

    public void ResetAttackTrigger(string triggerName)
    {
        ResetTrigger(triggerName);
    }

    public void PlayJump(Vector2 jumpDirection)
    {
        ApplyDirection(jumpDirection);
        SetTrigger(jumpTrigger);
    }

    public static Direction8 GetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return Direction8.South;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (angle < 0f)
            angle += 360f;

        int octant = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;

        return octant switch
        {
            0 => Direction8.East,
            1 => Direction8.NorthEast,
            2 => Direction8.North,
            3 => Direction8.NorthWest,
            4 => Direction8.West,
            5 => Direction8.SouthWest,
            6 => Direction8.South,
            7 => Direction8.SouthEast,
            _ => Direction8.South
        };
    }

    public static Vector2 GetDirectionVector(Direction8 direction)
    {
        return direction switch
        {
            Direction8.South => Vector2.down,
            Direction8.SouthEast => new Vector2(1f, -1f).normalized,
            Direction8.East => Vector2.right,
            Direction8.NorthEast => new Vector2(1f, 1f).normalized,
            Direction8.North => Vector2.up,
            Direction8.NorthWest => new Vector2(-1f, 1f).normalized,
            Direction8.West => Vector2.left,
            Direction8.SouthWest => new Vector2(-1f, -1f).normalized,
            _ => Vector2.down
        };
    }

    private static string GetDirectionSuffix(Direction8 direction)
    {
        return direction switch
        {
            Direction8.South => "S",
            Direction8.SouthEast => "SE",
            Direction8.East => "E",
            Direction8.NorthEast => "NE",
            Direction8.North => "N",
            Direction8.NorthWest => "NW",
            Direction8.West => "W",
            Direction8.SouthWest => "SW",
            _ => "S"
        };
    }

    private static string ResolveAttackClipNamePrefix(string clipNamePrefix, string triggerName)
    {
        if (!string.IsNullOrWhiteSpace(clipNamePrefix))
            return clipNamePrefix.Trim();

        if (string.IsNullOrWhiteSpace(triggerName))
            return string.Empty;

        int end = triggerName.Length - 1;
        while (end >= 0 && char.IsDigit(triggerName[end]))
            end--;

        if (end < triggerName.Length - 1)
            return $"Attack{triggerName.Substring(end + 1)}";

        return triggerName.Trim();
    }

    private void ApplyDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = currentDirectionVector.sqrMagnitude > 0.001f ? currentDirectionVector : Vector2.down;

        currentDirection = GetDirection(direction);
        currentDirectionVector = GetDirectionVector(currentDirection);

        SetFloat(directionParameter, (float)currentDirection);
        SetInteger(legacyLookParameter, GetLegacyLookValue(currentDirection));
        SetFloat(directionXParameter, currentDirectionVector.x);
        SetFloat(directionYParameter, currentDirectionVector.y);

        if (spriteRenderer != null)
            spriteRenderer.flipX = mirrorLeftDirections && IsLeftDirection(currentDirection);
    }

    private static int GetLegacyLookValue(Direction8 direction)
    {
        return direction switch
        {
            Direction8.South => 0,
            Direction8.SouthEast or Direction8.SouthWest => 1,
            Direction8.East or Direction8.West => 2,
            Direction8.NorthEast or Direction8.NorthWest => 3,
            Direction8.North => 4,
            _ => 0
        };
    }

    private static bool IsLeftDirection(Direction8 direction)
    {
        return direction == Direction8.West
            || direction == Direction8.NorthWest
            || direction == Direction8.SouthWest;
    }

    private void RebuildParameterCache()
    {
        parameters.Clear();
        cachedController = animator != null ? animator.runtimeAnimatorController : null;

        if (animator == null || cachedController == null)
            return;

        AnimatorControllerParameter[] animatorParameters = animator.parameters;
        for (int i = 0; i < animatorParameters.Length; i++)
        {
            parameters.Add(animatorParameters[i].nameHash);
        }
    }

    private bool HasParameter(string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        if (animator.runtimeAnimatorController != cachedController)
            RebuildParameterCache();

        return parameters.Contains(Animator.StringToHash(parameterName));
    }

    private void SetInteger(string parameterName, int value)
    {
        if (HasParameter(parameterName))
            animator.SetInteger(parameterName, value);
    }

    private void SetFloat(string parameterName, float value)
    {
        if (HasParameter(parameterName))
            animator.SetFloat(parameterName, value);
    }

    private void SetBool(string parameterName, bool value)
    {
        if (HasParameter(parameterName))
            animator.SetBool(parameterName, value);
    }

    private void SetTrigger(string parameterName)
    {
        if (HasParameter(parameterName))
            animator.SetTrigger(parameterName);
    }

    private void ResetTrigger(string parameterName)
    {
        if (HasParameter(parameterName))
            animator.ResetTrigger(parameterName);
    }
}
