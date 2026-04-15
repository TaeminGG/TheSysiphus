using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum EnemyControlLock
{
    None = 0,
    Move = 1 << 0,
    Attack = 1 << 1,
    All = Move | Attack
}

public enum EnemyCrowdControlType
{
    Custom,
    Stun,
    Slow,
    Knockback
}

public struct EnemyCrowdControlRequest
{
    public EnemyCrowdControlType Type;
    public float Duration;
    public float MoveSpeedMultiplier;
    public EnemyControlLock Locks;
    public Vector2 Knockback;

    public static EnemyCrowdControlRequest CreateStun(float duration)
    {
        return new EnemyCrowdControlRequest
        {
            Type = EnemyCrowdControlType.Stun,
            Duration = duration,
            MoveSpeedMultiplier = 1f,
            Locks = EnemyControlLock.All,
            Knockback = Vector2.zero
        };
    }

    public static EnemyCrowdControlRequest CreateSlow(float duration, float moveSpeedMultiplier)
    {
        return new EnemyCrowdControlRequest
        {
            Type = EnemyCrowdControlType.Slow,
            Duration = duration,
            MoveSpeedMultiplier = moveSpeedMultiplier,
            Locks = EnemyControlLock.None,
            Knockback = Vector2.zero
        };
    }

    public static EnemyCrowdControlRequest CreateKnockback(Vector2 knockback)
    {
        return new EnemyCrowdControlRequest
        {
            Type = EnemyCrowdControlType.Knockback,
            Duration = 0f,
            MoveSpeedMultiplier = 1f,
            Locks = EnemyControlLock.All,
            Knockback = knockback
        };
    }
}

public class EnemyCrowdControl : MonoBehaviour
{
    [SerializeField] private float knockbackMoveSpeed = 10f;
    [SerializeField] private float legacySlowMultiplier = 0.5f;

    private readonly List<TimedEffectInstance> activeEffects = new List<TimedEffectInstance>();

    private Vector2 knockbackDirection = Vector2.zero;
    private float knockbackRemainingDistance = 0f;
    private EnemyControlLock knockbackLocks = EnemyControlLock.None;

    public bool CanMove => !HasLock(EnemyControlLock.Move);
    public bool CanAttack => !HasLock(EnemyControlLock.Attack);
    public bool ShouldCancelCurrentAttack => HasTimedLock(EnemyControlLock.Attack);
    public bool IsKnockbackActive => knockbackRemainingDistance > 0f && knockbackDirection.sqrMagnitude > 0.001f;
    public float LegacySlowMultiplier => legacySlowMultiplier;

    public float MoveSpeedMultiplier
    {
        get
        {
            float multiplier = 1f;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                multiplier = Mathf.Min(multiplier, activeEffects[i].MoveSpeedMultiplier);
            }

            return Mathf.Clamp(multiplier, 0f, 1f);
        }
    }

    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            TimedEffectInstance effect = activeEffects[i];
            effect.RemainingDuration -= Time.deltaTime;

            if (effect.RemainingDuration <= 0f)
            {
                activeEffects.RemoveAt(i);
            }
            else
            {
                activeEffects[i] = effect;
            }
        }
    }

    public void ApplyEffect(EnemyCrowdControlRequest request)
    {
        switch (request.Type)
        {
            case EnemyCrowdControlType.Stun:
                ApplyStun(request.Duration);
                break;
            case EnemyCrowdControlType.Slow:
                ApplySlow(request.Duration, request.MoveSpeedMultiplier);
                break;
            case EnemyCrowdControlType.Knockback:
                ApplyKnockback(request.Knockback, request.Locks);
                break;
            default:
                ApplyTimedEffect(request.Type, request.Duration, request.Locks, request.MoveSpeedMultiplier);
                break;
        }
    }

    public void ApplyStun(float duration)
    {
        ApplyTimedEffect(EnemyCrowdControlType.Stun, duration, EnemyControlLock.All, 1f);
    }

    public void ApplySlow(float duration, float moveSpeedMultiplier)
    {
        ApplyTimedEffect(EnemyCrowdControlType.Slow, duration, EnemyControlLock.None, moveSpeedMultiplier);
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        ApplyKnockback(knockback, EnemyControlLock.All);
    }

    public void ApplyKnockback(Vector2 knockback, EnemyControlLock locks)
    {
        float distance = knockback.magnitude;
        if (distance <= 0.001f)
        {
            return;
        }

        knockbackDirection = knockback.normalized;
        knockbackRemainingDistance = distance;
        knockbackLocks = locks;
    }

    public Vector2 GetKnockbackDirection()
    {
        return knockbackDirection;
    }

    public float GetKnockbackMoveSpeed()
    {
        return knockbackMoveSpeed;
    }

    public void ConsumeKnockbackDistance(float distance)
    {
        if (!IsKnockbackActive)
        {
            return;
        }

        knockbackRemainingDistance -= distance;
        if (knockbackRemainingDistance <= 0f)
        {
            ClearKnockback();
        }
    }

    public void ClearKnockback()
    {
        knockbackDirection = Vector2.zero;
        knockbackRemainingDistance = 0f;
        knockbackLocks = EnemyControlLock.None;
    }

    private void ApplyTimedEffect(EnemyCrowdControlType type, float duration, EnemyControlLock locks, float moveSpeedMultiplier)
    {
        if (duration <= 0f)
        {
            return;
        }

        activeEffects.Add(new TimedEffectInstance
        {
            Type = type,
            RemainingDuration = duration,
            Locks = locks,
            MoveSpeedMultiplier = Mathf.Clamp(moveSpeedMultiplier, 0f, 1f)
        });
    }

    private bool HasLock(EnemyControlLock targetLock)
    {
        if (IsKnockbackActive && (knockbackLocks & targetLock) != 0)
        {
            return true;
        }

        for (int i = 0; i < activeEffects.Count; i++)
        {
            if ((activeEffects[i].Locks & targetLock) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasTimedLock(EnemyControlLock targetLock)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if ((activeEffects[i].Locks & targetLock) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private struct TimedEffectInstance
    {
        public EnemyCrowdControlType Type;
        public float RemainingDuration;
        public EnemyControlLock Locks;
        public float MoveSpeedMultiplier;
    }
}
