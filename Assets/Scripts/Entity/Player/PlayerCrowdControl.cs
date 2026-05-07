using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum PlayerControlLock
{
    None = 0,
    Move = 1 << 0,
    Attack = 1 << 1,
    Item = 1 << 2,
    Heal = 1 << 3,
    Dash = 1 << 4,
    Interact = 1 << 5,
    All = Move | Attack | Item | Heal | Dash | Interact
}

public enum PlayerCrowdControlType
{
    Custom,
    Stun,
    Petrify,
    Slow,
    Knockback
}

public struct PlayerCrowdControlRequest
{
    public PlayerCrowdControlType Type;
    public float Duration;
    public float MoveSpeedMultiplier;
    public PlayerControlLock Locks;
    public Vector2 Knockback;

    public static PlayerCrowdControlRequest CreateStun(float duration)
    {
        return new PlayerCrowdControlRequest
        {
            Type = PlayerCrowdControlType.Stun,
            Duration = duration,
            MoveSpeedMultiplier = 1f,
            Locks = PlayerControlLock.All,
            Knockback = Vector2.zero
        };
    }

    public static PlayerCrowdControlRequest CreatePetrify(float duration)
    {
        return new PlayerCrowdControlRequest
        {
            Type = PlayerCrowdControlType.Petrify,
            Duration = duration,
            MoveSpeedMultiplier = 0f,
            Locks = PlayerControlLock.All,
            Knockback = Vector2.zero
        };
    }

    public static PlayerCrowdControlRequest CreateSlow(float duration, float moveSpeedMultiplier)
    {
        return new PlayerCrowdControlRequest
        {
            Type = PlayerCrowdControlType.Slow,
            Duration = duration,
            MoveSpeedMultiplier = moveSpeedMultiplier,
            Locks = PlayerControlLock.None,
            Knockback = Vector2.zero
        };
    }

    public static PlayerCrowdControlRequest CreateKnockback(Vector2 knockback)
    {
        return new PlayerCrowdControlRequest
        {
            Type = PlayerCrowdControlType.Knockback,
            Duration = 0f,
            MoveSpeedMultiplier = 1f,
            Locks = PlayerControlLock.Move | PlayerControlLock.Attack | PlayerControlLock.Item | PlayerControlLock.Heal | PlayerControlLock.Dash | PlayerControlLock.Interact,
            Knockback = knockback
        };
    }
}

public class PlayerCrowdControl : MonoBehaviour
{
    [SerializeField] private float knockbackMoveSpeed = 10f;

    private readonly List<TimedEffectInstance> activeEffects = new List<TimedEffectInstance>();
    private readonly List<ControlLockInstance> activeControlLocks = new List<ControlLockInstance>();

    private Vector2 knockbackDirection = Vector2.zero;
    private float knockbackRemainingDistance = 0f;
    private PlayerControlLock knockbackLocks = PlayerControlLock.None;
    private int controlLockTokenCounter;

    public bool CanMove => !HasLock(PlayerControlLock.Move);
    public bool CanAttack => !HasLock(PlayerControlLock.Attack);
    public bool CanUseItems => !HasLock(PlayerControlLock.Item);
    public bool CanHeal => !HasLock(PlayerControlLock.Heal);
    public bool CanDash => !HasLock(PlayerControlLock.Dash);
    public bool CanInteract => !HasLock(PlayerControlLock.Interact);
    public bool IsKnockbackActive => knockbackRemainingDistance > 0f && knockbackDirection.sqrMagnitude > 0.001f;

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

    public void ApplyEffect(PlayerCrowdControlRequest request)
    {
        switch (request.Type)
        {
            case PlayerCrowdControlType.Stun:
                ApplyStun(request.Duration);
                break;
            case PlayerCrowdControlType.Petrify:
                ApplyPetrify(request.Duration);
                break;
            case PlayerCrowdControlType.Slow:
                ApplySlow(request.Duration, request.MoveSpeedMultiplier);
                break;
            case PlayerCrowdControlType.Knockback:
                ApplyKnockback(request.Knockback, request.Locks);
                break;
            default:
                ApplyTimedEffect(request.Type, request.Duration, request.Locks, request.MoveSpeedMultiplier);
                break;
        }
    }

    public void ApplyStun(float duration)
    {
        ApplyTimedEffect(PlayerCrowdControlType.Stun, duration, PlayerControlLock.All, 1f);
    }

    public void ApplyPetrify(float duration)
    {
        ApplyTimedEffect(PlayerCrowdControlType.Petrify, duration, PlayerControlLock.All, 0f);
    }

    public void ApplySlow(float duration, float moveSpeedMultiplier)
    {
        ApplyTimedEffect(PlayerCrowdControlType.Slow, duration, PlayerControlLock.None, moveSpeedMultiplier);
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        ApplyKnockback(knockback, PlayerControlLock.Move | PlayerControlLock.Attack | PlayerControlLock.Item | PlayerControlLock.Heal | PlayerControlLock.Dash | PlayerControlLock.Interact);
    }

    public void ApplyKnockback(Vector2 knockback, PlayerControlLock locks)
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
        knockbackLocks = PlayerControlLock.None;
    }

    public int AddControlLock(PlayerControlLock locks)
    {
        if (locks == PlayerControlLock.None)
            return 0;

        int token = ++controlLockTokenCounter;
        activeControlLocks.Add(new ControlLockInstance
        {
            Token = token,
            Locks = locks
        });
        return token;
    }

    public bool RemoveControlLock(int token)
    {
        if (token == 0)
            return false;

        for (int i = 0; i < activeControlLocks.Count; i++)
        {
            if (activeControlLocks[i].Token != token)
                continue;

            activeControlLocks.RemoveAt(i);
            return true;
        }

        return false;
    }

    private void ApplyTimedEffect(PlayerCrowdControlType type, float duration, PlayerControlLock locks, float moveSpeedMultiplier)
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

    private bool HasLock(PlayerControlLock targetLock)
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

        for (int i = 0; i < activeControlLocks.Count; i++)
        {
            if ((activeControlLocks[i].Locks & targetLock) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private struct TimedEffectInstance
    {
        public PlayerCrowdControlType Type;
        public float RemainingDuration;
        public PlayerControlLock Locks;
        public float MoveSpeedMultiplier;
    }

    private struct ControlLockInstance
    {
        public int Token;
        public PlayerControlLock Locks;
    }
}
