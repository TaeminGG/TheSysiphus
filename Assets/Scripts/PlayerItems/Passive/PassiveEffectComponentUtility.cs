using UnityEngine;

public static class PassiveEffectComponentUtility
{
    public static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        if (!target.TryGetComponent(out T component))
            component = target.AddComponent<T>();

        return component;
    }

    public static AttackData CreateRuntimeAttackData(
        string attackName,
        float baseDamage,
        float damageScaling,
        Vector2 hitboxSize,
        float attackDuration,
        float knockbackForce,
        float zUpperRange = 0f,
        float zLowerRange = 0f)
    {
        AttackData data = ScriptableObject.CreateInstance<AttackData>();
        data.name = attackName;
        data.hideFlags = HideFlags.HideAndDontSave;
        data.damage = baseDamage;
        data.damageScaling = damageScaling;
        data.hitboxSize = hitboxSize;
        data.hitboxOffset = Vector2.zero;
        data.attackDuration = attackDuration;
        data.comboInputWindow = attackDuration;
        data.knockbackForce = knockbackForce;
        data.zUpperRange = zUpperRange;
        data.zLowerRange = zLowerRange;
        return data;
    }
}

public sealed class TimedPassiveVisual : MonoBehaviour
{
    private float remainingLifetime = 0.1f;

    public void Configure(float lifetime)
    {
        remainingLifetime = Mathf.Max(0.01f, lifetime);
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
            Destroy(gameObject);
    }
}
