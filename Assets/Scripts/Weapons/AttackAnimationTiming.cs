using UnityEngine;

public static class AttackAnimationTiming
{
    private const float MinimumDuration = 0.01f;
    private const float MinimumSpeed = 0.01f;

    public static float GetScaledAttackDuration(float baseAttackDuration, float attackSpeed)
    {
        return Mathf.Max(MinimumDuration, baseAttackDuration / GetSafeAttackSpeed(attackSpeed));
    }

    public static float GetScaledComboWindow(float baseComboWindow, float attackSpeed)
    {
        return baseComboWindow / GetSafeAttackSpeed(attackSpeed);
    }

    public static float GetPlaybackSpeed(float clipLength, float targetDuration, float fallbackAttackSpeed)
    {
        if (clipLength <= 0f || targetDuration <= 0f)
            return GetSafeAttackSpeed(fallbackAttackSpeed);

        return Mathf.Max(MinimumSpeed, clipLength / targetDuration);
    }

    public static float GetSafeAttackSpeed(float attackSpeed)
    {
        return Mathf.Max(MinimumSpeed, attackSpeed);
    }
}
